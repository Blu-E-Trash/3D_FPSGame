using UnityEngine;

// 마우스 좌우 움직임으로 플레이어 몸을 좌우(Y축)로만 회전한다.
public class PlayerRotate : MonoBehaviour
{
    [SerializeField]
    private float _mouseSensitivity = 3f;

    private float _acculationX = 0f; // 좌우

    private void Awake()
    {
        // 씬에 배치된 처음 방향에서 시작한다.
        _acculationX = transform.eulerAngles.y;
    }

    private void Update()
    {
        // 게임 중이 아니면(준비, 게임오버) 아무것도 하지 않는다.
        if (GameManager.Instance.State != GameState.Playing) return;

        // 1. 마우스 입력을 받아온다.
        // 마우스 입력은 이미 "이번 프레임에 움직인 양"이라 Time.deltaTime을 곱하지 않는다.
        float mouseX = Input.GetAxis("Mouse X");

        // 2. 마우스 입력을 기반으로 회전값을 누적한다.
        _acculationX += mouseX * _mouseSensitivity;

        // 3. 누적한 회전값을 플레이어에 적용한다.
        transform.eulerAngles = new Vector3(0f, _acculationX, 0f);
    }
}
