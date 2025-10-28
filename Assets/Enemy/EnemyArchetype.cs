using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Enemy Archetype")]
public class EnemyArchetype : ScriptableObject

{
    [Header("전투 스펙")]
    public float health = 100f;
    public float damagePerShot = 10f;
    public float attackDelay = 0.3f;    // 발사 간격
    public int bulletsPerShot = 1;      // 산탄/버스트 수
    public float spreadDegrees = 1.5f;  // 약간의 퍼짐
    public float fireRange = 120f;

    [Header("인식/교전")]
    public float detectRange = 60f;
    public float visionCheckInterval = 0.2f;

    [Header("시각화(선택)")]
    public Material tracerMaterial;
    public float tracerWidth = 0.02f;
    public float tracerDuration = 0.06f;
}
