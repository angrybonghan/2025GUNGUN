using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class EnemyHealth : MonoBehaviour
{
    [Header("HP")]
    public float maxHp = 100f;
    public float currentHp;

    [Header("UI 프리팹(월드 스페이스)")]
    public GameObject healthBarPrefab;  // EnemyHealthBar.prefab
    public Transform headAnchor;        // 없으면 본체 transform 사용

    // C# 이벤트 (UI가 구독)
    public event Action<float, float> OnHealthChanged; // (current, max)
    public event Action OnDied;

    private EnemyHealthBarUI _ui;

    void Awake()
    {
        currentHp = maxHp;

        if (healthBarPrefab)
        {
            var uiGo = Instantiate(healthBarPrefab);
            _ui = uiGo.GetComponent<EnemyHealthBarUI>();
            if (_ui)
            {
                _ui.Bind(this);                     // ★ 여기서 즉시 바인딩 + 초기값 1.0 반영
                _ui.follow = headAnchor ? headAnchor : transform;
            }
        }

        // 초기 브로드캐스트(있어도 무방)
        OnHealthChanged?.Invoke(currentHp, maxHp);
    }

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (currentHp <= 0f) return;

        currentHp = Mathf.Max(0f, currentHp - amount);
        OnHealthChanged?.Invoke(currentHp, maxHp);

        if (currentHp <= 0f)
            Die();
    }

    void Die()
    {
        OnDied?.Invoke();

        // UI도 제거
        if (_ui) Destroy(_ui.gameObject);

        // 실제 적 제거 (필요시 애니/딜레이 등 넣어도 됨)
        Destroy(gameObject);
    }
}
