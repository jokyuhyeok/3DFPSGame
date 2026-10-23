using UnityEngine;
using UnityEngine.UI;

// 플레이어의 체력과 스태미나를 막대(Slider)로 보여 준다.
public class UI_PlayerStats : MonoBehaviour
{
    [SerializeField] private Player _player;
    [SerializeField] private PlayerMove _playerMove;
    [SerializeField] private Slider _healthSlider;
    [SerializeField] private Slider _staminaSlider;

    private void Update()
    {
        // 슬라이더의 value는 0 ~ 1 사이 비율이므로 현재값 ÷ 최대값을 넣는다.
        _healthSlider.value = _player.Health / _player.MaxHealth;
        _staminaSlider.value = _playerMove.Stamina / PlayerMove.MaxStamina;
    }
}