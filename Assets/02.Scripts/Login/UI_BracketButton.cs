using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// [ 로그인 ] 모양 버튼의 반응
// - 마우스를 올리면: 왼쪽 괄호가 오른쪽으로 펼쳐지며 버튼이 녹색으로 채워지고, 글자가 어두워지며 삑 소리
// - 누르면: 납작하게 눌리고, 떼면 살짝 튀어 오른다. (스프링)
// - 클릭하면: 딸깍 소리와 함께 화면이 잠깐 찢어진다.
[RequireComponent(typeof(Button))]
public class UI_BracketButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private RectTransform _visual;     // 크기가 바뀌는 부분 (클릭 영역은 그대로 둬서 가장자리 떨림을 막는다)
    [SerializeField] private RectTransform _fill;       // 채워지는 녹색 판 (피벗 왼쪽)
    [SerializeField] private RectTransform _left;       // 왼쪽 괄호
    [SerializeField] private RectTransform _right;      // 오른쪽 괄호
    [SerializeField] private CanvasGroup _rightGroup;
    [SerializeField] private TextMeshProUGUI _label;
    [SerializeField] private Color _labelNormal = new Color(0.79f, 1f, 0.82f);
    [SerializeField] private Color _labelHover = new Color(0.02f, 0.08f, 0.04f);
    [SerializeField] private float _bracketInset = 10f;

    [Header("스프링")]
    [SerializeField] private float _stiffness = 420f;
    [SerializeField] private float _damping = 18f;

    private bool _hover;
    private bool _pressed;
    private float _h;       // 0(평소) ~ 1(마우스 올림)
    private float _hVel;
    private Vector2 _scale = Vector2.one;
    private Vector2 _scaleVel;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    public void OnPointerEnter(PointerEventData e) { _hover = true; LoginSfx.Instance?.Blip(); }
    public void OnPointerExit(PointerEventData e) { _hover = false; _pressed = false; }
    public void OnSelect(BaseEventData e) { _hover = true; }
    public void OnDeselect(BaseEventData e) { _hover = false; }
    public void OnPointerDown(PointerEventData e) { _pressed = true; }

    public void OnPointerUp(PointerEventData e)
    {
        if (_pressed) _scaleVel += new Vector2(2.5f, 4f); // 떼는 순간 위로 튕겨 오른다.
        _pressed = false;
    }

    private void OnClick()
    {
        LoginSfx.Instance?.Click();
        UI_CCTVFeed.Instance?.Glitch(0.12f);
    }

    private void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);

        // 1. 펼쳐짐 정도 (스프링)
        float target = _hover ? 1f : 0f;
        _hVel += (target - _h) * _stiffness * dt;
        _hVel *= Mathf.Exp(-_damping * dt);
        _h += _hVel * dt;

        // 2. 눌림 크기 (스프링)
        Vector2 sTarget = _pressed ? new Vector2(0.95f, 0.88f) : (_hover ? new Vector2(1.03f, 1.03f) : Vector2.one);
        _scaleVel += (sTarget - _scale) * _stiffness * dt;
        _scaleVel *= Mathf.Exp(-_damping * dt);
        _scale += _scaleVel * dt;
        _visual.localScale = new Vector3(_scale.x, _scale.y, 1f);

        // 3. 모양 적용
        float h = Mathf.Clamp01(_h);
        _fill.localScale = new Vector3(Mathf.Max(0f, _h), 1f, 1f);
        _left.anchoredPosition = new Vector2(Mathf.Lerp(_bracketInset, 0f, h), 0f);
        _right.anchoredPosition = new Vector2(Mathf.Lerp(-_bracketInset, 0f, h), 0f);
        _rightGroup.alpha = 1f - h;
        _label.color = Color.Lerp(_labelNormal, _labelHover, h);
    }
}
