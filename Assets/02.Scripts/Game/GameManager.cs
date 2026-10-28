using System.Collections;
using TMPro;
using UnityEngine;

// 게임의 진행 상태
public enum GameState
{
    Ready,      // 준비: 시작 전 (조작 불가)
    Playing,    // 진행 중
    GameOver    // 게임 오버
}

// 게임 전체의 상태를 관리한다. 씬에 하나만 존재한다. (싱글톤)
public class GameManager : MonoBehaviour
{
    private static GameManager _instance = null;
    public static GameManager Instance => _instance;

    private GameState _state = GameState.Ready;
    public GameState State => _state;

    [SerializeField] private TextMeshProUGUI _stateText; // 화면 가운데에 상태를 알려주는 글자 (준비, 시작!)
    [SerializeField] private CursorManager _cursorManager; // 게임오버 때 마우스 커서를 풀어주기 위해
    [SerializeField] private Animator _playerAnimator;     // 게임오버 때 군인을 서 있는 동작으로 돌리기 위해

    private void Awake()
    {
        // 이미 게임 매니저가 있으면 나는 사라진다. (하나만 존재하도록)
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    private void Start()
    {
        StartCoroutine(StartSequence_Coroutine());
    }

    // "준비" -> "시작!"을 보여준 뒤 게임을 시작한다.
    private IEnumerator StartSequence_Coroutine()
    {
        _stateText.gameObject.SetActive(true);

        _stateText.text = "준비";
        yield return new WaitForSeconds(1.5f);

        _stateText.text = "시작!";
        yield return new WaitForSeconds(0.5f);

        _stateText.gameObject.SetActive(false);

        _state = GameState.Playing;
    }

    // 게임 오버: 모든 조작을 멈추고 화면에 "게임오버"를 띄운다.
    public void GameOver()
    {
        _state = GameState.GameOver;

        // 마우스 커서를 풀어서 화면의 버튼 등을 누를 수 있게 한다.
        if (_cursorManager != null)
        {
            _cursorManager.UnlockCursor();
        }

        // 플레이어는 게임 중이 아니면 움직임 처리를 멈추니까, 달리던 중이었으면 계속 제자리에서 달리게 된다.
        // 그래서 서 있는 동작으로 돌려 놓는다.
        if (_playerAnimator != null)
        {
            _playerAnimator.SetFloat("MoveMotion", 0f);
        }

        _stateText.gameObject.SetActive(true);
        _stateText.text = "게임오버";
        _stateText.color = Color.red;
    }
}
