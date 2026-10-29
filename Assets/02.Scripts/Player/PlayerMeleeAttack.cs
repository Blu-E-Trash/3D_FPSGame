using UnityEngine;

// 근접 모드에서 마우스 오른쪽 버튼을 누르면 왼손으로 환도를 휘두른다.
[RequireComponent(typeof(PlayerWeaponMode))]
public class PlayerMeleeAttack : MonoBehaviour
{
    private const string MeleeLayerName = "Melee Layer";
    private const string MeleeTrigger = "Melee";

    [Header("환도")]
    [SerializeField] private GameObject _weapon;                       // 왼손 뼈 아래의 Hwando
    [SerializeField, Range(0f, 1f)] private float _fadeStart = 0.7f;   // 동작이 이만큼 지나면 팔을 돌려놓기 시작한다. (0~1)
    [SerializeField] private float _fadeTime = 0.2f;                   // 팔이 총으로 돌아오는 시간 (초)

    private PlayerWeaponMode _weaponMode;
    private Animator _animator;
    private int _meleeLayer;      // Melee Layer의 번호
    private float _meleeWeight;   // Melee Layer의 지금 무게

    private void Awake()
    {
        _weaponMode = GetComponent<PlayerWeaponMode>();
        _animator = GetComponentInChildren<Animator>();
        _meleeLayer = _animator.GetLayerIndex(MeleeLayerName);  // 이름으로 레이어 번호를 찾는다.
    }

    private void Update()
    {
        UpdateMeleeWeight();  // 게임 상태와 상관없이 팔은 돌려놓는다.

        // 게임 중이 아니면 아무것도 하지 않는다.
        if (GameManager.Instance.State != GameState.Playing)
        {
            return;
        }

        // 환도는 근접 모드에서만 휘두른다. 다른 모드의 오른쪽 버튼은 PlayerBombFire와 PlayerZoom이 쓴다.
        if (_weaponMode.Mode != WeaponMode.Melee)
        {
            return;
        }

        // 1. 마우스 오른쪽 버튼을 누른 순간, 쉬고 있으면 휘두른다.
        if (Input.GetMouseButtonDown(1) && IsResting())
        {
            _meleeWeight = 1f;
            _animator.SetTrigger(MeleeTrigger);
        }
    }

    // Melee Layer가 Empty에 있고 다른 상태로 넘어가는 중도 아니면, 쉬고 있는 것이다.
    private bool IsResting()
    {
        AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(_meleeLayer);
        return state.IsName("Empty") && !_animator.IsInTransition(_meleeLayer);
    }

    // 다 휘둘렀거나 쉬고 있으면 Melee Layer의 무게를 천천히 0으로 줄인다.
    // 환도는 무게가 남아 있는 동안만 보인다.
    private void UpdateMeleeWeight()
    {
        AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(_meleeLayer);
        bool isFinishing = state.IsName("Melee") && state.normalizedTime >= _fadeStart;
        if (isFinishing || IsResting())
        {
            _meleeWeight = Mathf.MoveTowards(_meleeWeight, 0f, Time.deltaTime / _fadeTime);
        }

        _animator.SetLayerWeight(_meleeLayer, _meleeWeight);
        _weapon.SetActive(_meleeWeight > 0f);
    }
}
