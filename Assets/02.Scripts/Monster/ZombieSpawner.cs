using System;
using System.Collections.Generic;
using UnityEngine;

public class ZombieSpawner : MonoBehaviour
{
    [SerializeField] private Zombie _zombiePrefab;

    [Header("스폰 설정")]
    [SerializeField] private float _spawnInterval = 10f;

    [SerializeField] private int _maxZombieCount = 5;

    private float _spawnTimer = 0f;
    private readonly List<Zombie> _spawnedZombies = new List<Zombie>();

    public float SpawnInterval => _spawnInterval;
    public int AliveZombiesCount => _spawnedZombies.Count;

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        _spawnedZombies.RemoveAll(zombie => zombie == null);

        if (_spawnedZombies.Count >= _maxZombieCount) return;

        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer > 0f) return;

        _spawnTimer = _spawnInterval;
        Spawn();
    }

    private void Spawn()
    {
        if (_zombiePrefab == null) return;

        Zombie zombie = Instantiate(_zombiePrefab, transform.position, transform.rotation);
        _spawnedZombies.Add(zombie);
    }
}