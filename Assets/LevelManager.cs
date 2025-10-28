using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    List<Health> enemies = new List<Health>();
    int alive;

    void Start()
    {
        var all = FindObjectsOfType<Health>();
        foreach (var h in all)
        {
            if (h.isEnemy)
            {
                enemies.Add(h);
                h.OnDied += HandleEnemyDied;
            }
        }
        alive = enemies.Count;
        // Debug.Log($"Enemies alive: {alive}");
    }

    void HandleEnemyDied(Health h)
    {
        alive--;
        if (alive <= 0) LoadNextScene();
    }

    void LoadNextScene()
    {
        int total = SceneManager.sceneCountInBuildSettings;
        if (total == 0) return; // 보호

        int idx = SceneManager.GetActiveScene().buildIndex;
        int next = (idx + 1) % total;
        SceneManager.LoadScene(next);
    }

    void OnDestroy()
    {
        // 안전하게 핸들러 해제
        foreach (var e in enemies)
            if (e != null) e.OnDied -= HandleEnemyDied;
    }
}
