using System.Collections;
using UnityEngine;

// 로밍: 처음 자리 주변을 무작위로 돌아다닌다.
// 벽을 만나면 확률에 따라 두리번거리거나, 벽 반대쪽으로 방향을 바꾼다.
public class ZombieRoamingState : IZombieState
{
    private const float ArriveDistance = 0.5f;
    private const float StuckLimit = 0.5f;

    private readonly Zombie _zombie;
    private Vector3 _target;                // 지금 걸어가고 있는 목적지
    private float _stuckTimer;              // 제자리에 끼어 있던 시간
    private bool _isLookingAround;          // 벽 앞에서 두리번거리는 중인지
    private Coroutine _lookAroundCoroutine; // 두리번거리기 코루틴 (상태가 바뀌면 멈추려고 기억해 둔다)

    public ZombieRoamingState(Zombie zombie)
    {
        _zombie = zombie;
    }

    public Vector3 Target => _target;

    public void Enter()
    {
        // Todo. Walk 애니메이션 실행
        _stuckTimer = 0f;
        PickTarget(Vector3.zero); // 아무 방향으로나 새 목적지를 정한다.
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

        Vector3 direction = _target - _zombie.transform.position;
        direction.y = 0f;

        // 목적지에 도착하면 잠깐 쉰다. 쉬고 나면 또 새 목적지를 정한다.
        if (direction.magnitude < ArriveDistance)
        {
            _zombie.ChangeState(ZombieState.Idle);
            return;
        }

        direction.Normalize();

        // 1. 앞에 벽이 있으면 멈추고 어떻게 할지 정한다.
        if (_zombie.FindWall(direction, _zombie.WallCheckDistance, out Vector3 wallNormal))
        {
            OnMeetWall(wallNormal);
            return;
        }

        // 2. 걸어간다. 레이에 안 걸리는 낮은 턱이나 모서리에 끼어 있으면 벽을 만난 것으로 친다.
        bool isMoved = _zombie.Move(direction, _zombie.PatrolSpeed);
        if (isMoved)
        {
            _stuckTimer = 0f;
        }
        else
        {
            _stuckTimer += Time.deltaTime;
            if (_stuckTimer >= StuckLimit)
            {
                OnMeetWall(-direction); // 막힌 반대쪽(뒤쪽)을 벽의 바깥 방향으로 본다.
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

    // 벽을 만났을 때: 확률에 따라 두리번거리거나, 바로 다른 방향으로 간다.
    private void OnMeetWall(Vector3 wallNormal)
    {
        _stuckTimer = 0f;

        if (Random.value < _zombie.LookAroundChance)
        {
            _lookAroundCoroutine = _zombie.StartCoroutine(LookAround_Coroutine(wallNormal));
        }
        else
        {
            PickTarget(wallNormal);
        }
    }

    // 두리번거리기: 왼쪽 보고, 오른쪽 보고, 벽 반대쪽으로 새 목적지를 정한다.
    private IEnumerator LookAround_Coroutine(Vector3 wallNormal)
    {
        _isLookingAround = true;

        // Todo. 두리번거리는 애니메이션 실행

        float baseAngle = _zombie.transform.eulerAngles.y;

        yield return RotateTo_Coroutine(baseAngle - _zombie.LookAroundAngle); // 왼쪽을 보고
        yield return new WaitForSeconds(0.4f);

        yield return RotateTo_Coroutine(baseAngle + _zombie.LookAroundAngle); // 오른쪽을 보고
        yield return new WaitForSeconds(0.4f);

        // 다 둘러봤으니 벽 반대쪽으로 간다.
        PickTarget(wallNormal);
        _isLookingAround = false;
        _lookAroundCoroutine = null;
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

    // 새 목적지를 정한다.
    // - preferDirection이 zero면 아무 방향이나 고른다.
    // - 아니면 그 방향 기준 좌우 80도 안에서 고른다. (벽을 만났을 때 벽 반대쪽으로 가려고)
    private void PickTarget(Vector3 preferDirection)
    {
        preferDirection.y = 0f;
        Vector3 position = _zombie.transform.position;

        // 조건에 맞는 목적지가 나올 때까지 최대 10번 뽑아 본다.
        for (int i = 0; i < 10; i++)
        {
            Vector3 direction;
            if (preferDirection == Vector3.zero)
            {
                direction = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward;
            }
            else
            {
                direction = Quaternion.Euler(0f, Random.Range(-80f, 80f), 0f) * preferDirection.normalized;
            }

            float distance = Random.Range(_zombie.MinRoamingDistance, _zombie.RoamingRadius);
            Vector3 candidate = position + direction * distance;

            // 조건 1. 처음 자리에서 로밍 반경 밖이면 안 된다.
            Vector3 fromStart = candidate - _zombie.StartPosition;
            fromStart.y = 0f;
            if (fromStart.magnitude > _zombie.RoamingRadius)
            {
                continue;
            }

            // 조건 2. 가는 길에 벽이 있으면 안 된다.
            if (_zombie.FindWall(direction, distance, out _))
            {
                continue;
            }

            _target = candidate;
            return;
        }

        // 10번 다 실패하면(구석에 몰렸으면) 처음 자리로 돌아간다.
        _target = _zombie.StartPosition;
    }
}
