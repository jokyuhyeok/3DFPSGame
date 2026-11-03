using System.Collections;
using UnityEngine;

// 타이틀 화면 등장 연출
// 제목이 크게 떨어지듯 쾅 내려앉으며 화면이 흔들리고, 이어서 시작 버튼도 진동과 함께 나타난다.
// 버튼은 다 나타나기 전까지 눌리지 않는다.
public class UI_TitleIntro : MonoBehaviour
{
    [SerializeField] private RectTransform _title;
    [SerializeField] private RectTransform _button;
    [SerializeField] private TitleCameraSway _cameraSway; // 함께 흔들 카메라 (없어도 됨)

    [Header("타이밍")]
    [SerializeField] private float _startDelay = 0.6f; // 씬 시작 후 제목이 나오기까지

    [SerializeField] private float _buttonDelay = 0.4f; // 제목 진동이 끝난 뒤 버튼이 나오기까지

    [Header("등장")]
    [SerializeField] private float _popScale = 1.8f; // 이 크기에서 시작해 원래 크기로 내려앉는다.

    [SerializeField] private float _popTime = 0.18f;

    [Header("진동")]
    [SerializeField] private float _shakeTime = 0.45f;

    [SerializeField] private float _titleShakeAmount = 25f; // 픽셀
    [SerializeField] private float _buttonShakeAmount = 12f;
    [SerializeField] private float _titleCameraShake = 0.12f; // 미터
    [SerializeField] private float _buttonCameraShake = 0.05f;

    private CanvasGroup _titleGroup;
    private CanvasGroup _buttonGroup;

    private void Awake()
    {
        _titleGroup = GetOrAddCanvasGroup(_title);
        _buttonGroup = GetOrAddCanvasGroup(_button);

        // 처음에는 숨기고, 버튼은 눌리지 않게 한다.
        _titleGroup.alpha = 0f;
        _buttonGroup.alpha = 0f;
        _buttonGroup.interactable = false;
        _buttonGroup.blocksRaycasts = false;
    }

    private void Start()
    {
        StartCoroutine(Intro_Coroutine());
    }

    private IEnumerator Intro_Coroutine()
    {
        yield return new WaitForSeconds(_startDelay);
        yield return Appear_Coroutine(_title, _titleGroup, _titleShakeAmount, _titleCameraShake, true);

        // 버튼은 반투명해지면 아래에 깔린 Outline 색이 비쳐 보이므로, 서서히 나타나지 않고 바로 불투명하게 내려앉는다.
        yield return new WaitForSeconds(_buttonDelay);
        yield return Appear_Coroutine(_button, _buttonGroup, _buttonShakeAmount, _buttonCameraShake, false);

        _buttonGroup.interactable = true;
        _buttonGroup.blocksRaycasts = true;
    }

    private IEnumerator Appear_Coroutine(RectTransform target, CanvasGroup group, float shakeAmount, float cameraShake, bool isFadeIn)
    {
        Vector3 baseScale = target.localScale;
        Vector2 basePosition = target.anchoredPosition;
        if (!isFadeIn) group.alpha = 1f;

        // 1. 크게 시작해서 원래 크기로 쾅 내려앉는다. (점점 빨라지게)
        float time = 0f;
        while (time < _popTime)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / _popTime);
            float eased = t * t;

            target.localScale = baseScale * Mathf.Lerp(_popScale, 1f, eased);
            if (isFadeIn) group.alpha = t;
            yield return null;
        }

        target.localScale = baseScale;
        group.alpha = 1f;

        // 2. 내려앉는 순간 진동한다. (점점 잦아든다)
        if (_cameraSway != null) _cameraSway.Shake(cameraShake, _shakeTime);

        time = 0f;
        while (time < _shakeTime)
        {
            time += Time.deltaTime;
            float strength = 1f - time / _shakeTime;
            target.anchoredPosition = basePosition + Random.insideUnitCircle * (shakeAmount * strength);
            yield return null;
        }

        target.anchoredPosition = basePosition;
    }

    private static CanvasGroup GetOrAddCanvasGroup(RectTransform target)
    {
        CanvasGroup group = target.GetComponent<CanvasGroup>();
        if (group == null) group = target.gameObject.AddComponent<CanvasGroup>();
        return group;
    }
}
