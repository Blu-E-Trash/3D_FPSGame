using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 플레이어가 대미지를 받으면 화면 테두리가 빨갛게 번쩍였다가 서서히 원래대로 돌아온다.
public class UI_HitEffect : MonoBehaviour
{
    [SerializeField] private PlayerHealth _health;
    [SerializeField] private Image _vignetteImage;     // 가운데는 투명하고 테두리만 빨간 이미지

    [SerializeField] private float _maxAlpha = 0.8f;   // 맞은 순간의 진하기 (0 ~ 1)
    [SerializeField] private float _fadeDuration = 0.5f; // 원래대로 돌아오는 데 걸리는 시간

    private Coroutine _flashCoroutine;

    private void Awake()
    {
        if (_health == null)
        {
            GameLogger.LogError("UI_HitEffect: Health is not assigned.");
        }

        // 처음에는 안 보이게 한다.
        SetAlpha(0f);
    }

    private void OnEnable()
    {
        if (_health != null)
        {
            _health.Damaged += HandleDamaged;
        }
    }

    private void OnDisable()
    {
        if (_health != null)
        {
            _health.Damaged -= HandleDamaged;
        }
    }

    private void HandleDamaged(DamageInfo damageInfo)
    {
        // 연달아 맞으면 처음부터 다시 번쩍인다.
        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
        }

        _flashCoroutine = StartCoroutine(Flash_Coroutine());
    }

    // 맞은 순간 진하게 보였다가 서서히 사라진다.
    private IEnumerator Flash_Coroutine()
    {
        float timer = 0f;
        while (timer < _fadeDuration)
        {
            timer += Time.deltaTime;
            SetAlpha(Mathf.Lerp(_maxAlpha, 0f, timer / _fadeDuration));
            yield return null;
        }

        SetAlpha(0f);
        _flashCoroutine = null;
    }

    private void SetAlpha(float alpha)
    {
        Color color = _vignetteImage.color;
        color.a = alpha;
        _vignetteImage.color = color;
    }
}
