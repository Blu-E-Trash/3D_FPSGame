using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// 로딩 씬 진행: 다음 씬을 비동기로 불러오면서 UI_LoadingProgress에 진행률을 넘긴다.
// 다른 씬에서는 LoadingSceneController.LoadScene("SampleScene"); 처럼 부른다.
// 불러올 씬 없이 로딩 씬을 바로 Play하면 LoadingSceneTester(테스트 모드)에 맡긴다.
public class LoadingSceneController : MonoBehaviour
{
    private const string LoadingSceneName = "LoadingScene";
    private const float AsyncLoadReadyProgress = 0.9f; // allowSceneActivation이 false면 progress가 0.9에서 멈춘다.

    [SerializeField] private UI_LoadingProgress _progress;
    [SerializeField] private LoadingSceneTester _tester;
    [SerializeField] private float _introDuration = 2.5f; // 등장 연출(UI_LoginIntro)이 끝날 때까지 진행 바를 0%로 둔다.

    private static string _nextSceneName;

    // 로딩 씬을 거쳐 sceneName 씬으로 간다. (두 씬 모두 Build Settings에 들어 있어야 한다)
    public static void LoadScene(string sceneName)
    {
        _nextSceneName = sceneName;
        SceneManager.LoadScene(LoadingSceneName);
    }

    private void Awake()
    {
        if (_progress == null || _tester == null)
        {
            Debug.LogError("[LoadingSceneController] _progress 또는 _tester 참조가 비어 있다.", this);
            enabled = false;
        }
    }

    private void Start()
    {
        if (string.IsNullOrEmpty(_nextSceneName))
        {
            _tester.BeginTestMode();
            return;
        }

        string sceneName = _nextSceneName;
        _nextSceneName = null;
        StartCoroutine(LoadRoutine(sceneName));
    }

    private IEnumerator LoadRoutine(string sceneName)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        if (operation == null)
        {
            Debug.LogError($"[LoadingSceneController] '{sceneName}' 씬을 불러올 수 없다. Build Settings에 추가했는지 확인한다.", this);
            yield break;
        }
        operation.allowSceneActivation = false;

        // 등장 연출 동안에는 뒤에서 불러오기만 하고, 바는 연출이 끝난 뒤부터 채운다.
        yield return new WaitForSecondsRealtime(_introDuration);
        _progress.Restart();

        while (operation.progress < AsyncLoadReadyProgress)
        {
            _progress.SetTarget(operation.progress / AsyncLoadReadyProgress);
            yield return null;
        }

        _progress.SetTarget(1f);
        yield return _progress.WaitForComplete();
        operation.allowSceneActivation = true;
    }
}
