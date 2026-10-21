using System;
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

    [Header("점프 게임필")]
    [SerializeField] private float _fallMultiplier = 2f;       // 내려올 때 중력 배율 (클수록 묵직하게 떨어진다)
    [SerializeField] private float _lowJumpMultiplier = 2.5f;  // 점프키를 일찍 떼면 올라가는 중에 곱할 중력 배율 (가변 점프 높이)
    [SerializeField] private float _coyoteTime = 0.1f;         // 땅에서 떨어진 직후에도 땅 점프를 허용하는 시간
    [SerializeField] private float _jumpBufferTime = 0.1f;     // 착지 직전에 누른 점프를 기억하는 시간

    [Header("참조")]
    [SerializeField] private CharacterController _characterController;
    [SerializeField] private PlayerStamina _stamina;

    private float _coyoteTimer = 0f;     // 코요테 타임 남은 시간
    private float _jumpBufferTimer = 0f; // 점프 버퍼 남은 시간
    private bool _wasGrounded = true;    // 지난 프레임에 땅에 있었는지 (착지 판정용)

    // 착지한 순간 호출된다. 인자는 착지 직전의 낙하 속도(양수). 카메라 연출 등에 쓴다.
    public event Action<float> Landed;

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

        bool isGrounded = _characterController.isGrounded;

        // 3. 코요테 타임: 땅에 있으면 채우고, 공중이면 줄인다.
        if (isGrounded)
        {
            _coyoteTimer = _coyoteTime;
        }
        else
        {
            _coyoteTimer -= Time.deltaTime;
        }

        // 4. 땅에 서 있으면 낙하 속도와 점프 횟수를 초기화하고, 공중이면 중력을 누적한다.
        if (isGrounded && _yVelocity < 0f)
        {
            _yVelocity = GroundedYVelocity;
            _jumpCount = 0;
        }
        else
        {
            _yVelocity += GetGravity() * Time.deltaTime;

            // 점프하지 않고 떨어졌는데 코요테 타임도 끝났으면 1단 점프를 쓴 것으로 친다. (낙하 중에는 한 번만 점프)
            if (isGrounded == false && _jumpCount == 0 && _coyoteTimer <= 0f)
            {
                _jumpCount = 1;
            }
        }

        // 5. 점프 버퍼: 점프키를 누르면 잠깐 기억해 둔다.
        if (Input.GetButtonDown("Jump"))
        {
            _jumpBufferTimer = _jumpBufferTime;
        }
        else
        {
            _jumpBufferTimer -= Time.deltaTime;
        }

        // 6. 점프한다.
        bool canGroundJump = _coyoteTimer > 0f && _jumpCount == 0;
        bool canAirJump = isGrounded == false && _jumpCount > 0 && _jumpCount < _maxJumpCount;

        // 6-1. 땅 점프: 땅이거나 코요테 타임 안이면, 버퍼에 남아 있는 입력으로도 뛴다.
        if (_jumpBufferTimer > 0f && canGroundJump && _stamina.TryUse(_jumpStaminaCost))
        {
            Jump();
        }
        // 6-2. 공중 점프(2단 점프): 공중에서 누른 그 순간에만 뛴다.
        else if (Input.GetButtonDown("Jump") && canAirJump && _stamina.TryUse(_jumpStaminaCost))
        {
            Jump();
        }

        // 7. 왼쪽 Shift를 누르고 움직이는 중이고 스태미나가 있으면 질주한다.
        bool isMoving = direction != Vector3.zero;
        bool isRunning = Input.GetKey(KeyCode.LeftShift) && isMoving && _stamina.CanUse;
        if (isRunning)
        {
            _stamina.UseOverTime(_runStaminaPerSecond);
        }

        float speed = isRunning ? _runSpeed : _moveSpeed;

        // 8. 속도를 구한다.
        Vector3 velocity = direction * speed;
        velocity.y = _yVelocity;

        // 9. 속도(방향과 속력)에 따라 이동한다.
        CollisionFlags collision = _characterController.Move(velocity * Time.deltaTime);

        // 10. 점프 중에 천장에 머리를 부딪히면 바로 떨어지기 시작한다.
        if ((collision & CollisionFlags.Above) != 0 && _yVelocity > 0f)
        {
            _yVelocity = 0f;
        }

        // 11. 공중에 있다가 이번 프레임에 땅에 닿았으면 착지를 알린다.
        bool isGroundedNow = _characterController.isGrounded;
        if (isGroundedNow && _wasGrounded == false)
        {
            Landed?.Invoke(-velocity.y);
        }
        _wasGrounded = isGroundedNow;
    }

    private void Jump()
    {
        _yVelocity = _jumpPower;
        _jumpCount++;
        _coyoteTimer = 0f;     // 같은 코요테 타임으로 두 번 뛰지 않게 한다.
        _jumpBufferTimer = 0f; // 쓴 입력은 지운다.
    }

    // 지금 상황에 맞는 중력을 돌려준다.
    private float GetGravity()
    {
        // 내려오는 중: 중력을 키워서 빠르고 묵직하게 떨어진다.
        if (_yVelocity < 0f)
            return Gravity * _fallMultiplier;

        // 올라가는 중인데 점프키를 뗐다: 중력을 키워서 낮게 뛴다. (가변 점프 높이)
        if (_yVelocity > 0f && Input.GetButton("Jump") == false)
            return Gravity * _lowJumpMultiplier;

        return Gravity;
    }
}
