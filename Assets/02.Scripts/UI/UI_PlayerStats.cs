using System;
using UnityEngine;
using UnityEngine.UI;

// 플레이어의 체력과 스태미나를 막대(Slider)로 보여 준다.
public class UI_PlayerStats : MonoBehaviour
{
    [SerializeField] private Player _player;
    [SerializeField] private PlayerMove _playerMove;
    [SerializeField] private Slider _healthSlider;
    [SerializeField] private Slider _staminaSlider;

    private void Start()
    {
        // 플레이어 공격 이벤트 구독!
        _player.OnDamaged += RefreshHealth;
    }

    private void Update()
    {
        // RefreshHealth(); // 풀링
        _staminaSlider.value = _playerMove.Stamina / PlayerMove.MaxStamina;
    }

    private void RefreshHealth()
    {
        // 문제: 체력값이 변하지 않았음에도 초당 60회 이상 캔버스를 불필요하게 다시 그린다. -> 성능 낭비
        // 불필요한 연산이 누적될수록 느려진다. 
        // 해결: 값이 바뀌었을 때만 캔버스를 갱신한다.

        // 이때 쓰는게 옵저버 패턴
        // 옵저버 패턴은 한 객체의 상태가 바뀔 때 이를 구독 중인 여러 객체에게 자동으로 알려주는 패턴.
        // c# 에서는 이것을 Action으로 구현할 수 있다.

        _healthSlider.value = _player.Health / _player.MaxHealth;
    }
}