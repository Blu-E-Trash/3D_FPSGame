using UnityEngine;

// 피격: 공격(총알, 폭탄)을 받아 잠깐 멈칫한 뒤 플레이어를 쫓아간다.
public class ZombieHitState : IZombieState
{
    private const float HitDuration = 0.5f; // 멈칫하는 시간 (피격 애니메이션 길이에 맞춤)

    private readonly Zombie _zombie;
    private float _hitTimer;

    public ZombieHitState(Zombie zombie)
    {
        _zombie = zombie;
    }

    public void Enter()
    {
        _hitTimer = 0f; // 연달아 맞으면 처음부터 다시 멈칫한다.
        _zombie.StopAgent(); // 맞자마자 멈칫! (멈칫한 뒤에 멈추면 맞고도 조금 더 걸어온다)
    }

    public void Execute()
    {
        _hitTimer += Time.deltaTime;
        if (_hitTimer >= HitDuration)
        {
            // 맞았으니 쫓아온다.
            _zombie.ChangeState(ZombieState.Trace);
        }
    }

    public void Exit()
    {
    }
}
