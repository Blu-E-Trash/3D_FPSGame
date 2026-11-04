using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 로딩 화면의 진행 표시: 진행 바, 퍼센트, "불러오는 중..." 글자, 진행 로그 세 줄을 그린다.
// 목표 진행률은 SetTarget으로 받고, 화면에는 그 값을 부드럽게 따라가며 보여 준다.
// (로딩이 순식간에 끝나도 바가 한 번에 튀지 않도록)
public class UI_LoadingProgress : MonoBehaviour
{
    [Header("진행 바")]
    [SerializeField] private Image _fill;              // Image Type: Filled (Horizontal)
    [SerializeField] private RectTransform _head;      // 채워진 끝에 붙어 다니는 흰 선
    [SerializeField] private TextMeshProUGUI _percent;
    [SerializeField] private TextMeshProUGUI _label;

    [Header("진행 로그 (위에서부터, 세 배열의 길이가 같아야 한다)")]
    [SerializeField] private TextMeshProUGUI[] _logs;
    [SerializeField] private string[] _logLabels;      // 점선까지 포함한 앞부분
    [SerializeField] private float[] _logThresholds;   // 진행률이 이 값에 닿으면 그 줄이 OK가 된다.

    [Header("움직임")]
    [SerializeField] private float _fillSpeed = 0.6f;         // 화면 진행률이 1초에 따라갈 수 있는 최대 양
    [SerializeField] private float _dotInterval = 0.35f;      // "불러오는 중..." 점이 늘어나는 간격(초)
    [SerializeField] private float _completeHoldTime = 0.8f;  // 100%를 보여 준 뒤 머무는 시간(초)

    private const string CompleteText = "불러오기 완료";
    private const string OkStatus = "<color=#C9FFD1>OK</color>";
    private const string WaitStatus = "<color=#9BE8A666>WAIT</color>";
    private const string ActiveStatusCaretOn = "<color=#FFFFFF>{0}%</color>_";   // {0}: TMP SetText가 숫자를 넣는 자리
    private const string ActiveStatusCaretOff = "<color=#FFFFFF>{0}%</color> ";
    private const string PercentFormat = "{0}%";
    private const float PercentScale = 100f;
    private const float CaretBlinkRate = 2f;            // 진행 중인 줄 끝 커서가 1초에 바뀌는 횟수
    private const float StartGlitchDuration = 0.15f;
    private const float CompleteGlitchDuration = 0.25f;

    // 로그 줄 상태 캐시. 0 이상은 진행 중인 줄의 퍼센트다.
    private const int LogNotDrawn = int.MinValue;
    private const int LogDone = -1;
    private const int LogWaiting = -2;
    private const int LabelNotDrawn = -1;

    // 점 개수별 글자. 매 프레임 문자열을 새로 만들지 않도록 미리 만들어 둔다.
    private static readonly string[] LoadingTexts = { "불러오는 중", "불러오는 중.", "불러오는 중..", "불러오는 중..." };
    private static readonly Vector2 SpritePivot = new Vector2(0.5f, 0.5f);

    private float _target;
    private float _shown;
    private int _labelIndex = LabelNotDrawn;
    private int[] _logStates;
    private bool _isCaretOn;
    private string[] _activeFormatsCaretOn;
    private string[] _activeFormatsCaretOff;
    private Sprite _runtimeFillSprite;

    private bool IsShownFull => _shown >= 1f;

    // 목표 진행률(0~1)을 알려 준다. 뒤로 줄어들지는 않는다.
    public void SetTarget(float progress)
    {
        _target = Mathf.Clamp01(Mathf.Max(_target, progress));
    }

    // 진행률을 0으로 되돌리고 화면을 한 번 튀게 한다. (로딩 시작)
    public void Restart()
    {
        _target = 0f;
        _shown = 0f;
        RenderAll();
        PlayGlitch(StartGlitchDuration);
    }

    // 화면의 바가 100%까지 다 찰 때까지 기다린 뒤, 완료 연출을 보여 주고 잠깐 머문다.
    public IEnumerator WaitForComplete()
    {
        while (!IsShownFull)
        {
            yield return null;
        }

        PlayGlitch(CompleteGlitchDuration);
        if (LoginSfx.Instance != null)
        {
            LoginSfx.Instance.Click();
        }
        yield return new WaitForSecondsRealtime(_completeHoldTime);
    }

    private void Awake()
    {
        if (!HasValidSetup())
        {
            enabled = false;
            return;
        }

        PrepareLogs();
        EnsureFillSprite();
    }

    private void Start()
    {
        RenderAll();
    }

    private void Update()
    {
        float previous = _shown;
        _shown = Mathf.MoveTowards(_shown, _target, _fillSpeed * Time.unscaledDeltaTime);
        if (_shown != previous)
        {
            RenderBar();
        }

        RenderLabel();
        RenderLogs();
    }

    private void OnDestroy()
    {
        if (_runtimeFillSprite != null)
        {
            Destroy(_runtimeFillSprite);
        }
    }

    private bool HasValidSetup()
    {
        if (_fill == null || _head == null || _percent == null || _label == null)
        {
            Debug.LogError("[UI_LoadingProgress] 진행 바 참조(_fill, _head, _percent, _label)가 비어 있다.", this);
            return false;
        }

        if (_logs.Length != _logLabels.Length || _logs.Length != _logThresholds.Length)
        {
            Debug.LogError("[UI_LoadingProgress] _logs, _logLabels, _logThresholds의 길이가 같아야 한다.", this);
            return false;
        }

        foreach (TextMeshProUGUI log in _logs)
        {
            if (log == null)
            {
                Debug.LogError("[UI_LoadingProgress] _logs에 비어 있는 칸이 있다.", this);
                return false;
            }
        }
        return true;
    }

    // 줄마다 "앞부분 + 퍼센트 자리" 형식 문자열을 한 번만 만들어 둔다.
    private void PrepareLogs()
    {
        int count = _logs.Length;
        _logStates = new int[count];
        _activeFormatsCaretOn = new string[count];
        _activeFormatsCaretOff = new string[count];
        for (int i = 0; i < count; i++)
        {
            _logStates[i] = LogNotDrawn;
            _activeFormatsCaretOn[i] = _logLabels[i] + ActiveStatusCaretOn;
            _activeFormatsCaretOff[i] = _logLabels[i] + ActiveStatusCaretOff;
        }
    }

    // Image는 Sprite가 비어 있으면 Type(Filled)을 무시하고 항상 꽉 찬 사각형을 그린다.
    // 그래서 fillAmount가 먹히도록, Inspector에서 Sprite를 넣지 않았다면 흰색 Sprite를 만들어 넣는다.
    private void EnsureFillSprite()
    {
        if (_fill.sprite == null)
        {
            Texture2D white = Texture2D.whiteTexture;
            _runtimeFillSprite = Sprite.Create(white, new Rect(0f, 0f, white.width, white.height), SpritePivot);
            _fill.sprite = _runtimeFillSprite;
        }

        _fill.type = Image.Type.Filled;
        _fill.fillMethod = Image.FillMethod.Horizontal;
        _fill.fillOrigin = (int)Image.OriginHorizontal.Left;
    }

    private void RenderAll()
    {
        RenderBar();
        RenderLabel();
        RenderLogs();
    }

    // 진행 바, 끝의 흰 선, 퍼센트 (진행률이 바뀔 때만)
    private void RenderBar()
    {
        _fill.fillAmount = _shown;
        _head.anchorMin = new Vector2(_shown, 0f);
        _head.anchorMax = new Vector2(_shown, 1f);
        _head.anchoredPosition = Vector2.zero;

        bool isHeadVisible = _shown > 0f && !IsShownFull;
        if (_head.gameObject.activeSelf != isHeadVisible)
        {
            _head.gameObject.SetActive(isHeadVisible);
        }

        _percent.SetText(PercentFormat, Mathf.Floor(_shown * PercentScale));
    }

    // "불러오는 중..." 점 개수가 바뀔 때만 글자를 바꾼다.
    private void RenderLabel()
    {
        int index = IsShownFull
            ? LoadingTexts.Length
            : (int)(Time.unscaledTime / _dotInterval) % LoadingTexts.Length;
        if (index == _labelIndex)
        {
            return;
        }

        _labelIndex = index;
        _label.text = index == LoadingTexts.Length ? CompleteText : LoadingTexts[index];
    }

    // 줄 상태(WAIT / 진행 % / OK)나 커서가 바뀐 줄만 다시 쓴다. 새로 OK가 된 줄이 있으면 삑 소리.
    private void RenderLogs()
    {
        bool isCaretOn = (int)(Time.unscaledTime * CaretBlinkRate) % 2 == 0;
        bool hasCaretChanged = isCaretOn != _isCaretOn;
        _isCaretOn = isCaretOn;
        bool hasNewDone = false;

        for (int i = 0; i < _logs.Length; i++)
        {
            int state = GetLogState(i);
            int previous = _logStates[i];
            bool isActive = state >= 0;
            if (state == previous && !(isActive && hasCaretChanged))
            {
                continue;
            }

            if (state == LogDone && previous != LogNotDrawn)
            {
                hasNewDone = true;
            }
            _logStates[i] = state;

            if (state == LogDone)
            {
                _logs[i].text = _logLabels[i] + OkStatus;
            }
            else if (state == LogWaiting)
            {
                _logs[i].text = _logLabels[i] + WaitStatus;
            }
            else
            {
                string format = isCaretOn ? _activeFormatsCaretOn[i] : _activeFormatsCaretOff[i];
                _logs[i].SetText(format, state);
            }
        }

        if (hasNewDone && LoginSfx.Instance != null)
        {
            LoginSfx.Instance.Blip();
        }
    }

    private int GetLogState(int index)
    {
        float start = index == 0 ? 0f : _logThresholds[index - 1];
        float end = _logThresholds[index];
        if (_shown >= end)
        {
            return LogDone;
        }
        if (_shown < start)
        {
            return LogWaiting;
        }
        return Mathf.FloorToInt(Mathf.InverseLerp(start, end, _shown) * PercentScale);
    }

    private static void PlayGlitch(float duration)
    {
        if (UI_CCTVFeed.Instance != null)
        {
            UI_CCTVFeed.Instance.Glitch(duration);
        }
    }
}
