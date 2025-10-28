using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Enemy Archetype")]
public class EnemyArchetype : ScriptableObject

{
    [Header("���� ����")]
    public float health = 100f;
    public float damagePerShot = 10f;
    public float attackDelay = 0.3f;    // �߻� ����
    public int bulletsPerShot = 1;      // ��ź/����Ʈ ��
    public float spreadDegrees = 1.5f;  // �ణ�� ����
    public float fireRange = 120f;

    [Header("�ν�/����")]
    public float detectRange = 60f;
    public float visionCheckInterval = 0.2f;

    [Header("�ð�ȭ(����)")]
    public Material tracerMaterial;
    public float tracerWidth = 0.02f;
    public float tracerDuration = 0.06f;
}
