using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CoverNodeHealth : MonoBehaviour
{
    [Header("HP")]
    public float maxHp = 200f;
    public float currentHp;

    [Tooltip("한 번의 피격량이 이 값 이상이면 즉시 '부서진 상태'로 전환")]
    public float breakOnHitDamage = 99999f; // 원하면 50~100 등으로 세팅. 사용 안 하려면 아주 크게.

    [Header("파괴 / 복구")]
    public float respawnDelay = 6f;

    [Tooltip("커버가 '살아있을 때' 사용하는 레이어 (예: Cover)")]
    public int aliveLayer = -1;             // -1이면 현재 레이어를 자동 저장
    [Tooltip("파괴 동안 사용할 레이어 (Cover 마스크에 포함되지 않는 레이어, 예: Ignore Raycast=2)")]
    public int brokenLayer = 2;             // 기본 2: Ignore Raycast

    [Header("제어 대상 (없으면 자동 수집)")]
    public Renderer[] renderers;
    public Collider[] colliders;

    public bool IsDestroyed { get; private set; }

    // 원복용 저장소
    int _originalNodeLayer;
    List<(GameObject go, int layer)> _originalChildLayers = new();

    void Awake()
    {
        currentHp = maxHp;

        // 제어 대상 자동 수집(수동 지정해도 됨)
        if (renderers == null || renderers.Length == 0) renderers = GetComponentsInChildren<Renderer>(true);
        if (colliders == null || colliders.Length == 0) colliders = GetComponentsInChildren<Collider>(true);

        // "노드 앵커"로 쓰는 자기 자신의 콜라이더는 꺼지지 않도록 제외(있다면)
        var selfCol = GetComponent<Collider>();
        if (selfCol)
        {
            var list = new List<Collider>(colliders);
            list.Remove(selfCol);
            colliders = list.ToArray();
        }

        // 레이어 원본 저장
        _originalNodeLayer = gameObject.layer;
        if (aliveLayer < 0) aliveLayer = _originalNodeLayer; // 최초 레이어를 alive로 사용
        CacheChildLayers();
    }

    void CacheChildLayers()
    {
        _originalChildLayers.Clear();
        var trs = GetComponentsInChildren<Transform>(true);
        foreach (var t in trs)
        {
            _originalChildLayers.Add((t.gameObject, t.gameObject.layer));
        }
    }

    public void TakeDamage(float dmg, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (IsDestroyed) return;

        currentHp -= dmg;

        // 한 번의 피격량이 임계치를 넘으면 즉시 파괴
        if (dmg >= breakOnHitDamage || currentHp <= 0f)
        {
            Break();
        }
    }

    void Break()
    {
        if (IsDestroyed) return;
        IsDestroyed = true;

        // 1) 메시/콜라이더 OFF (시각/충돌 사라짐)
        foreach (var r in renderers) if (r) r.enabled = false;
        foreach (var c in colliders) if (c) c.enabled = false;

        // 2) 레이어를 Cover 마스크에서 빠지는 레이어(기본: Ignore Raycast)로 변경
        SetLayerRecursively(brokenLayer);

        // 3) 일정 시간 후 복구
        StartCoroutine(CoRespawn());
    }

    IEnumerator CoRespawn()
    {
        yield return new WaitForSeconds(respawnDelay);

        currentHp = maxHp;
        IsDestroyed = false;

        // 1) 메시/콜라이더 ON
        foreach (var r in renderers) if (r) r.enabled = true;
        foreach (var c in colliders) if (c) c.enabled = true;

        // 2) 레이어 원복(노드 + 자식 전부)
        RestoreLayers();
    }

    void SetLayerRecursively(int layer)
    {
        var trs = GetComponentsInChildren<Transform>(true);
        foreach (var t in trs) t.gameObject.layer = layer;
    }

    void RestoreLayers()
    {
        // 노드
        gameObject.layer = aliveLayer;

        // 자식들
        foreach (var pair in _originalChildLayers)
        {
            if (pair.go != null) pair.go.layer = pair.layer;
        }
    }
}
