using System;
using UnityEngine;

// 타이틀 화면 연출용 좀비
// 정해진 방향으로 일정 거리를 달려간 뒤 사라진다. (사라지면 스포너가 출발점에서 다시 내보낸다)
// 게임용 Zombie와 달리 GameManager, 체력, 공격이 없다.
public class TitleZombie : MonoBehaviour
{
    private Animator _animator;
    private Vector3 _direction;
    private float _speed;
    private float _runDistance;
    private float _traveled;
    private bool _isRunning = false;

    public event Action<TitleZombie> OnFinished;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _animator.applyRootMotion = false; // 달리기 클립이 제자리(InPlace)라서 이동은 스크립트가 한다.
    }

    // startTraveled: 처음부터 길 중간에 있게 하고 싶을 때 이미 달린 거리
    public void Run(Vector3 position, Vector3 direction, float speed, float runDistance, float startTraveled)
    {
        _direction = direction.normalized;
        _speed = speed;
        _runDistance = runDistance;
        _traveled = startTraveled;
        _isRunning = true;

        transform.SetPositionAndRotation(position, Quaternion.LookRotation(_direction));
        gameObject.SetActive(true);

        // 여러 마리가 똑같이 움직이지 않도록 달리기 동작의 시작 시점과 속도를 조금씩 다르게 한다.
        _animator.Rebind();
        _animator.Play("Move", 0, UnityEngine.Random.value);
        _animator.speed = UnityEngine.Random.Range(0.9f, 1.1f);
    }

    private void Update()
    {
        if (!_isRunning) return;

        float step = _speed * Time.deltaTime;
        transform.position += _direction * step;
        _traveled += step;

        if (_traveled >= _runDistance)
        {
            _isRunning = false;
            gameObject.SetActive(false);
            OnFinished?.Invoke(this);
        }
    }
}
