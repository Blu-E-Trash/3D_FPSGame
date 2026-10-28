using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// 1인칭 화면(메인 카메라)에서만 내 머리(헬멧)가 보이지 않게 숨긴다.
// 달리거나 아래를 보면 군인이 몸을 숙여서 머리가 카메라 앞으로 나온다.
//
// 머리뼈 크기를 줄이는 방법은 모든 카메라와 그림자에서 머리가 사라진다.
// (스키닝은 한 프레임에 한 번만 계산되어서, 카메라마다 다르게 보이게 할 수 없다.)
// 그래서 시작할 때 군인 메시를 "몸"과 "머리" 두 개로 나누고,
// 머리만 FirstPersonHidden 레이어에 두어 메인 카메라의 Culling Mask에서 뺀다.
// → Scene 뷰, 다른 카메라에는 머리가 보이고, 그림자 전용 머리 덕분에 1인칭에서도 머리 그림자가 생긴다.
public class FirstPersonHeadHider : MonoBehaviour
{
    [SerializeField] private bool _hideHead = true;
    [SerializeField] private string _hiddenLayerName = "FirstPersonHidden"; // 메인 카메라가 그리지 않는 레이어

    private void Start()
    {
        if (_hideHead == false)
            return;

        // 1. 군인 모델(자식)의 Humanoid 아바타에서 머리뼈를 찾는다.
        Animator animator = GetComponentInChildren<Animator>();
        Transform head = (animator != null && animator.isHuman) ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
        if (head == null)
        {
            GameLogger.LogError("FirstPersonHeadHider: 머리뼈를 찾지 못했습니다. (모델이 Humanoid인지 확인)");
            return;
        }

        int hiddenLayer = LayerMask.NameToLayer(_hiddenLayerName);
        if (hiddenLayer < 0)
        {
            GameLogger.LogError($"FirstPersonHeadHider: '{_hiddenLayerName}' 레이어가 없습니다. Tags & Layers에서 추가해 주세요.");
            return;
        }

        // 2. 메인 카메라는 이 레이어를 그리지 않는다.
        Camera.main.cullingMask &= ~(1 << hiddenLayer);

        // 3. 머리에 붙은 부분이 있는 메시마다 몸/머리로 나눈다.
        foreach (SkinnedMeshRenderer body in animator.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            SplitHead(body, head, hiddenLayer);
        }
    }

    private void SplitHead(SkinnedMeshRenderer body, Transform head, int hiddenLayer)
    {
        Mesh mesh = body.sharedMesh;
        Transform[] bones = body.bones;

        // 머리뼈와 그 아래 뼈(헬멧 등)가 이 메시의 몇 번 뼈인지 표시해 둔다.
        bool[] isHeadBone = new bool[bones.Length];
        for (int i = 0; i < bones.Length; i++)
        {
            isHeadBone[i] = bones[i] != null && bones[i].IsChildOf(head);
        }

        // 정점마다 머리뼈 영향이 절반 이상이면 머리 정점이다.
        BoneWeight[] weights = mesh.boneWeights;
        bool[] isHeadVertex = new bool[weights.Length];
        for (int v = 0; v < weights.Length; v++)
        {
            BoneWeight w = weights[v];
            float headWeight = (isHeadBone[w.boneIndex0] ? w.weight0 : 0f) + (isHeadBone[w.boneIndex1] ? w.weight1 : 0f)
                             + (isHeadBone[w.boneIndex2] ? w.weight2 : 0f) + (isHeadBone[w.boneIndex3] ? w.weight3 : 0f);
            isHeadVertex[v] = headWeight >= 0.5f;
        }

        // 삼각형의 정점 중 하나라도 머리 정점이면 머리 삼각형으로 보낸다.
        Mesh bodyMesh = Instantiate(mesh);
        Mesh headMesh = Instantiate(mesh);
        bool hasHead = false;
        for (int sub = 0; sub < mesh.subMeshCount; sub++)
        {
            int[] triangles = mesh.GetTriangles(sub);
            List<int> bodyTriangles = new List<int>();
            List<int> headTriangles = new List<int>();
            for (int t = 0; t < triangles.Length; t += 3)
            {
                bool headTriangle = isHeadVertex[triangles[t]] || isHeadVertex[triangles[t + 1]] || isHeadVertex[triangles[t + 2]];
                List<int> target = headTriangle ? headTriangles : bodyTriangles;
                target.Add(triangles[t]);
                target.Add(triangles[t + 1]);
                target.Add(triangles[t + 2]);
            }
            bodyMesh.SetTriangles(bodyTriangles, sub);
            headMesh.SetTriangles(headTriangles, sub);
            hasHead |= headTriangles.Count > 0;
        }

        if (hasHead == false)
        {
            Destroy(bodyMesh);
            Destroy(headMesh);
            return;
        }

        body.sharedMesh = bodyMesh;

        // 메인 카메라에는 안 보이는 머리 (Scene 뷰, 다른 카메라에는 보인다)
        CreateHeadRenderer(body, headMesh, "Head (FirstPersonHidden)", hiddenLayer, ShadowCastingMode.Off);
        // 그림자만 드리우는 머리 (1인칭에서도 머리 그림자가 생긴다)
        CreateHeadRenderer(body, headMesh, "Head (ShadowOnly)", body.gameObject.layer, ShadowCastingMode.ShadowsOnly);
    }

    private void CreateHeadRenderer(SkinnedMeshRenderer body, Mesh headMesh, string objectName, int layer, ShadowCastingMode shadowMode)
    {
        GameObject headObject = new GameObject(objectName);
        headObject.layer = layer;
        headObject.transform.SetParent(body.transform.parent, false);
        headObject.transform.localPosition = body.transform.localPosition;
        headObject.transform.localRotation = body.transform.localRotation;
        headObject.transform.localScale = body.transform.localScale;

        // 몸과 같은 뼈를 써서 애니메이션을 그대로 따라간다.
        SkinnedMeshRenderer headRenderer = headObject.AddComponent<SkinnedMeshRenderer>();
        headRenderer.sharedMesh = headMesh;
        headRenderer.bones = body.bones;
        headRenderer.rootBone = body.rootBone;
        headRenderer.localBounds = body.localBounds;
        headRenderer.sharedMaterials = body.sharedMaterials;
        headRenderer.updateWhenOffscreen = body.updateWhenOffscreen;
        headRenderer.shadowCastingMode = shadowMode;
    }
}
