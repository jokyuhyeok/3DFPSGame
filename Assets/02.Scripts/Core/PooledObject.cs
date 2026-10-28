using UnityEngine;
using UnityEngine.Pool;
using System.Collections;

// 풀해서 만들어진 오브젝트에 PoolManager가 자동으로 붙이는 이름표
// - 자신이 어느 풀 소속인지 기억해서 원래 풀로 반납
public class PooledObject : MonoBehaviour
{
    private IObjectPool<GameObject> _pool;
    private bool _isReleased = false;

    // 생성 직후 PoolManager가 한 번 호출한다.
    public void Initialize(IObjectPool<GameObject> pool)
    {
        _pool = pool;
    }

    public void OnTakenFromPool()
    {
        _isReleased = false;
    }

    public void Release()
    {
        if (_isReleased) return; // 중복 반납 방지

        _isReleased = true;
        _pool.Release(gameObject);
    }

    public void ReleaseAfter(float delay)
    {
        StartCoroutine(ReleaseAfter_Coroutine(delay));
    }

    private IEnumerator ReleaseAfter_Coroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        Release();
    }
}