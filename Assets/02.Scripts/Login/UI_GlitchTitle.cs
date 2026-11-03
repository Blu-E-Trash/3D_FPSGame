using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 제목: 빨강/파랑 잔상이 살짝 어긋난 채로 떨리고(색 번짐), 끝에 커서가 깜빡인다.
// 글자는 maxVisibleCharacters로 한 글자씩 보여 주므로 자리가 흔들리지 않는다. (타자기 효과는 UI_LoginIntro가 부른다)
public class UI_GlitchTitle : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _main;
    [SerializeField] private TextMeshProUGUI _red;   // 왼쪽으로 어긋난 빨간 잔상
    [SerializeField] private TextMeshProUGUI _cyan;  // 오른쪽으로 어긋난 파란 잔상
    [SerializeField] private RectTransform _cursor;
    [SerializeField] private float _offset = 4f;      // 평소 잔상 거리
    [SerializeField] private float _glitchOffset = 14f;

    private float _glitchTimer;
    private int _visible = int.MaxValue;
    private float _jitterTimer;
    private Vector2 _jitter;

    public int Visible
    {
        get => _visible;
        set
        {
            _visible = value;
            _main.maxVisibleCharacters = value;
            _red.maxVisibleCharacters = value;
            _cyan.maxVisibleCharacters = value;
        }
    }

    public int Length => _main.text.Length;

    public void Glitch(float duration)
    {
        _glitchTimer = Mathf.Max(_glitchTimer, duration);
    }

    private void LateUpdate()
    {
        float dt = Time.unscaledDeltaTime;
        _glitchTimer -= dt;
        bool glitching = _glitchTimer > 0f;

        // 1. 잔상 떨림: 가끔씩 아주 조금 위치가 바뀐다.
        _jitterTimer -= dt;
        if (_jitterTimer <= 0f)
        {
            _jitterTimer = Random.Range(0.05f, 0.25f);
            _jitter = Random.insideUnitCircle * 1.5f;
        }

        float off = glitching ? _glitchOffset : _offset;
        _red.rectTransform.anchoredPosition = new Vector2(-off, 0f) + _jitter;
        _cyan.rectTransform.anchoredPosition = new Vector2(off, 0f) - _jitter;
        _main.rectTransform.anchoredPosition = glitching ? new Vector2(Random.Range(-10f, 10f), 0f) : Vector2.zero;
        _main.rectTransform.localScale = glitching ? new Vector3(1.04f, 1f, 1f) : Vector3.one;

        // 2. 커서: 마지막으로 보이는 글자 바로 뒤에 붙어서 깜빡인다.
        if (_cursor != null)
        {
            _main.ForceMeshUpdate();
            TMP_TextInfo info = _main.textInfo;
            int count = Mathf.Min(_visible, info.characterCount);
            float x;
            if (count <= 0)
            {
                x = info.characterCount > 0 ? info.characterInfo[0].origin : 0f;
            }
            else
            {
                x = info.characterInfo[count - 1].xAdvance;
            }
            float mid = info.lineCount > 0 ? (info.lineInfo[0].ascender + info.lineInfo[0].baseline) * 0.5f : 0f;
            _cursor.anchoredPosition = new Vector2(x + 14f, mid);
            _cursor.gameObject.SetActive((int)(Time.unscaledTime * 2f) % 2 == 0);
        }
    }
}
