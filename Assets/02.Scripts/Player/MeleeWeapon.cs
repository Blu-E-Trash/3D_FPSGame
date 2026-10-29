using UnityEngine;

// 환도의 칼날(트리거)에 닿은 것을 Player의 PlayerMeleeAttack에게 알려 준다.
// 트리거 이벤트는 콜라이더가 붙은 오브젝트(Hwando)의 스크립트가 받는다.
public class MeleeWeapon : MonoBehaviour
{
    private PlayerMeleeAttack _meleeAttack;

    private void Awake()
    {
        _meleeAttack = GetComponentInParent<PlayerMeleeAttack>();  // 부모 쪽에서 PlayerMeleeAttack을 찾는다.
    }

    private void OnTriggerEnter(Collider other)
    {
        _meleeAttack.Hit(other);
    }
}
