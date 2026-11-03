using UnityEngine;

// 저격 모드에서 마우스 오른쪽 버튼을 누르고 있는동안 시야각을 좁혀 확대한다.
public class PlayerZoom : MonoBehaviour
{
    [SerializeField] private GameObject _sniperZoom;
    [SerializeField] private GameObject _crosshair;
    [SerializeField] private float _normalPov = 60f;
    [SerializeField] private float _sniperPov = 15f;
    private Player _player;
    private Camera _camera;
    private bool _isZooming;

    private void Awake()
    {
        _player = GetComponent<Player>();
        _camera = Camera.main; // 매 프레임 Camera.main을 찾지 않도록 캐싱
    }

    private void Start()
    {
        ApplyZoom(false);
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;
        bool isZooming = _player.WeaponMode == WeaponMode.Sniper && Input.GetMouseButton(1);
        if (isZooming == _isZooming) return; // 줌 상태가 바뀔 때만 적용

        ApplyZoom(isZooming);
    }

    private void ApplyZoom(bool isZooming)
    {
        _isZooming = isZooming;
        _camera.fieldOfView = isZooming ? _sniperPov : _normalPov;
        _sniperZoom.SetActive(isZooming);
        _crosshair.SetActive(!isZooming);
    }
}
