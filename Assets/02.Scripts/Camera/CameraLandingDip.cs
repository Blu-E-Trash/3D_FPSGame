using UnityEngine;

// 플레이어가 착지하면 카메라를 살짝 아래로 꺼졌다가 스프링처럼 돌아오게 한다.
// 카메라가 따라가는 위치(CameraPotiotion)에 붙인다.
public class CameraLandingDip : MonoBehaviour
{
    [SerializeField] private PlayerMove _playerMove;

    [Header("착지 세기")]
    [SerializeField] private float _minFallSpeed = 4f;   // 이보다 느리게 떨어지면 반응하지 않는다. (계단 내려가기 등)
    [SerializeField] private float _maxFallSpeed = 25f;  // 이 속도 이상이면 가장 세게 꺼진다.
    [SerializeField] private float _minDipSpeed = 0.6f;  // 약한 착지에서 아래로 꺼지는 속도
    [SerializeField] private float _maxDipSpeed = 2.5f;  // 강한 착지에서 아래로 꺼지는 속도

    [Header("스프링")]
    [SerializeField] private float _stiffness = 200f;    // 클수록 빨리 돌아온다.
    [SerializeField] private float _damping = 22f;       // 클수록 덜 출렁인다.

    private Vector3 _baseLocalPosition;
    private float _offset;          // 원래 위치에서 아래로 내려간 양
    private float _offsetVelocity;  // 오프셋이 변하는 속도

    private void Awake()
    {
        _baseLocalPosition = transform.localPosition;

        if (_playerMove == null)
        {
            GameLogger.LogError("CameraLandingDip: PlayerMove is not assigned.");
        }
    }

    private void OnEnable()
    {
        if (_playerMove != null)
        {
            _playerMove.Landed += HandleLanded;
        }
    }

    private void OnDisable()
    {
        if (_playerMove != null)
        {
            _playerMove.Landed -= HandleLanded;
        }
    }

    private void Update()
    {
        // 1. 스프링: 원래 위치로 당기는 힘과 출렁임을 줄이는 힘을 더한다.
        float force = -_stiffness * _offset - _damping * _offsetVelocity;
        _offsetVelocity += force * Time.deltaTime;
        _offset += _offsetVelocity * Time.deltaTime;

        // 2. 오프셋을 위치에 적용한다. CameraFollow가 이 위치를 따라간다.
        transform.localPosition = _baseLocalPosition + Vector3.up * _offset;
    }

    // 착지하면 낙하 속도에 비례해서 아래로 꺼지는 속도를 준다.
    private void HandleLanded(float fallSpeed)
    {
        if (fallSpeed < _minFallSpeed)
            return;

        float t = Mathf.InverseLerp(_minFallSpeed, _maxFallSpeed, fallSpeed);
        _offsetVelocity -= Mathf.Lerp(_minDipSpeed, _maxDipSpeed, t);
    }
}
