using UnityEngine;

// 피격: 맞으면 잠시 멈췄다가 추적으로 돌아간다.
// 기존의 Hit_Coroutine()을 상태 객체로 변경하였다.
public class ZombieHitState : IZombieState
{
    private readonly Zombie _zombie;
    private float _timer = 0f;

    public ZombieHitState(Zombie zombie)
    {
        _zombie = zombie;
    }

    public void Enter()
    {
        // 맞는 중에 다시 맞으면 ChangeState(Hit)이 다시 호출되어 타이머가 초기화된다.
        // 기존의 StopAllCoroutines() + 코루틴 재시작과 같은 효과
        _timer = 0f;
        _zombie.StopAgent();
        _zombie.Animator.SetTrigger("Hit");
    }

    public void Execute()
    {
        _timer += Time.deltaTime;
        if (_timer >= _zombie.HitDuration)
        {
            _zombie.ChangeState(ZombieState.Trace);
        }
    }

    public void Exit()
    {
    }
}