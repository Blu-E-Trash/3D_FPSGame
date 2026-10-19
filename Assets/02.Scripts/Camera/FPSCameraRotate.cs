using UnityEngine;

// 마우스를 움직이면 카메라를 그 방향으로 회전하고 싶다
public class FPSCameraRotate : MonoBehaviour
{
    [SerializeField]
    private float _rotationSpeed = 200f;

    private float _acculationX = 0f; // 좌우
    private float _acculationY = 0f; // 위아래

    private void Update()
    {
        // 1. 마우스 입력을 받아옵니다.
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // 2, 마우스 입력을 기반으로 카메라 회전 방향을 구한다.
        //Vector3 dir = new Vector3(-mouseY, mouseX, 0f);
        // 2. 마우스 입력을 기반으로 회전값을 누적한다.
        _acculationX += mouseX * _rotationSpeed * Time.deltaTime;
        _acculationY += mouseY * _rotationSpeed * Time.deltaTime;

        // 3. 누적하기 전에 내 변수에서 먼저 -90 ~ 90도로 제한.
        _acculationY = Mathf.Clamp(_acculationY, -90f, 90f); // 위아래 회전값을 -90도 ~ 90도로 제한한다.

        // 4. 누적한 회전값을 카메라에 적용한다.
        transform.eulerAngles = new Vector3(-_acculationY, _acculationX, 0f);


        // 3. 카메라를 회전시킨다.
        // 새로운 각도 = 현재 각도 + 회전 속도 * 시간
        //transform.eulerAngles += dir * _rotationSpeed * Time.deltaTime;

        // 4. 위 아래 회전값을 -90도 ~ 90도로 제한한다.
        //Vector3 currentRotation = transform.eulerAngles;

        // 정면: 0도
        // 하늘을 살짝 1도만큼 쳐다보면: -1 -> 359
        // 유니티는 0~360도로 회전값을 표현하기 때문에 음수는 자동으로 변환되어서 반환한다.

        //currentRotation.x = Mathf.Clamp(currentRotation.x, -90f, 90f);
        //transform.eulerAngles = currentRotation;
    }
}
