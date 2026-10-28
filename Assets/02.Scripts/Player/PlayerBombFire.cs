using UnityEngine;

// 마우스 오른쪽 버튼을 누르고 있는 동안 힘을 모으고, 떼면 폭탄을 던지고 싶다.
// 오래 누를수록 멀리 날아가고, 폭탄은 가지고 있는 10개까지만 던질 수 있다. (자동으로 채워지지 않는다)
public class PlayerBombFire : MonoBehaviour
{
    private const string IsChargingBombParameter = "IsChargingBomb"; // 힘을 모으는 동안 오른팔을 뒤로 젖힌다. (RightArm 레이어)
    private const string ThrowTrigger = "Throw";                     // 던지는 동작 트리거 (RightArm 레이어)

    [SerializeField]
    private BombPool _bombPool;
    [SerializeField]
    private Transform _firePoint;

    [Header("폭탄 개수")]
    [SerializeField]
    private int _maxBombCount = 10;     // 가지고 있을 수 있는 최대 폭탄 수
    private int _currentBombCount;      // 남은 폭탄 수

    [Header("힘 모으기")]
    [SerializeField]
    private float _minThrowForce = 5f;  // 바로 떼었을 때 던지는 힘
    [SerializeField]
    private float _maxThrowForce = 25f; // 끝까지 모았을 때 던지는 힘
    [SerializeField]
    private float _maxChargeTime = 1.5f; // 최대 힘까지 모으는 데 걸리는 시간(초)
    private float _chargeTimer = 0f;    // 지금까지 모은 시간
    private bool _isCharging = false;   // 힘을 모으는 중인지

    [Header("던진 뒤 텀")]
    [SerializeField]
    private float _throwCooldown = 0.5f; // 던진 뒤 다음 폭탄을 모으기 시작할 수 있을 때까지의 시간(초)
    private float _cooldownTimer = 0f;   // 다음 폭탄까지 남은 시간

    public int CurrentBombCount => _currentBombCount;
    public int MaxBombCount => _maxBombCount;
    public float ChargeRatio => _chargeTimer / _maxChargeTime; // 0 ~ 1, 힘 게이지 UI에 쓴다.
    public bool IsCharging => _isCharging;

    private Animator _animator; // 군인 모델의 애니메이터

    private void Awake()
    {
        // 처음에는 가득 가진 상태로 시작한다.
        _currentBombCount = _maxBombCount;
        _animator = GetComponentInChildren<Animator>(); // 자식인 Model에 붙어 있다.
    }

    private void Update()
    {
        // 게임 중이 아니면(준비, 게임오버) 아무것도 하지 않는다.
        if (GameManager.Instance.State != GameState.Playing) return;

        // 0. 던진 뒤 텀(쿨타임)을 줄인다.
        _cooldownTimer -= Time.deltaTime;

        // 1. 오른쪽 버튼을 누르는 순간 && 텀이 끝났고 && 폭탄이 남아 있으면 힘 모으기를 시작한다.
        if (Input.GetButtonDown("Fire2") && _cooldownTimer <= 0f && _currentBombCount > 0)
        {
            _isCharging = true;
            _chargeTimer = 0f;
            // 오른팔만 들어가는 RightArm 레이어(아바타 마스크)에서 팔을 뒤로 젖힌다.
            // 그래서 왼손은 총을 그대로 들고, 다리는 걷기/달리기 동작을 그대로 한다.
            _animator.SetBool(IsChargingBombParameter, true);
        }

        // 2. 누르고 있는 동안 힘을 모은다. (최대 시간까지만)
        if (_isCharging && Input.GetButton("Fire2"))
        {
            _chargeTimer = Mathf.Min(_chargeTimer + Time.deltaTime, _maxChargeTime);
        }

        // 3. 버튼을 떼면 모은 힘으로 던진다.
        if (_isCharging && Input.GetButtonUp("Fire2"))
        {
            float throwForce = Mathf.Lerp(_minThrowForce, _maxThrowForce, ChargeRatio);
            Throw(throwForce);
            _animator.SetBool(IsChargingBombParameter, false);
            _animator.SetTrigger(ThrowTrigger);

            _isCharging = false;
            _chargeTimer = 0f;
            _cooldownTimer = _throwCooldown; // 던졌으니 잠깐 텀을 둔다.
        }
    }

    private void Throw(float throwForce)
    {
        if (_bombPool == null || _firePoint == null)
            return;

        // 4. 풀에서 폭탄을 꺼내 발사 위치에 놓는다.
        Bomb bomb = _bombPool.Get(_firePoint.position, _firePoint.rotation);

        // 5. 시선 방향으로 힘을 가해 던진다.
        bomb.Rigidbody.AddForce(Camera.main.transform.forward * throwForce, ForceMode.Impulse);

        _currentBombCount--;
        GameLogger.Log($"폭탄 던짐 (힘: {throwForce:F1}) / 남은 폭탄: {_currentBombCount} / {_maxBombCount}");
    }

    // 탄약 상자 등을 주웠을 때 폭탄을 채운다. (최대 개수를 넘지 않는다)
    public void AddBomb(int amount)
    {
        _currentBombCount = Mathf.Min(_currentBombCount + amount, _maxBombCount);
    }
}
