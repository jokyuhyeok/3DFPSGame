using System;
using UnityEngine;
using TMPro;

// 화면 위쪽에 "현재 좀비 수 / 최대 좀비 수"를 보여준다.
// 옵저버 패턴: ZombieSpawner의 OnZombieCountChanged 이벤트를 구독해서 값이 바뀔 때만 갱신.
public class UI_ZombiCount : MonoBehaviour
{
    [SerializeField] private ZombieSpawner _zombieSpawner;
    [SerializeField] private TextMeshProUGUI _countText;

    private void OnEnable()
    {
        if (_zombieSpawner == null) return;

        _zombieSpawner.OnZombieCountChanged += RefreshCount;
        RefreshCount(_zombieSpawner.AliveZombieCount, _zombieSpawner.MaxZombieCount);
    }

    private void OnDisable()
    {
        if (_zombieSpawner == null) return;

        _zombieSpawner.OnZombieCountChanged -= RefreshCount;
    }

    private void RefreshCount(int aliveCount, int maxCount)
    {
        _countText.text = $"좀비 {aliveCount} / {maxCount}";
    }
}