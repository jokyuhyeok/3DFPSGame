using System;
using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

// 오브젝트 풀 중앙 관리자
// 프리팹마다 Unity 내장 ObjectPool을 하나씩 만들어서 관리한다.
// 풀은 처음 요청될 때 자동 생성
public class PoolManager : MonoBehaviour
{
    private static PoolManager _instance = null;
    public static PoolManager Instance => _instance;

    [Header("풀 설정")]
    [SerializeField] private int _defaultPoolSize = 10;

    [SerializeField] private int _maxSize = 100;

    private readonly Dictionary<GameObject, ObjectPool<GameObject>> _pools =
        new Dictionary<GameObject, ObjectPool<GameObject>>();

    private Transform _inactiveRoot;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        GameObject inactiveRoot = new GameObject("InactiveRoot");
        inactiveRoot.SetActive(false);
        inactiveRoot.transform.SetParent(transform);
        _inactiveRoot = inactiveRoot.transform;
    }

    // 풀에서 꺼내기: 위치/회전을 먼저 잡고 활성화한다. (OnEnable이 올바른 위치에서 실행되도록)
    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        ObjectPool<GameObject> pool = GetOrCreatePool(prefab);
        GameObject obj = pool.Get();

        obj.transform.SetPositionAndRotation(position, rotation);
        obj.GetComponent<PooledObject>().OnTakenFromPool();
        obj.SetActive(true);

        return obj;
    }

    // 컴포넌트 타입으로 꺼내기 (예: Bomb 프리팹 → Bomb 반환)
    public T Get<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component
    {
        return Get(prefab.gameObject, position, rotation).GetComponent<T>();
    }

    // 풀로 반납 (풀에서 만든 게 아니면 그냥 파괴)
    public void Release(GameObject obj)
    {
        if (obj.TryGetComponent(out PooledObject pooled))
        {
            pooled.Release();
        }
        else
        {
            Destroy(obj);
        }
    }

    // delay초 뒤에 반납
    public void Release(GameObject obj, float delay)
    {
        if (obj.TryGetComponent(out PooledObject pooled))
        {
            pooled.ReleaseAfter(delay);
        }
        else
        {
            Destroy(obj, delay);
        }
    }

    // 이펙트 재생: 꺼내서 재생하고, 재생이 끝나는 시간에 자동 반납한다.
    public ParticleSystem PlayEffect(ParticleSystem prefab, Vector3 position, Quaternion rotation)
    {
        ParticleSystem effect = Get(prefab, position, rotation);

        // 자식 파티클까지 포함해 가장 늦게 끝나는 시간을 구한다.
        float lifetime = 0f;
        foreach (ParticleSystem system in effect.GetComponentsInChildren<ParticleSystem>())
        {
            ParticleSystem.MainModule main = system.main;
            main.stopAction = ParticleSystemStopAction.None; // 프리팹이 Destroy로 되어 있으면 풀 오브젝트가 파괴되므로 끈다.
            lifetime = Mathf.Max(lifetime,
                main.startDelay.constantMax + main.duration + main.startLifetime.constantMax);
        }

        effect.Clear(true); // 이전 재생에서 남은 입자 제거
        effect.Play(true);
        Release(effect.gameObject, lifetime);

        return effect;
    }

    private ObjectPool<GameObject> GetOrCreatePool(GameObject prefab)
    {
        if (_pools.TryGetValue(prefab, out ObjectPool<GameObject> pool)) return pool;

        pool = new ObjectPool<GameObject>(
            createFunc: () => CreatePooledObject(prefab, pool), // 풀이 비었을 때 새로 만드는 방법
            actionOnGet: null, // 활성화는 Get()에서 위치를 잡은 뒤 직접 한다.
            actionOnRelease: obj => obj.SetActive(false), // 반납 = 비활성화
            actionOnDestroy: obj => Destroy(obj), // 최대 개수를 넘쳐 버려질 때
            collectionCheck: true, // 에디터에서 중복 반납 검사
            defaultCapacity: _defaultPoolSize,
            maxSize: _maxSize);

        _pools.Add(prefab, pool);
        return pool;
    }

    private GameObject CreatePooledObject(GameObject prefab, IObjectPool<GameObject> pool)
    {
        GameObject obj = Instantiate(prefab, _inactiveRoot); // 비활성 부모 아래 → 아직 Awake/OnEnable 실행 안 됨
        obj.SetActive(false);
        obj.transform.SetParent(transform); // Hierarchy 정리: PoolManager 아래에 모아 둔다.

        PooledObject pooled = obj.AddComponent<PooledObject>();
        pooled.Initialize(pool);

        return obj;
    }
}