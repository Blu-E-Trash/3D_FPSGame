using UnityEngine;

// 좀비 모델(Animator가 붙어 있는 오브젝트)에 붙인다.
// 애니메이션 이벤트는 "Animator가 붙어 있는 오브젝트"의 함수만 부를 수 있다.
// 그래서 모델에 이 스크립트를 붙여서 이벤트를 받고, 부모의 Zombie에게 전달한다.
public class ZombieHitEvent : MonoBehaviour
{
    private Zombie _zombie;

    private void Awake()
    {
        _zombie = GetComponentInParent<Zombie>();
    }

    // Z_Attack 애니메이션의 팔을 휘두르는 순간(0.37초)에 호출된다.
    public void HitEvent()
    {
        _zombie.AttackAction();
    }
}
