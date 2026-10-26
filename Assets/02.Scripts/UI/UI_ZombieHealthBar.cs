using UnityEngine;
using UnityEngine.UI;

// 좀비 머리 위에 체력바를 띄운다. 항상 카메라를 바라보게 해서 어느 방향에서 봐도 똑바로 보인다.
// 체력은 매 프레임 확인하지 않고, 좀비의 HealthChanged / Died 이벤트를 구독해서 바뀔 때만 갱신한다.
// 좀비가 풀로 돌아갔다가 다시 나오면 체력바도 다시 보여야 하니까, 숨길 때는 슬라이더만 끈다.
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

    // 켜질 때 구독하고, 꺼질 때 구독을 해제한다. (해제하지 않으면 사라진 체력바를 계속 부르게 된다)
    private void OnEnable()
    {
        if (_zombie == null)
            return;

        _zombie.HealthChanged += Refresh;
        _zombie.Died += Hide;

        // 켜질 때(처음, 또는 풀에서 다시 나올 때) 지금 체력으로 맞춘다.
        Refresh(_zombie.HealthRatio);
    }

    private void OnDisable()
    {
        if (_zombie == null)
            return;

        _zombie.HealthChanged -= Refresh;
        _zombie.Died -= Hide;
    }

    private void Start()
    {
        _cameraTransform = Camera.main.transform;
    }

    // 체력이 바뀌었을 때만 불린다.
    private void Refresh(float healthRatio)
    {
        _slider.gameObject.SetActive(true);
        _slider.value = healthRatio;
    }

    // 죽으면 체력바를 숨긴다.
    private void Hide()
    {
        _slider.gameObject.SetActive(false);
    }

    // 좀비가 Update에서 움직이고 회전한 뒤에 맞추기 위해 LateUpdate에서 처리한다.
    private void LateUpdate()
    {
        // 카메라와 같은 방향을 바라보게 한다. (빌보드: 좀비가 돌아도 체력바는 항상 정면)
        transform.rotation = _cameraTransform.rotation;
    }
}
