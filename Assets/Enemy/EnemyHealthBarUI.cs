using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBarUI : MonoBehaviour
{
    [Header("�ʼ� ����")]
    public EnemyHealth target;
    public Slider slider;
    public Transform follow;

    [Header("ǥ��/���� �ɼ�")]
    public Vector3 worldOffset = new Vector3(0f, 2.0f, 0f);
    public bool hideWhenFull = true;
    public float hideDelay = 1.0f;
    public float smoothSpeed = 10f;

    Camera cam;
    float _targetRatio;
    float _hideTimer;
    bool _bound; // �̹� ���ε� �ߴ���

    void Awake()
    {
        cam = Camera.main;
        if (!slider) slider = GetComponentInChildren<Slider>(true);
    }

    void OnEnable()
    {
        // target�� �̹� ���õ� �ִٸ� �ٷ� ���ε� �õ�
        if (target && !_bound) Bind(target);
    }

    void OnDisable()
    {
        if (_bound && target)
        {
            target.OnHealthChanged -= HandleHealthChanged;
            target.OnDied -= HandleDied;
        }
        _bound = false;
    }

    // �ܺ�(EnemyHealth)���� �ݵ�� ȣ���� �ָ� ���� ����
    public void Bind(EnemyHealth t)
    {
        // ���� ����
        if (_bound && target)
        {
            target.OnHealthChanged -= HandleHealthChanged;
            target.OnDied -= HandleDied;
        }

        target = t;
        if (!follow && target) follow = target.transform;

        // ���� + �ʱ� �� ����ȭ
        target.OnHealthChanged += HandleHealthChanged;
        target.OnDied += HandleDied;

        _targetRatio = SafeRatio(target.currentHp, target.maxHp);
        if (slider)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.value = _targetRatio;   // �� �ٷ� �ݿ�
        }

        _hideTimer = 0f;
        _bound = true;
    }

    void HandleHealthChanged(float cur, float max)
    {
        _targetRatio = SafeRatio(cur, max);
        if (hideWhenFull)
        {
            bool full = cur >= max - 0.001f;
            if (!full) _hideTimer = hideDelay;
        }
    }

    void HandleDied()
    {
        Destroy(gameObject);
    }

    void LateUpdate()
    {
        if (!_bound || !target || !slider) return;

        // ��ġ/������
        if (follow) transform.position = follow.position + worldOffset;
        if (cam) transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position, Vector3.up);

        // �� ����
        slider.value = Mathf.MoveTowards(slider.value, _targetRatio, smoothSpeed * Time.deltaTime);

        // Ǯ HP�� �� �����
        if (hideWhenFull)
        {
            bool full = _targetRatio >= 0.999f;
            if (!full) _hideTimer = hideDelay;
            else if (_hideTimer > 0f) _hideTimer -= Time.deltaTime;

            slider.gameObject.SetActive(!full || _hideTimer > 0f);
        }
    }

    float SafeRatio(float cur, float max) => (max <= 0.0001f) ? 0f : Mathf.Clamp01(cur / max);
}
