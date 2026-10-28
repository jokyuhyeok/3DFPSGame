using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 포인트별 스포너 -> 1개의 스포너가 여러 스폰 포인트를 관리한다.
// 동시에 살아있는 좀비가 최대 수 미만 -> 랜덤 시간후 랜덤 포인트(작전 포인트 제외)에서 한 마리씩 스폰한다.
public class ZombieSpawner : MonoBehaviour
{
    [SerializeField] private Zombie _zombiePrefab;
    [SerializeField] private SpawnPoint[] _spawnPoints; // 씬의 스폰 포인트들을 연결

    [Header("스폰 설정")]
    [SerializeField] private int _maxZombieCount = 5;

    [SerializeField] private float _minSpawnDelay = 5f;
    [SerializeField] private float _maxSpawnDelay = 10f;

    // 살아있는 좀비 목록
    private readonly List<Zombie> _aliveZombies = new List<Zombie>();
    private Coroutine _spawnCoroutine = null; // 스폰 대기 중인지 확인
    private int _lastSpawnIndex = -1; // 직전에 사용한 스폰 포인트 인덱스

    public event Action<int, int> OnZombieCountChanged; // 좀비 수의 변경 알림 -> UI가 구독

    public int AliveZombieCount => _aliveZombies.Count;
    public int MaxZombieCount => _maxZombieCount;

    private void Start()
    {
        TryStartSpawn(); // [변경] 바로 코루틴 시작 → 조건 확인 후 시작
    }

    private void OnDestroy()
    {
        // 살아있는 모든 좀비의 구독 해제
        foreach (Zombie zombie in _aliveZombies)
        {
            if (zombie != null)
            {
                zombie.OnDeath -= HandleZombieDeath;
            }
        }
    }

    // 최대 수 미만이고 스폰 대기 중이 아닐 때만 스폰 코루틴을 시작한다.
    // 좀비 스폰 직후와 좀비 사망 직후, 두 시점에서만 호출된다. (폴링 없음)
    private void TryStartSpawn()
    {
        if (_spawnCoroutine != null) return;
        if (_aliveZombies.Count >= _maxZombieCount) return;

        _spawnCoroutine = StartCoroutine(Spawn_Coroutine());
    }

    private IEnumerator Spawn_Coroutine()
    {
        // 고정 지연 → 최소~최대 사이 랜덤 지연
        float delay = UnityEngine.Random.Range(_minSpawnDelay, _maxSpawnDelay);
        yield return new WaitForSeconds(delay);

        yield return new WaitUntil(() => GameManager.Instance.State == GameState.Playing);

        Spawn();

        _spawnCoroutine = null;
        TryStartSpawn(); // 아직 자리가 남았으면 다음 스폰 예약
    }

    private void Spawn()
    {
        if (_zombiePrefab == null || _spawnPoints.Length == 0) return;

        SpawnPoint spawnPoint = _spawnPoints[GetRandomSpawnIndex()]; // 랜덤 포인트 선택

        Zombie zombie =
            PoolManager.Instance.Get(_zombiePrefab, spawnPoint.transform.position, spawnPoint.transform.rotation);
        zombie.SetPatrolPoints(spawnPoint.PatrolPoints);

        zombie.OnDeath += HandleZombieDeath;
        _aliveZombies.Add(zombie);

        OnZombieCountChanged?.Invoke(_aliveZombies.Count, _maxZombieCount); // 수 변경 알림
    }

    // 직전 인덱스를 제외한 랜덤 인덱스
    // 예) 포인트 3개, 직전 1 → 0~1 중 뽑고, 1 이상이면 +1 → 결과는 0 또는 2
    private int GetRandomSpawnIndex()
    {
        int count = _spawnPoints.Length;
        int index;

        if (count == 1 || _lastSpawnIndex < 0)
        {
            index = UnityEngine.Random.Range(0, count);
        }
        else
        {
            index = UnityEngine.Random.Range(0, count - 1);
            if (index >= _lastSpawnIndex)
            {
                index++;
            }
        }

        _lastSpawnIndex = index;
        return index;
    }

    // 매개변수로 어떤 좀비가 죽었는지 전달받는다.
    private void HandleZombieDeath(Zombie zombie)
    {
        zombie.OnDeath -= HandleZombieDeath;
        _aliveZombies.Remove(zombie);

        OnZombieCountChanged?.Invoke(_aliveZombies.Count, _maxZombieCount); // [추가] 수 변경 알림
        TryStartSpawn(); // [변경] 바로 코루틴 시작 → 대기 중이 아닐 때만 시작
    }
}