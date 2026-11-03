using System.Collections;
using UnityEngine;

// 로그인 ↔ 회원가입 화면 전환 (화면 연출만, 로그인/가입 기능은 없다)
// 회원가입/돌아가기 버튼의 On Click에 ShowSignup / ShowLogin을 연결한다.
public class UI_LoginViewSwitch : MonoBehaviour
{
    [SerializeField] private RectTransform _frame;   // 단말기 테두리 (높이가 바뀐다)
    [SerializeField] private CanvasGroup _login;
    [SerializeField] private CanvasGroup _signup;
    [SerializeField] private float _loginHeight = 330f;
    [SerializeField] private float _signupHeight = 494f;

    private Coroutine _running;

    private void Awake()
    {
        Show(_login, true);
        Show(_signup, false);
    }

    public void ShowSignup() => Switch(_login, _signup, _signupHeight);
    public void ShowLogin() => Switch(_signup, _login, _loginHeight);

    private void Switch(CanvasGroup from, CanvasGroup to, float height)
    {
        if (_running != null) StopCoroutine(_running);
        _running = StartCoroutine(SwitchRoutine(from, to, height));
    }

    private IEnumerator SwitchRoutine(CanvasGroup from, CanvasGroup to, float height)
    {
        UI_CCTVFeed.Instance?.Glitch(0.18f);

        // 1. 지금 화면을 빠르게 지운다.
        from.interactable = false;
        from.blocksRaycasts = false;
        float a = from.alpha;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.12f)
        {
            from.alpha = Mathf.Lerp(a, 0f, t);
            yield return null;
        }
        Show(from, false);

        // 2. 테두리 높이를 바꾼다. (살짝 지나쳤다가 돌아오는 느낌)
        float h0 = _frame.sizeDelta.y;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.28f)
        {
            float e = EaseOutBack(Mathf.Clamp01(t));
            _frame.sizeDelta = new Vector2(_frame.sizeDelta.x, Mathf.LerpUnclamped(h0, height, e));
            yield return null;
        }
        _frame.sizeDelta = new Vector2(_frame.sizeDelta.x, height);

        // 3. 새 화면을 보여 준다.
        to.gameObject.SetActive(true);
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.18f)
        {
            to.alpha = Mathf.Clamp01(t);
            yield return null;
        }
        Show(to, true);
        LoginSfx.Instance?.Type();
        _running = null;
    }

    private static void Show(CanvasGroup g, bool on)
    {
        g.gameObject.SetActive(on);
        g.alpha = on ? 1f : 0f;
        g.interactable = on;
        g.blocksRaycasts = on;
    }

    private static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}
