using UnityEngine;

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
    }

    public void Execute()
    {
        Vector3 direction = _zombie.StartPosition - _zombie.transform.position;
        direction.y = 0f;

        // 처음 자리에 도착하면 잠깐 쉬었다가 다시 돌아다닌다.
        if (direction.magnitude < ArriveDistance)
        {
            _zombie.ChangeState(ZombieState.Idle);
            return;
        }

        _zombie.Move(direction.normalized, _zombie.MoveSpeed);
    }

    public void Exit()
    {
    }
}
