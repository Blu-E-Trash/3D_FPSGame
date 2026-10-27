using UnityEngine;

// 죽음: 쓰러지고 잠시 뒤 사라진다. (풀로 돌아간다)
public class ZombieDeathState : IZombieState
{
    private const float DisappearDelay = 2f; // 쓰러지는 애니메이션을 보여 줄 시간

    private readonly Zombie _zombie;
    private float _deathTimer;

    public ZombieDeathState(Zombie zombie)
    {
        _zombie = zombie;
    }

    public void Enter()
    {
        _deathTimer = 0f;
        _zombie.StopAgent(); // 죽은 자리에 멈춘다.
    }

    public void Execute()
    {
        _deathTimer += Time.deltaTime;
        if (_deathTimer >= DisappearDelay)
        {
            _zombie.Despawn();
        }
    }

    public void Exit()
    {
    }
}
