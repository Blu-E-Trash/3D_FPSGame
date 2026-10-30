using UnityEngine;

// 휘두르는 동작의 애니메이션 이벤트를 받아 부모의 PlayerMeleeAttack에게 전해 준다.
// 애니메이션 이벤트는 Animator가 붙은 오브젝트(Model)의 스크립트만 부를 수 있다.
public class PlayerMeleeEvent : MonoBehaviour
{
    private PlayerMeleeAttack _meleeAttack;

    private void Start()
    {
        _meleeAttack = GetComponentInParent<PlayerMeleeAttack>();  // 부모 쪽에서 PlayerMeleeAttack을 찾는다.
    }

    // MeleeSwing 동작에서 칼날이 앞으로 나가기 시작할 때 불린다.
    public void ActivateCollider()
    {
        _meleeAttack.ActivateCollider();
    }

    // MeleeSwing 동작에서 칼날이 다 지나갔을 때 불린다.
    public void DeactivateCollider()
    {
        _meleeAttack.DeactivateCollider();
    }

    // MeleeSwing 동작에서 칼이 몸 바로 앞을 지날 때 불린다. (20.9 부채꼴 방법)
    public void FanAttack()
    {
        _meleeAttack.FanAttack();
    }

    // MeleeSwing 동작에서 베기 시작할 때 불린다. (베기 이펙트)
    public void PlaySlashEffect()
    {
        _meleeAttack.PlaySlashEffect();
    }
}
