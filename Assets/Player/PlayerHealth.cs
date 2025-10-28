using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("HP")]
    public float maxHp = 200f;
    public float cur;

    [Header("사망 시 이동할 씬 이름")]
    public string deathSceneName = "GameOver";   // 이동할 씬 이름 (Build Settings에 등록된 이름)

    [Header("이펙트 옵션")]
    public GameObject deathEffect; // 사망 이펙트(없어도 됨)

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

        //  gameObject.SetActive(false);   주석처리 또는 삭제

        StartCoroutine(LoadDeathScene());
    }

    System.Collections.IEnumerator LoadDeathScene()
    {
        yield return new WaitForSeconds(1.5f); // 연출용 대기 시간
        SceneManager.LoadScene(deathSceneName);
    }
}