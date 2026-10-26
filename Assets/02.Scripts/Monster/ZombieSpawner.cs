using System;
using System.Collections;
using UnityEngine;

public class ZombieSpawner : MonoBehaviour
{
    [SerializeField] private Zombie _zombiePrefab;

    [Header("스폰 설정")]
    [SerializeField] private float _respawnDelay = 2f;

    private Zombie _currentZombie = null;

    public float RespawnDelay => _respawnDelay;
    public bool HasZombie => _currentZombie != null;

    private void Start()
    {
        StartCoroutine(Spawn_Coroutine(0f));
    }

    private void OnDestroy()
    {
        if (_currentZombie != null)
        {
            _currentZombie.OnDeath -= HandleZombieDeath;
        }
    }

    private IEnumerator Spawn_Coroutine(float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        yield return new WaitUntil(() => GameManager.Instance.State == GameState.Playing);

        Spawn();
    }

    private void Spawn()
    {
        if (_zombiePrefab == null) return;

        _currentZombie = Instantiate(_zombiePrefab, transform.position, transform.rotation);
        _currentZombie.OnDeath += HandleZombieDeath;
    }

    private void HandleZombieDeath()
    {
        _currentZombie.OnDeath -= HandleZombieDeath;
        _currentZombie = null;

        StartCoroutine(Spawn_Coroutine(_respawnDelay));
    }
}