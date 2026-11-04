using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 로딩 화면: SceneLoader가 정해 둔 다음 씬을 비동기로 불러오면서 진행률을 글자와 슬라이더로 보여 준다.
public class UI_Loding : MonoBehaviour
{
    [Header("구성")]
    [SerializeField] private Slider _progressSlider;

    [SerializeField] private TextMeshProUGUI _progressText; // "불러오는 중 62%"

    [Header("연출")]
    [SerializeField] private float _fillSpeed = 1f; // 막대가 초당 차오르는 비율. 로딩이 한 프레임에 끝나도 막대가 순식간에 차지 않게 한다.

    // allowSceneActivation이 false이면 progress는 0.9에서 멈춘다. 그래서 0.9를 100%로 본다.
    private const float LoadDoneProgress = 0.9f;

    private void Start()
    {
        SetProgress(0f);
        StartCoroutine(Load_Coroutine());
    }

    private IEnumerator Load_Coroutine()
    {
        // 비동기 씬 로드
        // 반환 : 씬의 진행사항을 확인할 수 있는 객체
        AsyncOperation operation = SceneManager.LoadSceneAsync(SceneLoader.NextSceneName);

        // 다 불러와도 허락할 때까지 씬을 바꾸지 않는다.
        operation.allowSceneActivation = false;

        // 화면에 보이는 진행률은 실제 진행률을 일정한 속도로 따라간다.
        float shown = 0f;
        while (shown < 1f)
        {
            // 0에서 1사이의 값으로 보간
            float loaded = Mathf.Clamp01(operation.progress / LoadDoneProgress);
            shown = Mathf.MoveTowards(shown, loaded, _fillSpeed * Time.deltaTime);
            SetProgress(shown);

            yield return null;
        }

        // 막대가 100%까지 찬 뒤에 씬을 바꾼다.
        operation.allowSceneActivation = true;
    }

    private void SetProgress(float value)
    {
        _progressSlider.value = value;
        _progressText.text = $"불러오는 중 {Mathf.RoundToInt(value * 100f)}%";
    }
}
