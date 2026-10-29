using UnityEngine;

// 마우스 오른쪽 버튼을 누르면 조준 방향으로 폭탄을 던진다.
// [변경] 커서 조준 시점에서는 커서 쪽으로 포물선을 그리며 던진다.
public class PlayerBombFire : MonoBehaviour
{
    [SerializeField] private Transform _firePoint;
    [SerializeField] private Bomb _bombPrefab;
    [SerializeField] private CameraViewSwitcher _cameraView; // [추가]

    [Header("폭탄 개수 제한 및 쿨타임 설정")]
    [SerializeField] private int _maxbombcount = 10;

    [SerializeField] private float _cooldownDuration = 60f;

    [Header("투척 파워 설정")]
    [SerializeField] private float _minThrowPower = 5f;

    [SerializeField] private float _maxThrowPower = 25f;
    [SerializeField] private float _chargeSpeed = 5f;
    [SerializeField] private float _cursorThrowArc = 0.5f; // [추가] 커서 조준 시 위로 띄우는 정도 (클수록 높은 포물선)

    private int _currentbombcount = 0;
    private float _cooldownTimer = 0;
    private bool _isCooldown = false;

    private float _currentThrowPower = 0f;
    private bool _isCharging = false;

    private void Awake()
    {
        _currentbombcount = _maxbombcount;
        _currentThrowPower = _minThrowPower;
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        // 쿨타임에 따라 타이머 감소 및 폭탄 충전
        if (_isCooldown)
        {
            _cooldownTimer -= Time.deltaTime;

            if (_cooldownTimer <= 0f)
            {
                _currentbombcount = _maxbombcount;
                _isCooldown = false;
            }

            return;
        }

        if (_currentbombcount <= 0) return;

        // 마우스 오른쪽 버튼 입력 받기 (이제 회전과 겹치지 않는 폭탄 전용 버튼)
        if (Input.GetButtonDown("Fire2"))
        {
            _isCharging = true;
            _currentThrowPower = _minThrowPower;
        }

        if (Input.GetButton("Fire2") && _isCharging)
        {
            _currentThrowPower += _chargeSpeed * Time.deltaTime;
            _currentThrowPower = Mathf.Min(_maxThrowPower, _currentThrowPower);
        }

        if (Input.GetButtonUp("Fire2"))
        {
            _isCharging = false;
            Shoot();
        }
    }

    private void Shoot()
    {
        // 발사 위치에서 폭탄 풀에서 꺼내기
        Bomb bomb = PoolManager.Instance.Get(_bombPrefab, _firePoint.position, Quaternion.identity);

        _currentbombcount--;
        Debug.Log($"폭탄 투척.. 남은 개수: {_currentbombcount}/{_maxbombcount}");

        if (_currentbombcount <= 0)
        {
            _isCooldown = true;
            _cooldownTimer = _cooldownDuration;
        }

        // [변경] 던지는 방향
        // - 중앙 조준: 시선 방향 (기존과 동일)
        // - 커서 조준: 커서 쪽 + 위로 살짝 → 포물선 (기존처럼 카메라 정면이면 탑다운에서 발밑에 떨어짐)
        Vector3 throwDirection = Camera.main.transform.forward;

        if (_cameraView != null && _cameraView.IsCursorAim)
        {
            Vector3 aimDirection = _cameraView.GetAimRay(_firePoint.position).direction;
            throwDirection = (aimDirection + Vector3.up * _cursorThrowArc).normalized;
        }

        bomb.Throw(throwDirection * _currentThrowPower);

        _currentThrowPower = _minThrowPower;
    }
}