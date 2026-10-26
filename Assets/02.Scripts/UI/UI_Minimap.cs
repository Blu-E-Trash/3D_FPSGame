using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// 화면 우측 상단 미니맵: 지역(씬) 이름 + 확대/축소 버튼
// (미니맵 화면 자체는 RawImage에 렌더 텍스처를 연결해서 보여준다)
public class UI_Minimap : MonoBehaviour
{
    [SerializeField] private Camera _minimapCamera;
    [SerializeField] private TextMeshProUGUI _mapNameText;  // 지역 이름

    [Header("확대/축소")]
    [SerializeField] private float _zoomStep = 2f;  // 한 번에 바뀌는 양
    [SerializeField] private float _minSize = 4f;   // 가장 확대했을 때의 Orthographic Size
    [SerializeField] private float _maxSize = 16f;  // 가장 축소했을 때의 Orthographic Size

    private void Awake()
    {
        // 지금 씬 이름을 지역 이름으로 보여준다.
        _mapNameText.text = SceneManager.GetActiveScene().name;
    }

    // + 버튼의 On Click에 연결: Orthographic Size(보이는 범위)를 줄여서 확대한다.
    public void ZoomIn()
    {
        _minimapCamera.orthographicSize = Mathf.Max(_minimapCamera.orthographicSize - _zoomStep, _minSize);
        Deselect();
    }

    // - 버튼의 On Click에 연결: Orthographic Size(보이는 범위)를 넓혀서 축소한다.
    public void ZoomOut()
    {
        _minimapCamera.orthographicSize = Mathf.Min(_minimapCamera.orthographicSize + _zoomStep, _maxSize);
        Deselect();
    }

    // 버튼이 선택된 채로 남아 있으면 Space(점프)가 Submit으로 들어가서 버튼이 또 눌린다.
    // 그래서 누른 뒤에는 선택을 풀어 준다.
    private void Deselect()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
