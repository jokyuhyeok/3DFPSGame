using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 타이틀 화면에서 좀비 무리가 끊임없이 달려가게 한다.
// 이 오브젝트의 앞쪽(파란 화살표, forward) 방향으로 달려가고, 끝까지 간 좀비는 잠시 뒤 출발 영역에서 다시 출발한다.
public class TitleZombieSpawner : MonoBehaviour
{
    [Header("좀비")]
    [SerializeField] private GameObject[] _zombiePrefabs;

    [SerializeField] private RuntimeAnimatorController _animatorController;

    [Header("무리")]
    [SerializeField] private int _count = 12;

    [SerializeField] private Vector2 _areaSize = new Vector2(5f, 3f); // 출발 영역: 로컬 x(폭), z(깊이)
    [SerializeField] private float _runDistance = 40f; // 출발점에서 이만큼 달리면 사라진다.
    [SerializeField] private Vector2 _speedRange = new Vector2(3.5f, 5f);
    [SerializeField] private Vector2 _respawnDelayRange = new Vector2(0f, 1.5f);

    private readonly List<TitleZombie> _zombies = new List<TitleZombie>();

    private void Start()
    {
        for (int i = 0; i < _count; i++)
        {
            GameObject prefab = _zombiePrefabs[i % _zombiePrefabs.Length];
            GameObject go = Instantiate(prefab, transform);
            go.GetComponentInChildren<Animator>().runtimeAnimatorController = _animatorController;

            TitleZombie zombie = go.AddComponent<TitleZombie>();
            zombie.OnFinished += HandleFinished;
            _zombies.Add(zombie);

            // 처음에는 길 전체에 흩어져 있도록 이미 달린 거리를 무작위로 준다.
            StartRunning(zombie, Random.Range(0f, _runDistance));
        }
    }

    private void HandleFinished(TitleZombie zombie)
    {
        StartCoroutine(Respawn_Coroutine(zombie));
    }

    private IEnumerator Respawn_Coroutine(TitleZombie zombie)
    {
        yield return new WaitForSeconds(Random.Range(_respawnDelayRange.x, _respawnDelayRange.y));
        StartRunning(zombie, 0f);
    }

    private void StartRunning(TitleZombie zombie, float startTraveled)
    {
        Vector3 local = new Vector3(
            Random.Range(-_areaSize.x / 2f, _areaSize.x / 2f),
            0f,
            Random.Range(-_areaSize.y / 2f, _areaSize.y / 2f));
        Vector3 position = transform.TransformPoint(local) + transform.forward * startTraveled;
        position.y = GetGroundHeight(position);

        float speed = Random.Range(_speedRange.x, _speedRange.y);
        zombie.Run(position, transform.forward, speed, _runDistance, startTraveled);
    }

    // 위에 다른 물체가 있어도 가장 아래(바닥)에 맞은 높이를 쓴다.
    private float GetGroundHeight(Vector3 position)
    {
        RaycastHit[] hits = Physics.RaycastAll(position + Vector3.up * 5f, Vector3.down, 20f, ~0, QueryTriggerInteraction.Ignore);
        float lowest = position.y;
        bool found = false;
        foreach (RaycastHit hit in hits)
        {
            if (!found || hit.point.y < lowest)
            {
                lowest = hit.point.y;
                found = true;
            }
        }

        return lowest;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(_areaSize.x, 0.1f, _areaSize.y));
        Gizmos.DrawLine(Vector3.zero, Vector3.forward * _runDistance);
    }
}
