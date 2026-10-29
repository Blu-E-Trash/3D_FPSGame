using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

// 마우스 좌클릭을 하면 바라보는 방향으로 총알을 발사하고 싶다.
// 탄창에 총알이 있을 때만 발사하고, R키를 누르면 재장전한다.
public class PlayerGunFire : MonoBehaviour
{
    private const string AttackTrigger = "Attack"; // 애니메이터의 쏘는 동작 트리거 (UpperBody 레이어)

    [SerializeField]
    private GameObject _hitEffectPrefab;
    [SerializeField]
    private BulletTrail _bulletTrail; // 총 쏠 때 그리는 총알 궤적 (라인 렌더러)

    private float _range = 100f; // 총알이 날아가는 최대 거리
    private float _damage = 10f; // 총알이 적에게 입히는 피해량

    [SerializeField]
    private float _fireCooldown = 0.1f; // 발사 간격(초)
    private float _fireTimer = 0f;      // 다음 발사까지 남은 시간

    [Header("탄창")]
    [SerializeField]
    private int _maxAmmo = 50;          // 탄창 하나에 들어가는 총알 수
    [SerializeField]
    private float _reloadTime = 1.5f;   // 재장전에 걸리는 시간(초)
    private int _currentAmmo;           // 탄창에 남은 총알 수
    private bool _isReloading = false;  // 재장전 중인지

    public int CurrentAmmo => _currentAmmo;
    public int MaxAmmo => _maxAmmo;
    public bool IsReloading => _isReloading;
    public event System.Action<int, int> OnBulletChanged;   // 총알 수가 바뀌면 (남은 수, 탄창 크기)를 알린다.

    private Animator _animator; // 군인 모델의 애니메이터

    private void Awake()
    {
        // 처음에는 탄창을 가득 채운 상태로 시작한다.
        _currentAmmo = _maxAmmo;
        _animator = GetComponentInChildren<Animator>(); // 자식인 Model에 붙어 있다.
    }

    private void Update()
    {
        // 게임 중이 아니면(준비, 게임오버) 아무것도 하지 않는다.
        if (GameManager.Instance.State != GameState.Playing) return;

        // 0. 쿨타임 타이머를 줄인다.
        _fireTimer -= Time.deltaTime;

        // 1. R키를 누르면 재장전하기 (재장전 중이거나 탄창이 가득 차 있으면 무시)
        if (Input.GetKeyDown(KeyCode.R) && _isReloading == false && _currentAmmo < _maxAmmo)
        {
            StartCoroutine(Reload());
        }

        // 2. 마우스가 UI(미니맵 버튼 등) 위에 있으면 총을 쏘지 않는다.
        bool isPointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        // 3. 좌클릭을 누르고 있고 && UI 위가 아니고 && 쿨타임이 끝났고 && 재장전 중이 아니고 && 총알이 있으면 발사하기
        if (Input.GetButton("Fire1") && isPointerOverUI == false && _fireTimer <= 0f && _isReloading == false && _currentAmmo > 0)
        {
            Fire();
            _fireTimer = _fireCooldown;
            // 쏘는 동작은 상체에만 적용되는 UpperBody 레이어(아바타 마스크)에서 재생된다.
            // 그래서 달리면서 쏴도 다리는 Base Layer의 달리기 동작을 그대로 한다.
            _animator.SetTrigger(AttackTrigger);
        }
    }

    private void Fire()
    {
        // 총알 한 발을 쓴다.
        _currentAmmo--;
        OnBulletChanged?.Invoke(_currentAmmo, _maxAmmo);  // 한 발 줄었다고 알린다.
        GameLogger.Log($"남은 총알: {_currentAmmo} / {_maxAmmo}");

        // 3. 레이캐스트를 생성하고 발사될 위치와 진행 방향 설정하기
        Transform cameraTransform = Camera.main.transform;
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        // 4. 레이가 부딛힌 대상의 정보를 저장할 변수 생성하기
        RaycastHit hitInfo = new RaycastHit();

        // 5. 레이캐스트를 발사하고, 부딛힌 대상이 있으면 이팩트 출력하기
        // (바닥을 보고 쏘면 레이가 내 몸을 먼저 지나가서 내가 맞는다 → 내 몸은 빼고 가장 가까운 것을 찾는다)
        bool isHit = RaycastIgnoringSelf(ray, out hitInfo);

        // 총알 궤적: 맞았으면 맞은 곳까지, 안 맞았으면 사거리 끝까지 선을 그린다.
        if (_bulletTrail != null)
        {
            Vector3 endPoint = isHit ? hitInfo.point : ray.origin + ray.direction * _range;
            _bulletTrail.Show(endPoint);
        }
        if (isHit)
        {
            Debug.Log($"총알이 {hitInfo.collider.name}에 맞았다.");

            // 이팩트 생성
            Instantiate(_hitEffectPrefab, hitInfo.point, Quaternion.LookRotation(hitInfo.normal));

            // 맞은 대상이 무엇이든 "대미지를 받을 수 있다(IDamageable)"면 대미지를 준다.
            // (콜라이더가 좀비의 눈썹, 코 같은 자식에 붙어 있을 수도 있으니 부모까지 찾아본다)
            // 대미지 구조체 만들기
            DamageInfo damageInfo = new DamageInfo();
            damageInfo.Value = _damage;
            damageInfo.HitPoint = hitInfo.point;
            damageInfo.HitNormal = hitInfo.normal;

            hitInfo.collider.GetComponentInParent<IDamageable>()?.TakeDamage(damageInfo);
        }
    }

    // 레이에 맞은 것을 전부 받아서, 내 몸(플레이어와 자식 오브젝트)이 아닌 것 중 가장 가까운 것을 고른다.
    private bool RaycastIgnoringSelf(Ray ray, out RaycastHit closestHit)
    {
        closestHit = new RaycastHit();
        bool found = false;
        float closestDistance = float.MaxValue;

        RaycastHit[] hits = Physics.RaycastAll(ray, _range);
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(transform))
            {
                continue;
            }
            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                closestHit = hit;
                found = true;
            }
        }

        return found;
    }

    // 재장전 시간만큼 기다린 뒤 탄창을 가득 채운다.
    private IEnumerator Reload()
    {
        _isReloading = true;
        GameLogger.Log("재장전 시작");

        yield return new WaitForSeconds(_reloadTime);

        _currentAmmo = _maxAmmo;
        OnBulletChanged?.Invoke(_currentAmmo, _maxAmmo);  // 탄창을 채웠다고 알린다.
        _isReloading = false;
        GameLogger.Log("재장전 완료");
    }
}
