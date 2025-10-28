using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Health : MonoBehaviour
{
    public float maxHP = 100f;
    public bool isEnemy = true;

    public float current;

    public event Action<Health> OnDied;

    void Awake()
    {
        current = maxHP;
    }

    public void TakeDamage(float amount)
    {
        current -= amount;
        if (current <= 0f)
        {
            current = 0f;
            OnDied?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
