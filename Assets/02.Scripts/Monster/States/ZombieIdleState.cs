using UnityEngine;

// 대기 : 제자리에 서 있다가 플레이어가 감지되면 추적한다.
public class ZombieIdleState : IZombieState
{
    private readonly Zombie _zombie;
    private float _timer = 0f;

    public ZombieIdleState(Zombie zombie)
    {
        _zombie = zombie;
    }

    public void Enter()
    {
        _zombie.StopAgent(); // 들어올 때 1번 실행
        _timer = 0f;
    }

    public void Execute()
    {
        if (_zombie.DistanceToPlayer < _zombie.DetectDistance)
        {
            _zombie.Animator.SetTrigger("IdleToMove");
            _zombie.ChangeState(ZombieState.Trace);
            return;
        }

        _timer += Time.deltaTime;
        if (_zombie.HasPatrolPoints && _timer >= _zombie.IdleDuration)
        {
            _zombie.Animator.SetTrigger("IdleToPatrol");
            _zombie.ChangeState(ZombieState.Patrol);
        }
    }

    public void Exit()
    {
        _zombie.SetReturnPosition();
    }
}