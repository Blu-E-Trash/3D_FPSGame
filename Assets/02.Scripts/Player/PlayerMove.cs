using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [SerializeField]
    private float _moveSpeed = 7f;
    private void Update()
    {
        // 1, 플레이어 이동 입력을 받아옵니다.
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // 2. 플레이어 이동 방향을 구합니다.
        Vector3 moveDir = new Vector3(horizontal, 0f, vertical).normalized;

        // 3. 플레이어 이동 속도를 적용합니다.
        transform.position += moveDir * _moveSpeed * Time.deltaTime;
    }
}
