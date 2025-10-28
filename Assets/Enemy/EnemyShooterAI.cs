using System.Collections;
using System.Collections.Generic;

using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class EnemyShooterAI : MonoBehaviour
{
    [Header("필수 참조")]
    public EnemyArchetype archetype;
    public Transform shootMuzzle;
    public Transform player;
    public PlayerCoverController playerController;

    [Header("레이어 마스크")]
    public LayerMask playerMask;
    public LayerMask coverMask;
    public LayerMask obstacleMask;
    private LayerMask _combinedMask;

    [Header("사격/발사 보정")]
    public float rayStartOffset = 0.05f;    // 총구에서 약간 앞으로
    public float heightOffset = 1.2f;     // shootMuzzle 없을 때 총구 높이
    public float aimYOffset = 1.0f;     // 플레이어 가슴 높이

    [Header("디버그/트레이서")]
    public bool drawDebug = true;
    public bool debugHitMarkers = false;
    public float tracerWidth = 0.04f;
    public float tracerDuration = 0.08f;
    public Material tracerMaterial;

    private EnemyHealth _hp;
    private float _nextFireTime;
    private float _nextVisionTime;
    private LineRenderer _tracer;
    private Collider[] _selfCols;
    private RaycastHit[] _hitsBuf = new RaycastHit[32];

    void Awake()
    {
        _hp = GetComponent<EnemyHealth>();
        if (archetype) _hp.maxHp = archetype.health;

        // 마스크 자동 보정
        if (playerMask.value == 0) playerMask = LayerMask.GetMask("Player");
        if (coverMask.value == 0) coverMask = LayerMask.GetMask("Cover");
        if (obstacleMask.value == 0) obstacleMask = LayerMask.GetMask("Obstacle");
        _combinedMask = playerMask | coverMask | obstacleMask;
        if (_combinedMask.value == 0)
        {
            Debug.LogWarning("[AI] Combined mask=0 → ~0로 임시 설정");
            _combinedMask = ~0;
        }

        _selfCols = GetComponentsInChildren<Collider>(true);

        // 트레이서 초기화
        var go = new GameObject($"{name}_Tracer");
        _tracer = go.AddComponent<LineRenderer>();
        _tracer.numCapVertices = 4;           // 양끝 라운드
        _tracer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _tracer.receiveShadows = false;
        _tracer.alignment = LineAlignment.View; // 항상 화면 정면으로
        _tracer.widthMultiplier = tracerWidth;  // 0.04 ~ 0.06
        var sh = Shader.Find("HDRP/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
        _tracer.material = tracerMaterial ? tracerMaterial : new Material(sh);
    }

    void Update()
    {
        if (_hp == null || archetype == null || player == null) return;

        // 테스트용 (T 눌러 발사)
        if (Input.GetKeyDown(KeyCode.T))
        {
            Vector3 muzzle = GetMuzzle();
            Vector3 aim = GetAimPoint();
            FireBurst(muzzle, aim, null); // 기본 샷(커버 무시 X)
        }

        if (Time.time < _nextVisionTime) return;
        _nextVisionTime = Time.time + Mathf.Max(0.05f, archetype.visionCheckInterval);

        Vector3 muzzlePos = GetMuzzle();
        Vector3 aimPoint = GetAimPoint();
        Vector3 dir = (aimPoint - muzzlePos).normalized;

        // 수직 보정: 너무 위로 향하면 완만하게
        if (dir.y > 0.3f) dir.y *= 0.3f;
        dir.Normalize();

        float distToAim = Vector3.Distance(muzzlePos, aimPoint);
        if (distToAim > archetype.detectRange) return;

        bool isAiming = playerController && playerController.IsAiming();

        // 기본 레이캐스트(무엇이 먼저 막는지)
        if (RaycastFirstNonSelf(muzzlePos, dir, archetype.detectRange, _combinedMask, out RaycastHit hit))
        {
            int hLayer = hit.collider.gameObject.layer;
            bool hitPlayer = (playerMask.value & (1 << hLayer)) != 0;
            bool hitCover = (coverMask.value & (1 << hLayer)) != 0;

            // 에임 중이면: 커버 무시하고 플레이어 보이는지 재확인
            if (isAiming && hitCover)
            {
                LayerMask coverIgnoredMask = playerMask | obstacleMask; // 커버 제외
                if (RaycastFirstNonSelf(muzzlePos, dir, archetype.detectRange, coverIgnoredMask, out RaycastHit hit2))
                {
                    int h2 = hit2.collider.gameObject.layer;
                    bool hit2Player = (playerMask.value & (1 << h2)) != 0;

                    if (hit2Player)
                    {
                        // 이번 탄은 커버 무시해서 실제로 플레이어를 맞게 쏜다
                        TryScheduleFire(muzzlePos, hit2.point, null, ignoreCoverThisShot: true);
                        DebugLine(muzzlePos, hit2.point, Color.green);
                        return;
                    }
                }
            }

            // 일반 분기
            if (hitPlayer)
            {
                TryScheduleFire(muzzlePos, hit.point, null, ignoreCoverThisShot: false);
                DebugLine(muzzlePos, hit.point, Color.green);
            }
            else if (hitCover)
            {
                var cHp = hit.collider.GetComponentInParent<CoverNodeHealth>();
                //  수정 전: if (cHp != null && !cHp.IsDestroyed)
                //  수정 후: 커버가 부서져도 위치로 계속 공격하게
                if (cHp != null)
                {
                    TryScheduleFire(muzzlePos, hit.point, cHp);
                    DebugLine(muzzlePos, hit.point, Color.yellow);
                }
            }
        }
        else
        {
            DebugLine(muzzlePos, muzzlePos + dir * 3f, Color.gray);
        }
    }

    // 머리 대신 몸통 근처에서 쏘도록 조정
    Vector3 GetMuzzle()
    {
        if (shootMuzzle) return shootMuzzle.position;
        return transform.position + Vector3.up * heightOffset;
    }

    // 플레이어 조준점 (가슴 or headAnchor)
    Vector3 GetAimPoint()
    {
        if (playerController && playerController.headAnchor)
            return playerController.headAnchor.position;
        return player.position + Vector3.up * aimYOffset;
    }

    bool RaycastFirstNonSelf(Vector3 origin, Vector3 dir, float maxDist, LayerMask mask, out RaycastHit best)
    {
        origin += dir * rayStartOffset;
        int n = Physics.RaycastNonAlloc(origin, dir, _hitsBuf, maxDist, mask, QueryTriggerInteraction.Ignore);

        float bestDist = float.MaxValue;
        best = default;
        for (int i = 0; i < n; i++)
        {
            var h = _hitsBuf[i];
            if (h.collider == null) continue;

            bool isSelf = false;
            foreach (var c in _selfCols) { if (c == h.collider) { isSelf = true; break; } }
            if (isSelf) continue;

            if (h.distance < bestDist)
            {
                bestDist = h.distance;
                best = h;
            }
        }
        return best.collider != null;
    }

    // ─────────────────────────────────────
    // 사격 예약 / 버스트 / 1발
    // ─────────────────────────────────────
    // (A) 새 시그니처: 커버 무시 여부 포함
    void TryScheduleFire(Vector3 muzzle, Vector3 aimPoint, CoverNodeHealth targetCover, bool ignoreCoverThisShot = false)
    {
        if (Time.time < _nextFireTime || this == null || !isActiveAndEnabled) return;

        _nextFireTime = Time.time + archetype.attackDelay;

        // 코루틴 시작 전에 체크
        if (isActiveAndEnabled)
            StartCoroutine(CoBurst(muzzle, aimPoint, targetCover, ignoreCoverThisShot));
    }

    // (B) 하위 호환용 오버로드: 기본값(false)
    void TryScheduleFire(Vector3 muzzle, Vector3 aimPoint, CoverNodeHealth targetCover)
        => TryScheduleFire(muzzle, aimPoint, targetCover, false);

    // (C) 새 시그니처: 버스트 코루틴(커버 무시 여부 포함)
    IEnumerator CoBurst(Vector3 muzzle, Vector3 aimPoint, CoverNodeHealth targetCover, bool ignoreCoverThisShot)
    {
        int n = Mathf.Max(1, archetype.bulletsPerShot);
        for (int i = 0; i < n; i++)
        {
            FireOnce(muzzle, aimPoint, targetCover, ignoreCoverThisShot);
            yield return null;
        }
    }

    // (D) 하위 호환용 오버로드: 기본값(false)
    IEnumerator CoBurst(Vector3 muzzle, Vector3 aimPoint, CoverNodeHealth targetCover)
        => CoBurst(muzzle, aimPoint, targetCover, false);

    void FireOnce(Vector3 muzzle, Vector3 aimPoint, CoverNodeHealth targetCover, bool ignoreCoverThisShot)
    {
        Vector3 dir = (aimPoint - muzzle).normalized;
        Vector3 start = muzzle + dir * (ignoreCoverThisShot ? Mathf.Max(0.15f, rayStartOffset) : rayStartOffset);
        Vector3 end = start + dir * archetype.fireRange;

        // 이번 샷에 사용할 마스크 결정
        LayerMask shotMask = ignoreCoverThisShot ? (_combinedMask & ~coverMask) : _combinedMask;

        if (Physics.Raycast(start, dir, out RaycastHit rh, archetype.fireRange, shotMask, QueryTriggerInteraction.Ignore))
        {
            end = rh.point;

            // 레이어에 의존하지 않고 컴포넌트로 판정
            var pHp = rh.collider.GetComponentInParent<PlayerHealth>() ?? rh.collider.GetComponent<PlayerHealth>();
            if (pHp != null)
            {
                pHp.TakeDamage(archetype.damagePerShot, rh.point, rh.normal);
            }
            else if (!ignoreCoverThisShot) // 커버 무시 샷이면 커버 맞아도 무시
            {
                var cHp = rh.collider.GetComponentInParent<CoverNodeHealth>() ?? rh.collider.GetComponent<CoverNodeHealth>();
                if (cHp != null)
                    cHp.TakeDamage(archetype.damagePerShot, rh.point, rh.normal);
            }
        }

        DrawTracer(start, end);
    }

    // 하위 호환용: 외부에서 직접 호출할 때(테스트용)
    public void FireBurst(Vector3 muzzle, Vector3 aimPoint, CoverNodeHealth targetCover)
    {
        StartCoroutine(CoBurst(muzzle, aimPoint, targetCover, false));
    }

    // ─────────────────────────────────────
    // 디버그/트레이서
    // ─────────────────────────────────────
    void DrawTracer(Vector3 s, Vector3 e)
    {
        if (_tracer == null) return;
        StartCoroutine(CoTracer(s, e));
        if (debugHitMarkers) { SpawnDot(s, 0.04f, Color.white); SpawnDot(e, 0.05f, Color.red); }
    }

    IEnumerator CoTracer(Vector3 s, Vector3 e)
    {
        _tracer.enabled = true;
        _tracer.SetPosition(0, s);
        _tracer.SetPosition(1, e);
        yield return new WaitForSeconds(tracerDuration);
        _tracer.enabled = false;
    }

    void DebugLine(Vector3 s, Vector3 e, Color c)
    {
        if (!drawDebug) return;
        Debug.DrawLine(s, e, c, 0.15f);
        if (debugHitMarkers) { SpawnDot(s, 0.03f, c); SpawnDot(e, 0.03f, c); }
    }

    void SpawnDot(Vector3 pos, float r, Color c)
    {
        var dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dot.transform.position = pos;
        dot.transform.localScale = Vector3.one * r;
        var mr = dot.GetComponent<MeshRenderer>();
        var sh = Shader.Find("HDRP/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
        mr.sharedMaterial = new Material(sh);
        mr.sharedMaterial.color = c;
        Destroy(dot.GetComponent<Collider>());
        Destroy(dot, 0.25f);
    }
}
