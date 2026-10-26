// 폭탄 오브젝트 풀. 실제 풀 기능은 ObjectPool<T>에 있다.
// 풀은 성능만 담당한다. 몇 개를 던질 수 있는지(게임 규칙)는 PlayerBombFire가 정한다.
public class BombPool : ObjectPool<Bomb>
{
    protected override void OnCreated(Bomb bomb)
    {
        bomb.SetPool(this);
    }
}
