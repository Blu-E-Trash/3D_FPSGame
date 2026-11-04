using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 로그인 화면. 입력을 확인해 AccountStorage에 부탁하고, 결과를 안내 글자로 보여 준다. (26장 UI_Login)
// 이 프로젝트의 감시 카메라 화면은 로그인 화면과 회원가입 화면이 따로 있어서
// 두 모드 사이를 오가는 연출은 UI_LoginViewSwitch가 맡고, 이 스크립트는 확인과 안내만 맡는다.
public class UI_Login : MonoBehaviour
{
    [Header("로그인 모드 입력 칸")]
    [SerializeField] private TMP_InputField _loginIdInput;
    [SerializeField] private TMP_InputField _loginPasswordInput;

    [Header("회원가입 모드 입력 칸")]
    [SerializeField] private TMP_InputField _registerIdInput;
    [SerializeField] private TMP_InputField _nicknameInput;
    [SerializeField] private TMP_InputField _registerPasswordInput;
    [SerializeField] private TMP_InputField _passwordConfirmInput;

    [Header("버튼")]
    [SerializeField] private Button _loginButton;         // 로그인 모드: 로그인
    [SerializeField] private Button _gotoRegisterButton;  // 로그인 모드: 회원가입 모드로
    [SerializeField] private Button _registerButton;      // 회원가입 모드: 가입하기
    [SerializeField] private Button _gotoLoginButton;     // 회원가입 모드: 로그인 모드로

    [Header("기타")]
    [SerializeField] private UI_LoginViewSwitch _viewSwitch;
    [SerializeField] private TextMeshProUGUI _messageText;          // 안내 글자
    [SerializeField] private RectTransform _shakeTarget;            // 틀렸을 때 흔들 단말기 창
    [SerializeField] private CanvasGroup _blackout;                 // 게임으로 넘어갈 때 덮을 검은 화면
    [SerializeField] private string _gameSceneName = "SampleScene"; // 로그인하면 불러올 씬

    [Header("안내 글자 색")]
    [SerializeField] private Color _errorColor = new Color(1f, 0.35f, 0.35f);
    [SerializeField] private Color _okColor = new Color(0.79f, 1f, 0.82f);

    private bool _entering;  // 게임으로 넘어가는 중이면 더 받지 않는다.
    private Coroutine _shake;

    private void Start()
    {
        _loginButton.onClick.AddListener(Login);
        _registerButton.onClick.AddListener(Register);
        // 모드를 바꾸는 화면 전환은 UI_LoginViewSwitch가 하고, 여기서는 안내 글자만 지운다.
        _gotoRegisterButton.onClick.AddListener(ClearMessage);
        _gotoLoginButton.onClick.AddListener(ClearMessage);

        // 비밀번호 칸에서 Enter를 누르면 바로 로그인/가입한다.
        _loginPasswordInput.onSubmit.AddListener(_ => Login());
        _passwordConfirmInput.onSubmit.AddListener(_ => Register());

        ClearMessage();
    }

    private void ClearMessage()
    {
        _messageText.text = "";
    }

    // 로그인: 입력을 확인하고, 맞으면 게임 씬을 불러온다.
    private void Login()
    {
        if (_entering) return;

        string id = _loginIdInput.text;
        string password = _loginPasswordInput.text;

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(password))
        {
            ShowError("아이디와 비밀번호를 입력해 주세요.");
            return;
        }

        if (!AccountStorage.CheckPassword(id, password))
        {
            // 아이디가 없는지 비밀번호가 틀렸는지는 알려 주지 않는다.
            ShowError("아이디 또는 비밀번호가 맞지 않습니다.");
            _loginPasswordInput.text = "";
            return;
        }

        StartCoroutine(EnterGame(AccountStorage.GetNickname(id)));
    }

    // 회원가입: 입력을 확인하고, 문제가 없으면 계정을 저장한 뒤 로그인 모드로 돌아간다.
    private void Register()
    {
        string id = _registerIdInput.text;
        string nickname = _nicknameInput.text;
        string password = _registerPasswordInput.text;
        string passwordConfirm = _passwordConfirmInput.text;

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(password))
        {
            ShowError("아이디와 비밀번호를 입력해 주세요.");
            return;
        }

        if (password != passwordConfirm)
        {
            ShowError("비밀번호 확인이 맞지 않습니다.");
            return;
        }

        if (AccountStorage.Exists(id))
        {
            ShowError("이미 있는 아이디입니다.");
            return;
        }

        AccountStorage.Register(id, password);
        AccountStorage.SetNickname(id, string.IsNullOrEmpty(nickname) ? id : nickname);

        // 로그인 화면으로 돌아가면서 방금 만든 아이디를 채워 둔다.
        _viewSwitch.ShowLogin();
        _loginIdInput.text = id;
        _loginPasswordInput.text = "";
        _registerIdInput.text = _nicknameInput.text = _registerPasswordInput.text = _passwordConfirmInput.text = "";
        ShowOk("가입했습니다. 로그인해 주세요.");
    }

    // ---------- 안내와 연출 ----------

    private void ShowError(string message)
    {
        _messageText.color = _errorColor;
        _messageText.text = $"ACCESS DENIED · {message}";
        LoginSfx.Instance?.Denied();
        UI_CCTVFeed.Instance?.Glitch(0.1f);
        if (_shake != null) StopCoroutine(_shake);
        _shake = StartCoroutine(Shake());
    }

    private void ShowOk(string message)
    {
        _messageText.color = _okColor;
        _messageText.text = message;
    }

    // 틀리면 단말기 창이 좌우로 짧게 떨린다.
    private IEnumerator Shake()
    {
        Vector2 home = _shakeTarget.anchoredPosition;
        for (float t = 0f; t < 0.3f; t += Time.unscaledDeltaTime)
        {
            float power = 14f * (1f - t / 0.3f);
            _shakeTarget.anchoredPosition = home + new Vector2(Mathf.Sin(t * 90f) * power, 0f);
            yield return null;
        }
        _shakeTarget.anchoredPosition = home;
        _shake = null;
    }

    // 로그인 성공: 승인 표시 → 화면이 크게 찢어지고 → 검게 꺼진 뒤 게임 씬으로 넘어간다.
    private IEnumerator EnterGame(string nickname)
    {
        _entering = true;
        _loginButton.interactable = false;
        _gotoRegisterButton.interactable = false;

        ShowOk($"ACCESS GRANTED · 환영합니다, {nickname}");
        LoginSfx.Instance?.Granted();
        yield return new WaitForSecondsRealtime(0.9f);

        UI_CCTVFeed.Instance?.Glitch(0.5f);
        yield return new WaitForSecondsRealtime(0.35f);

        _blackout.blocksRaycasts = true;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.4f)
        {
            _blackout.alpha = t;
            yield return null;
        }
        _blackout.alpha = 1f;

        SceneManager.LoadScene(_gameSceneName);
    }
}
