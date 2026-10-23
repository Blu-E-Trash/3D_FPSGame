using UnityEngine;
using UnityEngine.UI;

// 좀비 머리 위에 체력바를 띄운다. 항상 카메라를 바라보게 해서 어느 방향에서 봐도 똑바로 보인다.
public class UI_ZombieHealthBar : MonoBehaviour
{
    [SerializeField] private Zombie _zombie;
    [SerializeField] private Slider _slider;

    private Transform _cameraTransform;

    private void Awake()
    {
        if (_zombie == null)
        {
            _zombie = GetComponentInParent<Zombie>();
        }

        if (_zombie == null)
        {
            GameLogger.LogError("UI_ZombieHealthBar: Zombie is not assigned.");
        }
    }

    private void Start()
    {
        _cameraTransform = Camera.main.transform;
    }

    // 좀비가 Update에서 움직이고 회전한 뒤에 맞추기 위해 LateUpdate에서 처리한다.
    private void LateUpdate()
    {
        if (_zombie == null)
            return;

        // 1. 죽으면 체력바를 숨긴다.
        if (_zombie.IsDead)
        {
            gameObject.SetActive(false);
            return;
        }

        // 2. 체력 비율(0 ~ 1)을 슬라이더에 표시한다.
        _slider.value = _zombie.HealthRatio;

        // 3. 카메라와 같은 방향을 바라보게 한다. (빌보드: 좀비가 돌아도 체력바는 항상 정면)
        transform.rotation = _cameraTransform.rotation;
    }
}
