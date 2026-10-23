using UnityEngine;
using UnityEngine.UI;

// 플레이어 체력을 화면 좌측 하단(스태미나 바 위) 슬라이더로 표시한다.
public class UI_PlayerHealthBar : MonoBehaviour
{
    [SerializeField] private PlayerHealth _health;
    [SerializeField] private Slider _slider;

    private void Awake()
    {
        if (_health == null)
        {
            GameLogger.LogError("UI_PlayerHealthBar: Health is not assigned.");
        }
    }

    private void Update()
    {
        if (_health == null)
            return;

        // 체력 비율(0 ~ 1)을 슬라이더에 표시한다.
        _slider.value = _health.Ratio;
    }
}
