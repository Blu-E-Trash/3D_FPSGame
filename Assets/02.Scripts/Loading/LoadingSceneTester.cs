using System.Collections;
using TMPro;
using UnityEngine;

// [테스트 전용] 불러올 씬 없이 로딩 씬을 바로 Play했을 때 쓴다.
// 시작 키(기본 G)를 누르면 실제로 로딩되는 것처럼 진행률이 들쑥날쑥 오르는 가짜 로딩을 보여 준다.
// 처음에는 꺼져 있고, LoadingSceneController가 BeginTestMode를 불러야 켜진다. (실제 로딩 중에는 계속 꺼져 있다)
public class LoadingSceneTester : MonoBehaviour
{
    [SerializeField] private UI_LoadingProgress _progress;
    [SerializeField] private TextMeshProUGUI _hint;
    [SerializeField] private KeyCode _startKey = KeyCode.G;

    [Header("가짜 로딩")]
    [SerializeField] private Vector2 _stepRange = new Vector2(0.02f, 0.12f);   // 한 번에 오르는 양 (최소, 최대)
    [SerializeField] private Vector2 _delayRange = new Vector2(0.05f, 0.35f);  // 오르는 간격(초) (최소, 최대)
    [SerializeField] private float _stallChance = 0.15f;                       // 잠깐 멈칫할 확률
    [SerializeField] private float _stallTime = 0.7f;                          // 멈칫하는 시간(초)

    private const string ReadyHintFormat = "[TEST] {0} 키를 누르면 로딩을 시작한다";
    private const string RunningHint = "[TEST] 가짜 로딩 진행 중...";
    private const string DoneHintFormat = "[TEST] 로딩 완료 · {0} 키: 다시 테스트";

    private bool _hasValidSetup;
    private bool _isLoading;

    // 테스트 모드를 켠다. 안내 문구를 보이고 시작 키 입력을 받기 시작한다.
    public void BeginTestMode()
    {
        if (!_hasValidSetup)
        {
            return;
        }

        enabled = true;
        _hint.gameObject.SetActive(true);
        _hint.text = string.Format(ReadyHintFormat, _startKey);
    }

    private void Awake()
    {
        _hasValidSetup = _progress != null && _hint != null;
        if (!_hasValidSetup)
        {
            Debug.LogError("[LoadingSceneTester] _progress 또는 _hint 참조가 비어 있다.", this);
            enabled = false;
            return;
        }

        // 실제 로딩일 때는 안내 문구가 보이지 않아야 하므로 기본은 숨겨 둔다.
        _hint.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!_isLoading && Input.GetKeyDown(_startKey))
        {
            StartCoroutine(FakeLoadRoutine());
        }
    }

    private IEnumerator FakeLoadRoutine()
    {
        _isLoading = true;
        _hint.text = RunningHint;
        _progress.Restart();

        float progress = 0f;
        while (progress < 1f)
        {
            progress += Random.Range(_stepRange.x, _stepRange.y);
            _progress.SetTarget(progress);

            bool isStalled = Random.value < _stallChance;
            float delay = isStalled ? _stallTime : Random.Range(_delayRange.x, _delayRange.y);
            yield return new WaitForSecondsRealtime(delay);
        }

        _progress.SetTarget(1f);
        yield return _progress.WaitForComplete();

        _hint.text = string.Format(DoneHintFormat, _startKey);
        _isLoading = false;
    }
}
