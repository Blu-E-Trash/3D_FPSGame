using UnityEngine;

public class PlayerRotate : MonoBehaviour
{
    [SerializeField]
    private float _rotationSpeed = 200f;

    private float _acculationX = 0f; // 좌우

    private void Update()
    {
        // 1. 마우스 입력을 받아옵니다.
        float mouseX = Input.GetAxis("Mouse X");
        // 2. 마우스 입력을 기반으로 회전값을 누적한다.
        _acculationX += mouseX * _rotationSpeed * Time.deltaTime;
        // 3. 누적한 회전값을 플레이어에 적용한다.
        transform.eulerAngles = new Vector3(0f, _acculationX, 0f);
    }
}
