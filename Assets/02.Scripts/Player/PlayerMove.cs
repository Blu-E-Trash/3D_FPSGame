using UnityEngine;

// 키보드 방향키에 따라 그 방향으로 이동하고 싶다.
public class PlayerMove : MonoBehaviour
{
    private const float Gravity = -20f;
    private const float GroundedYVelocity = -2f; // 땅에 있을 때 바닥에 붙어 있도록 살짝 아래로 누르는 속도
    private float _yVelocity = 0f;  // 위아래 속도, 매 프레임마다 중력 가속도가 붙는다.
    private int _jumpCount = 0;     // 땅에서 떨어진 뒤 점프한 횟수

    [Header("이동")]
    [SerializeField] private float _moveSpeed = 7f;
    [SerializeField] private float _runSpeed = 12f;
    [SerializeField] private float _runStaminaPerSecond = 20f; // 질주할 때 초당 스태미나 소모량

    [Header("점프")]
    [SerializeField] private float _jumpPower = 10f;
    [SerializeField] private int _maxJumpCount = 2;            // 2 = 2단 점프
    [SerializeField] private float _jumpStaminaCost = 15f;     // 점프 한 번에 쓰는 스태미나

    [Header("참조")]
    [SerializeField] private CharacterController _characterController;
    [SerializeField] private PlayerStamina _stamina;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _stamina = GetComponent<PlayerStamina>();
    }

    private void Update()
    {
        // 1. 키보드 입력을 받는다.
        float h = Input.GetAxisRaw("Horizontal"); // -1, 0, 1
        float v = Input.GetAxisRaw("Vertical"); // -1, 0, 1

        // 2. 키보드 입력에 따라 방향을 구한다.
        Vector3 direction = new Vector3(h, 0, v);
        direction.Normalize(); // 대각선이어도 방향의 길이 1로 바꾸기 위해
        // 캐릭터의 회전 방향을 기준으로 로컬 방향 벡터를 월드 방향 벡터로 변환한다.
        direction = transform.TransformDirection(direction);
        // 로컬 좌표계: 한 대상을 중심
        // 월드 좌표계: 세상을 중심

        // 3. 땅에 서 있으면 낙하 속도와 점프 횟수를 초기화하고, 공중이면 중력을 누적한다.
        if (_characterController.isGrounded && _yVelocity < 0f)
        {
            _yVelocity = GroundedYVelocity;
            _jumpCount = 0;
        }
        else
        {
            _yVelocity += Gravity * Time.deltaTime;

            // 점프하지 않고 떨어지는 중이면 1단 점프를 쓴 것으로 친다. (낙하 중에는 한 번만 점프)
            if (_characterController.isGrounded == false && _jumpCount == 0)
            {
                _jumpCount = 1;
            }
        }

        // 4. 점프키를 누르면 && 점프 횟수가 남아 있고 && 스태미나가 있으면 점프한다. (공중에서 한 번 더 = 2단 점프)
        if (Input.GetButtonDown("Jump") && _jumpCount < _maxJumpCount && _stamina.TryUse(_jumpStaminaCost))
        {
            _yVelocity = _jumpPower;
            _jumpCount++;
        }

        // 5. 왼쪽 Shift를 누르고 움직이는 중이고 스태미나가 있으면 질주한다.
        bool isMoving = direction != Vector3.zero;
        bool isRunning = Input.GetKey(KeyCode.LeftShift) && isMoving && _stamina.CanUse;
        if (isRunning)
        {
            _stamina.UseOverTime(_runStaminaPerSecond);
        }

        float speed = isRunning ? _runSpeed : _moveSpeed;

        // 6. 속도를 구한다.
        Vector3 velocity = direction * speed;
        velocity.y = _yVelocity;

        // 7. 속도(방향과 속력)에 따라 이동한다.
        CollisionFlags collision = _characterController.Move(velocity * Time.deltaTime);

        // 8. 점프 중에 천장에 머리를 부딪히면 바로 떨어지기 시작한다.
        if ((collision & CollisionFlags.Above) != 0 && _yVelocity > 0f)
        {
            _yVelocity = 0f;
        }
    }
}
