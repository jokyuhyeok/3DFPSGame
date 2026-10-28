using UnityEngine;
using System;

// 플레이어의 체력. 맞으면 체력이 줄어든다.
public class Player : MonoBehaviour, IDamageable
{
    [Header("스탯")]
    [SerializeField] private float _maxHealth = 100f; // 최대 체력

    [Header("UI")]
    [SerializeField] private UI_HitEffect _hitEffect; // 맞았을 때 화면 효과

    private float _health; // 현재 체력

    // UI가 읽어 가는 값 (읽기만 할 수 있다)
    public float Health => _health;
    public float MaxHealth => _maxHealth;

    // Action 자료형: 함수를 저장할 수 있다.
    public event Action OnDamaged;

    private Animator _animator;
    private int _injuredLayerIndex;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _injuredLayerIndex = _animator.GetLayerIndex("Injured");
        _health = _maxHealth;
    }

    // 대미지를 받는다.
    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_health <= 0f)
        {
            return; // 이미 쓰러졌으면 받지 않는다.
        }

        _health = Mathf.Max(0f, _health - damageInfo.Value);

        // 체력에 따라 Injured Layer의 가중치를 바꿔준다.
        float healthPercent = _health / _maxHealth; // 0 ~ 1
        float injured = Mathf.InverseLerp(0.5f, 0f, healthPercent);
        _animator.SetLayerWeight(_injuredLayerIndex, injured);

        OnDamaged?.Invoke();

        _hitEffect.Play();
        Debug.Log($"플레이어가 맞았다. 대미지 {damageInfo.Value}, 남은 체력 {_health}");

        if (_health <= 0f)
        {
            Debug.Log("플레이어가 쓰러졌다."); // 11장에서 게임 오버로 바꾼다.
            GameManager.Instance.GameOver();
        }
    }
}