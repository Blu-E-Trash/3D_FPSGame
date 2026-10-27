using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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

// NavMeshAgent: 구워 둔 NavMesh(걸을 수 있는 바닥 지도) 위에서 목적지까지 길을 찾아 알아서 걸어가게 해 주는 컴포넌트
[RequireComponent(typeof(NavMeshAgent))]
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

    [Header("두리번거리기")]
    [SerializeField, Range(0f, 1f)] private float _lookAroundChance = 0.5f; // 로밍 목적지에 도착했을 때 두리번거릴 확률
    [SerializeField] private float _lookAroundAngle = 70f;          // 두리번거릴 때 좌우로 고개를 돌리는 각도
    [SerializeField] private float _turnSpeed = 180f;               // 몸을 돌리는 속도 (초당 각도)

    private NavMeshAgent _agent;
    private Vector3 _startPosition;
    private GameObject _player;
    private float _maxHealth; // 처음 체력 (체력바 비율 계산용, 풀에서 다시 꺼낼 때 이 값으로 되돌린다)
    private ZombiePool _pool; // 죽으면 돌아갈 풀 (없으면 Destroy)
    private readonly List<GameObject> _bloodSprays = new List<GameObject>(); // 몸에 붙어 있는 피 이펙트 (풀로 돌아갈 때 정리)

    // 상태 머신
    private Dictionary<ZombieState, IZombieState> _states;
    private ZombieState _stateType = ZombieState.Idle;
    private IZombieState _currentState;
    private ZombieRoamingState _roamingState; // 기즈모로 목적지를 보여주려고 따로 기억해 둔다.
    private ZombiePatrolState _patrolState;

    public ZombieState State => _stateType;
    public float HealthRatio => _maxHealth > 0f ? Mathf.Max(_health, 0f) / _maxHealth : 1f; // 0 ~ 1, 체력바에 쓴다.
    public bool IsDead => _stateType == ZombieState.Death;

    // 상태 클래스들이 쓰는 값
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
        // 길을 찾아 걷게 해 주는 NavMeshAgent를 찾아서 참조한다.
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = _moveSpeed;

        // 플레이어를 찾아 둔다.
        _player = GameObject.FindGameObjectWithTag("Player");

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
    }

    // 처음 생길 때, 그리고 풀에서 다시 꺼낼 때마다 호출된다. → 새 좀비처럼 초기화한다.
    private void OnEnable()
    {
        _health = _maxHealth;
        _startPosition = transform.position; // 풀이 위치를 옮긴 뒤에 켜 주므로 여기가 스폰 위치다.
        _agent.Warp(_startPosition);         // 에이전트도 스폰 위치에서 시작하게 순간이동시킨다.

        _currentState = null; // 지난 생의 상태(Death)에서 Exit를 부르지 않고 새로 시작한다.
        _stateType = ZombieState.Idle;
        ChangeState(ZombieState.Idle);

        HealthChanged?.Invoke(HealthRatio);

        // 살아 있는 좀비 수를 늘린다.
        AliveCount++;
        AliveCountChanged?.Invoke(AliveCount);
    }

    // 꺼질 때(풀로 돌아갈 때, 씬 종료 등) 호출된다.
    private void OnDisable()
    {
        // 죽어서 꺼지는 거면 이미 수를 줄였다. 죽지 않고 꺼지는 경우에만 수를 맞춰 준다.
        if (IsDead == false)
        {
            AliveCount--;
            AliveCountChanged?.Invoke(AliveCount);
        }
    }

    public void SetPool(ZombiePool pool)
    {
        _pool = pool;
    }

    // 사라진다: 풀이 있으면 풀로 돌아가고, 없으면(씬에 직접 놓은 경우) 제거한다.
    public void Despawn()
    {
        // 몸에 붙어 있던 피 이펙트는 정리한다. (다시 꺼낼 때 피가 남아 있지 않게)
        foreach (GameObject bloodSpray in _bloodSprays)
        {
            if (bloodSpray != null)
            {
                Destroy(bloodSpray);
            }
        }
        _bloodSprays.Clear();

        if (_pool != null)
        {
            _pool.Return(this);
        }
        else
        {
            Destroy(gameObject);
        }
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

    // 스포너가 순찰 지점을 정해 준다.
    public void SetWaypoints(Transform[] waypoints)
    {
        _waypoints = waypoints;
        _patrolState.ResetWaypointIndex(); // 다른 스폰 포인트에서 쓰던 좀비일 수 있으니 처음 지점부터 간다.
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
        _bloodSprays.Add(bloodSpray);
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

    // destination까지 speed로 걸어간다. 길은 NavMeshAgent가 알아서 찾는다. (벽, 계단, 경사로를 돌아서 간다)
    // SetDestination은 "목적지 설정"이다.
    // - 호출할 때마다 목적지를 향해 조금씩 이동 (X)
    // - 한 번 설정하면 에이전트가 내부에서 계속 이동 (O)
    public void MoveTo(Vector3 destination, float speed)
    {
        if (_agent.isOnNavMesh == false)
            return;

        _agent.speed = speed;
        _agent.SetDestination(destination);
    }

    // 지금 목적지에 도착했는지 (길이 막혀서 갈 수 있는 데까지만 간 경우도 도착으로 본다)
    public bool HasArrived(float arriveDistance)
    {
        if (_agent.isOnNavMesh == false)
            return true;

        // pathPending: 아직 길을 계산하는 중
        return _agent.pathPending == false && _agent.remainingDistance <= arriveDistance;
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

    // 씬 뷰에서 좀비를 선택하면 순찰 경로, 로밍 범위, 목적지, 플레이어 감지 범위를 보여준다.
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

        // 플레이어 감지 범위
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectDistance);
    }
}
