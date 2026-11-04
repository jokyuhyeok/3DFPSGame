using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoader
{
    private const string LoadingScene = "LoadingScene";

    public static string NextSceneName { get; private set; }

    // 가려는 씬 이름을 기억해 두고 로딩 씬으로 간다. 실제 로드는 로딩 씬의 UI_Loding이 한다.
    public static void Load(string sceneName)
    {
        NextSceneName = sceneName;
        SceneManager.LoadScene(LoadingScene);
    }
}
