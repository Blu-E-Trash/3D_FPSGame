using TMPro;
using UnityEngine;

// 숫자 키 1, 2로 무기 모드를 바꾸고, 지금 모드를 화면에 글자로 보여 준다.
public class PlayerWeaponMode : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _modeText;  // 화면 왼쪽 아래의 모드 글자

    private WeaponMode _mode = WeaponMode.Normal;
    public WeaponMode Mode => _mode;  // 밖에서는 읽기만 한다.
    public event System.Action<WeaponMode> OnModeChanged;  // 모드가 바뀌면 새 모드를 알린다.

    private void Start()
    {
        ShowMode();  // 처음 모드를 글자로 보여 준다.
    }

    private void Update()
    {
        // 게임 중이 아니면 아무것도 하지 않는다.
        if (GameManager.Instance.State != GameState.Playing)
        {
            return;
        }

        // 숫자 키 1은 일반 모드, 2는 저격 모드, 3은 근접 모드
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ChangeMode(WeaponMode.Normal);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ChangeMode(WeaponMode.Sniper);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            ChangeMode(WeaponMode.Melee);
        }
    }

    private void ChangeMode(WeaponMode mode)
    {
        _mode = mode;
        ShowMode();
        OnModeChanged?.Invoke(mode);  // 모드가 바뀌었다고 알린다.
        Debug.Log($"무기 모드: {mode}");
    }

    // 지금 모드를 글자로 보여 준다.
    private void ShowMode()
    {
        switch (_mode)
        {
            case WeaponMode.Normal:
                _modeText.text = "일반 모드";
                break;
            case WeaponMode.Sniper:
                _modeText.text = "저격 모드";
                break;
            case WeaponMode.Melee:
                _modeText.text = "근접 모드";
                break;
        }
    }
}
