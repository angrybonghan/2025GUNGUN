using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoverNode : MonoBehaviour
{
    [Tooltip("이 노드에서 바깥(엄폐물 표면의 바깥) 방향 법선. 보통 벽이 바라보는 방향.")]
    public Vector3 coverNormal = Vector3.forward;

    [Header("이웃 노드들")]
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
