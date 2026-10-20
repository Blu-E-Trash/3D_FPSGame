using UnityEngine;

// 질주와 점프에 쓰는 스태미나를 관리한다.
public class PlayerStamina : MonoBehaviour
{
    [SerializeField] private float _maxStamina = 100f;
    [SerializeField] private float _regenPerSecond = 20f;      // 초당 회복량
    [SerializeField] private float _regenDelay = 1f;           // 마지막으로 쓴 뒤 회복이 시작되기까지 걸리는 시간(초)
    [SerializeField] private float _exhaustRecoverRatio = 0.3f; // 바닥난 뒤 이 비율까지 회복해야 다시 쓸 수 있다.

    private float _currentStamina;
    private float _lastUseTime = float.NegativeInfinity;
    private bool _isExhausted = false; // 스태미나가 바닥나서 지친 상태

    public float CurrentStamina => _currentStamina;
    public float MaxStamina => _maxStamina;
    public float Ratio => _currentStamina / _maxStamina; // 0 ~ 1, 스태미나 UI 바에 쓴다.
    public bool IsExhausted => _isExhausted;
    public bool CanUse => _isExhausted == false && _currentStamina > 0f;

    private void Awake()
    {
        _currentStamina = _maxStamina;
    }

    private void Update()
    {
        // 1. 마지막으로 쓴 뒤 일정 시간이 지나야 회복을 시작한다.
        if (Time.time - _lastUseTime < _regenDelay)
            return;

        // 2. 초당 회복량만큼 회복한다.
        _currentStamina = Mathf.Min(_currentStamina + _regenPerSecond * Time.deltaTime, _maxStamina);

        // 3. 지친 상태라면 일정 비율 이상 회복했을 때 풀어준다.
        if (_isExhausted && Ratio >= _exhaustRecoverRatio)
        {
            _isExhausted = false;
        }
    }

    // 정해진 양을 한 번에 쓴다. 부족하면 쓰지 않고 false를 돌려준다. (점프)
    public bool TryUse(float amount)
    {
        if (CanUse == false || _currentStamina < amount)
            return false;

        Use(amount);
        return true;
    }

    // 매 프레임 조금씩 쓴다. 남은 양보다 많으면 0까지만 쓴다. (질주)
    public void UseOverTime(float amountPerSecond)
    {
        Use(amountPerSecond * Time.deltaTime);
    }

    private void Use(float amount)
    {
        _currentStamina = Mathf.Max(_currentStamina - amount, 0f);
        _lastUseTime = Time.time;

        // 바닥나면 지친 상태가 되어 어느 정도 회복할 때까지 질주/점프를 못 한다.
        if (_currentStamina <= 0f)
        {
            _isExhausted = true;
        }
    }
}
