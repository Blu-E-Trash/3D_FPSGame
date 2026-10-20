using UnityEngine;
using UnityEngine.UI;

// 플레이어 스태미나를 화면 좌측 하단 슬라이더로 표시한다.
public class UI_StaminaBar : MonoBehaviour
{
    [SerializeField] private PlayerStamina _stamina;
    [SerializeField] private Slider _slider;
    [SerializeField] private Image _fillImage;

    [SerializeField] private Color _normalColor = new Color(0.3f, 0.85f, 0.4f);
    [SerializeField] private Color _exhaustedColor = new Color(0.55f, 0.55f, 0.55f); // 지친 상태(바닥난 뒤 회복 중)

    private void Awake()
    {
        if (_stamina == null)
        {
            GameLogger.LogError("UI_StaminaBar: Stamina is not assigned.");
        }
    }

    private void Update()
    {
        if (_stamina == null)
            return;

        // 1. 스태미나 비율(0 ~ 1)을 슬라이더에 표시한다.
        _slider.value = _stamina.Ratio;

        // 2. 지친 상태면 회색으로 바꿔서 질주/점프를 못 한다는 걸 보여준다.
        _fillImage.color = _stamina.IsExhausted ? _exhaustedColor : _normalColor;
    }
}
