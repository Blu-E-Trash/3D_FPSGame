using UnityEngine;

// 저격 모드에서 마우스 오른쪽 버튼을 누르고 있는 동안 시야각을 좁혀 확대한다.
[RequireComponent(typeof(PlayerWeaponMode))]
public class PlayerZoom : MonoBehaviour
{
    [SerializeField] private GameObject _crosshair;   // 4장의 조준선
    [SerializeField] private float _normalFov = 60f;  // 평소 시야각
    [SerializeField] private float _zoomFov = 15f;    // 확대했을 때 시야각

    private PlayerWeaponMode _weaponMode;

    private void Awake()
    {
        _weaponMode = GetComponent<PlayerWeaponMode>();
    }

    private void Update()
    {
        // 게임 중이고, 저격 모드이고, 오른쪽 버튼을 누르고 있을 때만 확대한다.
        bool isZooming = GameManager.Instance.State == GameState.Playing
                         && _weaponMode.Mode == WeaponMode.Sniper
                         && Input.GetMouseButton(1);

        Camera.main.fieldOfView = isZooming ? _zoomFov : _normalFov;
        _crosshair.SetActive(!isZooming);  // 확대하는 동안은 조준선을 숨긴다.
    }
}
