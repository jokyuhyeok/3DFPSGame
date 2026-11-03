using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 타이틀 화면: 시작 버튼을 누르면 버튼이 일렁인 뒤 로그인 씬으로 넘어간다.
public class UI_Title : MonoBehaviour
{
    [SerializeField] private string _nextSceneName = "LoginScene";
    [SerializeField] private Button _startButton;
    [SerializeField] private UI_ButtonWobble _startButtonWobble; // 없으면 바로 넘어간다.

    // 시작 버튼 On Click ()에 연결
    public void OnClickStart()
    {
        StartCoroutine(Start_Coroutine());
    }

    private IEnumerator Start_Coroutine()
    {
        if (_startButton != null) _startButton.interactable = false; // 여러 번 눌리지 않도록

        if (_startButtonWobble != null)
        {
            _startButtonWobble.Play();
            yield return new WaitForSecondsRealtime(_startButtonWobble.Duration);
        }

        SceneManager.LoadScene(_nextSceneName);
    }
}
