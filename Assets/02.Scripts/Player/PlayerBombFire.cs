using UnityEngine;

// 마우스 오른쪽 버튼을 누르면 시선이 바라보는 방향으로 폭탄을 던지게 하겠다.
public class PlayerBombFire : MonoBehaviour
{
    [SerializeField] private Transform _firePoint;
    [SerializeField] private Bomb _bombPrefab;
    [SerializeField] private float _throwPower = 15f;

    [Header("폭탄 개수 제한 및 쿨타임 설정")]
    [SerializeField] private int _maxbombcount = 10;
    [SerializeField] private float _cooldownDuration = 60f;

    private int _currentbombcount = 0;
    private float _cooldownTimer = 0;
    private bool _isCooldown = false;

    private void Awake()
    {
        _currentbombcount = _maxbombcount;
    }

    private void Update()
    {
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

        // 마우스 오른쪽 버튼 입력 받기
        if (Input.GetButtonDown("Fire2"))
        {
            Shoot();
        }
    }

    private void Shoot()
    {
        // 발사 위치에서 폭탄 생성하기
        Bomb bomb = Instantiate(_bombPrefab, _firePoint.position, Quaternion.identity);

        _currentbombcount--;
        Debug.Log($"폭탄 투척.. 남은 개수: {_currentbombcount}/{_maxbombcount}");

        if (_currentbombcount <= 0)
        {
            _isCooldown = true;
            _cooldownTimer = _cooldownDuration;
        }

        // 생성된 수류탄을 시선 방향으로 던지기
        Rigidbody rigidbody = bomb.GetComponent<Rigidbody>();
        rigidbody.AddForce(Camera.main.transform.forward * _throwPower, ForceMode.Impulse);
    }
}