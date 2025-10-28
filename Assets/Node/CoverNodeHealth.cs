using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CoverNodeHealth : MonoBehaviour
{
    [Header("HP")]
    public float maxHp = 200f;
    public float currentHp;

    [Tooltip("�� ���� �ǰݷ��� �� �� �̻��̸� ��� '�μ��� ����'�� ��ȯ")]
    public float breakOnHitDamage = 99999f; // ���ϸ� 50~100 ������ ����. ��� �� �Ϸ��� ���� ũ��.

    [Header("�ı� / ����")]
    public float respawnDelay = 6f;

    [Tooltip("Ŀ���� '������� ��' ����ϴ� ���̾� (��: Cover)")]
    public int aliveLayer = -1;             // -1�̸� ���� ���̾ �ڵ� ����
    [Tooltip("�ı� ���� ����� ���̾� (Cover ����ũ�� ���Ե��� �ʴ� ���̾�, ��: Ignore Raycast=2)")]
    public int brokenLayer = 2;             // �⺻ 2: Ignore Raycast

    [Header("���� ��� (������ �ڵ� ����)")]
    public Renderer[] renderers;
    public Collider[] colliders;

    public bool IsDestroyed { get; private set; }

    // ������ �����
    int _originalNodeLayer;
    List<(GameObject go, int layer)> _originalChildLayers = new();

    void Awake()
    {
        currentHp = maxHp;

        // ���� ��� �ڵ� ����(���� �����ص� ��)
        if (renderers == null || renderers.Length == 0) renderers = GetComponentsInChildren<Renderer>(true);
        if (colliders == null || colliders.Length == 0) colliders = GetComponentsInChildren<Collider>(true);

        // "��� ��Ŀ"�� ���� �ڱ� �ڽ��� �ݶ��̴��� ������ �ʵ��� ����(�ִٸ�)
        var selfCol = GetComponent<Collider>();
        if (selfCol)
        {
            var list = new List<Collider>(colliders);
            list.Remove(selfCol);
            colliders = list.ToArray();
        }

        // ���̾� ���� ����
        _originalNodeLayer = gameObject.layer;
        if (aliveLayer < 0) aliveLayer = _originalNodeLayer; // ���� ���̾ alive�� ���
        CacheChildLayers();
    }

    void CacheChildLayers()
    {
        _originalChildLayers.Clear();
        var trs = GetComponentsInChildren<Transform>(true);
        foreach (var t in trs)
        {
            _originalChildLayers.Add((t.gameObject, t.gameObject.layer));
        }
    }

    public void TakeDamage(float dmg, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (IsDestroyed) return;

        currentHp -= dmg;

        // �� ���� �ǰݷ��� �Ӱ�ġ�� ������ ��� �ı�
        if (dmg >= breakOnHitDamage || currentHp <= 0f)
        {
            Break();
        }
    }

    void Break()
    {
        if (IsDestroyed) return;
        IsDestroyed = true;

        // 1) �޽�/�ݶ��̴� OFF (�ð�/�浹 �����)
        foreach (var r in renderers) if (r) r.enabled = false;
        foreach (var c in colliders) if (c) c.enabled = false;

        // 2) ���̾ Cover ����ũ���� ������ ���̾�(�⺻: Ignore Raycast)�� ����
        SetLayerRecursively(brokenLayer);

        // 3) ���� �ð� �� ����
        StartCoroutine(CoRespawn());
    }

    IEnumerator CoRespawn()
    {
        yield return new WaitForSeconds(respawnDelay);

        currentHp = maxHp;
        IsDestroyed = false;

        // 1) �޽�/�ݶ��̴� ON
        foreach (var r in renderers) if (r) r.enabled = true;
        foreach (var c in colliders) if (c) c.enabled = true;

        // 2) ���̾� ����(��� + �ڽ� ����)
        RestoreLayers();
    }

    void SetLayerRecursively(int layer)
    {
        var trs = GetComponentsInChildren<Transform>(true);
        foreach (var t in trs) t.gameObject.layer = layer;
    }

    void RestoreLayers()
    {
        // ���
        gameObject.layer = aliveLayer;

        // �ڽĵ�
        foreach (var pair in _originalChildLayers)
        {
            if (pair.go != null) pair.go.layer = pair.layer;
        }
    }
}
