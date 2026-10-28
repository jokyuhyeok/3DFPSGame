using System;
using UnityEngine;

// 총을 쏠 때 총구 -> 맞은 곳까지 잠깐 보이는 선
[RequireComponent(typeof(LineRenderer))]
public class BulletLine : MonoBehaviour
{
    [SerializeField] private float _duration = 0.05f;

    private LineRenderer _lineRenderer;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.positionCount = 2;
        _lineRenderer.useWorldSpace = true;
    }

    public void Show(Vector3 start, Vector3 end)
    {
        _lineRenderer.SetPosition(0, start);
        _lineRenderer.SetPosition(1, end);

        PoolManager.Instance.Release(gameObject, _duration);
    }
}