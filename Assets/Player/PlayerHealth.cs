using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("HP")]
    public float maxHp = 200f;
    public float cur;

    [Header("��� �� �̵��� �� �̸�")]
    public string deathSceneName = "GameOver";   // �̵��� �� �̸� (Build Settings�� ��ϵ� �̸�)

    [Header("����Ʈ �ɼ�")]
    public GameObject deathEffect; // ��� ����Ʈ(��� ��)

    private bool isDead = false;

    void Awake()
    {
        cur = maxHp;
    }

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (isDead) return;

        cur -= amount;
        cur = Mathf.Clamp(cur, 0f, maxHp);

        if (cur <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        if (deathEffect)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        //  gameObject.SetActive(false);   �ּ�ó�� �Ǵ� ����

        StartCoroutine(LoadDeathScene());
    }

    System.Collections.IEnumerator LoadDeathScene()
    {
        yield return new WaitForSeconds(1.5f); // ����� ��� �ð�
        SceneManager.LoadScene(deathSceneName);
    }
}