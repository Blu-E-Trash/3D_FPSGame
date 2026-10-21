using System.Collections.Generic;
using UnityEngine;

// 폭탄을 미리 만들어 두고 꺼내 쓴 뒤 다시 돌려받는 오브젝트 풀.
// Instantiate / Destroy를 반복하지 않아서 생성 비용과 GC(가비지 컬렉션)로 인한 끊김을 줄인다.
// 풀은 성능만 담당한다. 몇 개를 던질 수 있는지(게임 규칙)는 PlayerBombFire가 정한다.
public class BombPool : MonoBehaviour
{
    [SerializeField] private Bomb _bombPrefab;
    [SerializeField] private int _poolSize = 10; // 처음에 미리 만들어 둘 개수

    private readonly Queue<Bomb> _pool = new Queue<Bomb>(); // 쉬고 있는(비활성) 폭탄들

    private void Awake()
    {
        // 1. 시작할 때 폭탄을 미리 만들어서 꺼둔 채로 보관한다. (Prewarm)
        for (int i = 0; i < _poolSize; i++)
        {
            Bomb bomb = CreateBomb();
            bomb.gameObject.SetActive(false);
            _pool.Enqueue(bomb);
        }
    }

    // 풀에서 폭탄을 하나 꺼내서 지정한 위치에 활성화한다.
    public Bomb Get(Vector3 position, Quaternion rotation)
    {
        // 2. 쉬고 있는 폭탄이 없으면 하나 더 만든다. (풀 확장)
        Bomb bomb = _pool.Count > 0 ? _pool.Dequeue() : CreateBomb();

        // 3. 위치를 먼저 옮긴 뒤 켠다. (켜진 상태로 옮기면 한 프레임 이전 위치에 보일 수 있다)
        bomb.transform.SetPositionAndRotation(position, rotation);
        bomb.gameObject.SetActive(true);
        return bomb;
    }

    // 다 쓴 폭탄을 꺼서 풀로 돌려받는다.
    public void Return(Bomb bomb)
    {
        // 4. 이미 돌아와 있는 폭탄이면 무시한다. (같은 폭탄이 두 번 들어가는 것을 막는다)
        if (bomb.gameObject.activeSelf == false)
            return;

        bomb.gameObject.SetActive(false);
        _pool.Enqueue(bomb);
    }

    private Bomb CreateBomb()
    {
        Bomb bomb = Instantiate(_bombPrefab, transform); // 풀 오브젝트 아래에 모아서 Hierarchy를 정리한다.
        bomb.SetPool(this);
        return bomb;
    }
}
