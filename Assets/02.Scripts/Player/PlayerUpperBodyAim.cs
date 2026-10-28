using UnityEngine;

// 카메라로 위아래를 볼 때 상반신(허리~가슴)도 같이 숙이거나 젖혀서, 총구가 카메라가 보는 방향을 향하게 한다.
// 애니메이션은 매 프레임 뼈를 덮어쓰기 때문에, 애니메이션이 끝난 뒤(LateUpdate)에 뼈를 추가로 돌린다.
// 총구(Muzzle)는 손에 붙어 있어서 상체를 따라 같이 기울어진다. → 총알 궤적이 총구와 일직선이 된다.
//
// 상체는 허리를 축으로 기울어지는데 카메라가 제자리에 있으면, 위를 볼수록 총이 카메라 뒤로 빠져 화면에서 사라진다.
// 그래서 카메라도 가슴에 붙어 있는 것처럼 상체가 기울어진 만큼 같이 옮긴다. → 어느 각도에서든 총이 화면의 같은 자리에 보인다.
// 카메라 스크립트(CameraFollow 위치, CameraRotate 각도)가 끝난 뒤에 실행되어야 하므로 실행 순서를 뒤로 미룬다.
[DefaultExecutionOrder(100)]
public class PlayerUpperBodyAim : MonoBehaviour
{
    [SerializeField] private float _maxBendAngle = 70f; // 상체를 최대로 숙이거나 젖히는 각도 (카메라는 89도까지 돌아간다)

    private Transform[] _spineBones; // 각도를 나눠서 돌릴 뼈들 (허리, 가슴, 윗가슴)

    private void Awake()
    {
        // 군인 모델(자식)의 Humanoid 아바타에서 허리~가슴 뼈를 찾는다. (모델에 따라 윗가슴 뼈는 없을 수 있다)
        Animator animator = GetComponentInChildren<Animator>();
        if (animator == null || animator.isHuman == false)
        {
            GameLogger.LogError("PlayerUpperBodyAim: Humanoid 애니메이터를 찾지 못했습니다.");
            return;
        }

        _spineBones = new[]
        {
            animator.GetBoneTransform(HumanBodyBones.Spine),
            animator.GetBoneTransform(HumanBodyBones.Chest),
            animator.GetBoneTransform(HumanBodyBones.UpperChest),
        };
        _spineBones = System.Array.FindAll(_spineBones, bone => bone != null);
    }

    private void LateUpdate()
    {
        if (_spineBones == null || _spineBones.Length == 0)
            return;

        // 1. 카메라의 위아래 각도를 -180 ~ 180도로 바꾼다. (eulerAngles는 0 ~ 360도: -10도 -> 350도)
        //    양수 = 아래를 봄, 음수 = 위를 봄
        Transform cameraTransform = Camera.main.transform;
        float pitch = Mathf.DeltaAngle(0f, cameraTransform.eulerAngles.x);
        pitch = Mathf.Clamp(pitch, -_maxBendAngle, _maxBendAngle);

        // 2. 기울이기 전에, 카메라가 가장 위쪽 가슴뼈 기준으로 어디에 있는지 기억해 둔다.
        Transform topBone = _spineBones[_spineBones.Length - 1];
        Vector3 cameraOnChest = topBone.InverseTransformPoint(cameraTransform.position);

        // 3. 한 뼈만 크게 꺾으면 어색하므로, 여러 뼈에 똑같이 나눠서 몸의 오른쪽 축을 기준으로 돌린다.
        Quaternion bend = Quaternion.AngleAxis(pitch / _spineBones.Length, transform.right);
        foreach (Transform bone in _spineBones)
        {
            bone.rotation = bend * bone.rotation;
        }

        // 4. 가슴이 기울어진 만큼 카메라도 따라 옮긴다. (CameraFollow가 매 프레임 위치를 다시 잡으므로 누적되지 않는다)
        cameraTransform.position = topBone.TransformPoint(cameraOnChest);
    }
}
