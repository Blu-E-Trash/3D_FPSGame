using System.Collections;
using UnityEngine;

// 마우스 좌클릭을 하면 바라보는 방향으로 총알을 발사하고 싶다.
// 탄창에 총알이 있을 때만 발사하고, R키를 누르면 재장전한다.
public class PlayerGunFire : MonoBehaviour
{
    [SerializeField]
    private GameObject _hitEffectPrefab;

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

    private void Awake()
    {
        // 처음에는 탄창을 가득 채운 상태로 시작한다.
        _currentAmmo = _maxAmmo;
    }

    private void Update()
    {
        // 0. 쿨타임 타이머를 줄인다.
        _fireTimer -= Time.deltaTime;

        // 1. R키를 누르면 재장전하기 (재장전 중이거나 탄창이 가득 차 있으면 무시)
        if (Input.GetKeyDown(KeyCode.R) && _isReloading == false && _currentAmmo < _maxAmmo)
        {
            StartCoroutine(Reload());
        }

        // 2. 좌클릭을 누르고 있고 && 쿨타임이 끝났고 && 재장전 중이 아니고 && 총알이 있으면 발사하기
        if (Input.GetButton("Fire1") && _fireTimer <= 0f && _isReloading == false && _currentAmmo > 0)
        {
            Fire();
            _fireTimer = _fireCooldown;
        }
    }

    private void Fire()
    {
        // 총알 한 발을 쓴다.
        _currentAmmo--;
        GameLogger.Log($"남은 총알: {_currentAmmo} / {_maxAmmo}");

        // 3. 레이캐스트를 생성하고 발사될 위치와 진행 방향 설정하기
        Transform cameraTransform = Camera.main.transform;
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        // 4. 레이가 부딛힌 대상의 정보를 저장할 변수 생성하기
        RaycastHit hitInfo = new RaycastHit();

        // 5. 레이캐스트를 발사하고, 부딛힌 대상이 있으면 이팩트 출력하기
        bool isHit = Physics.Raycast(ray, out hitInfo, _range);
        if (isHit)
        {
            Debug.Log($"총알이 {hitInfo.collider.name}에 맞았다.");

            // 이팩트 생성
            Instantiate(_hitEffectPrefab, hitInfo.point, Quaternion.LookRotation(hitInfo.normal));
        }
    }

    // 재장전 시간만큼 기다린 뒤 탄창을 가득 채운다.
    private IEnumerator Reload()
    {
        _isReloading = true;
        GameLogger.Log("재장전 시작");

        yield return new WaitForSeconds(_reloadTime);

        _currentAmmo = _maxAmmo;
        _isReloading = false;
        GameLogger.Log("재장전 완료");
    }
}
