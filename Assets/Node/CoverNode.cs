using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoverNode : MonoBehaviour
{
    [Tooltip("�� ��忡�� �ٱ�(���� ǥ���� �ٱ�) ���� ����. ���� ���� �ٶ󺸴� ����.")]
    public Vector3 coverNormal = Vector3.forward;

    [Header("�̿� ����")]
    public CoverNode leftNode;
    public CoverNode rightNode;

    private void OnValidate()
    {
        if (coverNormal == Vector3.zero) coverNormal = Vector3.forward;
        coverNormal = coverNormal.normalized;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, coverNormal.normalized * 0.6f);
        if (leftNode)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, leftNode.transform.position);
        }
        if (rightNode)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, rightNode.transform.position);
        }
    }
}
