using System.Collections;
using System.Collections.Generic;
using UnityEngine;



[RequireComponent(typeof(CharacterController))]
public class PlayerCoverController : MonoBehaviour
{

    [Header("�ʼ� ����")]
    public CoverNode startNode;
    public Camera mainCam;
    public Transform headAnchor; // 1��Ī ī�޶� ��ġ(�Ӹ�)

    [Header("������/�ӵ�")]
    public float coverDepth = 0.45f;
    public float peekOut = 0.25f;
    public float slideSpeed = 6f;
    public float rotateSpeed = 12f;
    public float camLerpSpeed = 10f;

    [Header("�Է�")]
    public KeyCode moveLeftKey = KeyCode.LeftArrow;
    public KeyCode moveRightKey = KeyCode.RightArrow;
    public bool aimOnRightMouse = true;

    [Header("ī�޶� ����")]
    public Vector3 thirdPersonOffset = new Vector3(0.4f, 0.7f, -2.3f);
    public float thirdFOV = 60f;
    public float firstFOV = 70f;

    [Header("ī�޶� ��ġ/���")]
    public float pitchSpeed = 120f;
    public float pitchMin = -60f;
    public float pitchMax = 60f;
    private float pitch;

    public float yawSpeed = 180f;
    private float aimYaw;
    public bool rotateBodyTowardAim = false;

    [Header("���� �� ���콺 ���� ���")]
    public float aimYawMultiplier = 1.6f;
    public float aimPitchMultiplier = 1.4f;

    [Header("���� ���� ����")]
    public float yawLimit = 70f; // �� ���� �¿� �ִ� ����

    [Header("���� ��ƽ")]
    public float groundStickDown = 0.5f;

    // ===== ���/���� =====
    [Header("��Ʈ��ĵ ���")]
    public float clickDamage = 25f;
    public float fireRange = 200f;
    public LayerMask hitMask = ~0;          // Enemy | Cover | Obstacle ��
    public float fireCooldown = 0.12f;
    private float fireTimer = 0f;

    // ===== ���� �߰�: �̼� ����/��Ʈ��Ŀ/Ʈ���̼� =====
    [Header("���� ��� �̼� ����(��)")]
    [Tooltip("���� ��忡�� �� �� �߻� �� ����Ǵ� ���� ���� ����(�� ����). 0.2~0.8 ����")]
    public float aimJitterDegrees = 0.45f;
    [Tooltip("�� �ߴ� ���� ���� (���۽����� ū Ʀ ����)")]
    public float jitterMaxPerShot = 0.6f;

    [Header("��Ʈ��Ŀ UI")]
    public HitMarkerUI hitMarker;           // Canvas�� HitMarkerUI ����
    public bool showKillMarker = true;      // �� óġ �� ���� ǥ�� ���

    [Header("Ʈ���̼�")]
    public LineRenderer tracer;             // ����(��� OK)
    public float tracerDuration = 0.06f;

    private CharacterController cc;
    private CoverNode currentNode;
    private bool isAiming;
    private float currentDepth;
    public CoverNode CurrentNode => currentNode;
    // ===== ���� �ݵ� =====
    [Header("Recoil(�ݵ�)")]
    [Tooltip("�� �ߴ� ���� Ƣ�� ����(��). 0.4~1.2 ����")]
    public float recoilPitchPerShot = 0.7f;

    [Tooltip("�� �ߴ� ��/�� ���� ��鸲(��)")]
    public float recoilYawPerShot = 0.25f;

    [Tooltip("�ݵ��� 0���� �����ϴ� �ӵ�(��/��)")]
    public float recoilRecoverySpeed = 8f;

    [Tooltip("���� ���¿����� �ݵ� ����")]
    public bool recoilOnlyWhileAiming = true;

    // ���� ���� ������(���� ī�޶� ������)
    float _recoilPitchOff; // ���� Ʀ(������ �� �ž�, �Ʒ� ����)
    float _recoilYawOff;   // ��/�� ��鸲

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

        // 1) �Է�
        bool wantAim = aimOnRightMouse ? Input.GetMouseButton(1) : (Input.GetMouseButton(0) || Input.GetMouseButton(1));
        isAiming = wantAim;

        if (Input.GetKeyDown(moveLeftKey) || Input.GetKeyDown(KeyCode.A)) TryMoveTo(currentNode.leftNode);
        if (Input.GetKeyDown(moveRightKey) || Input.GetKeyDown(KeyCode.D)) TryMoveTo(currentNode.rightNode);

        // 2) ���� ����
        float targetDepth = isAiming ? Mathf.Max(0.05f, coverDepth - peekOut) : coverDepth;
        currentDepth = Mathf.Lerp(currentDepth, targetDepth, Time.deltaTime * slideSpeed);

        // 3) ��ġ ����
        Vector3 targetPos = currentNode.transform.position - currentNode.coverNormal * currentDepth;
        Vector3 delta = targetPos - transform.position;
        delta.y = 0f;
        cc.Move(delta * Mathf.Clamp01(Time.deltaTime * slideSpeed));

        // ���� ��ƽ
        Vector3 down = Vector3.down * groundStickDown * Time.deltaTime;
        cc.Move(down);

        // ǥ�� �������� ȸ��
        Quaternion targetRot = Quaternion.LookRotation(currentNode.coverNormal, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotateSpeed);

        // 4) ī�޶� ȸ�� ó��
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

        // 5) ī�޶� ��ȯ
        UpdateCamera();
        if (!recoilOnlyWhileAiming || isAiming)
        {
            _recoilPitchOff = Mathf.MoveTowards(_recoilPitchOff, 0f, recoilRecoverySpeed * Time.deltaTime);
            _recoilYawOff = Mathf.MoveTowards(_recoilYawOff, 0f, recoilRecoverySpeed * Time.deltaTime);
        }
        else
        {
            // ���� ���� �ÿ��� ������ ����
            _recoilPitchOff = Mathf.MoveTowards(_recoilPitchOff, 0f, recoilRecoverySpeed * Time.deltaTime);
            _recoilYawOff = Mathf.MoveTowards(_recoilYawOff, 0f, recoilRecoverySpeed * Time.deltaTime);
        }

        // 6) ��� ó��
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

            // (A) �̼� ����
            if (aimJitterDegrees > 0.01f)
            {
                float deg = Mathf.Clamp(aimJitterDegrees, 0f, jitterMaxPerShot);
                Vector2 r = Random.insideUnitCircle.normalized * Random.Range(0f, deg);
                Quaternion q = Quaternion.AngleAxis(r.x, mainCam.transform.up) * Quaternion.AngleAxis(r.y, mainCam.transform.right);
                dir = (q * dir).normalized;
            }

            Vector3 start = origin + dir * 0.05f;   // �ڱ� �ݶ��̴� ȸ��
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

            // (C) Ʈ���̼�/�����
            if (tracer) StartCoroutine(CoTracer(start, end));
            Debug.DrawRay(start, dir * 10f, Color.red, 0.2f);

            // (D)  �ݵ� ����
            if (!recoilOnlyWhileAiming || isAiming)
            {
                _recoilPitchOff -= recoilPitchPerShot;                                   // ���� Ƣ�� (pitch�� -�� ����)
                _recoilYawOff += Random.Range(-recoilYawPerShot, recoilYawPerShot);    // ��/�� ����
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

                //  ���⸸ ����: �ݵ� �������� ������
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
