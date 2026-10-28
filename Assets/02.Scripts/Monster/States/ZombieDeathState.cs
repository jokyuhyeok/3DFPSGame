using UnityEngine;

// 사망: 사망 애니메이션 이후 일정 시간 뒤 제거된다.
public class ZombieDeathState : IZombieState
{
    private readonly Zombie _zombie;

    public ZombieDeathState(Zombie zombie)
    {
        _zombie = zombie;
    }

    public void Enter()
    {
        _zombie.Animator.ResetTrigger("Hit");
        _zombie.Animator.SetTrigger("Death");
        _zombie.StopAgent();

        PoolManager.Instance.Release(_zombie.gameObject, _zombie.DeathDestroyDelay);
    }

    public void Execute()
    {
        // 죽은 뒤에는 할 일 없음.
    }

    public void Exit()
    {
    }
}