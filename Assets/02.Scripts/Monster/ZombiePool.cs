// 좀비 오브젝트 풀. 실제 풀 기능은 ObjectPool<T>에 있다.
// 좀비는 죽으면 Destroy 되지 않고 풀로 돌아왔다가, 스포너가 다시 꺼내 쓴다.
public class ZombiePool : ObjectPool<Zombie>
{
    protected override void OnCreated(Zombie zombie)
    {
        zombie.SetPool(this);
    }
}
