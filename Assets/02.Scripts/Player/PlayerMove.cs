using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    private const float Gravity = -9.81f;
    private const float GroundedYVelocity = -2f;

    [SerializeField]
    private float _moveSpeed = 7f;

    private CharacterController _characterController;
    private float _yVelocity;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        // 1. 플레이어 이동 입력을 받아옵니다.
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // 2. 플레이어 이동 방향을 구합니다.
        Vector3 moveDir = new Vector3(horizontal, 0f, vertical).normalized;
        moveDir = transform.TransformDirection(moveDir); // 플레이어가 바라보는 방향 기준으로 바꾼다.

        // 3. 땅에 있으면 낙하 속도를 초기화합니다.
        if (_characterController.isGrounded && _yVelocity < 0f)
        {
            _yVelocity = GroundedYVelocity;
        }

        // 4. 중력을 적용합니다.
        _yVelocity += Gravity * Time.deltaTime;

        // 5. CharacterController로 이동합니다.
        Vector3 velocity = moveDir * _moveSpeed;
        velocity.y = _yVelocity;
        _characterController.Move(velocity * Time.deltaTime);
    }
}
