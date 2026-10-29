using System;
using UnityEngine;

// [재작성] 시점 관리자
// 역할: V 키로 6가지 시점을 순환하고, 시점마다 카메라 위치/회전과 조준 방식을 관리한다.
// - 화면 중앙 조준(1인칭/3인칭/숄더): 좌우는 플레이어(PlayerRotate)를 따라가고, 마우스 Y로 위아래만 담당한다.
// - 마우스 커서 조준(쿼터/탑다운/사이드): 시점에 들어온 순간의 방향으로 카메라를 고정하고, 커서가 조준점이 된다.
// [삭제] CameraRotate 사용 → 카메라 회전은 이 스크립트가 직접 담당 (몸과 카메라가 어긋나지 않도록)
public class CameraViewSwitcher : MonoBehaviour
{
    // 카메라 시점 종류
    public enum CameraViewType
    {
        FirstPerson, // 1인칭 (FPS)
        ThirdPerson, // 3인칭 (TPS)
        Shoulder, // 숄더뷰
        QuarterView, // 쿼터뷰
        TopDown, // 탑다운
        SideView // 사이드뷰
    }

    // [추가] 조준 방식
    public enum AimMode
    {
        ScreenCenter, // 화면 중앙(조준점)으로 쏜다
        MouseCursor // 마우스 커서가 가리키는 곳으로 쏜다
    }

    [Serializable]
    public struct CameraPreset
    {
        [Header("시점 이름")]
        public CameraViewType viewType;

        [Header("조준 방식")]
        // [추가] (기존 enableMouseRotate 대체)
        public AimMode aimMode;

        [Header("기준점(CameraPosition)에서의 상대 위치 및 회전")]
        public Vector3 localPosition; // 중앙 조준: 바라보는 방향 기준 / 커서 조준: 고정 방향 기준

        public Vector3 localRotation; // 추가 회전 (Euler)
    }

    [SerializeField] private Transform _target; // 카메라 기준점 (Player의 CameraPosition)
    [SerializeField] private Transform _player; // [추가] 좌우 회전의 기준 (Player)
    [SerializeField] private CameraPreset[] _presets; // 인스펙터에서 설정할 시점 프리셋 목록
    [SerializeField] private GameObject _crosshair; // [추가] 화면 중앙 조준점 (중앙 조준 시점에서만 표시)

    [Header("마우스 위아래")]
    // [추가] 기존 CameraRotate와 같은 값 → 1인칭 감각 유지
    [SerializeField] private float _rotationSpeed = 200f;

    [SerializeField] private float _minPitch = -90f;
    [SerializeField] private float _maxPitch = 90f;

    [Header("시점 전환")]
    // [추가]
    [SerializeField] private float _transitionDuration = 0.3f; // 전환 시 카메라가 미끄러지듯 이동하는 시간

    private int _currentIndex = 0; // 현재 시점 인덱스
    private float _pitch = 0f; // [추가] 위아래 각도 (중앙 조준 시점)
    private float _fixedYaw = 0f; // [추가] 커서 조준 시점에서 고정된 좌우 방향

    // [추가] 부드러운 전환용
    private float _transitionTimer = 0f;
    private Vector3 _transitionStartPosition;
    private Quaternion _transitionStartRotation;

    private Camera _camera;

    public CameraPreset CurrentPreset => _presets[_currentIndex];
    public bool IsCursorAim => CurrentPreset.aimMode == AimMode.MouseCursor;

    // [추가] 이동(WASD)의 기준 방향: 중앙 조준은 몸 방향, 커서 조준은 고정된 화면 방향
    public float MoveYaw => IsCursorAim ? _fixedYaw : _player.eulerAngles.y;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        _transitionTimer = _transitionDuration; // 시작할 때는 전환 없이 바로 자리 잡기

        if (_target == null || _player == null)
        {
            GameLogger.LogWarning("CameraViewSwitcher: Target 또는 Player가 지정되지 않았습니다.");
        }
    }

    private void Start()
    {
        ApplyAimMode(); // 첫 시점의 조준점/커서 상태 적용
    }

    private void Update()
    {
        // V 키를 누를 때마다 다음 시점으로 순환 변경
        if (Input.GetKeyDown(KeyCode.V))
        {
            CycleNextView();
        }

        if (GameManager.Instance.State != GameState.Playing) return;

        // [추가] 중앙 조준 시점: 마우스 Y로 위아래 회전 (기존 CameraRotate와 같은 계산 → 1인칭 감각 동일)
        if (!IsCursorAim && !CursorManager.Instance.IsUIMode)
        {
            float mouseY = Input.GetAxis("Mouse Y");
            _pitch += -mouseY * _rotationSpeed * Time.deltaTime;
            _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);
        }
    }

    // 플레이어가 Update에서 움직이고 돈 "뒤에" 카메라를 맞춘다.
    private void LateUpdate()
    {
        if (_target == null || _player == null || _presets == null || _presets.Length == 0) return;

        CalculateTargetPose(out Vector3 position, out Quaternion rotation);

        // [추가] 전환 중이면 이전 위치 → 새 위치로 부드럽게
        if (_transitionTimer < _transitionDuration)
        {
            _transitionTimer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, _transitionTimer / _transitionDuration); // 처음과 끝이 느린 곡선
            position = Vector3.Lerp(_transitionStartPosition, position, t);
            rotation = Quaternion.Slerp(_transitionStartRotation, rotation, t);
        }

        transform.SetPositionAndRotation(position, rotation);
    }

    // [추가] 현재 시점에서 카메라가 있어야 할 위치/회전 계산
    private void CalculateTargetPose(out Vector3 position, out Quaternion rotation)
    {
        CameraPreset preset = CurrentPreset;

        if (preset.aimMode == AimMode.ScreenCenter)
        {
            // 바라보는 방향 = 위아래(_pitch) + 몸의 좌우
            // 1인칭(localPosition 0): 기준점 자리에서 그대로 바라봄 → 기존과 동일
            // 3인칭/숄더: 기준점을 중심으로 궤도를 돌듯 뒤쪽에 위치 → 위를 보면 카메라가 내려가며 올려다봄
            Quaternion look = Quaternion.Euler(_pitch, _player.eulerAngles.y, 0f);
            position = _target.position + look * preset.localPosition;
            rotation = look * Quaternion.Euler(preset.localRotation);
        }
        else
        {
            // 고정 방향(_fixedYaw) 기준 → 플레이어가 커서를 보며 돌아도 카메라는 돌지 않는다.
            Quaternion yaw = Quaternion.Euler(0f, _fixedYaw, 0f);
            position = _target.position + yaw * preset.localPosition;
            rotation = yaw * Quaternion.Euler(preset.localRotation);
        }
    }

    private void CycleNextView()
    {
        if (_presets == null || _presets.Length == 0) return;

        // [추가] 지금 카메라 자리에서 출발해 부드럽게 이동
        _transitionStartPosition = transform.position;
        _transitionStartRotation = transform.rotation;
        _transitionTimer = 0f;

        // 인덱스를 1씩 증가시키며 순환 (0 -> 1 -> ... -> 5 -> 0)
        _currentIndex = (_currentIndex + 1) % _presets.Length;

        // [추가] 커서 조준 시점은 들어온 순간의 몸 방향으로 화면을 고정
        if (IsCursorAim)
        {
            _fixedYaw = _player.eulerAngles.y;
        }

        ApplyAimMode();
        GameLogger.Log($"시점 변경: {CurrentPreset.viewType}");
    }

    // [추가] 조준 방식에 맞게 조준점/커서 상태를 바꾼다.
    private void ApplyAimMode()
    {
        if (_presets == null || _presets.Length == 0) return;

        bool isCursorAim = IsCursorAim;

        if (_crosshair != null)
        {
            _crosshair.SetActive(!isCursorAim); // 중앙 조준일 때만 조준점 표시
        }

        CursorManager.Instance.SetCursorAimMode(isCursorAim); // 커서 조준일 때만 커서 표시
    }

    // [추가] 조준 레이: 총/폭탄이 "어디로 쏠지" 물어본다.
    // - 중앙 조준: 카메라 정면
    // - 커서 조준: origin(총구) → 커서가 가리키는 곳
    public Ray GetAimRay(Vector3 origin)
    {
        if (!IsCursorAim)
        {
            return new Ray(transform.position, transform.forward);
        }

        Vector3 direction = GetCursorAimPoint(origin) - origin;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = _player.forward; // 커서가 총구와 겹치면 정면으로
        }

        return new Ray(origin, direction.normalized);
    }

    // [추가] 커서가 가리키는 조준점 (origin 높이/위치를 지나는 판과 커서 레이가 만나는 점)
    public Vector3 GetCursorAimPoint(Vector3 origin)
    {
        Ray mouseRay = _camera.ScreenPointToRay(Input.mousePosition);

        // 위에서 내려다보는 시점(쿼터/탑다운): 총구 높이의 수평판 → 수평 사격
        // 옆에서 보는 시점(사이드): 플레이어를 지나는, 화면과 나란한 세로판 → 좌우 + 위아래 조준
        bool isLookingDown = Vector3.Dot(transform.forward, Vector3.down) > 0.5f;
        Vector3 planeNormal = isLookingDown ? Vector3.up : transform.forward;
        Plane aimPlane = new Plane(planeNormal, origin);

        if (aimPlane.Raycast(mouseRay, out float distance))
        {
            return mouseRay.GetPoint(distance);
        }

        return origin + _player.forward; // 판과 만나지 않으면(지평선 너머) 정면
    }
}