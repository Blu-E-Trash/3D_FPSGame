using UnityEngine;

// 정해진 스폰 포인트마다 좀비를 한 마리씩 담당한다. (메이플스토리 방식)
// 일정 시간마다 확인해서, 자기 좀비가 죽어 있는 스폰 포인트에서만 새 좀비를 스폰한다.
// 그래서 동시에 살아 있는 좀비는 최대 "스폰 포인트 개수"만큼이다.
public class ZombieSpawner : MonoBehaviour
{
    [SerializeField] private Zombie _zombiePrefab;
    [SerializeField] private Transform[] _spawnPoints;     // 좀비가 나타날 위치들
    [SerializeField] private float _spawnInterval = 10f;   // 몇 초마다 빈 자리를 확인할지

    private Zombie[] _spawnedZombies;  // 스폰 포인트마다 지금 담당하고 있는 좀비 (같은 번호끼리 짝)
    private float _spawnTimer = 0f;

    // 동시에 살아 있을 수 있는 최대 좀비 수 (UI에 표시)
    public int MaxCount => _spawnPoints.Length;

    private void Awake()
    {
        _spawnedZombies = new Zombie[_spawnPoints.Length];
    }

    private void Start()
    {
        if (_zombiePrefab == null)
        {
            GameLogger.LogError("ZombieSpawner: Zombie Prefab is not assigned.");
            return;
        }

        // 맵에 들어오면 모든 스폰 포인트에 한 마리씩 채운다.
        SpawnToEmptyPoints();
    }

    private void Update()
    {
        // 게임 중이 아니면(준비, 게임오버) 스폰하지 않는다.
        if (GameManager.Instance.State != GameState.Playing) return;

        // 일정 시간마다 빈 스폰 포인트를 채운다.
        _spawnTimer += Time.deltaTime;
        if (_spawnTimer >= _spawnInterval)
        {
            _spawnTimer = 0f;
            SpawnToEmptyPoints();
        }
    }

    // 담당 좀비가 없거나 죽은 스폰 포인트에만 새 좀비를 만든다.
    private void SpawnToEmptyPoints()
    {
        for (int i = 0; i < _spawnPoints.Length; i++)
        {
            Zombie zombie = _spawnedZombies[i];
            bool isEmpty = zombie == null || zombie.IsDead;
            if (isEmpty == false)
                continue;

            Transform spawnPoint = _spawnPoints[i];
            _spawnedZombies[i] = Instantiate(_zombiePrefab, spawnPoint.position, spawnPoint.rotation);
        }
    }

    // 씬 뷰에서 스폰 위치를 볼 수 있게 표시한다.
    private void OnDrawGizmos()
    {
        if (_spawnPoints == null)
            return;

        Gizmos.color = Color.red;
        foreach (Transform spawnPoint in _spawnPoints)
        {
            if (spawnPoint != null)
            {
                Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);
            }
        }
    }
}
