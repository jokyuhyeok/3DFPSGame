using System;
using UnityEngine;
using UnityEngine.UI;

public class ZombieHealthBar : MonoBehaviour
{
    [SerializeField] private Slider _slider;

    private Zombie _zombie;

    private void Awake()
    {
        _zombie = GetComponent<Zombie>();
    }

    private void OnEnable()
    {
        // 좀비의 체력에 내 체력바 연결을 하겠다.
        _zombie.OnHealthChanged += UpdateHealthBar;
    }

    private void OnDisable()
    {
        // 좀비의 체력에 내 체력바 연결을 끊겠다.
        _zombie.OnHealthChanged -= UpdateHealthBar;
    }

    private void LateUpdate()
    {
        // 빌보드: 체력바가 카메라를 쳐다보게 한다.
        _slider.transform.forward = Camera.main.transform.forward;
    }

    private void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        // 0 ~ 100 -> 0 ~ 1
        _slider.value = currentHealth / maxHealth;
    }
}