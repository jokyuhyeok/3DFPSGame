using System;
using Unity.VisualScripting;
using UnityEngine;

// 무기 모드에 따라 손에 든 무기 모델을 바꿔 보여 준다.
// 모드가 바뀌었다는 알림(OnWeaponModeChanged)이 올 때만 바꾼다.
public class PlayerWeaponVisual : MonoBehaviour
{
    [SerializeField] private GameObject _gunHolder;
    [SerializeField] private GameObject _swordHolder;

    [SerializeField] private Collider _bladeCollider;

    private Player _player;

    private void Awake()
    {
        _player = GetComponent<Player>();
    }

    private void OnEnable()
    {
        _player.OnWeaponModeChanged += RefreshWeapon;
    }

    private void OnDisable()
    {
        _player.OnWeaponModeChanged -= RefreshWeapon;
    }

    private void Start()
    {
        RefreshWeapon(_player.WeaponMode);
    }

    private void RefreshWeapon(WeaponMode mode)
    {
        bool isMelee = mode == WeaponMode.Melee;

        _gunHolder.SetActive(!isMelee);
        _swordHolder.SetActive(isMelee);

        if (!isMelee && _bladeCollider != null)
        {
            _bladeCollider.enabled = false;
        }
    }
}