using UnityEngine;

// 공격 : 플레이어를 바라보며 일정 간격으로 공격 애니메이션을 실행한다.
// 데미지는 애니메이션 이벤트에서 들어간다. -> Zombie.AttackAction()
public class ZombieAttackState : IZombieState
{
    private readonly Zombie _zombie;
    private float _attackTimer = 0f;

    public ZombieAttackState(Zombie zombie)
    {
        _zombie = zombie;
    }

    public void Enter()
    {
        _attackTimer = 0f;
        _zombie.StopAgent();
    }

    public void Execute()
    {
        // 플레이어가 멀어지면 추적
        if (_zombie.DistanceToPlayer > _zombie.AttackRange)
        {
            _zombie.Animator.ResetTrigger("StartAttack"); // 아직 실행되지 않은 공격은 취소한다.
            _zombie.Animator.SetTrigger("AttackMove");
            _zombie.ChangeState(ZombieState.Trace);
            return;
        }

        _zombie.LookAtPlayer();

        _attackTimer += Time.deltaTime;
        if (_attackTimer >= _zombie.AttackInterval)
        {
            _zombie.Animator.SetTrigger("StartAttack");
            _attackTimer = 0f;
        }
    }

    public void Exit()
    {
    }
}