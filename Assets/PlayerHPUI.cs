using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHPUI : MonoBehaviour
{
    [Header("필수 참조")]
    public PlayerHealth player;    // 이미 있는 PlayerHealth
    public Slider hpSlider;        // 화면 고정 슬라이더

    [Header("옵션")]
    public bool smooth = true;
    public float smoothSpeed = 10f;   // 높을수록 빨리 따라감
    public Image fillImage;           // 색 바꾸고 싶을 때 (선택)
    public Gradient colorByRatio;     // 0~1 비율 색상 (선택)

    float target;

    void Start()
    {
        if (!player) player = FindObjectOfType<PlayerHealth>();
        if (!hpSlider) { Debug.LogError("[PlayerHPHUD] hpSlider 미지정"); enabled = false; return; }

        hpSlider.minValue = 0f;
        hpSlider.maxValue = player.maxHp;
        hpSlider.value = player.cur;
        target = player.cur;
        UpdateColor();
    }

    void Update()
    {
        // 플레이어 HP 읽어와 목표값 갱신
        target = Mathf.Clamp(player.cur, 0f, player.maxHp);

        // 부드럽게 혹은 즉시
        if (smooth)
            hpSlider.value = Mathf.Lerp(hpSlider.value, target, Time.deltaTime * smoothSpeed);
        else
            hpSlider.value = target;

        // 최대치가 변하는 경우(버프 등)에 대응
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
