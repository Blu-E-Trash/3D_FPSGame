using UnityEngine;

// 공격 받으면 체력이 깎이고, 체력이 0이 되면 산산조각 나듯 부서진다.
public class Box : MonoBehaviour, IDamageable
{
    [Header("체력")]
    [SerializeField] private float _health = 30f;

    [Header("조각")]
    [SerializeField] private int _piecesPerAxis = 3;         // 한 변을 몇 조각으로 나눌지 (3이면 3x3x3 = 27조각)
    [SerializeField] private float _pieceMass = 0.1f;        // 조각 하나의 무게 (가벼울수록 멀리 날아간다)

    [Header("부서지는 힘")]
    [SerializeField] private float _explosionForce = 1f;     // 조각을 밀어내는 힘
    [SerializeField] private float _explosionRadius = 2f;    // 힘이 미치는 반경
    [SerializeField] private float _upwardsModifier = 0.5f;  // 위로 튀어오르는 정도

    [Header("조각 사라짐")]
    [SerializeField] private float _pieceLifeTime = 3f;      // 조각이 바닥에 남아 있는 시간
    [SerializeField] private float _pieceShrinkTime = 0.5f;  // 사라질 때 작아지는 데 걸리는 시간

    private Renderer _renderer;
    private bool _isBroken = false; // 같은 프레임에 여러 번 맞아서 두 번 부서지는 것을 막는다.

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_isBroken)
            return;

        _health -= damageInfo.Value;

        if (_health <= 0f)
        {
            Break(damageInfo.HitPoint);
        }
    }

    // 상자를 작은 조각들로 나누고, 맞은 자리에서 바깥쪽으로 날려 보낸 뒤 상자는 사라진다.
    private void Break(Vector3 hitPoint)
    {
        _isBroken = true;

        Material material = _renderer != null ? _renderer.sharedMaterial : null;
        Vector3 pieceScale = transform.lossyScale / _piecesPerAxis;

        // 1. 상자를 한 변에 _piecesPerAxis개씩, 정육면체 조각들로 나눈다.
        for (int x = 0; x < _piecesPerAxis; x++)
        {
            for (int y = 0; y < _piecesPerAxis; y++)
            {
                for (int z = 0; z < _piecesPerAxis; z++)
                {
                    // 상자 로컬 좌표(-0.5 ~ 0.5)를 n등분한 칸의 중심 위치
                    Vector3 localPosition = new Vector3(
                        (x + 0.5f) / _piecesPerAxis - 0.5f,
                        (y + 0.5f) / _piecesPerAxis - 0.5f,
                        (z + 0.5f) / _piecesPerAxis - 0.5f);

                    CreatePiece(transform.TransformPoint(localPosition), pieceScale, material, hitPoint);
                }
            }
        }

        // 2. 원래 상자는 지운다.
        Destroy(gameObject);
    }

    private void CreatePiece(Vector3 position, Vector3 scale, Material material, Vector3 hitPoint)
    {
        // 조각 만들기 (큐브 + 상자와 같은 머티리얼)
        GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
        piece.name = "BoxPiece";
        piece.transform.SetPositionAndRotation(position, transform.rotation);
        piece.transform.localScale = scale;

        if (material != null)
        {
            piece.GetComponent<Renderer>().sharedMaterial = material;
        }

        // 물리를 붙여서 맞은 자리에서 바깥쪽으로 날려 보낸다.
        Rigidbody pieceRigidbody = piece.AddComponent<Rigidbody>();
        pieceRigidbody.mass = _pieceMass;
        pieceRigidbody.AddExplosionForce(_explosionForce, hitPoint, _explosionRadius, _upwardsModifier, ForceMode.Impulse);

        // 잠시 뒤 작아지면서 사라지게 한다.
        piece.AddComponent<BoxPiece>().Init(_pieceLifeTime, _pieceShrinkTime);
    }
}
