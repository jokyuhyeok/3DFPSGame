using UnityEngine;

// 추적: 플레이어를 쫓아간다.
// 시작 위치에서 너무 멀어지면 복귀, 공격 범위에 들어오면 공격한다.
public class ZombieTraceState : IZombieState
{
    private readonly Zombie _zombie;

    public ZombieTraceState(Zombie zombie)
    {
        _zombie = zombie;
    }

    public void Enter()
    {
    }

    public void Execute()
    {
        if (_zombie.DistanceToStart > _zombie.ComebackDistance)
        {
            _zombie.ChangeState(ZombieState.Comeback);
            return;
        }

        if (_zombie.DistanceToPlayer <= _zombie.AttackRange)
        {
            _zombie.Animator.SetTrigger("MoveToAttackDelay");
            _zombie.ChangeState(ZombieState.Attack);
            return;
        }

        // SetDestination은 목적지 설정 (한 번 세팅하면 내부에서 계속 이동)
        _zombie.Agent.SetDestination(_zombie.Player.transform.position);
    }

    public void Exit()
    {
    }
}