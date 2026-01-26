using System.Collections;
using UnityEngine;

public sealed class BootFlow : MonoBehaviour
{
    [Header("Next Scene")]
    [SerializeField] private EnumData.sceneType nextScene = EnumData.sceneType.LobbyScene;

    [Header("Guards")]
    [SerializeField] private float minDelaySeconds = 2f;          // 최소 대기(방어장치)
    [SerializeField] private float maxWaitSeconds = 30f;          // 무한 대기 방지(필요 없으면 크게)
    [SerializeField] private bool requirePokedexInitialized = true;

    private bool _started;

    private void Start()
    {
        if (_started) return;
        _started = true;
        StartCoroutine(CoBoot());
    }

    private IEnumerator CoBoot()
    {
        float start = Time.realtimeSinceStartup;

        //(옵션) 부트에서 로딩 패널을 먼저 보여주고 싶으면 여기서 호출
        //SceneController.Instance?.ShowLoadingOnly("로딩중..."); // 이런 메서드가 있으면

        while (!IsBootReady())
        {
            if (Time.realtimeSinceStartup - start >= maxWaitSeconds)
            {
                Debug.LogWarning($"BootFlow: boot ready timeout({maxWaitSeconds}s). Forcing scene transition.");
                break;
            }
            yield return null;
        }

        float elapsed = Time.realtimeSinceStartup - start;
        float remain = Mathf.Max(0f, minDelaySeconds - elapsed);
        if (remain > 0f)
        {
            yield return new WaitForSecondsRealtime(remain);
        }

        //씬 전환 (SceneController 우선, 없으면 SceneLoader fallback)
        if (SceneController.Instance != null)
        {
            SceneController.Instance.LoadScene(nextScene);
            yield break;
        }

        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadScene(nextScene);
            yield break;
        }

        Debug.LogError("BootFlow: SceneController/SceneLoader not found. Cannot transition scene.");
    }

    private bool IsBootReady()
    {
        if (GameManager.Instance == null) return false;

        if (requirePokedexInitialized)
        {
            if (GameManager.Instance.Pokedex == null) return false;
            if (!GameManager.Instance.Pokedex.IsInitialized) return false;
        }

        return true;
    }
}
