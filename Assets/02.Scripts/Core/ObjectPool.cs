using System.Collections.Generic;
using UnityEngine;

// 제네릭 오브젝트 풀: 미리 만들어 두고 꺼내 쓴 뒤 다시 돌려받는다.
// Instantiate / Destroy를 반복하지 않아서 생성 비용과 GC(가비지 컬렉션)로 인한 끊김을 줄인다.
// T에 풀링할 컴포넌트 타입을 넣어서 상속해 쓴다. (예: BombPool : ObjectPool<Bomb>, ZombiePool : ObjectPool<Zombie>)
// Unity는 제네릭 컴포넌트를 오브젝트에 바로 붙일 수 없어서, 타입을 정한 자식 클래스를 만들어 붙인다.
public abstract class ObjectPool<T> : MonoBehaviour where T : Component
{
    [SerializeField] private T _prefab;
    [SerializeField] private int _poolSize = 10; // 처음에 미리 만들어 둘 개수

    private readonly Queue<T> _pool = new Queue<T>(); // 쉬고 있는(비활성) 오브젝트들

    protected virtual void Awake()
    {
        // 1. 시작할 때 미리 만들어서 꺼둔 채로 보관한다. (Prewarm)
        for (int i = 0; i < _poolSize; i++)
        {
            T item = Create();
            item.gameObject.SetActive(false);
            _pool.Enqueue(item);
        }
    }

    // 풀에서 하나 꺼내서 지정한 위치에 활성화한다.
    public T Get(Vector3 position, Quaternion rotation)
    {
        // 2. 쉬고 있는 오브젝트가 없으면 하나 더 만든다. (풀 확장)
        T item = _pool.Count > 0 ? _pool.Dequeue() : Create();

        // 3. 위치를 먼저 옮긴 뒤 켠다. (켜질 때 OnEnable에서 새 위치를 쓸 수 있게)
        item.transform.SetPositionAndRotation(position, rotation);
        item.gameObject.SetActive(true);
        return item;
    }

    // 다 쓴 오브젝트를 꺼서 풀로 돌려받는다.
    public void Return(T item)
    {
        // 4. 이미 돌아와 있으면 무시한다. (같은 오브젝트가 두 번 들어가는 것을 막는다)
        if (item.gameObject.activeSelf == false)
            return;

        item.gameObject.SetActive(false);
        _pool.Enqueue(item);
    }

    private T Create()
    {
        T item = Instantiate(_prefab, transform); // 풀 오브젝트 아래에 모아서 Hierarchy를 정리한다.
        OnCreated(item);
        return item;
    }

    // 새로 만들었을 때 한 번 호출된다. (자식 클래스에서 "나를 풀로 돌려보내라"고 알려줄 때 쓴다)
    protected virtual void OnCreated(T item)
    {
    }
}
