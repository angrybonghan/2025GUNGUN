using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class SceneButtonLoader : MonoBehaviour
{
    [Header("�̵��� �� �̸� (Build Settings�� ��ϵǾ�� ��)")]
    public string sceneToLoad = "NextScene";

    // ��ư���� ȣ���� �Լ�
    public void LoadTargetScene()
    {
        SceneManager.LoadScene(sceneToLoad);
    }

    // ��ư ������ ���� ���峪 ����Ʈ �ְ� ������ �Ʒ�ó�� Ȱ�� ����
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
