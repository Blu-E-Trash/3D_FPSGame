using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 감시 카메라 화면 효과를 움직인다.
// - 노이즈가 매 프레임 바뀌고, 밝은 띠가 아래에서 위로 흐른다.
// - ● REC 깜빡임, 실시간 타임스탬프, 신호 세기 흔들림
// - 가끔(또는 버튼을 누르면) 화면이 가로 띠로 찢어지며 옆으로 튄다. (Glitch)
public class UI_CCTVFeed : SingletonBehaviour<UI_CCTVFeed>
{
    [Header("화면")]
    [SerializeField] private RawImage _feed;      // 카메라가 그린 화면 (Render Texture)
    [SerializeField] private RawImage[] _slices;  // 찢어질 때 쓰는 띠 (같은 Render Texture)
    [SerializeField] private RawImage _noise;
    [SerializeField] private RectTransform _rollBar;

    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI _rec;
    [SerializeField] private TextMeshProUGUI _timestamp;
    [SerializeField] private TextMeshProUGUI _signal;
    [SerializeField] private UI_GlitchTitle _title;

    [Header("자동 글리치 간격(초)")]
    [SerializeField] private Vector2 _glitchInterval = new Vector2(3f, 7f);
    [SerializeField] private float _rollSpeed = 140f;

    private float _nextGlitch;
    private float _glitchTimer;
    private int _lastSecond = -1;

    private void Start()
    {
        _nextGlitch = UnityEngine.Random.Range(_glitchInterval.x, _glitchInterval.y);
        SetSlices(false);
    }

    private void Update()
    {
        float t = Time.unscaledTime;
        float dt = Time.unscaledDeltaTime;

        // 1. 노이즈: 텍스처를 무작위 위치로 옮겨서 매 프레임 다른 잡음처럼 보이게 한다.
        if (_noise != null)
        {
            Rect r = _noise.uvRect;
            _noise.uvRect = new Rect(UnityEngine.Random.value, UnityEngine.Random.value, r.width, r.height);
        }

        // 2. 밝은 띠가 아래에서 위로 흐른다.
        if (_rollBar != null)
        {
            float h = ((RectTransform)_rollBar.parent).rect.height;
            _rollBar.anchoredPosition = new Vector2(0f, Mathf.Repeat(t * _rollSpeed, h + 400f) - 200f);
        }

        // 3. HUD
        if (_rec != null) _rec.alpha = (int)(t * 1.6f) % 2 == 0 ? 1f : 0.15f;

        DateTime now = DateTime.Now;
        if (_timestamp != null && now.Second != _lastSecond)
        {
            _lastSecond = now.Second;
            _timestamp.text = now.ToString("yyyy-MM-dd  HH:mm:ss");
            if (_signal != null) _signal.text = $"SIGNAL ▂▃▅▆ {UnityEngine.Random.Range(86, 97)}%";
        }

        // 4. 글리치: 정해진 간격마다 자동으로, 끝나면 띠를 숨긴다.
        _nextGlitch -= dt;
        if (_nextGlitch <= 0f)
        {
            Glitch(UnityEngine.Random.Range(0.12f, 0.22f));
            _nextGlitch = UnityEngine.Random.Range(_glitchInterval.x, _glitchInterval.y);
        }

        if (_glitchTimer > 0f)
        {
            _glitchTimer -= dt;
            if (_glitchTimer <= 0f)
            {
                SetSlices(false);
                if (_feed != null) _feed.rectTransform.anchoredPosition = Vector2.zero;
            }
            else if (UnityEngine.Random.value < 0.5f)
            {
                ShuffleSlices(); // 찢어진 동안에도 띠가 계속 바뀌며 떨린다.
            }
        }
    }

    // 화면을 잠깐 찢는다. 버튼 클릭, 화면 전환, 좀비 재등장 때도 부른다.
    public void Glitch(float duration)
    {
        _glitchTimer = Mathf.Max(_glitchTimer, duration);
        SetSlices(true);
        ShuffleSlices();
        if (_feed != null) _feed.rectTransform.anchoredPosition = new Vector2(UnityEngine.Random.Range(-8f, 8f), 0f);
        if (_title != null) _title.Glitch(duration);
        LoginSfx.Instance?.Glitch();
    }

    private void ShuffleSlices()
    {
        foreach (RawImage s in _slices)
        {
            float y = UnityEngine.Random.Range(0f, 0.95f);
            float h = UnityEngine.Random.Range(0.015f, 0.07f);
            RectTransform rt = s.rectTransform;
            rt.anchorMin = new Vector2(0f, y);
            rt.anchorMax = new Vector2(1f, y + h);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = new Vector2(UnityEngine.Random.Range(-70f, 70f), 0f);
            s.uvRect = new Rect(0f, y, 1f, h);
        }
    }

    private void SetSlices(bool on)
    {
        foreach (RawImage s in _slices) s.enabled = on;
    }
}
