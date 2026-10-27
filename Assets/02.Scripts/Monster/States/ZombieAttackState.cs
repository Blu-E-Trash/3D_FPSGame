using UnityEngine;

// 공격: 일정 시간마다 플레이어를 때린다.
public class ZombieAttackState : IZombieState
{
    private readonly Zombie _zombie;
    private float _attackTimer;

    public ZombieAttackState(Zombie zombie)
    {
        _zombie = zombie;
    }

    public void Enter()
    {
        _attackTimer = 0f;
        _zombie.StopAgent(); // 쫓아오던 속도로 미끄러지지 않게 멈춰서 때린다.
    }

    public void Execute()
    {
        // 플레이어가 공격 거리 밖으로 나가면 쫓아간다.
        if (_zombie.DistanceToPlayer() > _zombie.AttackRange)
        {
            _zombie.ChangeState(ZombieState.Trace);
            return;
        }

        _attackTimer += Time.deltaTime;
        if (_attackTimer >= _zombie.AttackInterval)
        {
            _attackTimer = 0f;
            _zombie.StartAttack();
        }
    }

    public void Exit()
    {
    }
}
