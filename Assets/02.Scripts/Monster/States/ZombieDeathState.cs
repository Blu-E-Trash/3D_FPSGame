using UnityEngine;

// 죽음: 쓰러지고 잠시 뒤 사라진다. (풀로 돌아간다)
public class ZombieDeathState : IZombieState
{
    private const float DisappearDelay = 0.5f;

    private readonly Zombie _zombie;
    private float _deathTimer;

    public ZombieDeathState(Zombie zombie)
    {
        _zombie = zombie;
    }

    public void Enter()
    {
        // Todo. Death 애니메이션 실행
        _deathTimer = 0f;
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
