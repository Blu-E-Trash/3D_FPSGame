using UnityEngine;

// 무언가에 부딪히면 폭발하고, 오브젝트 풀로 돌아간다.
public class Bomb : MonoBehaviour
{
    [SerializeField]
    private ParticleSystem _explosionEffectPrefab;
    [SerializeField]
    private float _maxLifeTime = 5f; // 아무 데도 부딪히지 않아도 이 시간이 지나면 터진다. (맵 밖으로 떨어진 경우 대비)

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
            Instantiate(_explosionEffectPrefab, transform.position, Quaternion.identity);
        }

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
}
