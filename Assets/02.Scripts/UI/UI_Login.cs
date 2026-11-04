using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 로그인 화면. 로그인 모드와 회원가입 모드를 바꾸고, 입력을 확인해 AccountStorage에 부탁한다.
public class UI_Login : MonoBehaviour
{
    private enum Mode
    {
        Login,     // 로그인
        Register,  // 회원가입
    }

    [Header("입력 칸")]
    [SerializeField] private TMP_InputField _idInput;
    [SerializeField] private TMP_InputField _passwordInput;
    [SerializeField] private TMP_InputField _passwordConfirmInput;  // 회원가입 모드에서만 보인다.

    [Header("버튼")]
    [SerializeField] private Button _loginButton;         // 로그인 모드: 로그인
    [SerializeField] private Button _gotoRegisterButton;  // 로그인 모드: 회원가입 모드로
    [SerializeField] private Button _registerButton;      // 회원가입 모드: 가입하기
    [SerializeField] private Button _gotoLoginButton;     // 회원가입 모드: 로그인 모드로

    [Header("기타")]
    [SerializeField] private TextMeshProUGUI _messageText;        // 안내 글자
    [SerializeField] private string _gameSceneName = "GameScene";  // 로그인하면 불러올 씬

    // ID 규칙: 이메일 (아이디@도메인.주소)
    // @ 앞: 영문, 숫자, . _ + - 를 1글자 이상
    // @ 뒤: 점으로 나뉜 두 덩어리 이상 (점이 @ 바로 뒤·맨 끝에 오거나 연달아 올 수 없다.)
    private static readonly Regex EmailRegex = new Regex(@"^[A-Za-z0-9._+-]+@[A-Za-z0-9_+-]+(\.[A-Za-z0-9_+-]+)+$");

    // 비밀번호 규칙: 대문자, 소문자, 숫자, 기호를 모두 포함한 8~30글자
    // (?=.*X)는 "어딘가에 X가 하나 이상 있다"는 조건이다. 기호는 영문·숫자가 아닌 문자다.
    // [!-~]는 공백을 뺀 키보드 문자(ASCII 33~126)라서 공백이나 한글은 쓸 수 없다.
    private static readonly Regex PasswordRegex = new Regex(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*[0-9])(?=.*[^A-Za-z0-9])[!-~]{8,30}$");

    private Mode _mode = Mode.Login;

    private void Start()
    {
        _loginButton.onClick.AddListener(Login);
        _gotoRegisterButton.onClick.AddListener(GotoRegister);
        _registerButton.onClick.AddListener(Register);
        _gotoLoginButton.onClick.AddListener(GotoLogin);
        Refresh();
    }

    // 지금 모드에 맞는 칸과 버튼만 보여 준다.
    private void Refresh()
    {
        bool isLogin = _mode == Mode.Login;
        _passwordConfirmInput.gameObject.SetActive(!isLogin);
        _loginButton.gameObject.SetActive(isLogin);
        _gotoRegisterButton.gameObject.SetActive(isLogin);
        _registerButton.gameObject.SetActive(!isLogin);
        _gotoLoginButton.gameObject.SetActive(!isLogin);
    }

    private void GotoRegister()
    {
        _mode = Mode.Register;
        _messageText.text = "";
        Refresh();
    }

    private void GotoLogin()
    {
        _mode = Mode.Login;
        _messageText.text = "";
        Refresh();
    }

    // 로그인: 입력을 확인하고, 맞으면 게임 씬을 불러온다.
    private void Login()
    {
        string id = _idInput.text;
        string password = _passwordInput.text;

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

        SceneManager.LoadScene(_gameSceneName);
    }

    // 회원가입: 입력을 확인하고, 문제가 없으면 계정을 저장한 뒤 로그인 모드로 돌아간다.
    private void Register()
    {
        string id = _idInput.text;
        string password = _passwordInput.text;
        string passwordConfirm = _passwordConfirmInput.text;

        // 정규표현식 (정규식)
        // 문자열에서 특정한 규칙이나 패턴을 찾기 위해 사용하느 문법
        // - 데이터 검증(이메일 형식, 주민번호 형식, 비밀번호 형식)
        // - 문자열 검색 (Todo라는게 있나?, 한국어가 있나?)
        // - 문자열 치환 (규칙에 맞는 문자열을 찾아서 다른 문자열로 변경)
        // - 웹 크롤링 & 파싱

        // Todo: ID, (닉네임)과 비밀번호 규칙
        // ID는 이메일
        // 비밀번호는 영어만 가능, 특수문자 대문자 하나 이상을 포함한 7 ~ 15글자
        // 규칙이 여기서만 쓰일까? (비밀번호 변경 페이지, 닉네임 변경권 아이템 사용시 등등)

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(password))
        {
            _messageText.text = "아이디와 비밀번호를 입력해 주세요.";
            return;
        }

        if (!EmailRegex.IsMatch(id))
        {
            _messageText.text = "아이디는 이메일 형식이어야 합니다.";
            return;
        }

        if (!PasswordRegex.IsMatch(password))
        {
            _messageText.text = "비밀번호는 대문자, 소문자, 숫자, 기호를 모두 포함한 8~30글자여야 합니다.";
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
        _messageText.text = "가입했습니다. 로그인해 주세요.";
    }
}