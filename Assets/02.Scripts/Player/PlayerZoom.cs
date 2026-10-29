using System;
using UnityEngine;

// 저격 모드에서 마우스 오른쪽 버튼을 누르고 있는동안 시야각을 좁혀 확대한다. 
public class PlayerZoom : MonoBehaviour
{
    [SerializeField] private GameObject _sniperZoom;
    [SerializeField] private GameObject _crosshair;
    [SerializeField] private float _normalPov = 60f;
    [SerializeField] private float _sniperPov = 15f;
    private Player _player;

    private void Awake()
    {
        _player = GetComponent<Player>();
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;
        bool isZooming = _player.WeaponMode == WeaponMode.Sniper && Input.GetMouseButton(1);
        Camera.main.fieldOfView = isZooming ? _sniperPov : _normalPov;
        _sniperZoom.SetActive(isZooming);
        _crosshair.SetActive(!isZooming);
    }
}