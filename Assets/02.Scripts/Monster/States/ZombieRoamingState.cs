using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// 로밍: 처음 자리 주변을 무작위로 돌아다닌다.
// 목적지는 NavMesh 위(걸을 수 있는 곳)에서만 고르고, 길은 NavMeshAgent가 찾는다.
// 목적지에 도착하면 확률에 따라 두리번거린 뒤 잠깐 쉰다.
public class ZombieRoamingState : IZombieState
{
    private const float ArriveDistance = 0.5f;
    private const float SampleDistance = 2f; // 뽑은 점 근처 이 거리 안에서 NavMesh 위의 점을 찾는다.

    private readonly Zombie _zombie;
    private Vector3 _target;                // 지금 걸어가고 있는 목적지
    private bool _isLookingAround;          // 두리번거리는 중인지
    private Coroutine _lookAroundCoroutine; // 두리번거리기 코루틴 (상태가 바뀌면 멈추려고 기억해 둔다)

    public ZombieRoamingState(Zombie zombie)
    {
        _zombie = zombie;
    }

    public Vector3 Target => _target;

    public void Enter()
    {
        // Todo. Walk 애니메이션 실행
        _target = PickTarget();
        _zombie.MoveTo(_target, _zombie.PatrolSpeed);
    }

    public void Execute()
    {
        // 돌아다니는 중에도 플레이어가 가까워지면 쫓아간다. (두리번거리는 중이어도!)
        if (_zombie.IsPlayerDetected())
        {
            _zombie.ChangeState(ZombieState.Trace);
            return;
        }

        // 두리번거리는 중이면 코루틴이 알아서 하니까 기다린다.
        if (_isLookingAround)
        {
            return;
        }

        // 목적지에 도착하면 두리번거리거나 바로 쉰다.
        if (_zombie.HasArrived(ArriveDistance))
        {
            if (Random.value < _zombie.LookAroundChance)
            {
                _lookAroundCoroutine = _zombie.StartCoroutine(LookAround_Coroutine());
            }
            else
            {
                _zombie.ChangeState(ZombieState.Idle);
            }
        }
    }

    public void Exit()
    {
        // 두리번거리던 중에 상태가 바뀌면(플레이어 발견, 피격) 두리번거리기를 멈춘다.
        if (_lookAroundCoroutine != null)
        {
            _zombie.StopCoroutine(_lookAroundCoroutine);
            _lookAroundCoroutine = null;
        }
        _isLookingAround = false;
    }

    // 두리번거리기: 왼쪽 보고, 오른쪽 보고, 잠깐 쉰다.
    private IEnumerator LookAround_Coroutine()
    {
        _isLookingAround = true;

        // Todo. 두리번거리는 애니메이션 실행

        float baseAngle = _zombie.transform.eulerAngles.y;

        yield return RotateTo_Coroutine(baseAngle - _zombie.LookAroundAngle); // 왼쪽을 보고
        yield return new WaitForSeconds(0.4f);

        yield return RotateTo_Coroutine(baseAngle + _zombie.LookAroundAngle); // 오른쪽을 보고
        yield return new WaitForSeconds(0.4f);

        _isLookingAround = false;
        _lookAroundCoroutine = null;
        _zombie.ChangeState(ZombieState.Idle);
    }

    // 정해진 각도(y축)까지 천천히 몸을 돌린다.
    private IEnumerator RotateTo_Coroutine(float targetAngle)
    {
        Transform transform = _zombie.transform;
        Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
        while (Quaternion.Angle(transform.rotation, targetRotation) > 1f)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _zombie.TurnSpeed * Time.deltaTime);
            yield return null;
        }
        transform.rotation = targetRotation;
    }

    // 처음 자리 주변(로밍 반경 안)에서 걸어갈 수 있는 목적지를 고른다.
    private Vector3 PickTarget()
    {
        Vector3 position = _zombie.transform.position;

        // 조건에 맞는 목적지가 나올 때까지 최대 10번 뽑아 본다.
        for (int i = 0; i < 10; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * _zombie.RoamingRadius;
            Vector3 candidate = _zombie.StartPosition + new Vector3(randomCircle.x, 0f, randomCircle.y);

            // 조건 1. 너무 가까우면 찔끔 움직이니까 다시 뽑는다.
            Vector3 offset = candidate - position;
            offset.y = 0f;
            if (offset.magnitude < _zombie.MinRoamingDistance)
            {
                continue;
            }

            // 조건 2. NavMesh 위(걸을 수 있는 곳)여야 한다. 근처의 NavMesh 위 점으로 맞춰 준다.
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, SampleDistance, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }

        // 10번 다 실패하면 처음 자리로 돌아간다.
        return _zombie.StartPosition;
    }
}
