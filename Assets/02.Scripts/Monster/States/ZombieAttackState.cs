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
            // Todo. 공격 애니메이션 실행
            _attackTimer = 0f;
            _zombie.AttackPlayer();
        }
    }

    public void Exit()
    {
    }
}
