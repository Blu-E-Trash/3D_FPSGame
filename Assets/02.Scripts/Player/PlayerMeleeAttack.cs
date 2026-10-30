using UnityEngine;

// 근접 모드에서 마우스 오른쪽 버튼을 누르면 왼손으로 환도를 휘둘러, 칼날에 닿은 대상에 대미지를 준다.
[RequireComponent(typeof(PlayerWeaponMode))]
public class PlayerMeleeAttack : MonoBehaviour
{
    private const string MeleeLayerName = "Melee Layer";
    private const string MeleeTrigger = "Melee";

    [Header("공격")]
    [SerializeField] private float _damage = 25f;                      // 대미지

    [Header("판정")]
    [SerializeField] private MeleeHitMode _hitMode = MeleeHitMode.Collider;  // 맞힐 대상을 고르는 방법
    [SerializeField] private float _range = 2f;                              // 부채꼴의 반지름 (m)
    [SerializeField, Range(0f, 360f)] private float _angle = 90f;            // 부채꼴 각도 (도)
    [SerializeField] private LayerMask _targetMask;                          // 때릴 대상 (Default, Drum)
    [SerializeField] private LayerMask _obstacleMask;                        // 가로막는 것 (Default)

    [Header("베기 이펙트")]
    [SerializeField] private GameObject _slashEffectPrefab;                          // 휘두를 때 보여 줄 이펙트 (Slash_Ink)
    [SerializeField] private Vector3 _slashEffectOffset = new Vector3(0f, -0.3f, 1f); // 카메라 기준 위치 (앞으로 1m, 조금 아래)
    [SerializeField] private Vector3 _slashEffectRotation = Vector3.zero;             // 카메라 기준 추가 회전 (도)
    [SerializeField] private float _slashEffectLifetime = 1.5f;                      // 이펙트를 지우기까지의 시간 (초)

    [Header("환도")]
    [SerializeField] private GameObject _weapon;                       // 왼손 뼈 아래의 Hwando
    [SerializeField, Range(0f, 1f)] private float _fadeStart = 0.85f;  // 동작이 이만큼 지나면 팔을 돌려놓기 시작한다. (0~1)
    [SerializeField] private float _fadeTime = 0.2f;                   // 팔이 총으로 돌아오는 시간 (초)

    private PlayerWeaponMode _weaponMode;
    private Animator _animator;
    private Collider _weaponCollider;  // 환도의 칼날 (트리거)
    private int _meleeLayer;      // Melee Layer의 번호
    private float _meleeWeight;   // Melee Layer의 지금 무게

    private void Awake()
    {
        _weaponMode = GetComponent<PlayerWeaponMode>();
        _animator = GetComponentInChildren<Animator>();
        _meleeLayer = _animator.GetLayerIndex(MeleeLayerName);  // 이름으로 레이어 번호를 찾는다.
        _weaponCollider = _weapon.GetComponent<Collider>();
    }

    private void Start()
    {
        DeactivateCollider();  // 처음에는 칼날을 꺼 둔다.
    }

    private void Update()
    {
        UpdateMeleeWeight();  // 게임 상태와 상관없이 팔은 돌려놓는다.

        // 게임 중이 아니면 아무것도 하지 않는다.
        if (GameManager.Instance.State != GameState.Playing)
        {
            return;
        }

        // 환도는 근접 모드에서만 휘두른다. 다른 모드의 오른쪽 버튼은 PlayerBombFire와 PlayerZoom이 쓴다.
        if (_weaponMode.Mode != WeaponMode.Melee)
        {
            return;
        }

        // 1. 마우스 오른쪽 버튼을 누른 순간, 쉬고 있으면 휘두른다.
        if (Input.GetMouseButtonDown(1) && IsResting())
        {
            DeactivateCollider();  // 지난번에 켜진 채 남은 칼날이 있으면 끄고 시작한다.
            _meleeWeight = 1f;
            _animator.SetTrigger(MeleeTrigger);
        }
    }

    // Melee Layer가 Empty에 있고 다른 상태로 넘어가는 중도 아니면, 쉬고 있는 것이다.
    private bool IsResting()
    {
        AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(_meleeLayer);
        return state.IsName("Empty") && !_animator.IsInTransition(_meleeLayer);
    }

    // 다 휘둘렀거나 쉬고 있으면 Melee Layer의 무게를 천천히 0으로 줄인다.
    // 환도는 무게가 남아 있는 동안만 보인다.
    private void UpdateMeleeWeight()
    {
        AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(_meleeLayer);
        bool isFinishing = state.IsName("Melee") && state.normalizedTime >= _fadeStart;
        if (isFinishing || IsResting())
        {
            _meleeWeight = Mathf.MoveTowards(_meleeWeight, 0f, Time.deltaTime / _fadeTime);
        }

        _animator.SetLayerWeight(_meleeLayer, _meleeWeight);
        _weapon.SetActive(_meleeWeight > 0f);
    }

    // 칼날을 켠다. 휘두르는 동작의 ActivateCollider 이벤트가 불러 준다.
    public void ActivateCollider()
    {
        // 부채꼴 방법일 때는 칼날을 켜지 않는다.
        if (_hitMode != MeleeHitMode.Collider)
        {
            return;
        }

        _weaponCollider.enabled = true;
    }

    // 칼날을 끈다. 휘두르는 동작의 DeactivateCollider 이벤트가 불러 준다.
    public void DeactivateCollider()
    {
        _weaponCollider.enabled = false;
    }

    // 칼날에 무언가가 닿으면 MeleeWeapon이 불러 준다.
    public void Hit(Collider other)
    {
        // 1. 내 몸(Player와 그 자식)에 닿은 것은 무시한다.
        if (other.transform.IsChildOf(transform))
        {
            return;
        }

        // 2. 대미지를 받을 수 없으면 무시한다. (콜라이더가 자식에 붙어 있을 수도 있으니 부모까지 찾아본다)
        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null)
        {
            return;
        }

        // 3. 한 번 휘두를 때 하나만 맞힌다. 칼날을 바로 끈다.
        DeactivateCollider();

        // 4. 대상에서 플레이어에 가장 가까운 점을 맞은 자리로 삼아 대미지를 준다.
        Vector3 hitPoint = other.ClosestPoint(transform.position);
        GiveDamage(damageable, hitPoint);
    }

    // 대미지 정보를 만들어 넘긴다. 맞은 자리의 방향은 플레이어 쪽이다.
    private void GiveDamage(IDamageable damageable, Vector3 hitPoint)
    {
        DamageInfo damageInfo = new DamageInfo
        {
            Value = _damage,
            HitPoint = hitPoint,
            HitNormal = (transform.position - hitPoint).normalized,
        };
        damageable.TakeDamage(damageInfo);
    }

    // 앞쪽 부채꼴 안의 대상을 한 번에 때린다. 휘두르는 동작의 FanAttack 이벤트가 불러 준다.
    public void FanAttack()
    {
        // 칼날 콜라이더 방법일 때는 하지 않는다.
        if (_hitMode != MeleeHitMode.Fan)
        {
            return;
        }

        Vector3 origin = transform.position;

        // 1. 사거리 안의 대상을 모두 찾는다.
        Collider[] colliders = Physics.OverlapSphere(origin, _range, _targetMask, QueryTriggerInteraction.Ignore);

        foreach (Collider col in colliders)
        {
            // 내 몸은 건너뛴다.
            if (col.transform.IsChildOf(transform))
            {
                continue;
            }

            // 바닥, 벽 같은 볼록하지 않은 MeshCollider는 ClosestPoint를 쓸 수 없고 대미지도 받지 않으므로 건너뛴다.
            if (col is MeshCollider meshCollider && meshCollider.convex == false)
            {
                continue;
            }

            // 2. 대상에서 플레이어에 가장 가까운 점을 맞을 자리로 삼는다.
            Vector3 hitPoint = col.ClosestPoint(origin);

            // 3. 높이를 뺀 방향이 앞쪽에서 Angle의 절반 안에 있는지 본다.
            Vector3 direction = hitPoint - origin;
            direction.y = 0f;
            if (Vector3.Angle(transform.forward, direction) > _angle / 2f)
            {
                continue;   // 부채꼴 밖
            }

            // 4. 가는 길에 벽이 있으면 때리지 않는다.
            //    (좀비와 상자도 Default 레이어라서, 선이 대상 자신에 걸린 것은 막힌 것으로 치지 않는다)
            if (Physics.Linecast(origin, hitPoint, out RaycastHit blockHit, _obstacleMask, QueryTriggerInteraction.Ignore)
                && blockHit.collider != col)
            {
                continue;
            }

            // 5. 대미지를 받을 수 있으면 대미지 정보를 넘긴다.
            IDamageable damageable = col.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                GiveDamage(damageable, hitPoint);
            }
        }
    }

    // Player를 고르면 Scene 창에 부채꼴 방법의 공격 범위를 그린다.
    private void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position;
        Gizmos.color = Color.yellow;

        // 앞쪽을 y축으로 -Angle/2, +Angle/2만큼 돌린 두 방향이 부채꼴의 양쪽 끝이다.
        Vector3 left = Quaternion.Euler(0f, -_angle / 2f, 0f) * transform.forward;
        Vector3 right = Quaternion.Euler(0f, _angle / 2f, 0f) * transform.forward;
        Gizmos.DrawLine(origin, origin + left * _range);
        Gizmos.DrawLine(origin, origin + right * _range);

        // 호: 10도마다 짧은 선을 이어 그린다.
        int count = Mathf.Max(1, Mathf.CeilToInt(_angle / 10f));
        Vector3 previous = origin + left * _range;
        for (int i = 1; i <= count; i++)
        {
            float angle = -_angle / 2f + _angle * i / count;
            Vector3 next = origin + Quaternion.Euler(0f, angle, 0f) * transform.forward * _range;
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }

    // 베기 이펙트를 카메라 앞에 만든다. 휘두르는 동작의 PlaySlashEffect 이벤트가 불러 준다.
    public void PlaySlashEffect()
    {
        if (_slashEffectPrefab == null)
        {
            return;
        }

        // 카메라가 보는 방향 기준으로 위치와 방향을 정해서, 어디를 보고 휘둘러도 눈앞에 보이게 한다.
        Transform cameraTransform = Camera.main.transform;
        Vector3 position = cameraTransform.TransformPoint(_slashEffectOffset);
        Quaternion rotation = cameraTransform.rotation * Quaternion.Euler(_slashEffectRotation);

        GameObject effect = Instantiate(_slashEffectPrefab, position, rotation);
        Destroy(effect, _slashEffectLifetime);  // 다 보여 준 뒤 스스로 지운다.
    }
}
