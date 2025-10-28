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

    [Header("UI ������(���� �����̽�)")]
    public GameObject healthBarPrefab;  // EnemyHealthBar.prefab
    public Transform headAnchor;        // ������ ��ü transform ���

    // C# �̺�Ʈ (UI�� ����)
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
                _ui.Bind(this);                     // �� ���⼭ ��� ���ε� + �ʱⰪ 1.0 �ݿ�
                _ui.follow = headAnchor ? headAnchor : transform;
            }
        }

        // �ʱ� ��ε�ĳ��Ʈ(�־ ����)
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

        // UI�� ����
        if (_ui) Destroy(_ui.gameObject);

        // ���� �� ���� (�ʿ�� �ִ�/������ �� �־ ��)
        Destroy(gameObject);
    }
}
