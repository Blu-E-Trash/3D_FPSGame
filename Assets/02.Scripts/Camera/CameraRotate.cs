using UnityEngine;

// 마우스 위아래 움직임으로 카메라를 위아래로 회전하고,
// 좌우 방향은 플레이어 몸의 방향을 그대로 따라간다.
public class CameraRotate : MonoBehaviour
{
    private const float MaxPitch = 89f;

    [SerializeField]
    private Transform _player;
    [SerializeField]
    private float _mouseSensitivity = 3f;

    private float _acculationY = 0f; // 위아래

    private void Awake()
    {
        if (_player == null)
        {
            GameLogger.LogError("CameraRotate: Player is not assigned.");
        }
    }

    // 플레이어가 Update에서 회전한 뒤에 방향을 맞추기 위해 LateUpdate에서 처리한다.
    private void LateUpdate()
    {
        // 게임 중이 아니면(준비, 게임오버) 아무것도 하지 않는다.
        if (GameManager.Instance.State != GameState.Playing) return;

        // 1. 마우스 입력을 받아온다.
        float mouseY = Input.GetAxis("Mouse Y");

        GameLogger.Log($"Mouse Y: {mouseY}");

        // 2. 위아래 회전값을 누적하고 -89도 ~ 89도로 제한한다.
        // transform.eulerAngles는 0~360도로 돌려주기 때문에(-1도 -> 359도) 내 변수에서 먼저 제한한다.
        _acculationY += mouseY * _mouseSensitivity;
        _acculationY = Mathf.Clamp(_acculationY, -MaxPitch, MaxPitch);

        // 3. 좌우는 플레이어 몸 방향을 그대로 쓴다. (얼굴과 카메라 방향이 항상 일치)
        float yaw = _player != null ? _player.eulerAngles.y : transform.eulerAngles.y;

        // 4. 회전값을 카메라에 적용한다.
        transform.eulerAngles = new Vector3(-_acculationY, yaw, 0f);
    }
}
