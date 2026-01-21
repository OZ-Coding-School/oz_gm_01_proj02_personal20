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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // why: UI 전체 루트를 씬 전환에도 유지
        DontDestroyOnLoad(transform.root.gameObject);

        RegisterAllScreens();
        ApplyScreenForActiveScene();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RegisterAllScreens();
        ApplyScreenForActiveScene();
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

    // Screen
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

        ScreenId target = GetScreenIdForActiveScene();
        if (target == ScreenId.None)
        {
            Debug.LogWarning($"UIManager: ScreenId.None (ActiveScene={SceneManager.GetActiveScene().name})");
            return;
        }

        if (!screenTable.TryGetValue(target, out UIScreen screen) || screen == null)
        {
            Debug.LogWarning($"//등록되지않은스크린:{target}");
            return;
        }

        ShowScreen(screen, clearStack: true);
    }

    private static string NormalizeSceneName(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        // 공백/점/언더스코어 제거 후 소문자
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

        // fallback
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

        // ✅ 이미 같은 스크린이 최상단이면 아무것도 하지 않음(중요)
        if (screenStack.Count > 0)
        {
            UIScreen top = screenStack.Peek();
            if (top == screen)
            {
                if (!screen.gameObject.activeSelf) screen.Show(); // 혹시 꺼져있으면만 켬
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

    // Popup(Register)
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

    // Popup(Open)
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

    // Popup(Close)
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

    // System
    public void AttachSystemUI(MonoBehaviour systemUI)
    {
        if (systemUI == null) return;
        if (systemRoot == null) return;

        systemUI.transform.SetParent(systemRoot, false);
    }

    // Toast
    public void ShowToast(MonoBehaviour toastUI)
    {
        if (toastUI == null) return;
        if (toastRoot == null) return;

        toastUI.transform.SetParent(toastRoot, false);
        toastUI.gameObject.SetActive(true);
    }

    // 코루틴 러너
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
        //팝업이 있으면 팝업부터 한단계 뒤로
        if (popupStack.Count > 0)
        {
            CloseTopPopup();
            return;
        }

        //스크린 스택이 2개 이상이면 이전 스크린으로
        if (screenStack.Count > 1)
        {
            UIScreen current = screenStack.Pop();
            current?.Hide();

            UIScreen prev = screenStack.Peek();
            prev?.Show();
            return;
        }

        //여기까지 오면 더이상 뒤로갈게 없음
        //원하면 여기서 종료확인팝업/로비로 이동 같은 처리 가능
        //Debug.Log("//Back: no more history");
    }
}