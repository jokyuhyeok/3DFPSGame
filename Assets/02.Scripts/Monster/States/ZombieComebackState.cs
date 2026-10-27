using UnityEngine;

// 복귀: 시작 위치로 돌아가고, 도착하면 대기한다.
public class ZombieComebackState : IZombieState
{
    private readonly Zombie _zombie;

    public ZombieComebackState(Zombie zombie)
    {
        _zombie = zombie;
    }

    public void Enter()
    {
        _zombie.Agent.SetDestination(_zombie.ReturnPosition);
    }

    public void Execute()
    {
        if (_zombie.DistanceToReturn < _zombie.Agent.stoppingDistance)
        {
            _zombie.Animator.SetTrigger("MoveToIdle");
            _zombie.ChangeState(ZombieState.Idle);
        }
    }

    public void Exit()
    {
    }
}