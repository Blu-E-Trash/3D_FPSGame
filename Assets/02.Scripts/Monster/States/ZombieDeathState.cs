using UnityEngine;

// 죽음: 쓰러지고 잠시 뒤 사라진다.
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
        _zombie.Controller.enabled = false; // 더 이상 부딪히지 않게 한다.
    }

    public void Execute()
    {
        _deathTimer += Time.deltaTime;
        if (_deathTimer >= DisappearDelay)
        {
            Object.Destroy(_zombie.gameObject);
        }
    }

    public void Exit()
    {
    }
}
