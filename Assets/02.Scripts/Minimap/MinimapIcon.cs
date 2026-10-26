using UnityEngine;

// 미니맵에만 보이는 동그란 아이콘을 머리 위에 띄운다. (플레이어 = 초록, 좀비 = 빨강)
// 아이콘은 Minimap 레이어에 있어서 메인 카메라에는 보이지 않는다.
public class MinimapIcon : MonoBehaviour
{
    [SerializeField] private Sprite _circleSprite;
    [SerializeField] private Color _color = Color.red;
    [SerializeField] private Color _outlineColor = new Color(1f, 0.85f, 0.2f); // 테두리 색 (노랑)
    [SerializeField] private float _size = 1.6f;          // 아이콘 크기 (미터)
    [SerializeField] private float _outlineWidth = 0.2f;  // 테두리 두께 (미터)
    [SerializeField] private float _height = 10f;         // 캐릭터 위로 얼마나 띄울지 (벽, 지붕보다 위에 그려지게)
    [SerializeField] private string _layerName = "Minimap";

    private void Start()
    {
        if (_circleSprite == null)
        {
            GameLogger.LogError("MinimapIcon: Circle Sprite is not assigned.");
            return;
        }

        int layer = LayerMask.NameToLayer(_layerName);
        if (layer < 0)
        {
            GameLogger.LogError($"MinimapIcon: '{_layerName}' 레이어가 없습니다. Tags & Layers에서 추가해 주세요.");
            return;
        }

        // 테두리(조금 더 큰 원)를 먼저 깔고, 그 위에 색 원을 올린다.
        CreateCircle("MinimapIcon_Outline", _outlineColor, _size + _outlineWidth * 2f, 0, layer);
        CreateCircle("MinimapIcon", _color, _size, 1, layer);
    }

    private void CreateCircle(string objectName, Color color, float size, int sortingOrder, int layer)
    {
        GameObject icon = new GameObject(objectName);
        icon.layer = layer;

        // 캐릭터를 따라 움직이도록 자식으로 붙인다.
        icon.transform.SetParent(transform, false);
        icon.transform.position = transform.position + Vector3.up * _height;
        icon.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // 바닥에 눕혀서 위에서 보이게 한다.

        // 스프라이트 원래 크기와 부모 크기에 영향받지 않고, 원하는 크기(size 미터)가 되도록 맞춘다.
        float spriteSize = _circleSprite.bounds.size.x;
        Vector3 parentScale = transform.lossyScale;
        icon.transform.localScale = new Vector3(size / spriteSize / parentScale.x, size / spriteSize / parentScale.z, 1f);

        SpriteRenderer spriteRenderer = icon.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = _circleSprite;
        spriteRenderer.color = color;
        spriteRenderer.sortingOrder = sortingOrder;
    }
}
