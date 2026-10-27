// 복귀: 처음 자리(스폰 위치)로 돌아간다.
public class ZombieComebackState : IZombieState
{
    private const float ArriveDistance = 1f;

    private readonly Zombie _zombie;

    public ZombieComebackState(Zombie zombie)
    {
        _zombie = zombie;
    }

    public void Enter()
    {
        _zombie.MoveTo(_zombie.StartPosition, _zombie.MoveSpeed);
    }

    public void Execute()
    {
        // 처음 자리에 도착하면 잠깐 쉬었다가 다시 돌아다닌다.
        if (_zombie.HasArrived(ArriveDistance))
        {
            _zombie.ChangeState(ZombieState.Idle);
        }
    }

    public void Exit()
    {
    }
}
