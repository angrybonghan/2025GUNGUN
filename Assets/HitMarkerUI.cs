using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

using UnityEngine;

public class HitMarkerUI : MonoBehaviour
{
    [Header("필수 참조")]
    public Image marker;                 // 표시용 이미지(하얀 X, + 등)

    [Header("연출")]
    public float showTime = 0.12f;       // 완전 보이는 시간
    public float fadeTime = 0.15f;       // 페이드 아웃 시간
    public float popScale = 1.25f;       // 튀어나오는 스케일
    public float normalScale = 1.0f;     // 기본 스케일
    public Color normalColor = Color.white;
    public Color killColor = new Color(1f, 0.2f, 0.2f); // 적 사망시(선택)

    Coroutine _co;

    void Reset()
    {
        marker = GetComponent<Image>();
    }

    void Awake()
    {
        if (!marker) marker = GetComponent<Image>();
        SetVisible(0f);
        transform.localScale = Vector3.one * normalScale;
    }

    public void Show(bool killed = false)
    {
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(CoShow(killed));
    }

    IEnumerator CoShow(bool killed)
    {
        if (!marker) yield break;

        // 색/스케일 초기화
        marker.color = killed ? killColor : normalColor;
        transform.localScale = Vector3.one * popScale;
        SetVisible(1f);

        // 짧게 유지
        yield return new WaitForSeconds(showTime);

        // 스케일 자연 복귀 + 알파 페이드
        float t = 0f;
        var startCol = marker.color;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(1f, 0f, t / fadeTime);
            var c = startCol; c.a = a;
            marker.color = c;

            float s = Mathf.Lerp(popScale, normalScale, t / fadeTime);
            transform.localScale = Vector3.one * s;

            yield return null;
        }
        SetVisible(0f);
        transform.localScale = Vector3.one * normalScale;
        _co = null;
    }

    void SetVisible(float alpha)
    {
        if (!marker) return;
        var c = marker.color; c.a = alpha;
        marker.color = c;
    }
}
