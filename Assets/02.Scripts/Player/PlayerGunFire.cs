using UnityEngine;

// 마우스 좌클릭을 하면 바라보는 방향으로 총알을 발사하고 싶다.
public class PlayerGunFire : MonoBehaviour
{
    [SerializeField]
    private GameObject _hitEffectPrefab;

    private float _range = 100f; // 총알이 날아가는 최대 거리
    private float _damage = 10f; // 총알이 적에게 입히는 피해량

    [SerializeField]
    private float _fireCooldown = 0.1f; // 발사 간격(초)
    private float _fireTimer = 0f;      // 다음 발사까지 남은 시간

    private void Update()
    {
        // 0. 쿨타임 타이머를 줄인다.
        _fireTimer -= Time.deltaTime;

        // 1. 좌클릭을 누르고 있고 && 쿨타임이 끝났으면 발사하기
        if (Input.GetButton("Fire1") && _fireTimer <= 0f)
        {
            Fire();
            _fireTimer = _fireCooldown;
        }
    }

    private void Fire()
    {
        // 2. 레이캐스트를 생성하고 발사될 위치와 진행 방향 설정하기
        Transform cameraTransform = Camera.main.transform;
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        // 3. 레이가 부딛힌 대상의 정보를 저장할 변수 생성하기
        RaycastHit hitInfo = new RaycastHit();

        // 4. 레이캐스트를 발사하고, 부딛힌 대상이 있으면 이팩트 출력하기
        bool isHit = Physics.Raycast(ray, out hitInfo, _range);
        if (isHit)
        {
            Debug.Log($"총알이 {hitInfo.collider.name}에 맞았다.");

            // 이팩트 생성
            Instantiate(_hitEffectPrefab, hitInfo.point, Quaternion.LookRotation(hitInfo.normal));
        }
    }

}
