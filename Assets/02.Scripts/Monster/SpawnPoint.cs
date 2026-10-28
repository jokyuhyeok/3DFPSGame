using System;
using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [SerializeField] private Transform[] _patrolPoints;

    [Header("에디터 표시")]
    [SerializeField] private Color _gizmoColor = Color.green;

    public Transform[] PatrolPoints => _patrolPoints;

    private void OnDrawGizmos()
    {
        Gizmos.color = _gizmoColor;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        if (_patrolPoints == null || _patrolPoints.Length == 0) return;

        if (_patrolPoints[0] != null)
        {
            Gizmos.DrawLine(transform.position, _patrolPoints[0].position);
        }

        for (int i = 0; i < _patrolPoints.Length; i++)
        {
            Transform current = _patrolPoints[i];
            Transform next = _patrolPoints[(i + 1) % _patrolPoints.Length];

            if (current == null) continue;

            Gizmos.DrawSphere(current.position, 0.25f);

            if (next != null)
            {
                Gizmos.DrawLine(current.position, next.position);
            }
        }
    }
}