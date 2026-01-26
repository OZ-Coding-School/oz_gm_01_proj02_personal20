using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static EnumData;

/*
 UI 전체를 관리하는 매니저
 - Screen: 로비/배틀HUD/결과 등 "큰 화면"(보통 1개 유지)
 - Popup: 상점/아이템상세/레벨업 등(스택)
 - System: 로딩 같은 시스템 UI
 - Toast: 잠깐 뜨는 알림

 원칙
 1) GameManager는 "게임 상태"를 관리
 2) UIManager는 "보이기/숨기기/스택 관리"만 담당
*/
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Canvas Layer Roots")]
    [SerializeField] private Transform screenRoot;
    [SerializeField] private Transform popupRoot;
    [SerializeField] private Transform systemRoot;
    [SerializeField] private Transform toastRoot;

    private readonly Stack<UIScreen> screenStack = new();
    private readonly Stack<UIPopup> popupStack = new();

    private readonly Dictionary<PopupId, UIPopup> popupTable = new();
    private readonly Dictionary<ScreenId, UIScreen> screenTable = new();

    private RunManager runManager;
    private Coroutine bindRunRoutine;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        //why: UI 전체 루트를 씬 전환에도 유지
        DontDestroyOnLoad(transform.root.gameObject);

        RegisterAllScreens();
        ApplyScreenForActiveScene();

        // RunManager는 나중에 뜰 수 있으니 코루틴으로 바인딩 시도
        StartBindRunManager();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        StartBindRunManager();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnbindRunManager();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RegisterAllScreens();

        // 씬 로드 직후는 UI 등록/활성 순서가 섞일 수 있어서 1프레임 뒤 적용
        StartCoroutine(CoApplyAfterOneFrame());

        // 씬이 바뀌면 RunManager 참조가 살아있을 수도/없을 수도 있으니 재바인딩 시도
        StartBindRunManager();
    }
    private IEnumerator CoApplyAfterOneFrame()
    {
        yield return null;
        ApplyScreenForActiveScene();
        ApplyScreenForRunStateIfNeeded();
    }

    private void RegisterAllScreens()
    {
        if (screenRoot == null) return;

        UIScreen[] screens = screenRoot.GetComponentsInChildren<UIScreen>(true);
        for (int i = 0; i < screens.Length; i++)
        {
            RegisterScreen(screens[i]);
        }
    }

    //Screen
    public void RegisterScreen(UIScreen screen)
    {
        if (screen == null) return;

        ScreenId id = screen.ScreenId;
        if (id == ScreenId.None)
        {
            Debug.LogWarning("//UIScreen ScreenId is None; skipped registration");
            return;
        }

        screenTable[id] = screen;
        screen.gameObject.SetActive(false);

        if (screenRoot != null && screen.transform.parent != screenRoot)
        {
            screen.transform.SetParent(screenRoot, false);
        }
    }

    public void ApplyScreenForActiveScene()
    {
        CleanDeadScreens();
        HideAllScreens();

        ScreenId id = GetScreenIdForActiveScene();
        if (id == ScreenId.None)
        {
            Debug.LogWarning($"[UIManager] No ScreenId for scene {SceneManager.GetActiveScene().name}");
            return;
        }

        ShowScreenById(id, clearStack: true);
    }

    private void ApplyScreenForRunStateIfNeeded()
    {
        // 03.GameScene에서만 RunState로 (Game <-> ShopReward) 스위칭
        if (!IsGameScene()) return;
        if (runManager == null) return;

        if (runManager.State == RunState.InShopOrReward)
        {
            ShowScreenById(ScreenId.ShopReward, clearStack: true);
        }
        else
        {
            // 전투/전환/게임오버 등은 기본적으로 게임 HUD 스크린
            ShowScreenById(ScreenId.Game, clearStack: true);
        }
    }

    private bool IsGameScene()
    {
        string active = NormalizeSceneName(SceneManager.GetActiveScene().name);
        string game = NormalizeSceneName(GetSceneName(sceneType.GameScene));
        if (!string.IsNullOrEmpty(game)) return active == game;
        return active.Contains("gamescene");
    }

    public void ShowScreenById(ScreenId id, bool clearStack = true)
    {
        if (!screenTable.TryGetValue(id, out UIScreen screen) || screen == null)
        {
            Debug.LogWarning($"[UIManager] Screen not registered: {id}");
            return;
        }

        ShowScreen(screen, clearStack);
    }

    private void HideAllScreens()
    {
        foreach (var kv in screenTable)
        {
            var s = kv.Value;
            if (s == null) continue;

            s.Hide();
            s.gameObject.SetActive(false); // Hide가 active를 안 끄는 구조 대비 안전장치
        }

        // 스택도 같이 비워서 "이전 씬 화면"이 스택에 남지 않게
        screenStack.Clear();
    }

    private static string NormalizeSceneName(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        return s.Replace(" ", "")
                .Replace(".", "")
                .Replace("_", "")
                .ToLowerInvariant();
    }

    private ScreenId GetScreenIdForActiveScene()
    {
        string active = NormalizeSceneName(SceneManager.GetActiveScene().name);

        string boot = NormalizeSceneName(GetSceneName(sceneType.BootScene));
        string lobby = NormalizeSceneName(GetSceneName(sceneType.LobbyScene));
        string game = NormalizeSceneName(GetSceneName(sceneType.GameScene));

        if (!string.IsNullOrEmpty(boot) && active == boot) return ScreenId.Boot;
        if (!string.IsNullOrEmpty(lobby) && active == lobby) return ScreenId.Lobby;
        if (!string.IsNullOrEmpty(game) && active == game) return ScreenId.Game;

        //fallback
        if (active.Contains("bootscene")) return ScreenId.Boot;
        if (active.Contains("lobbyscene")) return ScreenId.Lobby;
        if (active.Contains("gamescene")) return ScreenId.Game;

        return ScreenId.None;
    }

    private void CleanDeadScreens()
    {
        if (screenTable.Count == 0) return;

        List<ScreenId> dead = null;
        foreach (var kv in screenTable)
        {
            if (kv.Value != null) continue;
            dead ??= new List<ScreenId>();
            dead.Add(kv.Key);
        }

        if (dead == null) return;
        for (int i = 0; i < dead.Count; i++)
        {
            screenTable.Remove(dead[i]);
        }
    }

    public void ShowScreen(UIScreen screen, bool clearStack = true)
    {
        if (screen == null) return;

        // 이미 같은 스크린이 최상단이면 아무것도 하지 않음
        if (screenStack.Count > 0)
        {
            UIScreen top = screenStack.Peek();
            if (top == screen)
            {
                if (!screen.gameObject.activeSelf) screen.Show();
                return;
            }
        }

        if (screenRoot != null && screen.transform.parent != screenRoot)
        {
            screen.transform.SetParent(screenRoot, false);
        }

        if (clearStack)
        {
            while (screenStack.Count > 0)
            {
                UIScreen top = screenStack.Pop();
                top?.Hide();
            }
        }
        else
        {
            if (screenStack.Count > 0)
            {
                UIScreen top = screenStack.Peek();
                top?.Hide();
            }
        }

        screenStack.Push(screen);
        screen.Show();
    }

    // RunManager Bind

    private void StartBindRunManager()
    {
        if (bindRunRoutine != null) StopCoroutine(bindRunRoutine);
        bindRunRoutine = StartCoroutine(CoBindRunManagerNextFrame());
    }

    private IEnumerator CoBindRunManagerNextFrame()
    {
        // 1프레임 뒤에 Instance가 준비되는 경우 대비
        yield return null;

        var rm = RunManager.Instance;
        if (rm == null) yield break;

        if (runManager == rm) yield break;

        UnbindRunManager();

        runManager = rm;
        runManager.OnStateChanged += OnRunStateChanged;

        // 현재 상태를 즉시 반영
        ApplyScreenForRunStateIfNeeded();
    }

    private void UnbindRunManager()
    {
        if (runManager != null)
        {
            runManager.OnStateChanged -= OnRunStateChanged;
            runManager = null;
        }
    }

    private void OnRunStateChanged(RunState state)
    {
        ApplyScreenForRunStateIfNeeded();
    }

    // Popup / System / Toast
    public void RegisterPopup(UIPopup popup)
    {
        if (popup == null) return;

        PopupId id = popup.PopupId;
        popupTable[id] = popup;
        popup.gameObject.SetActive(false);

        if (popupRoot != null && popup.transform.parent != popupRoot)
        {
            popup.transform.SetParent(popupRoot, false);
        }
    }

    public void ShowPopup(PopupId id)
    {
        if (!popupTable.TryGetValue(id, out UIPopup popup))
        {
            Debug.LogError($"//등록되지않은팝업:{id}");
            return;
        }

        if (popup == null) return;
        if (popupStack.Contains(popup)) return;

        popupStack.Push(popup);
        popup.Open();
    }

    public void CloseTopPopup()
    {
        if (popupStack.Count == 0) return;

        UIPopup top = popupStack.Pop();
        top?.Close();
    }

    public void CloseAllPopups()
    {
        while (popupStack.Count > 0)
        {
            UIPopup top = popupStack.Pop();
            top?.Close();
        }
    }

    public void AttachSystemUI(MonoBehaviour systemUI)
    {
        if (systemUI == null) return;
        if (systemRoot == null) return;

        systemUI.transform.SetParent(systemRoot, false);
    }

    public void ShowToast(MonoBehaviour toastUI)
    {
        if (toastUI == null) return;
        if (toastRoot == null) return;

        toastUI.transform.SetParent(toastRoot, false);
        toastUI.gameObject.SetActive(true);
    }

    public Coroutine Run(IEnumerator routine)
    {
        if (routine == null) return null;
        return StartCoroutine(routine);
    }

    public void Stop(Coroutine coroutine)
    {
        if (coroutine == null) return;
        StopCoroutine(coroutine);
    }

    public void Back()
    {
        if (popupStack.Count > 0)
        {
            CloseTopPopup();
            return;
        }

        if (screenStack.Count > 1)
        {
            UIScreen current = screenStack.Pop();
            current?.Hide();

            UIScreen prev = screenStack.Peek();
            prev?.Show();
            return;
        }
    }
}