using System;
using UnityEngine;
using UnityEngine.AdaptivePerformance.Provider;

// FSM(유한 상태 머신)

// 좀비가 가질 수 있는 상태
public enum ZombieState
{
    Idle,
    Trace,
    Comeback,
    Attack,
    Hit,
    Death,
}

public class Zombie : MonoBehaviour
{
    [Header("스탯")]
    [SerializeField] private float _detectDistance = 8f;

    [SerializeField] private float _moveSpeed = 5f;

    private CharacterController _controller;
    private GameObject _player;
    private ZombieState _state = ZombieState.Idle;

    private void Start()
    {
        _controller = GetComponent<CharacterController>();
        _player = GameObject.FindGameObjectWithTag("Player");
    }

    private void Update()
    {
        switch (_state)
        {
            case ZombieState.Idle:
                {
                    Idle();
                    break;
                }
            case ZombieState.Trace:
                {
                    Trace();
                    break;
                }
            case ZombieState.Comeback:
                {
                    Comeback();
                    break;
                }
            case ZombieState.Attack:
                {
                    Attack();
                    break;
                }
            case ZombieState.Hit:
                {
                    Hit();
                    break;
                }
            case ZombieState.Death:
                {
                    Death();
                    break;
                }
        }
    }

    private void Idle()
    {
        // player가 가까워지면 Trace 상태로 전이
        float distance = Vector3.Distance(transform.position, _player.transform.position);
        if (distance < _detectDistance)
        {
            Debug.Log($"{_state} -> Trace");
            _state = ZombieState.Trace;
        }
    }

    private void Trace()
    {
        Vector3 direction = _player.transform.position - transform.position;
        direction.y = 0f;
        direction.Normalize();

        transform.forward = direction;
        _controller.SimpleMove(direction * _moveSpeed);
    }

    private void Comeback()
    {
    }

    private void Attack()
    {
    }

    private void Hit()
    {
    }

    private void Death()
    {
    }
}