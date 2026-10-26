using System.Collections.Generic;
using UnityEngine;

// 무언가에 부딪히면 폭발해서 주변에 대미지를 주고, 오브젝트 풀로 돌아간다.
public class Bomb : MonoBehaviour
{
    [SerializeField]
    private ParticleSystem _explosionEffectPrefab;
    [SerializeField]
    private float _effectBaseRadius = 1f; // 이펙트를 스케일 1로 터뜨렸을 때 보이는 반경 (폭발 반경에 맞춰 키울 때 기준)
    [SerializeField]
    private float _maxLifeTime = 5f; // 아무 데도 부딪히지 않아도 이 시간이 지나면 터진다. (맵 밖으로 떨어진 경우 대비)

    [Header("폭발 대미지")]
    [SerializeField]
    private float _explosionRange = 4.5f;   // 폭발 반경
    [SerializeField]
    private float _damage = 100f;           // 반경 안의 대상에게 주는 대미지
    [SerializeField]
    private LayerMask _damageLayer = ~0;    // 대미지를 줄 레이어 (기본: 전부)

    private BombPool _pool;
    private Rigidbody _rigidbody;
    private bool _isExploded = false; // 한 번 터진 뒤 같은 프레임에 또 부딪혀서 두 번 터지는 것을 막는다.
    private float _lifeTimer = 0f;

    public Rigidbody Rigidbody => _rigidbody;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    // 풀에서 꺼낼 때마다 호출된다. 이전에 쓰던 상태를 초기화한다.
    private void OnEnable()
    {
        _isExploded = false;
        _lifeTimer = 0f;

        // 이전에 던져졌던 속도와 회전이 남아 있지 않게 지운다.
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
    }

    private void Update()
    {
        _lifeTimer += Time.deltaTime;
        if (_lifeTimer >= _maxLifeTime)
        {
            Explode();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Explode();
    }

    public void SetPool(BombPool pool)
    {
        _pool = pool;
    }

    private void Explode()
    {
        if (_isExploded)
            return;

        _isExploded = true;

        // 폭발 효과 생성 (Play On Awake로 바로 재생된다)
        if (_explosionEffectPrefab != null)
        {
            // 실제 폭발 반경만큼 이펙트를 키워서 터뜨린다.
            ExplosionEffect.Spawn(_explosionEffectPrefab, transform.position, _explosionRange, _effectBaseRadius);
        }

        // 주변에 대미지를 준다.
        ApplyAreaDamage();

        // 폭탄을 풀로 돌려보낸다. 풀이 없으면(씬에 직접 놓은 경우) 제거한다.
        if (_pool != null)
        {
            _pool.Return(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 폭발 반경 안에 있는 대미지를 받을 수 있는 대상(IDamageable)에게 대미지를 준다.
    private void ApplyAreaDamage()
    {
        // 0. 주위에 있는 콜라이더를 모두 찾는다.
        // Physics.OverlapSphere(원점, 반지름, 찾을 레이어들)
        Collider[] colliders = Physics.OverlapSphere(transform.position, _explosionRange, _damageLayer, QueryTriggerInteraction.Ignore);

        // 좀비처럼 콜라이더가 여러 개인 대상이 여러 번 맞지 않도록 이미 때린 대상을 기억해 둔다.
        HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();

        // 1. 대미지를 받을 수 있는 대상(드럼통, 좀비, 상자 등)에게 대미지를 준다.
        foreach (Collider col in colliders)
        {
            IDamageable damageable = col.GetComponentInParent<IDamageable>();
            if (damageable != null && damagedTargets.Add(damageable))
            {
                damageable.TakeDamage(CreateExplosionDamageInfo(col, _damage));
            }
        }
    }

    // 폭발 중심에서 대상 콜라이더까지의 대미지 정보를 만든다.
    private DamageInfo CreateExplosionDamageInfo(Collider col, float damage)
    {
        Vector3 center = transform.position;
        Vector3 hitPoint = col.ClosestPoint(center); // 폭발 중심에서 가장 가까운 콜라이더 표면 위치

        // 맞은 자리에서 폭발 중심을 바라보는 방향
        // (CharacterController처럼 ClosestPoint를 지원하지 않는 콜라이더는 중심 위치를 그대로 돌려줘서 방향이 0이 될 수 있다 -> 콜라이더 중심 기준으로 대신 구한다)
        Vector3 hitNormal = center - hitPoint;
        if (hitNormal.sqrMagnitude < 0.0001f)
        {
            hitPoint = col.bounds.center;
            hitNormal = center - hitPoint;
        }

        return new DamageInfo
        {
            Value = damage,
            HitPoint = hitPoint,
            HitNormal = hitNormal.normalized
        };
    }

    // 씬 뷰에서 폭탄을 선택하면 폭발 반경을 보여준다.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _explosionRange);
    }
}
