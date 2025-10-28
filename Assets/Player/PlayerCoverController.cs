using System.Collections;
using System.Collections.Generic;
using UnityEngine;



[RequireComponent(typeof(CharacterController))]
public class PlayerCoverController : MonoBehaviour
{

    [Header("필수 참조")]
    public CoverNode startNode;
    public Camera mainCam;
    public Transform headAnchor; // 1인칭 카메라 위치(머리)

    [Header("오프셋/속도")]
    public float coverDepth = 0.45f;
    public float peekOut = 0.25f;
    public float slideSpeed = 6f;
    public float rotateSpeed = 12f;
    public float camLerpSpeed = 10f;

    [Header("입력")]
    public KeyCode moveLeftKey = KeyCode.LeftArrow;
    public KeyCode moveRightKey = KeyCode.RightArrow;
    public bool aimOnRightMouse = true;

    [Header("카메라 세팅")]
    public Vector3 thirdPersonOffset = new Vector3(0.4f, 0.7f, -2.3f);
    public float thirdFOV = 60f;
    public float firstFOV = 70f;

    [Header("카메라 피치/요우")]
    public float pitchSpeed = 120f;
    public float pitchMin = -60f;
    public float pitchMax = 60f;
    private float pitch;

    public float yawSpeed = 180f;
    private float aimYaw;
    public bool rotateBodyTowardAim = false;

    [Header("에임 시 마우스 감도 배수")]
    public float aimYawMultiplier = 1.6f;
    public float aimPitchMultiplier = 1.4f;

    [Header("에임 각도 제한")]
    public float yawLimit = 70f; // 몸 기준 좌우 최대 각도

    [Header("지면 스틱")]
    public float groundStickDown = 0.5f;

    // ===== 사격/피해 =====
    [Header("히트스캔 사격")]
    public float clickDamage = 25f;
    public float fireRange = 200f;
    public LayerMask hitMask = ~0;          // Enemy | Cover | Obstacle 등
    public float fireCooldown = 0.12f;
    private float fireTimer = 0f;

    // ===== 새로 추가: 미세 떨림/히트마커/트레이서 =====
    [Header("조준 모드 미세 떨림(도)")]
    [Tooltip("조준 모드에서 한 발 발사 시 적용되는 아주 작은 지터(도 단위). 0.2~0.8 권장")]
    public float aimJitterDegrees = 0.45f;
    [Tooltip("한 발당 지터 상한 (갑작스러운 큰 튐 방지)")]
    public float jitterMaxPerShot = 0.6f;

    [Header("히트마커 UI")]
    public HitMarkerUI hitMarker;           // Canvas의 HitMarkerUI 연결
    public bool showKillMarker = true;      // 적 처치 시 빨간 표시 사용

    [Header("트레이서")]
    public LineRenderer tracer;             // 선택(없어도 OK)
    public float tracerDuration = 0.06f;

    private CharacterController cc;
    private CoverNode currentNode;
    private bool isAiming;
    private float currentDepth;
    public CoverNode CurrentNode => currentNode;
    // ===== 에임 반동 =====
    [Header("Recoil(반동)")]
    [Tooltip("한 발당 위로 튀는 각도(도). 0.4~1.2 권장")]
    public float recoilPitchPerShot = 0.7f;

    [Tooltip("한 발당 좌/우 랜덤 흔들림(도)")]
    public float recoilYawPerShot = 0.25f;

    [Tooltip("반동이 0으로 복귀하는 속도(도/초)")]
    public float recoilRecoverySpeed = 8f;

    [Tooltip("에임 상태에서만 반동 적용")]
    public bool recoilOnlyWhileAiming = true;

    // 내부 누적 오프셋(실제 카메라에 더해짐)
    float _recoilPitchOff; // 위로 튐(음수로 줄 거야, 아래 참고)
    float _recoilYawOff;   // 좌/우 흔들림

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (!mainCam) mainCam = Camera.main;

        currentNode = startNode;
        currentDepth = coverDepth;
        if (currentNode) SnapToNode(currentNode);

        aimYaw = transform.eulerAngles.y;
    }

    void Update()
    {
        if (!currentNode) return;
        fireTimer -= Time.deltaTime;

        // 1) 입력
        bool wantAim = aimOnRightMouse ? Input.GetMouseButton(1) : (Input.GetMouseButton(0) || Input.GetMouseButton(1));
        isAiming = wantAim;

        if (Input.GetKeyDown(moveLeftKey) || Input.GetKeyDown(KeyCode.A)) TryMoveTo(currentNode.leftNode);
        if (Input.GetKeyDown(moveRightKey) || Input.GetKeyDown(KeyCode.D)) TryMoveTo(currentNode.rightNode);

        // 2) 깊이 보간
        float targetDepth = isAiming ? Mathf.Max(0.05f, coverDepth - peekOut) : coverDepth;
        currentDepth = Mathf.Lerp(currentDepth, targetDepth, Time.deltaTime * slideSpeed);

        // 3) 위치 유지
        Vector3 targetPos = currentNode.transform.position - currentNode.coverNormal * currentDepth;
        Vector3 delta = targetPos - transform.position;
        delta.y = 0f;
        cc.Move(delta * Mathf.Clamp01(Time.deltaTime * slideSpeed));

        // 지면 스틱
        Vector3 down = Vector3.down * groundStickDown * Time.deltaTime;
        cc.Move(down);

        // 표면 방향으로 회전
        Quaternion targetRot = Quaternion.LookRotation(currentNode.coverNormal, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotateSpeed);

        // 4) 카메라 회전 처리
        float mouseY = Input.GetAxisRaw("Mouse Y");
        float mouseX = Input.GetAxisRaw("Mouse X");

        float yawMult = isAiming ? aimYawMultiplier : 1f;
        float pitchMult = isAiming ? aimPitchMultiplier : 1f;

        pitch -= mouseY * pitchSpeed * pitchMult * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        if (isAiming)
        {
            aimYaw += mouseX * yawSpeed * yawMult * Time.deltaTime;

            float bodyYaw = transform.eulerAngles.y;
            float deltaYaw = Mathf.DeltaAngle(bodyYaw, aimYaw);
            deltaYaw = Mathf.Clamp(deltaYaw, -yawLimit, yawLimit);
            aimYaw = bodyYaw + deltaYaw;

            if (rotateBodyTowardAim)
            {
                Quaternion bodyTarget = Quaternion.Euler(0f, aimYaw, 0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, bodyTarget, Time.deltaTime * 0.5f);
            }
        }
        else
        {
            aimYaw = transform.eulerAngles.y;
        }

        // 5) 카메라 전환
        UpdateCamera();
        if (!recoilOnlyWhileAiming || isAiming)
        {
            _recoilPitchOff = Mathf.MoveTowards(_recoilPitchOff, 0f, recoilRecoverySpeed * Time.deltaTime);
            _recoilYawOff = Mathf.MoveTowards(_recoilYawOff, 0f, recoilRecoverySpeed * Time.deltaTime);
        }
        else
        {
            // 에임 해제 시에도 서서히 복귀
            _recoilPitchOff = Mathf.MoveTowards(_recoilPitchOff, 0f, recoilRecoverySpeed * Time.deltaTime);
            _recoilYawOff = Mathf.MoveTowards(_recoilYawOff, 0f, recoilRecoverySpeed * Time.deltaTime);
        }

        // 6) 사격 처리
        HandleShooting();
    }


    void HandleShooting()
    {

        if (!isAiming || mainCam == null) return;

        if (Input.GetMouseButton(0) && fireTimer <= 0f)
        {
            fireTimer = fireCooldown;

            Vector3 origin = GetShootOrigin();
            Quaternion rot = GetShootRotation();
            Vector3 dir = rot * Vector3.forward;

            // (A) 미세 떨림
            if (aimJitterDegrees > 0.01f)
            {
                float deg = Mathf.Clamp(aimJitterDegrees, 0f, jitterMaxPerShot);
                Vector2 r = Random.insideUnitCircle.normalized * Random.Range(0f, deg);
                Quaternion q = Quaternion.AngleAxis(r.x, mainCam.transform.up) * Quaternion.AngleAxis(r.y, mainCam.transform.right);
                dir = (q * dir).normalized;
            }

            Vector3 start = origin + dir * 0.05f;   // 자기 콜라이더 회피
            Vector3 end = start + dir * fireRange;

            if (Physics.Raycast(start, dir, out RaycastHit hit, fireRange, hitMask, QueryTriggerInteraction.Ignore))
            {
                end = hit.point;

                var eHp = hit.collider.GetComponentInParent<EnemyHealth>() ?? hit.collider.GetComponent<EnemyHealth>();
                if (eHp != null)
                {
                    float pre = eHp.currentHp;
                    eHp.TakeDamage(clickDamage, hit.point, hit.normal);

                    if (hitMarker)
                    {
                        bool killed = showKillMarker && (pre > 0f && (pre - clickDamage) <= 0f);
                        hitMarker.Show(killed);
                    }
                }
                else
                {
                    var cHp = hit.collider.GetComponentInParent<CoverNodeHealth>() ?? hit.collider.GetComponent<CoverNodeHealth>();
                    if (cHp != null)
                    {
                        cHp.TakeDamage(clickDamage, hit.point, hit.normal);
                        if (hitMarker) hitMarker.Show(false);
                    }
                    else
                    {
                        if (hitMarker) hitMarker.Show(false);
                    }
                }
            }

            // (C) 트레이서/디버그
            if (tracer) StartCoroutine(CoTracer(start, end));
            Debug.DrawRay(start, dir * 10f, Color.red, 0.2f);

            // (D)  반동 주입
            if (!recoilOnlyWhileAiming || isAiming)
            {
                _recoilPitchOff -= recoilPitchPerShot;                                   // 위로 튀게 (pitch는 -가 위쪽)
                _recoilYawOff += Random.Range(-recoilYawPerShot, recoilYawPerShot);    // 좌/우 랜덤
            }
        }
    }

    IEnumerator CoTracer(Vector3 s, Vector3 e)
    {
        tracer.positionCount = 2;
        tracer.SetPosition(0, s);
        tracer.SetPosition(1, e);
        tracer.enabled = true;
        yield return new WaitForSeconds(tracerDuration);
        tracer.enabled = false;
    }

    void TryMoveTo(CoverNode next)
    {
        if (!next) return;
        currentNode = next;
    }

    void SnapToNode(CoverNode node)
    {
        transform.position = node.transform.position - node.coverNormal * coverDepth;
        transform.rotation = Quaternion.LookRotation(-node.coverNormal, Vector3.up);
        aimYaw = transform.eulerAngles.y;
    }

    void UpdateCamera()
    {
        if (isAiming)
        {
            if (isAiming)
            {
                Vector3 targetPos = headAnchor ? headAnchor.position : (transform.position + Vector3.up * 1.6f);
                mainCam.transform.position = Vector3.Lerp(mainCam.transform.position, targetPos, Time.deltaTime * camLerpSpeed);

                //  여기만 변경: 반동 오프셋을 더해줌
                Quaternion targetRot = Quaternion.Euler(pitch + _recoilPitchOff, aimYaw + _recoilYawOff, 0f);
                mainCam.transform.rotation = Quaternion.Slerp(mainCam.transform.rotation, targetRot, Time.deltaTime * camLerpSpeed);

                mainCam.fieldOfView = Mathf.Lerp(mainCam.fieldOfView, firstFOV, Time.deltaTime * camLerpSpeed);
            }
        }
        else
        {
            Vector3 pivot = transform.position + Vector3.up * 1.4f;
            Vector3 desired = pivot
                + transform.right * thirdPersonOffset.x
                + Vector3.up * (thirdPersonOffset.y - 1.4f)
                + transform.forward * thirdPersonOffset.z;

            mainCam.transform.position = Vector3.Lerp(mainCam.transform.position, desired, Time.deltaTime * camLerpSpeed);

            Quaternion targetRot = Quaternion.Euler(pitch * 0.35f, transform.eulerAngles.y, 0f);
            mainCam.transform.rotation = Quaternion.Slerp(mainCam.transform.rotation, targetRot, Time.deltaTime * camLerpSpeed);

            mainCam.fieldOfView = Mathf.Lerp(mainCam.fieldOfView, thirdFOV, Time.deltaTime * camLerpSpeed);
        }
    }

    public bool IsAiming() => isAiming;

    public Vector3 GetShootOrigin()
        => (headAnchor ? headAnchor.position : (transform.position + Vector3.up * 1.6f));

    public Quaternion GetShootRotation()
    {
        if (isAiming)
            return Quaternion.Euler(pitch, aimYaw, 0f);
        else
            return Quaternion.Euler(pitch * 0.35f, transform.eulerAngles.y, 0f);
    }

}
