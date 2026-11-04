using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// 게임의 진행 상태
public enum GameState
{
    Ready,      // 준비: 시작 전 (조작 불가)
    Playing,    // 진행 중
    Pause,      // 일시정지 (조작 불가, 시간 멈춤)
    GameOver    // 게임 오버
}

// 게임 전체의 상태를 관리한다. 씬에 하나만 존재한다. (싱글톤)
public class GameManager : SingletonBehaviour<GameManager>
{
    private GameState _state = GameState.Ready;
    public GameState State => _state;

    [SerializeField] private TextMeshProUGUI _stateText; // 화면 가운데에 상태를 알려주는 글자 (준비, 시작!)
    [SerializeField] private Animator _playerAnimator;     // 게임오버 때 군인을 서 있는 동작으로 돌리기 위해
    [SerializeField] private UI_PausePopup _pausePopup;    // ESC를 누르면 켜지는 일시정지 창
    [SerializeField] private UI_GameOverPopup _gameOverPopup; // 플레이어가 죽으면 켜지는 게임오버 창

    protected override void Awake()
    {
        base.Awake(); // 싱글톤 처리 (이미 있으면 나는 사라진다)

        // 내가 진짜 게임 매니저가 아니면(중복이라 사라지는 중이면) 여기서 멈춘다.
        if (Instance != this) return;

        // 다시 시작으로 씬을 새로 불러왔을 때를 대비해 시간과 소리를 원래대로 돌려 놓는다.
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    private void Start()
    {
        // 일시정지 창과 게임오버 창은 처음에 꺼 둔다.
        if (_pausePopup != null)
        {
            _pausePopup.Hide();
        }

        if (_gameOverPopup != null)
        {
            _gameOverPopup.Hide();
        }

        StartCoroutine(StartSequence_Coroutine());
    }

    private void Update()
    {
        // ESC로 일시정지를 켜고 끈다. (게임 중이거나 일시정지 중일 때만)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_state == GameState.Playing)
            {
                Pause();
            }
            else if (_state == GameState.Pause)
            {
                Continue();
            }
        }
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

    // 일시정지: 시간과 소리를 멈추고 일시정지 창을 띄운다.
    public void Pause()
    {
        if (_state != GameState.Playing) return;

        _state = GameState.Pause;

        // 시간이 흐르지 않으니 물리, 애니메이션, 좀비, 폭탄, 파티클이 모두 멈춘다.
        // (UI 버튼은 시간과 상관없이 동작한다.)
        Time.timeScale = 0f;
        AudioListener.pause = true;

        // 마우스 커서를 풀어서 버튼을 누를 수 있게 한다.
        if (CursorManager.Instance != null)
        {
            CursorManager.Instance.UnlockCursor();
        }

        if (_pausePopup != null)
        {
            _pausePopup.Show();
        }
    }

    // 계속하기: 일시정지 창을 닫고 게임을 이어서 진행한다.
    public void Continue()
    {
        if (_state != GameState.Pause) return;

        if (_pausePopup != null)
        {
            _pausePopup.Hide();
        }

        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (CursorManager.Instance != null)
        {
            CursorManager.Instance.LockCursor();
        }

        _state = GameState.Playing;
    }

    // 다시 시작: 지금 씬을 처음부터 다시 불러온다.
    public void Restart()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        // 로딩 화면을 지나 지금 씬을 처음부터 다시 불러온다.
        LoadingSceneController.LoadScene(SceneManager.GetActiveScene().name);
    }

    // 게임 종료: 에디터에서는 플레이를 멈추고, 빌드된 게임에서는 프로그램을 끈다.
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // 게임 오버: 모든 조작을 멈추고 게임오버 창을 띄운다.
    public void GameOver()
    {
        _state = GameState.GameOver;

        // 마우스 커서를 풀어서 화면의 버튼 등을 누를 수 있게 한다.
        if (CursorManager.Instance != null)
        {
            CursorManager.Instance.UnlockCursor();
        }

        // 플레이어는 게임 중이 아니면 움직임 처리를 멈추니까, 달리던 중이었으면 계속 제자리에서 달리게 된다.
        // 그래서 서 있는 동작으로 돌려 놓는다.
        if (_playerAnimator != null)
        {
            _playerAnimator.SetFloat("MoveMotion", 0f);
        }

        // 다시 시작 / 게임 종료를 고를 수 있는 게임오버 창을 띄운다.
        if (_gameOverPopup != null)
        {
            _gameOverPopup.Show();
        }
    }
}
