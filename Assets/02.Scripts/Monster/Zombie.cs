using System.Collections;
using UnityEngine;
using System;
using UnityEngine.AI;

// FSM(유한 상태 머신)
// 좀비가 가질 수 있는 상태
public enum ZombieState
{
    Idle,
    Trace,
    Comeback,
    Attack,
    Hit,
    Death,
}

[RequireComponent(typeof(NavMeshAgent))]
public class Zombie : MonoBehaviour, IDamageable
{
    [Header("스탯")]
    [SerializeField] private float _detectDistance = 8f;

    [SerializeField] private float _comebackDistance = 15f; // 처음 자리에서 이만큼 멀어지면 돌아간다.
    [SerializeField] private float _attackRange = 1.5f; // 공격 범위
    [SerializeField] private float _attackInterval = 1.2f; // 공격 간격
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _maxhealth = 30f;
    [SerializeField] private float _health = 30f;
    [SerializeField] private float _attackDamage = 10; // 공격력

    [Header("이펙트")]
    [SerializeField] private GameObject _bloodSprayPrefab;

    private float _attackTimer = 0f;
    private Vector3 _startPosition;
    private GameObject _player;
    private NavMeshAgent _agent;
    private Animator _animator;

    public float MaxHealth => _maxhealth;

    public float Health
    {
        get => _health;
        private set
        {
            _health = Mathf.Clamp(value, 0, _maxhealth);
            OnHealthChanged?.Invoke(_health, _maxhealth);
        }
    }

    public event Action<float, float> OnHealthChanged;
    public event Action<Zombie> OnDeath; // 스포너가 여러 좀비를 구독 -> 죽은 좀비 자신을 전달하도록 수정
    private ZombieState _state = ZombieState.Idle;

    private void Start()
    {
        _health = _maxhealth;

        _agent = GetComponent<NavMeshAgent>(); // 길을 찾아 걷게 해주는 컴포넌트를 찾아서 참조
        _agent.speed = _moveSpeed;

        _animator = GetComponentInChildren<Animator>();

        _startPosition = transform.position;
        _player = GameObject.FindGameObjectWithTag("Player");
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        switch (_state)
        {
            case ZombieState.Idle:
                {
                    Idle();
                    break;
                }
            case ZombieState.Trace:
                {
                    Trace();
                    break;
                }
            case ZombieState.Comeback:
                {
                    Comeback();
                    break;
                }
            case ZombieState.Attack:
                {
                    Attack();
                    break;
                }
        }
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_state == ZombieState.Death) return;

        Health -= damageInfo.Value;
        StopAllCoroutines();

        if (_health > 0f)
        {
            // Todo. 맞은 부위에 피 이펙트를 실행하고 싶다.
            GameObject bloodSpray = Instantiate(_bloodSprayPrefab, transform);
            bloodSpray.transform.position = damageInfo.HitPoint;
            bloodSpray.transform.forward = damageInfo.HitNormal;

            Debug.Log($"{_state} -> Hit");
            _state = ZombieState.Hit;
            StartCoroutine(Hit_Coroutine());
            Destroy(bloodSpray, 1f);
        }
        else
        {
            Debug.Log($"{_state} -> Death");
            _state = ZombieState.Death;
            OnDeath?.Invoke(this);
            StartCoroutine(Death_Coroutine());
        }
    }

    private IEnumerator Hit_Coroutine()
    {
        StopAgent();
        yield return new WaitForSeconds(0.3f); // 이래도 NavMesh는 계속 이동

        Debug.Log($"{_state} -> Hit");
        _state = ZombieState.Trace;
    }

    private IEnumerator Death_Coroutine()
    {
        StopAgent();
        yield return new WaitForSeconds(0.5f);
        Destroy(gameObject);
    }

    private void Idle()
    {
        StopAgent();

        // player가 가까워지면 Trace 상태로 전이
        float distance = Vector3.Distance(transform.position, _player.transform.position);
        if (distance <= _detectDistance)
        {
            Debug.Log($"{_state} -> Trace");
            _animator.SetTrigger("IdleToMove");
            _state = ZombieState.Trace;
        }
    }

    private void Trace()
    {
        // 처음 자리에서 너무 멀어지면 돌아간다.
        if (Vector3.Distance(transform.position, _startPosition) > _comebackDistance)
        {
            Debug.Log($"{_state} -> Comeback");
            _state = ZombieState.Comeback;
            return;
        }

        if (Vector3.Distance(transform.position, _player.transform.position) <= _attackRange)
        {
            _attackTimer = 0f;
            Debug.Log($"{_state} -> Attack");
            _animator.SetTrigger("MoveAttackDelay");
            _state = ZombieState.Attack;
            return;
        }

        // SetDestination은 목적지 설정
        // - 한 번 세팅하면 내부에서 계속해서 이동 (O)
        // - 호출할 때 마다 목적지를 향해 조금씩 이동 (X)
        _agent.SetDestination(_player.transform.position);
    }

    private void Comeback()
    {
        // if(_agent.pathStatus == NavMeshPathStatus.PathComplete)
        // if (_agent.remainingDistance < _agent.stoppingDistance)
        if (Vector3.Distance(transform.position, _startPosition) > _agent.stoppingDistance)
        {
            Debug.Log($"{_state} -> Idle");
            _animator.SetTrigger("MoveToIdle");
            _state = ZombieState.Idle;
            return;
        }

        _agent.SetDestination(_startPosition);
    }

    private void Attack()
    {
        StopAgent();

        // player가 멀어지면 Trace 상태로 전이
        float distance = Vector3.Distance(transform.position, _player.transform.position);
        if (distance > _attackRange)
        {
            Debug.Log($"{_state} -> Trace");
            _animator.SetTrigger("AttackMove");
            _state = ZombieState.Trace;
            return;
        }

        _attackTimer += Time.deltaTime;

        if (_attackTimer >= _attackInterval)
        {
            // 플레이어를 때린다.
            if (_player.TryGetComponent(out IDamageable damageable))
            {
                _animator.SetTrigger("StartAttack");
                DamageInfo damageInfo = new DamageInfo
                {
                    Value = _attackDamage,
                    HitPoint = _player.transform.position,
                    HitNormal = (transform.position - _player.transform.position).normalized, // 좀비 쪽
                };
                damageable.TakeDamage(damageInfo);
            }

            _attackTimer = 0f;
        }
    }

    private void StopAgent()
    {
        _agent.ResetPath(); // 에이전트의 목적지 삭제
        _agent.velocity = Vector3.zero; // 미끄럼 방지를 위해 가속도 제로!
    }
}