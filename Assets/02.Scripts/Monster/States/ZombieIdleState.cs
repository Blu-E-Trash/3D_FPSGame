using UnityEngine;

// 대기: 제자리에서 잠깐 쉬면서 플레이어를 기다린다.
// 다 쉬면 웨이포인트가 있으면 Patrol, 없으면 Roaming으로 간다.
public class ZombieIdleState : IZombieState
{
    private readonly Zombie _zombie;
    private float _idleTimer;

    public ZombieIdleState(Zombie zombie)
    {
        _zombie = zombie;
    }

    public void Enter()
    {
        // Todo. Idle 애니메이션 실행
        _idleTimer = 0f;
    }

    public void Execute()
    {
        // 플레이어가 가까워지면 쫓아간다.
        if (_zombie.IsPlayerDetected())
        {
            _zombie.ChangeState(ZombieState.Trace);
            return;
        }

        // 충분히 쉬었으면 다시 돌아다닌다.
        _idleTimer += Time.deltaTime;
        if (_idleTimer >= _zombie.IdleDuration)
        {
            _zombie.ChangeState(_zombie.HasWaypoints ? ZombieState.Patrol : ZombieState.Roaming);
        }
    }

    public void Exit()
    {
    }
}
