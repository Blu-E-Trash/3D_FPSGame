// 좀비 상태 하나가 가져야 하는 기능 (상태 패턴)
// 상태마다 클래스를 하나씩 만들고, Zombie는 "지금 상태"에게 할 일을 맡긴다.
public interface IZombieState
{
    // 이 상태로 들어올 때 한 번 호출된다. (타이머 초기화 등)
    public void Enter();

    // 이 상태인 동안 매 프레임 호출된다.
    public void Execute();

    // 이 상태에서 나갈 때 한 번 호출된다. (코루틴 정리 등)
    public void Exit();
}
