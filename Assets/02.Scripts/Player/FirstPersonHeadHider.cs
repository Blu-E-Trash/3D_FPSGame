using UnityEngine;

// 1인칭 화면에서 내 머리(헬멧)가 보이지 않게 숨긴다.
// 달리거나 아래를 보면 군인이 몸을 숙여서 머리가 카메라 앞으로 나온다.
// 그래서 애니메이션이 끝난 뒤(LateUpdate) 머리뼈 크기를 거의 0으로 줄여서, 머리에 붙은 메시를 한 점으로 모아 버린다.
// (다른 FPS 게임들이 자기 머리를 숨길 때 많이 쓰는 방법이다. 대신 그림자에서도 머리가 빠진다.)
public class FirstPersonHeadHider : MonoBehaviour
{
    [SerializeField] private bool _hideHead = true;

    private Transform _head;

    private void Awake()
    {
        // 군인 모델(자식)의 Humanoid 아바타에서 머리뼈를 찾는다.
        Animator animator = GetComponentInChildren<Animator>();
        if (animator != null && animator.isHuman)
        {
            _head = animator.GetBoneTransform(HumanBodyBones.Head);
        }

        if (_head == null)
        {
            GameLogger.LogError("FirstPersonHeadHider: 머리뼈를 찾지 못했습니다. (모델이 Humanoid인지 확인)");
        }
    }

    // 애니메이션이 뼈를 움직인 뒤에 처리해야 덮어써지지 않는다.
    private void LateUpdate()
    {
        if (_head == null)
            return;

        _head.localScale = _hideHead ? Vector3.one * 0.0001f : Vector3.one;
    }
}
