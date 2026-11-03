using UnityEngine;

// 씬에 하나만 존재해야 하는 매니저들의 부모 클래스 (싱글톤)
// 사용법: public class GameManager : SingletonBehaviour<GameManager>
//
// <T> 자리에는 상속하는 클래스 자기 자신을 넣는다.
// 그러면 GameManager.Instance는 GameManager 타입, SoundManager.Instance는 SoundManager 타입이 되고,
// _instance도 클래스마다 따로 하나씩 생긴다.
//
// where T : SingletonBehaviour<T>
// -> T는 반드시 나를 상속한 클래스여야 한다. (그래서 this를 T로 바꿀 수 있다)
public class SingletonBehaviour<T> : MonoBehaviour where T : SingletonBehaviour<T>
{
    private static T _instance = null;
    public static T Instance => _instance;

    // 자식에서 Awake를 쓰고 싶으면 protected override void Awake()로 만들고
    // 맨 처음에 base.Awake()를 불러야 한다.
    protected virtual void Awake()
    {
        // 이미 같은 매니저가 있으면 나는 사라진다. (하나만 존재하도록)
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this as T;
    }

    // 내가 사라질 때 Instance도 비워 둔다. (씬을 다시 불러올 때 새 매니저가 들어올 수 있도록)
    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
