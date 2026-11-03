using System.Collections;
using UnityEngine;

// 로그인 화면 인트로: 화면이 켜지고(깜빡) → 제목이 한 글자씩 찍히고 → 부제 → 입력창과 버튼이 차례로 올라온다.
// 아무 키나 마우스를 누르면 바로 끝까지 건너뛴다.
public class UI_LoginIntro : MonoBehaviour
{
    [SerializeField] private CanvasGroup _blackout;
    [SerializeField] private UI_GlitchTitle _title;
    [SerializeField] private CanvasGroup _subtitle;
    [SerializeField] private CanvasGroup _panel;        // 단말기 테두리
    [SerializeField] private CanvasGroup[] _items;      // 입력창, 버튼 줄 (위에서부터)
    [SerializeField] private float _typeInterval = 0.14f;
    [SerializeField] private float _itemRise = 24f;

    private bool _skip;

    private void Awake()
    {
        // 편집 중에는 화면이 보이도록 0으로 두고, 플레이를 시작하면 검게 덮는다.
        _blackout.alpha = 1f;
    }

    private IEnumerator Start()
    {
        // 0. 처음 상태: 모두 숨김
        _title.Visible = 0;
        _subtitle.alpha = 0f;
        _panel.alpha = 0f;
        Vector2[] home = new Vector2[_items.Length];
        for (int i = 0; i < _items.Length; i++)
        {
            RectTransform rt = (RectTransform)_items[i].transform;
            home[i] = rt.anchoredPosition;
            _items[i].alpha = 0f;
            _items[i].interactable = false;
        }
        _blackout.alpha = 1f;

        // 1. 전원이 들어오며 화면이 몇 번 깜빡인 뒤 밝아진다.
        LoginSfx.Instance?.Boot();
        yield return Wait(0.35f);
        float[] flicker = { 0.4f, 1f, 0.15f, 0.8f, 0f };
        foreach (float a in flicker)
        {
            _blackout.alpha = _skip ? 0f : a;
            yield return Wait(0.06f);
        }
        _blackout.alpha = 0f;
        UI_CCTVFeed.Instance?.Glitch(0.18f);
        yield return Wait(0.4f);

        // 2. 제목 타자기
        for (int i = 1; i <= _title.Length; i++)
        {
            _title.Visible = i;
            LoginSfx.Instance?.Type();
            yield return Wait(_typeInterval);
        }
        _title.Visible = int.MaxValue;
        _title.Glitch(0.15f);
        LoginSfx.Instance?.Glitch();
        yield return Wait(0.25f);

        // 3. 부제와 단말기 테두리
        yield return Fade(_subtitle, 0.4f);
        yield return Fade(_panel, 0.25f);

        // 4. 입력창과 버튼이 차례로 올라온다.
        for (int i = 0; i < _items.Length; i++)
        {
            StartCoroutine(Rise(_items[i], home[i]));
            LoginSfx.Instance?.Type();
            yield return Wait(0.1f);
        }
    }

    private void Update()
    {
        if (!_skip && (Input.anyKeyDown || Input.GetMouseButtonDown(0)))
        {
            _skip = true;
        }
    }

    private IEnumerator Rise(CanvasGroup g, Vector2 home)
    {
        RectTransform rt = (RectTransform)g.transform;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / (_skip ? 0.05f : 0.35f);
            float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f); // 끝에서 부드럽게 멈춘다.
            g.alpha = e;
            rt.anchoredPosition = home - new Vector2(0f, _itemRise * (1f - e));
            yield return null;
        }
        g.interactable = true;
    }

    private IEnumerator Fade(CanvasGroup g, float duration)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / (_skip ? 0.02f : duration);
            g.alpha = Mathf.Clamp01(t);
            yield return null;
        }
    }

    private IEnumerator Wait(float seconds)
    {
        float t = 0f;
        while (t < seconds && !_skip)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }
}
