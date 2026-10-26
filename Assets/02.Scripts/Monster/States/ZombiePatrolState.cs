using UnityEngine;

// 순찰(패트롤): 정해진 지점(웨이포인트)을 순서대로 돌아다닌다.
// 한 지점에 도착하면 잠깐 쉬고(Idle), 다음 지점으로 간다. 마지막 지점 다음은 다시 첫 지점이다.
public class ZombiePatrolState : IZombieState
{
    private const float ArriveDistance = 0.5f; // 이만큼 가까워지면 도착한 것으로 본다.
    private const float StuckLimit = 1f;       // 이 시간 동안 못 움직이면 다음 지점으로 넘어간다.

    private readonly Zombie _zombie;
    private int _waypointIndex = 0; // 지금 가고 있는 웨이포인트 번호 (쉬었다 와도 이어서 간다)
    private float _stuckTimer;

    public ZombiePatrolState(Zombie zombie)
    {
        _zombie = zombie;
    }

    public Vector3 Target => _zombie.Waypoints[_waypointIndex].position;

    public void Enter()
    {
        // Todo. Walk 애니메이션 실행
        _stuckTimer = 0f;
    }

    public void Execute()
    {
        // 순찰 중에도 플레이어가 가까워지면 쫓아간다.
        if (_zombie.IsPlayerDetected())
        {
            _zombie.ChangeState(ZombieState.Trace);
            return;
        }

        Vector3 direction = Target - _zombie.transform.position;
        direction.y = 0f; // 높이 차이는 무시한다.

        // 도착하면 다음 지점을 정해 두고 잠깐 쉰다.
        if (direction.magnitude < ArriveDistance)
        {
            GoToNextWaypoint();
            _zombie.ChangeState(ZombieState.Idle);
            return;
        }

        // 걸어간다. 어딘가에 끼어서 오래 못 움직이면 그 지점은 건너뛴다.
        bool isMoved = _zombie.Move(direction.normalized, _zombie.PatrolSpeed);
        if (isMoved)
        {
            _stuckTimer = 0f;
        }
        else
        {
            _stuckTimer += Time.deltaTime;
            if (_stuckTimer >= StuckLimit)
            {
                _stuckTimer = 0f;
                GoToNextWaypoint();
            }
        }
    }

    public void Exit()
    {
    }

    private void GoToNextWaypoint()
    {
        _waypointIndex = (_waypointIndex + 1) % _zombie.Waypoints.Length;
    }
}
