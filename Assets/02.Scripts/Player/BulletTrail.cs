using System.Collections;
using UnityEngine;

// 총을 쏠 때 총구에서 맞은 지점까지 노란 선(총알 궤적)을 잠깐 그린다. (라인 렌더러)
// 총구 위치(FirePoint)에 붙여서 쓴다. 카메라의 자식이라 화면 오른쪽 아래에서 시작하는 것처럼 보인다.
[RequireComponent(typeof(LineRenderer))]
public class BulletTrail : MonoBehaviour
{
    [SerializeField] private float _duration = 0.05f;     // 선이 보이는 시간(초)
    [SerializeField] private float _startWidth = 0.03f;   // 총구 쪽 두께
    [SerializeField] private float _endWidth = 0.01f;     // 맞은 쪽 두께
    [SerializeField] private Color _startColor = new Color(1f, 0.9f, 0.4f, 1f);  // 총구 쪽 색 (밝은 노랑)
    [SerializeField] private Color _endColor = new Color(1f, 0.6f, 0.1f, 0f);    // 맞은 쪽 색 (주황, 투명)

    private LineRenderer _lineRenderer;
    private Coroutine _hideCoroutine;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();

        // 선 모양을 코드로 설정한다.
        _lineRenderer.useWorldSpace = true;   // 점 위치를 월드 좌표로 넣는다.
        _lineRenderer.positionCount = 2;      // 시작점(총구)과 끝점(맞은 곳)
        _lineRenderer.startWidth = _startWidth;
        _lineRenderer.endWidth = _endWidth;
        _lineRenderer.startColor = _startColor;
        _lineRenderer.endColor = _endColor;
        _lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _lineRenderer.receiveShadows = false;

        // 머티리얼이 없으면 색을 그대로 보여 주는 기본 셰이더로 만든다.
        if (_lineRenderer.sharedMaterial == null)
        {
            _lineRenderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        }

        _lineRenderer.enabled = false; // 쏘기 전에는 숨긴다.
    }

    // 총구에서 endPoint까지 선을 잠깐 보여 준다.
    public void Show(Vector3 endPoint)
    {
        _lineRenderer.SetPosition(0, transform.position);
        _lineRenderer.SetPosition(1, endPoint);
        _lineRenderer.enabled = true;

        // 연사하면 이전 숨기기를 취소하고 다시 센다.
        if (_hideCoroutine != null)
        {
            StopCoroutine(_hideCoroutine);
        }
        _hideCoroutine = StartCoroutine(Hide_Coroutine());
    }

    private IEnumerator Hide_Coroutine()
    {
        yield return new WaitForSeconds(_duration);
        _lineRenderer.enabled = false;
        _hideCoroutine = null;
    }
}
