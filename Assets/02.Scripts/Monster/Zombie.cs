using System;
using System.Collections.Generic;
using UnityEngine;

// FSM(유한 상태 머신) : 유한개의 상태가 있고, 상태마다 행동을 다르게 함으로써 AI를 구현하는 유명한 방법
// - "상태": 객체가 가질 수 있는 어떤 조건이나 상황
// - "전이": 조건에 따라 현재 상태에서 다른 상태로 넘어가는 것
// - 그리고 위 방식에 따라 규칙을 세워둔 것을 "머신"
//
// 상태 패턴: 상태마다 할 일을 클래스 하나(States 폴더)로 나누고, Zombie는 "지금 상태"에게 일을 맡긴다.
// Zombie에는 여러 상태가 같이 쓰는 값(스탯, 플레이어, 처음 자리)과 기능(이동, 벽 찾기, 공격)만 남긴다.


// 좀비가 가질 수 있는 상태
public enum ZombieState
{
    Idle,       // 대기: 제자리에서 잠깐 쉬면서 플레이어를 기다린다.
    Patrol,     // 순찰: 정해진 지점(웨이포인트)을 순서대로 돌아다닌다.
    Roaming,    // 로밍: 처음 자리 주변을 무작위로 돌아다닌다. 벽을 만나면 두리번거리거나 방향을 바꾼다.
    Trace,      // 추격: 플레이어를 쫓아간다.
    Comeback,   // 복귀: 처음 자리로 돌아간다.
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

    [Header("순찰 / 로밍")]
    [SerializeField] private Transform[] _waypoints;        // 순찰 지점들 (있으면 Patrol, 없으면 Roaming)
    [SerializeField] private float _patrolRadius = 6f;      // 로밍: 처음 자리에서 이 반경 안에서만 돌아다닌다.
    [SerializeField] private float _minPatrolDistance = 2f; // 로밍: 한 번에 최소 이만큼은 걸어간다. (너무 짧게 찔끔 움직이지 않게)
    [SerializeField] private float _patrolSpeed = 2f;       // 순찰/로밍할 때 걷는 속도 (추격보다 느리게)
    [SerializeField] private float _idleDuration = 2f;      // 목적지에 도착해서 쉬는 시간

    [Header("벽 감지")]
    [SerializeField] private LayerMask _obstacleLayer = ~0;        // 벽으로 볼 레이어 (기본: 전부)
    [SerializeField] private float _wallCheckDistance = 1f;         // 앞으로 이만큼 안에 벽이 있으면 멈춘다.
    [SerializeField, Range(0f, 1f)] private float _lookAroundChance = 0.5f; // 벽을 만났을 때 두리번거릴 확률 (나머지는 바로 방향 전환)
    [SerializeField] private float _lookAroundAngle = 70f;          // 두리번거릴 때 좌우로 고개를 돌리는 각도
    [SerializeField] private float _turnSpeed = 180f;               // 몸을 돌리는 속도 (초당 각도)

    private CharacterController _controller;
    private Vector3 _startPosition;
    private GameObject _player;
    private float _maxHealth; // 처음 체력 (체력바 비율 계산용)

    // 상태 머신
    private Dictionary<ZombieState, IZombieState> _states;
    private ZombieState _stateType = ZombieState.Idle;
    private IZombieState _currentState;
    private ZombieRoamingState _roamingState; // 기즈모로 목적지를 보여주려고 따로 기억해 둔다.
    private ZombiePatrolState _patrolState;

    public ZombieState State => _stateType;
    public float HealthRatio => Mathf.Max(_health, 0f) / _maxHealth; // 0 ~ 1, 체력바에 쓴다.
    public bool IsDead => _stateType == ZombieState.Death;

    // 상태 클래스들이 쓰는 값
    public CharacterController Controller => _controller;
    public GameObject Player => _player;
    public Vector3 StartPosition => _startPosition;
    public float MoveSpeed => _moveSpeed;
    public float AttackRange => _attackRange;
    public float AttackInterval => _attackInterval;
    public float ComebackDistance => _combackDistance;
    public Transform[] Waypoints => _waypoints;
    public bool HasWaypoints => _waypoints != null && _waypoints.Length > 0;
    public float RoamingRadius => _patrolRadius;
    public float MinRoamingDistance => _minPatrolDistance;
    public float PatrolSpeed => _patrolSpeed;
    public float IdleDuration => _idleDuration;
    public float WallCheckDistance => _wallCheckDistance;
    public float LookAroundChance => _lookAroundChance;
    public float LookAroundAngle => _lookAroundAngle;
    public float TurnSpeed => _turnSpeed;

    // 체력이 바뀌었을 때 호출된다. (체력 비율 0 ~ 1을 넘겨준다) → 체력바가 구독한다.
    public event Action<float> HealthChanged;

    // 이 좀비가 죽었을 때 호출된다.
    public event Action Died;

    // 살아 있는 좀비 수 (모든 좀비가 함께 쓰는 값이라 static)
    public static int AliveCount { get; private set; }

    // 살아 있는 좀비 수가 바뀌었을 때 호출된다. → 남은 좀비 수 UI가 구독한다.
    public static event Action<int> AliveCountChanged;

    // static 값은 씬을 다시 시작해도 남아 있을 수 있어서, 게임을 시작할 때 초기화한다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        AliveCount = 0;
        AliveCountChanged = null;
    }

    private void Awake()
    {
        _maxHealth = _health;
        _controller = GetComponent<CharacterController>();

        // 상태 객체를 한 번씩만 만들어 두고 계속 쓴다.
        _roamingState = new ZombieRoamingState(this);
        _patrolState = new ZombiePatrolState(this);
        _states = new Dictionary<ZombieState, IZombieState>
        {
            { ZombieState.Idle, new ZombieIdleState(this) },
            { ZombieState.Patrol, _patrolState },
            { ZombieState.Roaming, _roamingState },
            { ZombieState.Trace, new ZombieTraceState(this) },
            { ZombieState.Comeback, new ZombieComebackState(this) },
            { ZombieState.Attack, new ZombieAttackState(this) },
            { ZombieState.Hit, new ZombieHitState(this) },
            { ZombieState.Death, new ZombieDeathState(this) },
        };

        // 좀비가 생겨나면 살아 있는 좀비 수를 늘린다.
        AliveCount++;
        AliveCountChanged?.Invoke(AliveCount);
    }

    private void OnDestroy()
    {
        // 죽지 않고 사라지는 경우(씬 종료 등)에도 수를 맞춰 준다.
        if (IsDead == false)
        {
            AliveCount--;
            AliveCountChanged?.Invoke(AliveCount);
        }
    }

    private void Start()
    {
        _startPosition = transform.position;

        // 게임을 시작하면 플레이어를 찾는다.
        _player = GameObject.FindGameObjectWithTag("Player");

        ChangeState(ZombieState.Idle);
    }

    private void Update()
    {
        // 게임 중이 아니면(준비, 게임오버) 아무것도 하지 않는다.
        if (GameManager.Instance.State != GameState.Playing) return;

        // 지금 상태에게 할 일을 맡긴다.
        _currentState?.Execute();
    }

    // 상태를 바꾼다: 지금 상태에서 나가고(Exit) → 새 상태로 들어간다(Enter).
    public void ChangeState(ZombieState nextState)
    {
        _currentState?.Exit();

        Debug.Log($"{_stateType} -> {nextState}");
        _stateType = nextState;
        _currentState = _states[nextState];

        _currentState.Enter();
    }

    // 스포너가 순찰 지점을 정해 준다. (Start 전에 불러야 한다)
    public void SetWaypoints(Transform[] waypoints)
    {
        _waypoints = waypoints;
    }

    // 대미지를 받는다.
    public void TakeDamage(DamageInfo damageInfo)
    {
        if (IsDead)
        {
            return;
        }

        _health -= damageInfo.Value;

        // 체력이 바뀌었다고 알린다. (체력바가 직접 확인하지 않고, 알림을 받아서 갱신한다)
        HealthChanged?.Invoke(HealthRatio);

        if (_health > 0f)
        {
            SpawnBloodSpray(damageInfo);

            // 잠깐 멈칫 하고 싶다!
            ChangeState(ZombieState.Hit);
        }
        else
        {
            ChangeState(ZombieState.Death);

            // 죽었다고 알리고, 살아 있는 좀비 수를 줄인다.
            Died?.Invoke();
            AliveCount--;
            AliveCountChanged?.Invoke(AliveCount);
        }
    }

    // 맞은 부위에 피 스프레이를 실행한다.
    // 좀비의 자식으로 생성해서, 좀비가 움직여도 맞은 자리에 붙어서 같이 움직이게 한다.
    private void SpawnBloodSpray(DamageInfo damageInfo)
    {
        if (_bloodSprayPrefab == null)
            return;

        GameObject bloodSpray = Instantiate(_bloodSprayPrefab, transform);
        bloodSpray.transform.position = damageInfo.HitPoint;
        if (damageInfo.HitNormal != Vector3.zero)
        {
            bloodSpray.transform.forward = damageInfo.HitNormal;
        }
    }

    // ---------------- 상태들이 같이 쓰는 기능 ----------------

    // 플레이어가 감지 거리 안에 있는지
    public bool IsPlayerDetected()
    {
        return DistanceToPlayer() <= _detectDistance;
    }

    public float DistanceToPlayer()
    {
        return Vector3.Distance(transform.position, _player.transform.position);
    }

    // direction 방향을 바라보며 speed로 이동한다. 실제로 움직였으면 true (끼어서 못 움직이면 false)
    public bool Move(Vector3 direction, float speed)
    {
        transform.forward = direction;

        Vector3 before = transform.position;
        _controller.SimpleMove(direction * speed);

        Vector3 moved = transform.position - before;
        moved.y = 0f;
        return moved.magnitude >= speed * Time.deltaTime * 0.3f;
    }

    // 플레이어를 때린다. (플레이어가 대미지를 받을 수 있으면)
    public void AttackPlayer()
    {
        DamageInfo damageInfo = new DamageInfo();
        damageInfo.Value = _attackDamage;
        damageInfo.HitPoint = _player.transform.position;
        damageInfo.HitNormal = (transform.position - _player.transform.position).normalized;

        _player.GetComponent<IDamageable>()?.TakeDamage(damageInfo);
        Debug.Log("플레이어를 때렸습니다.");
    }

    // 몸 두께만큼 굵은 레이(구)를 쏴서 가장 가까운 "진짜 벽"을 찾는다.
    // SphereCast는 처음 맞은 것 하나만 알려주는데, 그게 내 눈썹이나 코일 수도 있다!
    // 그래서 SphereCastAll로 맞은 것을 전부 받아서 내 몸이 아닌 것 중에 가장 가까운 것을 고른다.
    public bool FindWall(Vector3 direction, float distance, out Vector3 wallNormal)
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

    // 씬 뷰에서 좀비를 선택하면 순찰 경로, 로밍 범위, 목적지, 벽 감지 거리, 플레이어 감지 범위를 보여준다.
    private void OnDrawGizmosSelected()
    {
        // 게임 시작 전에는 아직 처음 자리를 저장 안 했으니 현재 위치로 보여준다.
        Vector3 center = Application.isPlaying ? _startPosition : transform.position;

        if (HasWaypoints)
        {
            // 순찰 경로
            Gizmos.color = Color.cyan;
            for (int i = 0; i < _waypoints.Length; i++)
            {
                Vector3 from = _waypoints[i].position;
                Vector3 to = _waypoints[(i + 1) % _waypoints.Length].position;
                Gizmos.DrawWireSphere(from, 0.3f);
                Gizmos.DrawLine(from, to);
            }
        }
        else
        {
            // 로밍 범위
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(center, _patrolRadius);
        }

        // 지금 가고 있는 목적지
        if (Application.isPlaying && (_stateType == ZombieState.Roaming || _stateType == ZombieState.Patrol))
        {
            Vector3 target = _stateType == ZombieState.Roaming ? _roamingState.Target : _patrolState.Target;
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, target);
            Gizmos.DrawWireSphere(target, 0.3f);
        }

        // 벽 감지 거리
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * _wallCheckDistance);

        // 플레이어 감지 범위
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectDistance);
    }
}
