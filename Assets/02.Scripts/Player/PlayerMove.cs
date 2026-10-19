using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    private const float Gravity = -9.81f;
    private const float GroundedYVelocity = -2f;

    [SerializeField]
    private float _moveSpeed = 7f;
    [SerializeField]
    private float _jumpPower = 5f;

    private CharacterController _characterController;
    private float _yVelocity;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        // 1. 플레이어 이동 입력을 받아옵니다.
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // 2. 플레이어 이동 방향을 구합니다.
        Vector3 moveDir = new Vector3(horizontal, 0f, vertical).normalized;

        // 3. 땅에 있으면 낙하 속도를 초기화하고, 점프 입력을 받습니다.
        if (_characterController.isGrounded)
        {
            if (_yVelocity < 0f)
            {
                _yVelocity = GroundedYVelocity;
            }

            if (Input.GetButtonDown("Jump"))
            {
                _yVelocity = _jumpPower;
            }
        }

        // 4. 중력을 적용합니다.
        _yVelocity += Gravity * Time.deltaTime;

        // 5. CharacterController로 이동합니다. (Step Offset / Slope Limit / 벽 충돌 적용)
        Vector3 velocity = moveDir * _moveSpeed;
        velocity.y = _yVelocity;
        _characterController.Move(velocity * Time.deltaTime);
    }
}
