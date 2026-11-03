using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 단말기 입력창: 선택하면 테두리가 밝아지며 삑, 글자를 칠 때마다 짧은 키 소리가 난다.
[RequireComponent(typeof(TMP_InputField))]
public class UI_TerminalInputField : MonoBehaviour
{
    [SerializeField] private Image _border;   // 테두리 이미지
    [SerializeField] private Color _normal = new Color(0.61f, 0.91f, 0.65f, 0.35f);
    [SerializeField] private Color _focused = new Color(0.79f, 1f, 0.82f, 1f);

    private TMP_InputField _input;

    private void Awake()
    {
        _input = GetComponent<TMP_InputField>();
        _input.onSelect.AddListener(_ => SetFocus(true));
        _input.onDeselect.AddListener(_ => SetFocus(false));
        _input.onValueChanged.AddListener(_ => LoginSfx.Instance?.Type());
        SetFocus(false);
    }

    private void SetFocus(bool on)
    {
        if (_border != null)
        {
            _border.color = on ? _focused : _normal;
        }
        if (on) LoginSfx.Instance?.Blip();
    }
}
