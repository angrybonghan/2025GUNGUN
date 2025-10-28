using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class SceneButtonLoader : MonoBehaviour
{
    [Header("이동할 씬 이름 (Build Settings에 등록되어야 함)")]
    public string sceneToLoad = "NextScene";

    // 버튼에서 호출할 함수
    public void LoadTargetScene()
    {
        SceneManager.LoadScene(sceneToLoad);
    }

    // 버튼 누르기 전에 사운드나 이펙트 넣고 싶으면 아래처럼 활용 가능
    public void LoadTargetSceneWithDelay(float delay = 1f)
    {
        StartCoroutine(LoadDelayed(delay));
    }

    private System.Collections.IEnumerator LoadDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneToLoad);
    }
}
