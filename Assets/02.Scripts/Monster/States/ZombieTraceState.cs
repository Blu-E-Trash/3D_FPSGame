using UnityEngine;

// 추격: 플레이어를 쫓아간다.
public class ZombieTraceState : IZombieState
{
    private readonly Zombie _zombie;

    public ZombieTraceState(Zombie zombie)
    {
        _zombie = zombie;
    }

    public void Enter()
    {
        // Todo. Run 애니메이션 실행
    }

    public void Execute()
    {
        // 처음 자리에서 너무 멀어지면 돌아간다.
        if (Vector3.Distance(_zombie.transform.position, _zombie.StartPosition) > _zombie.ComebackDistance)
        {
            _zombie.ChangeState(ZombieState.Comeback);
            return;
        }

        // 플레이어가 공격 거리 안이면 공격한다.
        if (_zombie.DistanceToPlayer() <= _zombie.AttackRange)
        {
            _zombie.ChangeState(ZombieState.Attack);
            return;
        }

        Vector3 direction = _zombie.Player.transform.position - _zombie.transform.position;
        direction.y = 0f;
        _zombie.Move(direction.normalized, _zombie.MoveSpeed);
    }

    public void Exit()
    {
    }
}
