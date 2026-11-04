using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// 타이틀 화면 UI: 로고·버전·저작권 표시와 "화면을 터치해주세요" 안내
// 연출이 끝나 Show()가 불리면 나타나고, 그 뒤 화면 아무 곳이나 누르면 어두워지며 로그인 씬으로 넘어간다.
public class UI_Title : MonoBehaviour
{
    [SerializeField] private string _nextSceneName = "LoginScene";

    [Header("구성")]
    [SerializeField] private CanvasGroup _hudGroup; // 로고·버전·저작권을 묶은 그룹

    [SerializeField] private RectTransform _logo;
    [SerializeField] private TMP_Text _versionText;
    [SerializeField] private CanvasGroup _touchGroup; // "화면을 터치해주세요"
    [SerializeField] private CanvasGroup _fade; // 화면 전체를 덮는 검은 판

    [Header("등장")]
    [SerializeField] private float _showTime = 0.8f;

    [SerializeField] private float _logoPopScale = 1.25f;
    [SerializeField] private float _touchDelay = 0.5f; // 로고가 나온 뒤 안내 문구가 나오기까지

    [Header("안내 문구 떠다니기")]
    [SerializeField] private float _floatHeight = 12f; // 픽셀

    [SerializeField] private float _floatSpeed = 2.2f; // 라디안/초
    [SerializeField] private Vector2 _blinkAlpha = new Vector2(0.45f, 1f);

    [Header("전환")]
    [SerializeField] private float _fadeOutTime = 0.6f;

    private RectTransform _touchRect;
    private Vector2 _touchBasePosition;
    private float _floatTime = 0f;
    private bool _isFloating = false;
    private bool _canTouch = false;
    private int _showFrame = -1;

    private void Awake()
    {
        _touchRect = (RectTransform)_touchGroup.transform;
        _touchBasePosition = _touchRect.anchoredPosition;

        _versionText.text = Application.version;

        _hudGroup.alpha = 0f;
        _touchGroup.alpha = 0f;
    }

    // 연출이 끝나면 TitleSequence가 부른다.
    public void Show()
    {
        _showFrame = Time.frameCount;
        StartCoroutine(Show_Coroutine());
    }

    private IEnumerator Show_Coroutine()
    {
        // 로고는 조금 크게 시작해 제자리로 내려앉는다.
        Vector3 logoScale = _logo.localScale;
        float time = 0f;
        while (time < _showTime)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / _showTime);
            float eased = 1f - (1f - t) * (1f - t) * (1f - t);

            _hudGroup.alpha = t;
            _logo.localScale = logoScale * Mathf.Lerp(_logoPopScale, 1f, eased);
            yield return null;
        }

        _hudGroup.alpha = 1f;
        _logo.localScale = logoScale;

        yield return new WaitForSeconds(_touchDelay);
        _isFloating = true;
        _canTouch = true;
    }

    private void Update()
    {
        if (_isFloating) Float();

        // Show를 부른 그 클릭(연출 건너뛰기)으로 바로 넘어가지 않도록 다음 프레임부터 받는다.
        if (!_canTouch || Time.frameCount <= _showFrame) return;

        // 마우스 클릭과 모바일 터치 모두 GetMouseButtonDown(0)으로 들어온다.
        if (Input.GetMouseButtonDown(0))
        {
            _canTouch = false;
            StartCoroutine(LoadNext_Coroutine());
        }
    }

    // 위아래로 둥둥 떠다니며 천천히 깜빡인다.
    private void Float()
    {
        _floatTime += Time.deltaTime;
        float wave = Mathf.Sin(_floatTime * _floatSpeed);

        _touchRect.anchoredPosition = _touchBasePosition + Vector2.up * (wave * _floatHeight);

        // 처음 나타날 때는 서서히 밝아지고, 그 뒤로는 물결에 맞춰 깜빡인다.
        float appear = Mathf.Clamp01(_floatTime / 0.6f);
        _touchGroup.alpha = appear * Mathf.Lerp(_blinkAlpha.x, _blinkAlpha.y, (wave + 1f) * 0.5f);
    }

    private IEnumerator LoadNext_Coroutine()
    {
        _fade.blocksRaycasts = true;

        float time = 0f;
        while (time < _fadeOutTime)
        {
            time += Time.deltaTime;
            _fade.alpha = Mathf.Clamp01(time / _fadeOutTime);
            yield return null;
        }

        SceneManager.LoadScene(_nextSceneName);
    }
}
