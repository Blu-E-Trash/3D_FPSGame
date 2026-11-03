using System;
using System.Collections;
using UnityEngine;

// 로그인 화면 배경(감시 카메라 화면)에서 좀비가 인도를 따라 카메라 쪽으로 천천히 걸어온다.
// 배경은 평평한 사진이라, 사진 속 인도 위치(화면 비율 좌표)와 거리로 걸어갈 길을 정한다.
// 끝까지 오면 처음 자리로 돌아가고 Looped 이벤트를 알린다. (그 순간 화면을 튀게 해서 순간이동을 가린다)
public class LoginZombieWalker : MonoBehaviour
{
    [SerializeField] private Camera _camera;

    [Header("걸어갈 길 (화면 비율 좌표: 왼쪽 아래 0,0 ~ 오른쪽 위 1,1)")]
    [SerializeField] private Vector2 _startViewport = new Vector2(0.665f, 0.395f);
    [SerializeField] private Vector2 _endViewport = new Vector2(0.885f, 0.12f);

    [Header("카메라에서의 거리 (멀수록 작게 보인다)")]
    [SerializeField] private float _startDepth = 20f;
    [SerializeField] private float _endDepth = 8.5f;

    [SerializeField] private float _duration = 26f; // 끝까지 걷는 데 걸리는 시간(초)
    [SerializeField] private float _height = 1.8f;  // 좀비 키(m). 모델 크기와 상관없이 이 키로 맞춘다.

    public event Action Looped;
    public float Progress => _progress;

    private Vector3 _start;
    private Vector3 _end;
    private float _progress;

    private IEnumerator Start()
    {
        if (_camera == null) _camera = Camera.main;

        _start = _camera.ViewportToWorldPoint(new Vector3(_startViewport.x, _startViewport.y, _startDepth));
        _end = _camera.ViewportToWorldPoint(new Vector3(_endViewport.x, _endViewport.y, _endDepth));

        Vector3 dir = _end - _start;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(dir);
        }

        transform.position = _start;

        // 모델마다 크기가 달라서, 실제 키를 재서 원하는 키로 맞춘다.
        // 애니메이션이 한두 프레임 돌아야 자세가 잡히므로 기다렸다가 잰다.
        yield return null;
        yield return null;
        float h = MeasureHeight();
        if (h > 0.01f)
        {
            transform.localScale *= _height / h;
        }
    }

    // 뼈대(자식 Transform)들 중 가장 높은 곳과 발 사이 높이. 머리 꼭대기 뼈는 머리끝보다 조금 아래라 8% 더한다.
    private float MeasureHeight()
    {
        float top = float.MinValue;
        foreach (Transform t in GetComponentsInChildren<Transform>())
        {
            top = Mathf.Max(top, t.position.y);
        }
        return (top - transform.position.y) * 1.08f;
    }

    private void Update()
    {
        _progress += Time.deltaTime / _duration;
        if (_progress >= 1f)
        {
            _progress = 0f;
            Looped?.Invoke();
        }

        transform.position = Vector3.Lerp(_start, _end, _progress);
    }
}
