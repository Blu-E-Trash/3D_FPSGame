using UnityEngine;

// 순찰(패트롤): 정해진 지점(웨이포인트)을 순서대로 돌아다닌다.
// 한 지점에 도착하면 잠깐 쉬고(Idle), 다음 지점으로 간다. 마지막 지점 다음은 다시 첫 지점이다.
// 길은 NavMeshAgent가 찾으니까, 지점 사이에 벽이 있어도 돌아서 간다.
public class ZombiePatrolState : IZombieState
{

    private readonly Zombie _zombie;
    private int _waypointIndex = 0; // 지금 가고 있는 웨이포인트 번호 (쉬었다 와도 이어서 간다)

    public ZombiePatrolState(Zombie zombie)
    {
        _zombie = zombie;
    }

    public Vector3 Target => _zombie.Waypoints[_waypointIndex].position;

    public void Enter()
    {
        _zombie.MoveTo(Target, _zombie.PatrolSpeed);
    }

    public void Execute()
    {
        // 순찰 중에도 플레이어가 가까워지면 쫓아간다.
        if (_zombie.IsPlayerDetected())
        {
            _zombie.ChangeState(ZombieState.Trace);
            return;
        }

        // 도착하면 다음 지점을 정해 두고 잠깐 쉰다.
        if (_zombie.HasArrived())
        {
            GoToNextWaypoint();
            _zombie.ChangeState(ZombieState.Idle);
        }
    }

    public void Exit()
    {
    }

    // 첫 번째 지점부터 다시 돈다. (웨이포인트가 바뀌었을 때)
    public void ResetWaypointIndex()
    {
        _waypointIndex = 0;
    }

    private void GoToNextWaypoint()
    {
        _waypointIndex = (_waypointIndex + 1) % _zombie.Waypoints.Length;
    }
}
