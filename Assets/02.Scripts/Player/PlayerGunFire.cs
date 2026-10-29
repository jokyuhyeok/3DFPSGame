using UnityEngine;
using System;
using System.Collections;

// 마우스 왼쪽 버튼을 누르면 조준 방향으로 총알을 발사한다.
// [변경] 조준 방향을 시점에 맞게 CameraViewSwitcher에게 물어본다.
public class PlayerGunFire : MonoBehaviour
{
    [SerializeField] private ParticleSystem _hitEffectPrefab;
    [SerializeField] private CameraViewSwitcher _cameraView; // [추가] 조준 방향 제공

    [Header("레이저 선")]
    [SerializeField] private Transform _muzzle; // 총구 위치 (선의 시작점)

    [SerializeField] private BulletLine _bulletLinePrefab;

    [Header("스탯")]
    [SerializeField] private float _range = 100f; // 사거리

    [SerializeField] private float _damage = 10f; // 공격력
    [SerializeField] private float _fireInterval = 0.1f; // 발사 속도
    [SerializeField] private int _maxBulletCount = 30;
    [SerializeField] private float _reloadingTime = 3f;

    private float _fireTimer = 0f;
    private int _bulletCount = 0;
    private Coroutine _reloadCoroutine = null;
    private Animator _animator;
    private int _hitLayerMask; // 총알이 맞을 수 있는 레이어

    public int BulletCount => _bulletCount;
    public int MaxBulletCount => _maxBulletCount;

    public event Action<int, int> OnBulletChanged;

    private void Awake()
    {
        _bulletCount = _maxBulletCount;
        _animator = GetComponentInChildren<Animator>();

        // Player 레이어 제외
        // 3인칭에서는 카메라 → 조준점 레이가 플레이어 몸을 지나가므로, 빼지 않으면 자기 자신을 쏜다.
        _hitLayerMask = ~LayerMask.GetMask("Player");
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        // 0. 발사 타이머를 지난 시간만큼 줄인다.
        _fireTimer -= Time.deltaTime;

        // 1. 쏘는 중인가? (버튼을 누르고 있고 쏠 수 있는 상태)
        // [변경] Cursor.visible 검사 → CursorManager.IsUIMode
        //        커서 조준 시점에서는 커서가 항상 보이므로, "Alt를 누르고 있는지"로 판단해야 한다.
        // [변경] (2번 수정) 애니메이션 Fire = 누르는 동안 계속 true (총알이 나간 프레임만 true였던 문제)
        bool isFiring = !CursorManager.Instance.IsUIMode && Input.GetButton("Fire1") && _bulletCount > 0;
        _animator.SetBool("Fire", isFiring);

        if (isFiring && _fireTimer <= 0f)
        {
            _bulletCount--;
            OnBulletChanged?.Invoke(_bulletCount, _maxBulletCount);

            _fireTimer = _fireInterval;
            Shoot();

            if (_reloadCoroutine != null)
            {
                StopCoroutine(_reloadCoroutine);
                Debug.Log("재장전 취소!");
                _reloadCoroutine = null;
            }
        }

        // 2. R키를 누르면 총알 재장전 시작
        if (Input.GetKeyDown(KeyCode.R) && _reloadCoroutine == null)
        {
            _reloadCoroutine = StartCoroutine(Reloading_Coroutine());
        }
    }

    private IEnumerator Reloading_Coroutine()
    {
        Debug.Log("재장전 시작!");
        yield return new WaitForSeconds(_reloadingTime);
        _bulletCount = _maxBulletCount;
        OnBulletChanged?.Invoke(_bulletCount, _maxBulletCount);

        Debug.Log("재장전 완료!");
        _reloadCoroutine = null;
    }

    private void Shoot()
    {
        // [변경] 레이 = 시점에 맞는 조준 레이
        // - 중앙 조준: 카메라 → 화면 중앙 (기존과 동일)
        // - 커서 조준: 총구 → 커서가 가리키는 곳
        Vector3 origin = _muzzle != null ? _muzzle.position : transform.position;
        Ray ray = _cameraView.GetAimRay(origin);

        RaycastHit hitInfo;
        bool isHit = Physics.Raycast(ray, out hitInfo, _range, _hitLayerMask); // [변경] Player 레이어 제외

        Vector3 endPoint = isHit ? hitInfo.point : ray.GetPoint(_range);
        ShowBulletLine(endPoint);

        if (isHit)
        {
            PoolManager.Instance.PlayEffect(_hitEffectPrefab, hitInfo.point, Quaternion.LookRotation(hitInfo.normal));

            // 데미지 구조체 만들기
            DamageInfo damageInfo = new DamageInfo();
            damageInfo.Value = _damage;
            damageInfo.HitPoint = hitInfo.point;
            damageInfo.HitNormal = hitInfo.normal;
            hitInfo.collider.GetComponent<IDamageable>()?.TakeDamage(damageInfo);
        }
    }

    private void ShowBulletLine(Vector3 endPoint)
    {
        if (_bulletLinePrefab == null || _muzzle == null) return;

        BulletLine line = PoolManager.Instance.Get(_bulletLinePrefab, _muzzle.position, Quaternion.identity);
        line.Show(_muzzle.position, endPoint);
    }
}