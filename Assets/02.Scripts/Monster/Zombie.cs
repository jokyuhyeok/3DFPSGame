using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.AI;

// FSM(유한 상태 머신)
// 좀비가 가질 수 있는 상태
public enum ZombieState
{
    Idle,
    Patrol,
    Trace,
    Comeback,
    Attack,
    Hit,
    Death,
}

// 상태 패턴의 Context 역할
// _Zombie는 데이터, 공용 기능, 현재 상태 관리만 담당
[RequireComponent(typeof(NavMeshAgent))]
public class Zombie : MonoBehaviour, IDamageable
{
    [Header("스탯")]
    [SerializeField] private float _detectDistance = 8f;

    [SerializeField] private float _comebackDistance = 15f; // 추적을 시작한 자리에서 이만큼 멀어지면 돌아간다.
    [SerializeField] private float _attackRange = 1.5f; // 공격 범위
    [SerializeField] private float _attackInterval = 1.2f; // 공격 간격
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _maxhealth = 30f;
    [SerializeField] private float _health = 30f;
    [SerializeField] private float _attackDamage = 10; // 공격력
    [SerializeField] private float _attackRotateSpeed = 10f; // 공격 중 플레이어를 바라보게 몸통 회전 속도
    [SerializeField] private float _hitDuration = 0.5f;
    [SerializeField] private float _deathDestroyDelay = 2f;

    [Header("패트롤")]
    [SerializeField] private float _patrolSpeed = 1.5f;

    [SerializeField] private float _idleDuration = 3f;

    [Header("이펙트")]
    [SerializeField] private GameObject _bloodSprayPrefab;

    private Vector3 _returnPosition;
    private GameObject _player;
    private NavMeshAgent _agent;
    private Animator _animator;
    private Transform[] _patrolPoints;

    // 상태 머신
    private Dictionary<ZombieState, IZombieState> _states;
    private IZombieState _currentState;
    private ZombieState _currentStateType;

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

    // 상태 객체들이 읽어갈 읽기 전용 프로퍼티
    public ZombieState CurrentState => _currentStateType;
    public NavMeshAgent Agent => _agent;
    public Animator Animator => _animator;
    public GameObject Player => _player;
    public Vector3 ReturnPosition => _returnPosition;
    public float DetectDistance => _detectDistance;
    public float ComebackDistance => _comebackDistance;
    public float AttackRange => _attackRange;
    public float AttackInterval => _attackInterval;
    public float HitDuration => _hitDuration;
    public float DeathDestroyDelay => _deathDestroyDelay;
    public float MoveSpeed => _moveSpeed; // Patrol.Exit에서 속도 복구용
    public float PatrolSpeed => _patrolSpeed;
    public float IdleDuration => _idleDuration;
    public Transform[] PatrolPoints => _patrolPoints;
    public bool HasPatrolPoints => _patrolPoints != null && _patrolPoints.Length > 0;
    public float DistanceToPlayer => Vector3.Distance(transform.position, _player.transform.position);
    public float DistanceToReturn => Vector3.Distance(transform.position, _returnPosition);

    public event Action<float, float> OnHealthChanged;
    public event Action<Zombie> OnDeath;

    private void Start()
    {
        _health = _maxhealth;

        _agent = GetComponent<NavMeshAgent>(); // 길을 찾아 걷게 해주는 컴포넌트를 찾아서 참조
        _agent.speed = _moveSpeed;

        _animator = GetComponentInChildren<Animator>();

        _returnPosition = transform.position;
        _player = GameObject.FindGameObjectWithTag("Player");

        _states = new Dictionary<ZombieState, IZombieState>
        {
            { ZombieState.Idle, new ZombieIdleState(this) },
            { ZombieState.Patrol, new ZombiePatrolState(this) },
            { ZombieState.Trace, new ZombieTraceState(this) },
            { ZombieState.Comeback, new ZombieComebackState(this) },
            { ZombieState.Attack, new ZombieAttackState(this) },
            { ZombieState.Hit, new ZombieHitState(this) },
            { ZombieState.Death, new ZombieDeathState(this) },
        };

        ChangeState(ZombieState.Idle);
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        _currentState?.Execute();
    }

    public void ChangeState(ZombieState nextState)
    {
        Debug.Log($"{_currentStateType} -> {nextState}");

        _currentState?.Exit();
        _currentStateType = nextState;
        _currentState = _states[nextState];
        _currentState.Enter();
    }

    // 스포너가 생성 직후 순찰 경로를 넘겨준다.
    public void SetPatrolPoints(Transform[] patrolPoints)
    {
        _patrolPoints = patrolPoints;
    }

    // 현재 위치를 복귀 위치로 기록
    public void SetReturnPosition()
    {
        _returnPosition = transform.position;
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_currentStateType == ZombieState.Death) return;

        Health -= damageInfo.Value;

        if (_health > 0f)
        {
            GameObject bloodSpray = Instantiate(_bloodSprayPrefab, transform);
            bloodSpray.transform.position = damageInfo.HitPoint;
            bloodSpray.transform.forward = damageInfo.HitNormal;
            Destroy(bloodSpray, 1f);

            ChangeState(ZombieState.Hit);
        }
        else
        {
            ChangeState(ZombieState.Death);
            OnDeath?.Invoke(this);
        }
    }

    public void LookAtPlayer()
    {
        Vector3 direction = _player.transform.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f) return; // 같은 위치면 방향을 구할 수 없음

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _attackRotateSpeed * Time.deltaTime);
    }

    // 애니메이션 이벤트(MonsterHitEvent.HitEvent)에서 호출된다.
    public void AttackAction()
    {
        if (_currentStateType != ZombieState.Attack) return;
        if (DistanceToPlayer > _attackRange) return;

        // 플레이어를 때린다.
        if (_player.TryGetComponent(out IDamageable damageable))
        {
            DamageInfo damageInfo = new DamageInfo
            {
                Value = _attackDamage,
                HitPoint = _player.transform.position,
                HitNormal = (transform.position - _player.transform.position).normalized, // 좀비 쪽
            };
            damageable.TakeDamage(damageInfo);
        }
    }

    public void StopAgent()
    {
        _agent.ResetPath(); // 에이전트의 목적지 삭제
        _agent.velocity = Vector3.zero; // 미끄럼 방지를 위해 가속도 제로!
    }
}