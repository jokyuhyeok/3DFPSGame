using UnityEngine;

// 순찰: 웨이포인트를 향해 걸어가고, 도착하면 대기한다.
public class ZombiePatrolState : IZombieState
{
    private readonly Zombie _zombie;
    private int _pointIndex = 0;

    public ZombiePatrolState(Zombie zombie)
    {
        _zombie = zombie;
    }

    public void Enter()
    {
        _zombie.Agent.speed = _zombie.PatrolSpeed;

        Transform target = _zombie.PatrolPoints[_pointIndex];

        if (target != null)
        {
            _zombie.Agent.SetDestination(target.position);
        }
    }

    public void Execute()
    {
        if (_zombie.DistanceToPlayer < _zombie.DetectDistance)
        {
            _zombie.Animator.SetTrigger("PatrolToMove");
            _zombie.ChangeState(ZombieState.Trace);
            return;
        }

        // 웨이포인트 도착 판정
        // pathPending: 경로 계산이 아직 안 끝났으면 remainingDistance가 0으로 나올 수 있어 먼저 확인한다.
        if (!_zombie.Agent.pathPending && _zombie.Agent.remainingDistance <= _zombie.Agent.stoppingDistance)
        {
            _pointIndex = (_pointIndex + 1) % _zombie.PatrolPoints.Length;
            _zombie.Animator.SetTrigger("PatrolToIdle");
            _zombie.ChangeState(ZombieState.Idle);
        }
    }

    public void Exit()
    {
        _zombie.Agent.speed = _zombie.MoveSpeed;
        _zombie.SetReturnPosition();
    }
}