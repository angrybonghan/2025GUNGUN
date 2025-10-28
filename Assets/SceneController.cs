using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    [Header("�� �̵� ����")]
    [Tooltip("��� ���� ���ŵǸ� �̵��� ���� �� �̸�")]
    public string nextSceneName;

    [Tooltip("���� ���ٰ� �Ǵ��ϱ� �� ��� �ð�(��)")]
    public float checkInterval = 2f;

    [Tooltip("�� ��ȯ �� �ణ�� ���� (�����)")]
    public float transitionDelay = 1f;

    float _nextCheckTime;

    void Update()
    {
        // ���� �ֱ�θ� �˻�
        if (Time.time < _nextCheckTime) return;
        _nextCheckTime = Time.time + checkInterval;

        // ���� Ȱ�� ������ EnemyHealth ������Ʈ�� ���� ��ü �˻�
        EnemyHealth[] enemies = FindObjectsOfType<EnemyHealth>(true); // true = ��Ȱ���� ����
        int aliveCount = 0;

        foreach (var e in enemies)
        {
            if (e != null && e.currentHp > 0f)
                aliveCount++;
        }

        // ����ִ� ���� ������ ���� �� �ε�
        if (aliveCount == 0 && !string.IsNullOrEmpty(nextSceneName))
        {
            StartCoroutine(LoadNextSceneAfterDelay());
        }
    }

    System.Collections.IEnumerator LoadNextSceneAfterDelay()
    {
        yield return new WaitForSeconds(transitionDelay);
        SceneManager.LoadScene(nextSceneName);
    }
}
