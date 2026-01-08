using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/*
씬 전환을 단일 책임으로 처리하는 로더.
- 중복 로드를 방지하고, 실패 시 로그를 남긴다.
- GameManager가 진입점이며, 다른 시스템은 SceneLoader만 호출한다.
*/
public class SceneLoader : MonoBehaviour
{
    private bool isLoading; //동시 로드 방지(연타/중복 호출 방지)

    public bool IsLoading => isLoading; //UI에서 로딩 상태 표시 등에 사용 가능

    public IEnumerator LoadSceneAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
    {
        //씬 로딩은 동시에 2개 이상 걸리면 상태 꼬이기 쉬움(특히 Single 모드)
        if (isLoading)
        {
            Debug.LogWarning($"이미 씬 로딩 중이야. 요청 무시됨: {sceneName}");
            yield break;
        }

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("LoadSceneAsync: sceneName이 비어있어.");
            yield break;
        }

        isLoading = true;

        //Build Settings 누락이면 op가 null일 수 있어서 방어
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, mode);
        if (op == null)
        {
            Debug.LogError($"씬 로드 요청 실패: {sceneName}. Build Settings에 씬이 등록되어 있는지 확인해줘.");
            isLoading = false;
            yield break;
        }

        //MVP에서는 바로 활성화(로딩 화면/프로그레스는 나중에 확장)
        op.allowSceneActivation = true;

        while (!op.isDone)
        {
            //필요하면 여기서 진행률(op.progress) 기반 UI 연결
            yield return null;
        }

        isLoading = false;
    }
}