using System.Collections;
using UnityEngine;

// 타이틀 오프닝 연출
// 1. 데드풀이 검을 든 자세로 클로즈업된다.
// 2. 검을 휘두르는 순간 불꽃 검기가 터지며 화면을 덮는다.
// 3. 덮인 사이에 넓은 구도로 바뀌어, 데드풀을 가운데 두고 좀비들이 양옆에 선 장면이 드러난다.
// 4. UI_Title을 보여 준다. 연출 중 화면을 누르면 바로 3의 장면으로 건너뛴다.
public class TitleSequence : MonoBehaviour
{
    [Header("카메라")]
    [SerializeField] private Transform _cameraRig; // 카메라의 부모. 흔들림(TitleCameraSway)은 카메라 쪽에서 따로 준다.

    [SerializeField] private Transform _closeUpPoint;
    [SerializeField] private Transform _widePoint;
    [SerializeField] private TitleCameraSway _cameraSway;
    [SerializeField] private float _dollyDistance = 0.35f; // 클로즈업에서 천천히 다가가는 거리

    [Header("데드풀")]
    [SerializeField] private TitlePose _hero;

    [SerializeField] private AnimationClip _stanceClip; // 검을 든 자세
    [SerializeField] private AnimationClip _slashClip;
    [SerializeField] private AnimationClip _finalClip; // 장면이 드러난 뒤의 자세

    [Header("좀비")]
    [SerializeField] private GameObject[] _zombies; // 장면이 드러날 때 나타난다.

    [Header("이펙트")]
    [SerializeField] private ParticleSystem _slashEffect;

    [SerializeField] private UI_FireFlash _fireFlash;
    [SerializeField] private float _slashShake = 0.08f; // 미터

    [Header("UI")]
    [SerializeField] private UI_Title _titleUI;

    [SerializeField] private CanvasGroup _fade; // 시작할 때 걷히는 검은 판

    [Header("타이밍")]
    [SerializeField] private float _fadeInTime = 0.7f;

    [SerializeField] private float _poseTime = 1.6f; // 검을 든 자세로 버티는 시간
    [SerializeField] private float _impactDelay = 0.2f; // 휘두르기 시작부터 검기가 터지기까지
    [SerializeField] private float _coverTime = 0.3f;
    [SerializeField] private float _holdTime = 0.45f; // 화면이 덮여 있는 시간
    [SerializeField] private float _revealTime = 1.1f;

    private Coroutine _introCoroutine;
    private bool _isFinished = false;

    private void Start()
    {
        foreach (GameObject zombie in _zombies)
        {
            zombie.SetActive(false);
        }

        MoveCamera(_closeUpPoint);
        _hero.Play(_stanceClip);
        _introCoroutine = StartCoroutine(Intro_Coroutine());
    }

    private void Update()
    {
        if (_isFinished) return;

        // 연출 건너뛰기
        if (Input.GetMouseButtonDown(0))
        {
            StopCoroutine(_introCoroutine);
            _fireFlash.Hide();
            _fade.alpha = 0f;
            ShowFinalScene();
            Finish();
        }
    }

    private IEnumerator Intro_Coroutine()
    {
        // 1. 어둠이 걷히며 클로즈업. 카메라가 천천히 다가간다.
        Vector3 dollyStart = _closeUpPoint.position;
        Vector3 dollyEnd = dollyStart + _closeUpPoint.forward * _dollyDistance;

        float time = 0f;
        while (time < _poseTime)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / _poseTime);

            _fade.alpha = 1f - Mathf.Clamp01(time / _fadeInTime);
            _cameraRig.position = Vector3.Lerp(dollyStart, dollyEnd, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        _fade.alpha = 0f;

        // 2. 휘두르는 순간 검기가 터지고 불꽃이 화면을 덮는다.
        _hero.Play(_slashClip);
        yield return new WaitForSeconds(_impactDelay);

        _slashEffect.Play(true);
        _cameraSway.Shake(_slashShake, _coverTime + _holdTime);
        yield return _fireFlash.Cover_Coroutine(_coverTime);

        // 3. 덮여 있는 사이에 장면을 바꾼다.
        ShowFinalScene();
        yield return new WaitForSeconds(_holdTime);
        yield return _fireFlash.Reveal_Coroutine(_revealTime);

        Finish();
    }

    private void ShowFinalScene()
    {
        MoveCamera(_widePoint);
        _hero.Play(_finalClip);

        foreach (GameObject zombie in _zombies)
        {
            zombie.SetActive(true);
        }
    }

    private void Finish()
    {
        _isFinished = true;
        _titleUI.Show();
    }

    private void MoveCamera(Transform point)
    {
        _cameraRig.SetPositionAndRotation(point.position, point.rotation);
    }
}
