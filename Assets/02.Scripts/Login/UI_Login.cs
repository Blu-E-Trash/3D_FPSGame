using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 로그인 화면. 로그인 모드와 회원가입 모드를 바꾸고, 입력을 확인해 AccountStorage에 부탁한다.
// 이 씬은 로그인 화면과 회원가입 화면이 따로 있어서, 모드를 바꿀 때 UI_LoginViewSwitch에 화면 전환을 맡긴다.
public class UI_Login : MonoBehaviour
{
    [Header("로그인 화면 입력 칸")]
    [SerializeField] private TMP_InputField _loginIdInput;
    [SerializeField] private TMP_InputField _loginPasswordInput;

    [Header("회원가입 화면 입력 칸")]
    [SerializeField] private TMP_InputField _registerIdInput;
    [SerializeField] private TMP_InputField _registerPasswordInput;
    [SerializeField] private TMP_InputField _passwordConfirmInput;

    [Header("버튼")]
    [SerializeField] private Button _loginButton;         // 로그인 모드: 로그인
    [SerializeField] private Button _gotoRegisterButton;  // 로그인 모드: 회원가입 모드로
    [SerializeField] private Button _registerButton;      // 회원가입 모드: 가입하기
    [SerializeField] private Button _gotoLoginButton;     // 회원가입 모드: 로그인 모드로

    [Header("기타")]
    [SerializeField] private UI_LoginViewSwitch _viewSwitch;        // 로그인 화면 ↔ 회원가입 화면 전환
    [SerializeField] private TextMeshProUGUI _messageText;          // 안내 글자
    [SerializeField] private string _gameSceneName = "SampleScene"; // 로그인하면 불러올 씬

    // 아이디 규칙: 이메일
    // (영문, 숫자, ., _, -)@(영문, 숫자, -)(.영문, 숫자, -)를 1번 이상. 예) abc.def@gmail.com, user_01@mail.co.kr
    private static readonly Regex EmailRegex = new Regex(@"^[A-Za-z0-9._-]+@[A-Za-z0-9-]+(\.[A-Za-z0-9-]+)+$");

    // 비밀번호 규칙: 대문자, 소문자, 숫자, 기호를 모두 포함한 8~30 글자
    // 전방탐색 (?=...)으로 대문자, 소문자, 숫자, 기호가 각각 하나 이상 있는지 보고,
    // 전체는 공백을 뺀 ASCII 출력 문자([!-~]) 8~30 글자인지 본다.
    // 기호 = [!-/] [:-@] [\[-`] [{-~] (ASCII 출력 문자 중 영문·숫자가 아닌 범위)
    private static readonly Regex PasswordRegex = new Regex(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*[0-9])(?=.*[!-/:-@\[-`{-~])[!-~]{8,30}$");

    private void Start()
    {
        _loginButton.onClick.AddListener(Login);
        _gotoRegisterButton.onClick.AddListener(GotoRegister);
        _registerButton.onClick.AddListener(Register);
        _gotoLoginButton.onClick.AddListener(GotoLogin);
        _messageText.text = "";
    }

    private void GotoRegister()
    {
        _messageText.text = "";
        _viewSwitch.ShowSignup();
    }

    private void GotoLogin()
    {
        _messageText.text = "";
        _viewSwitch.ShowLogin();
    }

    // 로그인: 입력을 확인하고, 맞으면 게임 씬을 불러온다.
    private void Login()
    {
        string id = _loginIdInput.text;
        string password = _loginPasswordInput.text;

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(password))
        {
            _messageText.text = "아이디와 비밀번호를 입력해 주세요.";
            return;
        }

        if (!AccountStorage.CheckPassword(id, password))
        {
            // 아이디가 없는지 비밀번호가 틀렸는지는 알려 주지 않는다.
            _messageText.text = "아이디 또는 비밀번호가 맞지 않습니다.";
            return;
        }

        // 게임 씬은 무거워서 바로 불러오면 화면이 멈춘다.
        // 로딩 씬을 먼저 띄우고, 그곳에서 게임 씬을 비동기(LoadSceneAsync)로 불러온다.
        LoadingSceneController.LoadScene(_gameSceneName);
    }

    // 회원가입: 입력을 확인하고, 문제가 없으면 계정을 저장한 뒤 로그인 모드로 돌아간다.
    private void Register()
    {
        string id = _registerIdInput.text;
        string password = _registerPasswordInput.text;
        string passwordConfirm = _passwordConfirmInput.text;

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(password))
        {
            _messageText.text = "아이디와 비밀번호를 입력해 주세요.";
            return;
        }

        // 아이디 규칙: 이메일
        if (!EmailRegex.IsMatch(id))
        {
            _messageText.text = "아이디는 이메일 형식이어야 합니다.";
            return;
        }

        // 비밀번호 규칙: 대문자, 소문자, 숫자, 기호를 모두 포함한 8~30 글자
        if (!PasswordRegex.IsMatch(password))
        {
            _messageText.text = "비밀번호는 대문자, 소문자, 숫자, 기호를 모두 포함한 8~30 글자여야 합니다.";
            return;
        }

        if (password != passwordConfirm)
        {
            _messageText.text = "비밀번호 확인이 맞지 않습니다.";
            return;
        }

        if (AccountStorage.Exists(id))
        {
            _messageText.text = "이미 있는 아이디입니다.";
            return;
        }

        AccountStorage.Register(id, password);
        GotoLogin();
        _loginIdInput.text = id;  // 교재는 칸을 같이 써서 아이디가 남는다. 여기서도 로그인 칸에 채워 둔다.
        _messageText.text = "가입했습니다. 로그인해 주세요.";
    }
}
