using UnityEngine;

// 플레이어 머리 위에서 아래를 내려다보는 미니맵 카메라.
// 카메라의 Output Texture에 연결한 렌더 텍스처(MinimapRenderTexture)에 그리고,
// UI의 RawImage(MinimapImage)가 그 렌더 텍스처를 보여준다.
public class MinimapCamera : MonoBehaviour
{
    [SerializeField] private Transform _target;     // 따라갈 대상 (플레이어)
    [SerializeField] private float _height = 10f;   // 대상 위로 얼마나 높이 있을지

    // 플레이어가 움직이고 회전한 뒤에 따라가기 위해 LateUpdate에서 처리한다.
    private void LateUpdate()
    {
        if (_target == null)
            return;

        // 1. 대상의 바로 위로 이동한다.
        transform.position = _target.position + Vector3.up * _height;

        // 2. 아래를 내려다보면서(X 90도), 플레이어가 보는 방향(Y)을 따라 회전한다.
        //    → 미니맵의 위쪽 = 항상 플레이어의 앞쪽
        transform.eulerAngles = new Vector3(90f, _target.eulerAngles.y, 0f);
    }
}
