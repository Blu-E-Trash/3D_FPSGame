using System.Collections;
using UnityEngine;

// FSM(유한 상태 머신) : 유한개의 상태가 있고, 상태마다 행동을 다르게 함으로써 AI를 구현하는 유명한 방법
// - "상태": 객체가 가질 수 있는 어떤 조건이나 상황
// - "전이": 조건에 따라 현재 상태에서 다른 상태로 넘어가는 것
// - 그리고 위 방식에 따라 규칙을 세워둔 것을 "머신"


// 좀비가 가질 수 있는 상태
public enum ZombieState
{
    Idle,       // 대기: 제자리에서 잠깐 쉬면서 플레이어를 기다린다.
    Patrol,     // 순찰: 처음 자리 주변을 무작위로 돌아다닌다. 벽을 만나면 두리번거리거나 방향을 바꾼다.
    Trace,      // 추격: 플레이어를 쫓아간다.
    Comeback,    // 복귀: 처음 자리로 돌아간다.
    Attack,     // 공격: 일정 시간마다 플레이어를 때린다.
    Hit,        // 피격: 공격(총알, 폭탄)을 받아 잠깐 멈칫한다.
    Death,      // 죽음: 쓰러지고 잠시 뒤 사라진다.
}

public class Zombie : MonoBehaviour, IDamageable
{
    [Header("스탯")]
    [SerializeField] private float _detectDistance = 8;    // 플레이어를 발견하는 거리
    [SerializeField] private float _combackDistance = 15;   // 처음 자리에서 이만큼 멀어지면 돌아가는 거리
    [SerializeField] private float _attackRange = 1.5f;     // 공격 범위
    [SerializeField] private float _attackInterval = 1.2f;  // 공격 간격
    [SerializeField] private float _attackDamage = 10f;     // 한 번 공격할 때 주는 대미지
    [SerializeField] private float _moveSpeed = 5;          // 이동속도
    [SerializeField] private float _health = 30;

    [Header("이펙트")]
    [SerializeField] private GameObject _bloodSprayPrefab;

    [Header("순찰")]
    [SerializeField] private float _patrolRadius = 6f;      // 처음 자리에서 이 반경 안에서만 돌아다닌다.
    [SerializeField] private float _minPatrolDistance = 2f; // 한 번에 최소 이만큼은 걸어간다. (너무 짧게 찔끔 움직이지 않게)
    [SerializeField] private float _patrolSpeed = 2f;       // 순찰할 때 걷는 속도 (추격보다 느리게)
    [SerializeField] private float _idleDuration = 2f;      // 목적지에 도착해서 쉬는 시간

    [Header("벽 감지")]
    [SerializeField] private LayerMask _obstacleLayer = ~0;        // 벽으로 볼 레이어 (기본: 전부)
    [SerializeField] private float _wallCheckDistance = 1f;         // 앞으로 이만큼 안에 벽이 있으면 멈춘다.
    [SerializeField, Range(0f, 1f)] private float _lookAroundChance = 0.5f; // 벽을 만났을 때 두리번거릴 확률 (나머지는 바로 방향 전환)
    [SerializeField] private float _lookAroundAngle = 70f;          // 두리번거릴 때 좌우로 고개를 돌리는 각도
    [SerializeField] private float _turnSpeed = 180f;               // 몸을 돌리는 속도 (초당 각도)


    private float _attackTimer = 0f;
    private CharacterController _controller;
    private Vector3 _startPosition;
    private GameObject _player;

    private Vector3 _patrolTarget;          // 지금 걸어가고 있는 순찰 목적지
    private float _idleTimer = 0f;          // 대기한 시간
    private bool _isLookingAround = false;  // 벽 앞에서 두리번거리는 중인지
    private Coroutine _lookAroundCoroutine; // 두리번거리기 코루틴 (플레이어를 발견하면 멈추려고 기억해 둔다)
    private float _stuckTimer = 0f;         // 제자리에 끼어 있던 시간


    private ZombieState _state = ZombieState.Idle;

    private float _maxHealth; // 처음 체력 (체력바 비율 계산용)

    public float HealthRatio => _health / _maxHealth;      // 0 ~ 1, 체력바에 쓴다.
    public bool IsDead => _state == ZombieState.Death;

    private void Awake()
    {
        _maxHealth = _health;
    }

    private void Start()
    {
        _controller = GetComponent<CharacterController>();
        _startPosition = transform.position;

        // 게임을 시작하면 플레이어를 찾는다.
        _player = GameObject.FindGameObjectWithTag("Player");
    }


    private void Update()
    {
        // 게임 중이 아니면(준비, 게임오버) 아무것도 하지 않는다.
        if (GameManager.Instance.State != GameState.Playing) return;

        switch (_state)
        {
            case ZombieState.Idle:
                Idle();
                break;

            case ZombieState.Patrol:
                Patrol();
                break;

            case ZombieState.Trace:
                Trace();
                break;

            case ZombieState.Comeback:
                Comeback();
                break;

            case ZombieState.Attack:
                Attack();
                break;
        }
    }

    // 대미지를 받는다.
    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_state == ZombieState.Death)
        {
            return;
        }

        _health -= damageInfo.Value;

        StopAllCoroutines();
        _isLookingAround = false; // 두리번거리던 중에 맞았을 수도 있으니 정리한다.

        if (_health > 0f)
        {
            // 맞은 부위에 피 스프레이를 실행한다.
            // 좀비의 자식으로 생성해서, 좀비가 움직여도 맞은 자리에 붙어서 같이 움직이게 한다.
            if (_bloodSprayPrefab != null)
            {
                GameObject bloodSpray = Instantiate(_bloodSprayPrefab, transform);
                bloodSpray.transform.position = damageInfo.HitPoint;
                if (damageInfo.HitNormal != Vector3.zero)
                {
                    bloodSpray.transform.forward = damageInfo.HitNormal;
                }
            }

            // 잠깐 멈칫 하고 싶다!
            Debug.Log($"{_state} -> Hit");
            _state = ZombieState.Hit;
            StartCoroutine(Hit_Coroutine());
        }
        else
        {
            Debug.Log($"{_state} -> Death");
            _state = ZombieState.Death;
            StartCoroutine(Death_Coroutine());
        }

    }

    private IEnumerator Hit_Coroutine()
    {
        yield return new WaitForSeconds(0.3f); // 잠깐 멈칫한다!

        // 맞았으니 쫒아온다.
        Debug.Log($"{_state} -> Trace");
        _state = ZombieState.Trace;
    }

    private IEnumerator Death_Coroutine()
    {
        _controller.enabled = false;
        yield return new WaitForSeconds(0.5f);
        Destroy(gameObject);
    }

    // 대기: 제자리에서 잠깐 쉬면서 플레이어를 기다린다.
    private void Idle()
    {
        // Todo. Idle 애니메이션 실행

        // 플레이어 가까워지면 Trace 상태로 전이한다.
        if (IsPlayerDetected())
        {
            Debug.Log($"{_state} -> Trace");
            _state = ZombieState.Trace;
            return;
        }

        // 충분히 쉬었으면 Patrol 상태로 전이한다.
        _idleTimer += Time.deltaTime;
        if (_idleTimer >= _idleDuration)
        {
            PickPatrolTarget(Vector3.zero); // 아무 방향으로나 새 목적지를 정한다.
            Debug.Log($"{_state} -> Patrol");
            _state = ZombieState.Patrol;
        }
    }

    // 순찰: 처음 자리 주변을 무작위로 돌아다닌다.
    private void Patrol()
    {
        // 순찰 중에도 플레이어가 가까워지면 Trace 상태로 전이한다. (두리번거리는 중이어도!)
        if (IsPlayerDetected())
        {
            StopLookAround();
            Debug.Log($"{_state} -> Trace");
            _state = ZombieState.Trace;
            return;
        }

        // 두리번거리는 중이면 코루틴이 알아서 하니까 기다린다.
        if (_isLookingAround)
        {
            return;
        }

        // Todo. Walk 애니메이션 실행

        Vector3 direction = _patrolTarget - transform.position;
        direction.y = 0f; // 높이 차이는 무시한다.

        // 목적지에 도착하면 잠깐 쉰다(Idle). 쉬고 나면 또 새 목적지를 정한다.
        if (direction.magnitude < 0.5f)
        {
            ChangeToIdle();
            return;
        }

        direction.Normalize();

        // 1. 앞에 벽이 있으면 멈추고 어떻게 할지 정한다.
        if (IsWallAhead(direction, out Vector3 wallNormal))
        {
            OnMeetWall(wallNormal);
            return;
        }

        // 2. 걸어간다.
        transform.forward = direction;
        Vector3 before = transform.position;
        _controller.SimpleMove(direction * _patrolSpeed);

        // 3. 레이에 안 걸리는 낮은 턱이나 모서리에 끼어서 못 움직이고 있으면 벽을 만난 것으로 친다.
        Vector3 moved = transform.position - before;
        moved.y = 0f;
        if (moved.magnitude < _patrolSpeed * Time.deltaTime * 0.3f)
        {
            _stuckTimer += Time.deltaTime;
            if (_stuckTimer >= 0.5f)
            {
                _stuckTimer = 0f;
                OnMeetWall(-direction); // 막힌 반대쪽(뒤쪽)을 벽의 바깥 방향으로 본다.
            }
        }
        else
        {
            _stuckTimer = 0f;
        }
    }

    // 벽을 만났을 때: 확률에 따라 두리번거리거나, 바로 다른 방향으로 간다.
    private void OnMeetWall(Vector3 wallNormal)
    {
        _stuckTimer = 0f;

        if (Random.value < _lookAroundChance)
        {
            Debug.Log("벽이다! 두리번두리번...");
            _lookAroundCoroutine = StartCoroutine(LookAround_Coroutine(wallNormal));
        }
        else
        {
            Debug.Log("벽이다! 다른 데로 가자.");
            PickPatrolTarget(wallNormal);
        }
    }

    // 두리번거리기: 왼쪽 보고, 오른쪽 보고, 벽 반대쪽으로 새 목적지를 정한다.
    private IEnumerator LookAround_Coroutine(Vector3 wallNormal)
    {
        _isLookingAround = true;

        // Todo. 두리번거리는 애니메이션 실행

        float baseAngle = transform.eulerAngles.y;

        yield return RotateTo_Coroutine(baseAngle - _lookAroundAngle); // 왼쪽을 보고
        yield return new WaitForSeconds(0.4f);

        yield return RotateTo_Coroutine(baseAngle + _lookAroundAngle); // 오른쪽을 보고
        yield return new WaitForSeconds(0.4f);

        // 다 둘러봤으니 벽 반대쪽으로 간다.
        PickPatrolTarget(wallNormal);
        _isLookingAround = false;
        _lookAroundCoroutine = null;
    }

    // 정해진 각도(y축)까지 천천히 몸을 돌린다.
    private IEnumerator RotateTo_Coroutine(float targetAngle)
    {
        Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
        while (Quaternion.Angle(transform.rotation, targetRotation) > 1f)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _turnSpeed * Time.deltaTime);
            yield return null; // 한 프레임 기다린다.
        }
        transform.rotation = targetRotation;
    }

    private void StopLookAround()
    {
        if (_lookAroundCoroutine != null)
        {
            StopCoroutine(_lookAroundCoroutine);
            _lookAroundCoroutine = null;
        }
        _isLookingAround = false;
    }

    // 새 순찰 목적지를 정한다.
    // - preferDirection이 zero면 아무 방향이나 고른다.
    // - 아니면 그 방향 기준 좌우 80도 안에서 고른다. (벽을 만났을 때 벽 반대쪽으로 가려고)
    private void PickPatrolTarget(Vector3 preferDirection)
    {
        preferDirection.y = 0f;

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

            float distance = Random.Range(_minPatrolDistance, _patrolRadius);
            Vector3 candidate = transform.position + direction * distance;

            // 조건 1. 처음 자리에서 순찰 반경 밖이면 안 된다.
            Vector3 fromStart = candidate - _startPosition;
            fromStart.y = 0f;
            if (fromStart.magnitude > _patrolRadius)
            {
                continue;
            }

            // 조건 2. 가는 길에 벽이 있으면 안 된다.
            if (IsBlocked(direction, distance))
            {
                continue;
            }

            _patrolTarget = candidate;
            return;
        }

        // 10번 다 실패하면(구석에 몰렸으면) 처음 자리로 돌아간다.
        _patrolTarget = _startPosition;
    }

    // 앞쪽(direction)으로 벽 감지 거리 안에 벽이 있는지. 있으면 벽의 바깥 방향(normal)을 알려준다.
    private bool IsWallAhead(Vector3 direction, out Vector3 wallNormal)
    {
        return FindWall(direction, _wallCheckDistance, out wallNormal);
    }

    // direction으로 distance만큼 가는 길이 막혀 있는지
    private bool IsBlocked(Vector3 direction, float distance)
    {
        return FindWall(direction, distance, out _);
    }

    // 몸 두께만큼 굵은 레이(구)를 쏴서 가장 가까운 "진짜 벽"을 찾는다.
    // SphereCast는 처음 맞은 것 하나만 알려주는데, 그게 내 눈썹이나 코일 수도 있다!
    // 그래서 SphereCastAll로 맞은 것을 전부 받아서 내 몸이 아닌 것 중에 가장 가까운 것을 고른다.
    private bool FindWall(Vector3 direction, float distance, out Vector3 wallNormal)
    {
        wallNormal = Vector3.zero;

        Vector3 origin = transform.position + _controller.center; // 몸통 가운데 높이에서 쏜다.
        float radius = _controller.radius * 0.8f;

        RaycastHit[] hits = Physics.SphereCastAll(origin, radius, direction, distance, _obstacleLayer, QueryTriggerInteraction.Ignore);

        bool found = false;
        float closestDistance = float.MaxValue;
        foreach (RaycastHit hit in hits)
        {
            if (IsWall(hit) == false)
            {
                continue;
            }
            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                wallNormal = hit.normal;
                found = true;
            }
        }

        wallNormal.y = 0f;
        return found;
    }

    // 부딪힌 것이 진짜 벽인지 (내 몸, 플레이어, 바닥/완만한 경사면은 벽이 아니다)
    private bool IsWall(RaycastHit hit)
    {
        // 내 몸(눈썹, 코 같은 자식 오브젝트 포함)은 벽이 아니다.
        if (hit.collider.transform.IsChildOf(transform))
        {
            return false;
        }
        if (hit.collider.gameObject == _player)
        {
            return false;
        }
        if (hit.normal.y > 0.7f) // 위를 향한 면 = 바닥이나 경사로
        {
            return false;
        }
        return true;
    }

    // 플레이어가 감지 거리 안에 있는지
    private bool IsPlayerDetected()
    {
        float distance = Vector3.Distance(transform.position, _player.transform.position);
        return distance <= _detectDistance;
    }

    // 대기 시간을 처음부터 다시 재면서 Idle 상태로 전이한다.
    private void ChangeToIdle()
    {
        Debug.Log($"{_state} -> Idle");
        _idleTimer = 0f;
        _state = ZombieState.Idle;
    }

    // 추격: 플레이어를 쫓아간다.
    private void Trace()
    {
        // Todo. Run 애니메이션 실행


        // 처음 자리에서 너무 멀어지면 돌아간다.
        if (Vector3.Distance(transform.position, _startPosition) > _combackDistance)
        {
            Debug.Log($"{_state} -> Comeback");
            _state = ZombieState.Comeback;
            return;
        }

        // 플레이어가 공격 거리 안이면 공격한다.
        if (Vector3.Distance(transform.position, _player.transform.position) <= _attackRange)
        {
            _attackTimer = 0f;
            Debug.Log($"{_state} -> Attack");
            _state = ZombieState.Attack;
            return;
        }


        Vector3 direction = _player.transform.position - transform.position;
        direction.y = 0f;
        direction.Normalize();

        transform.forward = direction;
        _controller.SimpleMove(direction * _moveSpeed);
    }

    // 복귀: 처음 자리로 돌아간다.
    private void Comeback()
    {
        // 처음 자리에 도착하면 Idle (잠깐 쉬었다가 다시 순찰한다)
        if (Vector3.Distance(transform.position, _startPosition) < 1f)
        {
            ChangeToIdle();
            return;
        }

        // 처음 자리로 돌아간다
        Vector3 direction = _startPosition - transform.position;
        direction.y = 0f;
        direction.Normalize();

        transform.forward = direction;
        _controller.SimpleMove(direction * _moveSpeed);
    }

    // 공격: 일정 시간마다 플레이어를 때린다.
    private void Attack()
    {
        // 플레이어가 공격 사정 거리 밖으로 나가면 쫓아!
        float distance = Vector3.Distance(transform.position, _player.transform.position);
        if (distance > _attackRange)
        {
            Debug.Log($"{_state} -> Trace");
            _state = ZombieState.Trace;
        }

        _attackTimer += Time.deltaTime;
        if (_attackTimer >= _attackInterval)
        {
            // Todo. 공격 애니메이션 실행
            // 플레이어를 때려! (플레이어가 대미지를 받을 수 있으면)
            DamageInfo damageInfo = new DamageInfo();
            damageInfo.Value = _attackDamage;
            damageInfo.HitPoint = _player.transform.position;
            damageInfo.HitNormal = (transform.position - _player.transform.position).normalized;

            _player.GetComponent<IDamageable>()?.TakeDamage(damageInfo);
            Debug.Log("플레이어를 때렸습니다.");
            _attackTimer = 0f;
        }

    }


    // 씬 뷰에서 좀비를 선택하면 순찰 범위, 목적지, 벽 감지 거리, 플레이어 감지 범위를 보여준다. (게임 화면에는 안 보인다)
    private void OnDrawGizmosSelected()
    {
        // 게임 시작 전에는 아직 처음 자리를 저장 안 했으니 현재 위치로 보여준다.
        Vector3 center = Application.isPlaying ? _startPosition : transform.position;

        // 순찰 범위
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(center, _patrolRadius);

        // 지금 가고 있는 목적지
        if (Application.isPlaying && _state == ZombieState.Patrol)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, _patrolTarget);
            Gizmos.DrawWireSphere(_patrolTarget, 0.3f);
        }

        // 벽 감지 거리
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * _wallCheckDistance);

        // 플레이어 감지 범위
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectDistance);
    }
}
