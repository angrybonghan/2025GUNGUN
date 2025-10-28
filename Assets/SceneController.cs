using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    [Header("씬 이동 설정")]
    [Tooltip("모든 적이 제거되면 이동할 다음 씬 이름")]
    public string nextSceneName;

    [Tooltip("적이 없다고 판단하기 전 대기 시간(초)")]
    public float checkInterval = 2f;

    [Tooltip("씬 전환 전 약간의 지연 (연출용)")]
    public float transitionDelay = 1f;

    float _nextCheckTime;

    void Update()
    {
        // 일정 주기로만 검사
        if (Time.time < _nextCheckTime) return;
        _nextCheckTime = Time.time + checkInterval;

        // 현재 활성 씬에서 EnemyHealth 컴포넌트가 붙은 객체 검색
        EnemyHealth[] enemies = FindObjectsOfType<EnemyHealth>(true); // true = 비활성도 포함
        int aliveCount = 0;

        foreach (var e in enemies)
        {
            if (e != null && e.currentHp > 0f)
                aliveCount++;
        }

        // 살아있는 적이 없으면 다음 씬 로드
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
