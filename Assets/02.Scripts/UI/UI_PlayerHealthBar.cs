using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 플레이어 체력을 화면 좌측 하단(스태미나 바 위) 슬라이더로 표시한다.
// 체력이 줄면 (1) 바가 살짝 커졌다가 돌아오고, (2) 줄어든 만큼 흰색으로 남았다가 천천히 따라 줄어든다.
public class UI_PlayerHealthBar : MonoBehaviour
{
    [SerializeField] private PlayerHealth _health;
    [SerializeField] private Slider _slider;

    [Header("줄어든 만큼 흰색으로 남기기")]
    [SerializeField] private RectTransform _damageFill;  // 빨간 체력 뒤에 깔린 흰색 바
    [SerializeField] private float _trailDelay = 0.4f;   // 맞은 뒤 흰색이 줄어들기 시작할 때까지 기다리는 시간
    [SerializeField] private float _trailSpeed = 0.8f;   // 흰색이 줄어드는 속도 (초당 비율, 1이면 1초에 바 전체)

    [Header("커졌다가 돌아오기")]
    [SerializeField] private float _punchScale = 1.08f;  // 맞았을 때 커지는 배율
    [SerializeField] private float _punchDuration = 0.2f; // 커졌다가 돌아오는 데 걸리는 시간

    private float _shownRatio = 1f;  // 지난 프레임에 표시한 체력 비율 (줄었는지 확인용)
    private float _trailRatio = 1f;  // 흰색 바가 가리키는 비율
    private float _trailTimer = 0f;  // 흰색이 줄어들기 시작할 때까지 남은 시간
    private Coroutine _punchCoroutine;

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

        float ratio = _health.Ratio;

        // 1. 체력이 줄었으면 게임필 연출을 시작한다.
        if (ratio < _shownRatio)
        {
            _trailTimer = _trailDelay; // 흰색은 잠깐 그대로 둔다.
            Punch();
        }
        // 회복했으면 흰색도 바로 맞춘다.
        else if (ratio > _trailRatio)
        {
            _trailRatio = ratio;
        }

        _shownRatio = ratio;

        // 2. 빨간 체력은 바로 줄인다.
        _slider.value = ratio;

        // 3. 흰색은 잠깐 기다렸다가 빨간 체력까지 천천히 따라 줄어든다.
        if (_trailTimer > 0f)
        {
            _trailTimer -= Time.deltaTime;
        }
        else
        {
            _trailRatio = Mathf.MoveTowards(_trailRatio, ratio, _trailSpeed * Time.deltaTime);
        }

        if (_damageFill != null)
        {
            _damageFill.anchorMax = new Vector2(_trailRatio, 1f);
        }
    }

    private void Punch()
    {
        // 연달아 맞으면 처음부터 다시 커지게 한다.
        if (_punchCoroutine != null)
        {
            StopCoroutine(_punchCoroutine);
        }

        _punchCoroutine = StartCoroutine(Punch_Coroutine());
    }

    // 살짝 커졌다가 원래 크기로 돌아온다.
    private IEnumerator Punch_Coroutine()
    {
        float timer = 0f;
        while (timer < _punchDuration)
        {
            timer += Time.deltaTime;

            // 0 -> 1 -> 0 으로 올라갔다 내려오는 곡선 (사인 반 바퀴)
            float t = Mathf.Sin(timer / _punchDuration * Mathf.PI);
            transform.localScale = Vector3.one * Mathf.Lerp(1f, _punchScale, t);
            yield return null;
        }

        transform.localScale = Vector3.one;
        _punchCoroutine = null;
    }
}
