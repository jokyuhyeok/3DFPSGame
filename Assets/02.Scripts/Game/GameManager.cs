using System;
using System.Collections;
using TMPro;
using UnityEngine;

public enum GameState
{
    Ready,
    Playing,
    GameOver
}

public class GameManager : MonoBehaviour
{
    private static GameManager _instance = null;
    public static GameManager Instance => _instance;
    private GameState _state = GameState.Ready;
    public GameState State => _state;

    [SerializeField] private TextMeshProUGUI _stateText;
    [SerializeField] private Animator _animator;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    private void Start()
    {
        StartCoroutine(StartSequence_Coroutine());
    }

    private IEnumerator StartSequence_Coroutine()
    {
        _stateText.gameObject.SetActive(true);
        _stateText.text = "준비";
        yield return new WaitForSeconds(2f);
        _stateText.text = "시작";
        yield return new WaitForSeconds(0.5f);
        _stateText.gameObject.SetActive(false);
        _state = GameState.Playing;
    }

    public void GameOver()
    {
        _state = GameState.GameOver;

        _stateText.gameObject.SetActive(true);
        _stateText.text = "게임 오버";
        _stateText.color = Color.red;

        // 서 있을 때(MoveMotion이 0)만 쏘는 동작을 한다.
        if (_animator.GetFloat("MoveMotion") == 0f)
        {
            _animator.SetTrigger("Attack");
        }
    }
}