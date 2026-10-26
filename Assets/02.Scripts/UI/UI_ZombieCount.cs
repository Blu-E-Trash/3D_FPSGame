using TMPro;
using UnityEngine;

// 화면 위쪽 가운데에 남은 좀비 수를 보여 준다.
// Zombie.AliveCountChanged 이벤트를 구독해서, 좀비 수가 바뀔 때만 글자를 갱신한다.
public class UI_ZombieCount : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _countText;

    private void OnEnable()
    {
        Zombie.AliveCountChanged += Refresh;
    }

    private void OnDisable()
    {
        Zombie.AliveCountChanged -= Refresh;
    }

    private void Start()
    {
        // 시작할 때의 좀비 수를 한 번 표시한다. (모든 좀비의 Awake가 끝난 뒤)
        Refresh(Zombie.AliveCount);
    }

    private void Refresh(int aliveCount)
    {
        _countText.text = $"남은 좀비: {aliveCount}";
    }
}
