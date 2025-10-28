using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

using UnityEngine;

public class HitMarkerUI : MonoBehaviour
{
    [Header("�ʼ� ����")]
    public Image marker;                 // ǥ�ÿ� �̹���(�Ͼ� X, + ��)

    [Header("����")]
    public float showTime = 0.12f;       // ���� ���̴� �ð�
    public float fadeTime = 0.15f;       // ���̵� �ƿ� �ð�
    public float popScale = 1.25f;       // Ƣ����� ������
    public float normalScale = 1.0f;     // �⺻ ������
    public Color normalColor = Color.white;
    public Color killColor = new Color(1f, 0.2f, 0.2f); // �� �����(����)

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

        // ��/������ �ʱ�ȭ
        marker.color = killed ? killColor : normalColor;
        transform.localScale = Vector3.one * popScale;
        SetVisible(1f);

        // ª�� ����
        yield return new WaitForSeconds(showTime);

        // ������ �ڿ� ���� + ���� ���̵�
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
