using UnityEngine;

// 무언가에 부딪히면 폭발 이펙트를 남기고 사라지는 폭탄
[RequireComponent(typeof(Rigidbody))]
public class Bomb : MonoBehaviour
{
    [SerializeField] private ParticleSystem _explosionEffectPrefab;
    [SerializeField] private float _damage = 10f;
    [SerializeField] private float _explosionRange = 4.5f;

    private Rigidbody _rigidbody;
    private bool _isExploded = false;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        _isExploded = false;
        _rigidbody.linearVelocity = Vector3.zero; // 이전 비행 속도 제거
        _rigidbody.angularVelocity = Vector3.zero; // 이전 회전 속도 제거
    }

    public void Throw(Vector3 force)
    {
        _rigidbody.AddForce(force, ForceMode.Impulse);
    }

    // 다른 콜라이더와 부딪힌 순간 실행되는 메서드
    private void OnCollisionEnter(Collision collision)
    {
        if (_isExploded) return;
        _isExploded = true;

        // Instantiate + Play → 풀에서 꺼내 재생 (끝나면 자동 반납)
        PoolManager.Instance.PlayEffect(_explosionEffectPrefab, transform.position, Quaternion.identity);

        // Destroy(gameObject) → 풀로 반납
        PoolManager.Instance.Release(gameObject);
    }

    private void ApplyAreaDamage()
    {
        // 주위에 있는 드럼통들을 모두 찾는다. 
        // Physics.OverlapSphere(원점, 반지름, 찾을 레이어들)
        Collider[] colliders =
            Physics.OverlapSphere(transform.position, _explosionRange, LayerMask.GetMask("Drum", "Zombie"));

        foreach (Collider col in colliders)
        {
            DamageInfo damageInfo = new()
            {
                Value = _damage,
                HitPoint = col.ClosestPoint(transform.position),
                HitNormal = (transform.position - col.ClosestPoint(transform.position)).normalized,
            };

            col.GetComponent<IDamageable>()?.TakeDamage(damageInfo);
        }
    }
}