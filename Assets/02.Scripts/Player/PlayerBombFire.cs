using UnityEngine;

public class PlayerBombFire : MonoBehaviour
{
    [SerializeField]
    private Bomb _bombPrefab;
    [SerializeField]
    private Transform _firePoint;
    [SerializeField]
    private float _throwForce = 10f; // 수류탄을 던지는 힘의 크기

    private void Update()
    {
        // 1. 마우스 오른쪽 버튼 입력 받기
        if (Input.GetButtonDown("Fire2"))
        {
            Shoot();
            // 2, 발사 위치에서 수류탄 생성하기

            // 3. 생성된 수류탄을 시선 방향으로 던지기
        }
    }
    private void Shoot()
    {
        if (_bombPrefab != null && _firePoint != null)
        {
            // 수류탄 생성
            Bomb bomb = Instantiate(_bombPrefab, _firePoint.position, _firePoint.rotation);
            // 수류탄에 힘을 가해 던지기
            Rigidbody rb = bomb.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(Camera.main.transform.forward * _throwForce, ForceMode.Impulse); // 힘의 크기와 방향 조절 가능
            }
        }
    }
}
