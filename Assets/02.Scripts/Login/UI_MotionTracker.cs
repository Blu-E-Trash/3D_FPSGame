using TMPro;
using UnityEngine;

// 감시 카메라의 "움직임 감지" 표시: 좀비를 붉은 모서리 상자로 따라가고 거리를 보여준다.
public class UI_MotionTracker : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    [SerializeField] private Transform _target;         // 좀비 (뼈대를 감싸는 상자를 그린다)
    [SerializeField] private LoginZombieWalker _walker;
    [SerializeField] private RectTransform _canvas;     // 화면 크기 기준
    [SerializeField] private RectTransform _box;        // 모서리 상자
    [SerializeField] private RectTransform _labels;     // 글자 묶음
    [SerializeField] private TextMeshProUGUI _detected; // MOTION DETECTED (깜빡임)
    [SerializeField] private TextMeshProUGUI _distance; // DIST 00.0m
    [SerializeField] private float _padding = 10f;

    private Transform[] _bones;

    private void OnEnable()
    {
        if (_walker != null) _walker.Looped += OnLooped;
    }

    private void OnDisable()
    {
        if (_walker != null) _walker.Looped -= OnLooped;
    }

    private void Start()
    {
        LoginSfx.Instance?.Alert();
    }

    // 좀비가 처음 자리로 돌아가면 다시 감지한 것처럼 알린다.
    private void OnLooped()
    {
        UI_CCTVFeed.Instance?.Glitch(0.2f);
        LoginSfx.Instance?.Alert();
    }

    private void LateUpdate()
    {
        if (_camera == null || _target == null) return;

        // 1. 뼈대 위치를 화면 비율 좌표로 바꿔 최소/최대를 구한다. (몸 전체를 감싸는 상자)
        if (_bones == null) _bones = _target.GetComponentsInChildren<Transform>();
        Vector2 vMin = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 vMax = new Vector2(float.MinValue, float.MinValue);
        foreach (Transform bone in _bones)
        {
            Vector3 v = _camera.WorldToViewportPoint(bone.position);
            vMin = Vector2.Min(vMin, v);
            vMax = Vector2.Max(vMax, v);
        }
        float headroom = (vMax.y - vMin.y) * 0.08f; // 머리끝까지
        vMax.y += headroom;

        // 2. 캔버스 좌표로 옮긴다. (상자는 왼쪽 아래 기준)
        Vector2 size = _canvas.rect.size;
        Vector2 p0 = Vector2.Scale(vMin, size) - Vector2.one * _padding;
        Vector2 p1 = Vector2.Scale(vMax, size) + Vector2.one * _padding;
        _box.anchoredPosition = p0;
        _box.sizeDelta = p1 - p0;

        // 3. 글자는 상자 오른쪽에 두되, 화면 오른쪽 끝에 가까우면 왼쪽으로 옮긴다.
        bool right = p1.x + 260f < size.x;
        _labels.anchorMin = _labels.anchorMax = new Vector2(right ? 1f : 0f, 1f);
        _labels.pivot = new Vector2(right ? 0f : 1f, 1f);
        _labels.anchoredPosition = new Vector2(right ? 12f : -12f, 0f);

        // 4. 깜빡임과 거리
        _detected.enabled = (int)(Time.unscaledTime * 2.5f) % 2 == 0;
        float dist = Vector3.Distance(_camera.transform.position, _target.position);
        _distance.text = $"DIST {dist:00.0}m";
    }
}
