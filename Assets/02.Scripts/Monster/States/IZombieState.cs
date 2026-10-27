// 좀비 상태 인터페이스
// 모든 상태는 "상태 진입", "상태에 머무는 동안 매 프레임", "상태 탈출" 세 시점이 있어야 한다.

public interface IZombieState
{
    void Enter();
    void Execute();
    void Exit();
}