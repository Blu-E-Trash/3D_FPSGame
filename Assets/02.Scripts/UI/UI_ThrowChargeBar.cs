using UnityEngine;
using UnityEngine.UI;

// 폭탄을 던지기 위해 힘을 모으는 동안, 얼마나 모았는지 화면 우측 하단 슬라이더로 표시한다.
public class UI_ThrowChargeBar : MonoBehaviour
{
    [SerializeField] private PlayerBombFire _bombFire;
    [SerializeField] private Slider _slider;
    [SerializeField] private Image _fillImage;
    [SerializeField] private CanvasGroup _canvasGroup; // 힘을 모으지 않을 때 숨기는 용도

    [SerializeField] private Color _minChargeColor = new Color(1f, 0.85f, 0.2f); // 조금 모았을 때 (노랑)
    [SerializeField] private Color _maxChargeColor = new Color(1f, 0.25f, 0.2f); // 끝까지 모았을 때 (빨강)

    private void Awake()
    {
        if (_bombFire == null)
        {
            GameLogger.LogError("UI_ThrowChargeBar: BombFire is not assigned.");
        }
    }

    private void Update()
    {
        if (_bombFire == null)
            return;

        // 1. 힘을 모으는 중일 때만 보이게 한다.
        bool isCharging = _bombFire.IsCharging;
        _canvasGroup.alpha = isCharging ? 1f : 0f;

        if (isCharging == false)
            return;

        // 2. 모은 비율(0 ~ 1)을 슬라이더에 표시하고, 많이 모을수록 빨갛게 바꾼다.
        float ratio = _bombFire.ChargeRatio;
        _slider.value = ratio;
        _fillImage.color = Color.Lerp(_minChargeColor, _maxChargeColor, ratio);
    }
}
