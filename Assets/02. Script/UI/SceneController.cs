using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

//Handles loading panel visibility (no progress bar).
//- Shows "로딩중..." text while waiting.
//- Delegates actual scene loading to SceneLoader.
public class SceneController : MonoBehaviour
{
    public static SceneController Instance { get; private set; }

    [Header("Loading UI References (씬에 배치된 오브젝트 연결)")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private Component loadingLabel; // TMP_Text 또는 UnityEngine.UI.Text 둘 다 가능

    [Header("Loading Text")]
    [SerializeField] private string loadingBaseText = "로딩중";
    [SerializeField] private bool animateDots = true;
    [SerializeField] private float dotInterval = 0.35f;

    [Header("Behavior")]
    [SerializeField] private float minLoadingShowTime = 0.2f; // 깜빡임 방지 최소 노출

    public event Action OnBeforeSceneLoad;  // ex) 게임 시간 리셋 등
    public event Action OnAfterSceneLoad;

    private AsyncOperation asyncOp;
    private bool isLoading;
    private Coroutine dotsRoutine;

    private void Awake()
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        //why: SceneController가 UIRoot 자식이어도 안전하게 루트 전체를 유지
        DontDestroyOnLoad(transform.root.gameObject);
    }

    //===== 버튼에서 바로 호출할 API =====
    public void LoadLobbyScene() => LoadScene(EnumData.sceneType.LobbyScene);
    public void LoadGameScene() => LoadScene(EnumData.sceneType.GameScene);

    //버튼에서 이 함수만 호출하면 됨
    public void LoadScene(EnumData.sceneType targetScene)
    {
        if (isLoading) return;

        Time.timeScale = 1f;
        OnBeforeSceneLoad?.Invoke();
        StartCoroutine(CoLoadSceneAuto(targetScene));
    }

    private IEnumerator CoLoadSceneAuto(EnumData.sceneType targetScene)
    {
        isLoading = true;
        asyncOp = null;

        ShowLoadingUI();

        if (SceneLoader.Instance == null)
        {
            Debug.LogError("//SceneLoader.Instance가 null임(SceneLoader 오브젝트/싱글톤 존재 확인)");
            HideLoadingUI();
            isLoading = false;
            yield break;
        }

        asyncOp = SceneLoader.Instance.LoadSceneAsync(targetScene);

        if (asyncOp == null)
        {
            Debug.LogError("//LoadSceneAsync가 null 반환(BuildSettings 또는 씬 이름 매핑 확인)");
            HideLoadingUI();
            isLoading = false;
            yield break;
        }

        asyncOp.allowSceneActivation = false;

        float shownAt = Time.realtimeSinceStartup;

        while (!asyncOp.isDone)
        {
            //progress는 0~0.9까지만 올라감
            if (asyncOp.progress >= 0.9f)
            {
                float elapsed = Time.realtimeSinceStartup - shownAt;
                if (elapsed >= minLoadingShowTime)
                {
                    asyncOp.allowSceneActivation = true;
                }
            }

            yield return null;
        }

        //씬 전환 완료: 현재 씬에 맞는 UIScreen 적용(부트 화면 고정 문제 방지)
        //UIManager.Instance?.ApplyScreenForActiveScene();
        OnAfterSceneLoad?.Invoke();

        HideLoadingUI();

        asyncOp = null;
        isLoading = false;
    }

    private void ShowLoadingUI()
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
            loadingPanel.transform.SetAsLastSibling();
            Canvas.ForceUpdateCanvases();
        }

        SetLoadingLabel(loadingBaseText);

        if (animateDots)
        {
            if (dotsRoutine != null) StopCoroutine(dotsRoutine);
            dotsRoutine = StartCoroutine(CoDots());
        }
    }

    private void HideLoadingUI()
    {
        if (dotsRoutine != null)
        {
            StopCoroutine(dotsRoutine);
            dotsRoutine = null;
        }

        SetLoadingLabel(string.Empty);

        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
    }

    private IEnumerator CoDots()
    {
        int dots = 0;
        while (true)
        {
            dots = (dots + 1) % 4; // 0~3
            SetLoadingLabel(loadingBaseText + new string('.', dots));
            yield return new WaitForSecondsRealtime(dotInterval);
        }
    }

    private void SetLoadingLabel(string value)
    {
        if (loadingLabel == null) return;

        //TMP_Text / Text 모두 public string text {get;set;} 를 가짐
        PropertyInfo prop = loadingLabel.GetType().GetProperty("text", BindingFlags.Instance | BindingFlags.Public);
        if (prop != null && prop.CanWrite && prop.PropertyType == typeof(string))
        {
            prop.SetValue(loadingLabel, value);
        }
    }
}