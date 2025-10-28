using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHPUI : MonoBehaviour
{
    [Header("�ʼ� ����")]
    public PlayerHealth player;    // �̹� �ִ� PlayerHealth
    public Slider hpSlider;        // ȭ�� ���� �����̴�

    [Header("�ɼ�")]
    public bool smooth = true;
    public float smoothSpeed = 10f;   // �������� ���� ����
    public Image fillImage;           // �� �ٲٰ� ���� �� (����)
    public Gradient colorByRatio;     // 0~1 ���� ���� (����)

    float target;

    void Start()
    {
        if (!player) player = FindObjectOfType<PlayerHealth>();
        if (!hpSlider) { Debug.LogError("[PlayerHPHUD] hpSlider ������"); enabled = false; return; }

        hpSlider.minValue = 0f;
        hpSlider.maxValue = player.maxHp;
        hpSlider.value = player.cur;
        target = player.cur;
        UpdateColor();
    }

    void Update()
    {
        // �÷��̾� HP �о�� ��ǥ�� ����
        target = Mathf.Clamp(player.cur, 0f, player.maxHp);

        // �ε巴�� Ȥ�� ���
        if (smooth)
            hpSlider.value = Mathf.Lerp(hpSlider.value, target, Time.deltaTime * smoothSpeed);
        else
            hpSlider.value = target;

        // �ִ�ġ�� ���ϴ� ���(���� ��)�� ����
        if (hpSlider.maxValue != player.maxHp)
            hpSlider.maxValue = player.maxHp;

        UpdateColor();
    }

    void UpdateColor()
    {
        if (!fillImage || colorByRatio == null) return;
        float ratio = (player.maxHp <= 0f) ? 0f : hpSlider.value / player.maxHp;
        fillImage.color = colorByRatio.Evaluate(ratio);
    }
}
