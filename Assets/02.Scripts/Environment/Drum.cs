using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 체력이 0이 되면 위로 튀어오르며 폭발하는 드럼통
// 폭발하면 주변 드럼통도 연쇄적으로 터뜨리고, 주변 좀비에게도 대미지를 준다.
[RequireComponent(typeof(Rigidbody))]
public class Drum : MonoBehaviour, IDamageable
{
    [SerializeField] private ParticleSystem _explosionEffectPrefab;

    [Header("스탯")]
    [SerializeField] private float _health = 30f;
    [SerializeField] private float _launchPower = 20f;
    [SerializeField] private float _spinPower = 5f;
    [SerializeField] private float _destoryDelay = 3f;

    [Header("연쇄 폭발")]
    [SerializeField] private float _explosionRadius = 5f;    // 이 반경 안의 드럼통도 터뜨린다.
    [SerializeField] private float _explosionDamage = 100f;  // 주변 드럼통에게 주는 대미지
    [SerializeField] private float _chainDelay = 0.15f;      // 옆 드럼통이 터지기까지 걸리는 시간 (도미노처럼 차례로 터지게)

    [Header("좀비 대미지")]
    [SerializeField] private float _zombieDamage = 50f;      // 폭발 반경 안의 좀비에게 주는 대미지

    [Header("폭발 대상 레이어")]
    [SerializeField] private LayerMask _explosionLayer = ~0; // 인스펙터에서 Drum, Zombie 레이어만 골라주세요. (기본: 전부)

    private Rigidbody _rigidbody;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    // 대미지를 받는다. (IDamageable 약속) 체력이 0이 되면 바로 터진다.
    public void TakeDamage(float damage)
    {
        TakeDamage(damage, 0f);
    }

    // 대미지를 받는다.
    // explodeDelay: 체력이 0이 되었을 때 몇 초 뒤에 터질지 (총에 맞으면 0초, 옆 드럼통 폭발에 휘말리면 조금 뒤)
    public void TakeDamage(float damage, float explodeDelay)
    {
        // 이미 터진(터지는 중인) 드럼통은 무시한다.
        // 이 검사 덕분에 A가 B를 터뜨리고, B가 다시 A를 터뜨리는 무한 반복이 생기지 않는다.
        if (_health <= 0)
        {
            return;
        }

        _health -= damage;

        if (_health <= 0)
        {
            StartCoroutine(Explode_Coroutine(explodeDelay));
        }
    }

    private IEnumerator Explode_Coroutine(float delay)
    {
        // 0. 연쇄 폭발이면 잠깐 기다렸다가 터진다.
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        // 1. 폭발 이펙트를 생성한다.
        if (_explosionEffectPrefab != null)
        {
            ParticleSystem effect = Instantiate(_explosionEffectPrefab, transform.position, Quaternion.identity);
            effect.Play();
        }

        // 2. 위로 튀어오르며 빙글빙글 돈다.
        _rigidbody.AddForce(Vector3.up * _launchPower, ForceMode.Impulse);
        _rigidbody.AddTorque(UnityEngine.Random.insideUnitSphere * _spinPower, ForceMode.Impulse);

        // 3. 주변 드럼통을 터뜨리고, 주변 좀비에게 대미지를 준다.
        DamageNearby();

        // 4. 딜레이 후 사라진다.
        yield return new WaitForSeconds(_destoryDelay);
        Destroy(gameObject);
    }

    // 폭발 반경 안에 있는 다른 드럼통과 좀비에게 대미지를 준다.
    private void DamageNearby()
    {
        // OverlapSphere: 구 안에 들어온 콜라이더를 전부 찾아준다.
        // 레이어 마스크를 넣으면 그 레이어에 있는 콜라이더만 찾는다. (바닥, 벽 같은 건 처음부터 빼고 찾아서 빠르다)
        Collider[] colliders = Physics.OverlapSphere(transform.position, _explosionRadius, _explosionLayer, QueryTriggerInteraction.Ignore);

        // 좀비는 몸통(CharacterController) + 눈썹 + 코처럼 콜라이더가 여러 개다.
        // 그대로 두면 한 좀비가 콜라이더 개수만큼 여러 번 맞으니까, 이미 때린 좀비를 기억해 둔다.
        HashSet<Zombie> damagedZombies = new HashSet<Zombie>();

        foreach (Collider collider in colliders)
        {
            // 콜라이더가 자식 오브젝트에 붙어 있을 수도 있으니 부모까지 찾아본다.

            // 1. 드럼통이면: 조금 뒤에 터지게 한다. -> 도미노처럼 퍼져나간다.
            Drum drum = collider.GetComponentInParent<Drum>();
            if (drum != null && drum != this)
            {
                // (같은 드럼통이 여러 번 잡혀도 TakeDamage 맨 위 검사에서 걸러진다.)
                drum.TakeDamage(_explosionDamage, _chainDelay);
                continue;
            }

            // 2. 좀비면: 대미지를 준다. (한 좀비에게 한 번만!)
            Zombie zombie = collider.GetComponentInParent<Zombie>();
            if (zombie != null && damagedZombies.Add(zombie)) // Add는 처음 넣을 때만 true
            {
                zombie.TakeDamage(_zombieDamage);
            }
        }
    }

    // 씬 뷰에서 드럼통을 선택하면 폭발 반경을 보여준다.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f); // 주황색
        Gizmos.DrawWireSphere(transform.position, _explosionRadius);
    }
}
