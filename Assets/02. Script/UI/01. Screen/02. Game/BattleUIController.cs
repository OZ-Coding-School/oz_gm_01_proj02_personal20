using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public sealed class BattleUIController : MonoBehaviour
{
    [Header("Screen Gate")]
    [SerializeField] private GameObject gameScreenRoot; // UIRoot/ScreenRoot/Game Screen

    [Header("HUD")]
    [SerializeField] private BattleLogUI battleLogUI;

    [Header("Command Popup (4버튼 팝업 루트)")]
    [SerializeField] private GameObject commandPopupRoot; // (Dim 포함 루트 추천)
    [SerializeField] private Button fightButton;
    [SerializeField] private Button ballButton;
    [SerializeField] private Button pokemonButton;
    [SerializeField] private Button runButton;

    [Header("Skill Popup")]
    [SerializeField] private GameObject skillPopupRoot; // (Dim 포함 루트)
    [SerializeField] private Button[] skillButtons = new Button[4];
    [SerializeField] private TMP_Text[] skillLabels = new TMP_Text[4];

    [Header("Optional Input Owner")]
    [SerializeField] private MonoBehaviour menuButtonController; // MenuButtonController 있으면 드래그

    [Header("Keys")]
    [SerializeField] private KeyCode confirmKey1 = KeyCode.Return;
    [SerializeField] private KeyCode confirmKey2 = KeyCode.Z;

    [Header("Debug")]
    [SerializeField] private bool debugLogs;

    private BattleManager battleManager;
    private TurnSystem turnSystem;

    private void Awake()
    {
        if (battleLogUI == null) battleLogUI = FindObjectOfType<BattleLogUI>(true);

        if (fightButton != null) fightButton.onClick.AddListener(OnClickFight);
        if (ballButton != null) ballButton.onClick.AddListener(OnClickBall);
        if (pokemonButton != null) pokemonButton.onClick.AddListener(OnClickPokemon);
        if (runButton != null) runButton.onClick.AddListener(OnClickRun);

        for (int i = 0; i < skillButtons.Length; i++)
        {
            int slot = i;
            if (skillButtons[i] != null)
                skillButtons[i].onClick.AddListener(() => OnClickSkill(slot));
        }

        CloseAllPopups();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryRebindBattle();
        ApplyGateAndMode();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnbindLog();
        battleManager = null;
        turnSystem = null;
        CloseAllPopups();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryRebindBattle();
        ApplyGateAndMode();
    }

    private void Update()
    {
        ApplyGateAndMode();
        if (!IsGameScreenActive()) return;

        bool logBusy = battleLogUI != null && battleLogUI.IsBusy;

        if (logBusy)
        {
            // 로그 모드: Confirm만 받기
            if (Input.GetKeyDown(confirmKey1) || Input.GetKeyDown(confirmKey2))
                battleLogUI.Confirm();

            return;
        }

        // 커맨드/스킬 선택 가능 여부
        bool waitingChoice = turnSystem != null && turnSystem.IsWaitingForPlayerChoice;

        // 내 턴이면 커맨드 팝업 자동 오픈
        if (waitingChoice)
        {
            if (!IsPopupActive(commandPopupRoot) && !IsPopupActive(skillPopupRoot))
                SetPopupVisible(commandPopupRoot, true);

            SetCommandButtonsInteractable(true);
        }
        else
        {
            // 내 턴 아니면 팝업 닫기
            SetPopupVisible(commandPopupRoot, false);
            SetPopupVisible(skillPopupRoot, false);
        }
    }

    private void TryRebindBattle()
    {
        UnbindLog();

        battleManager = FindObjectOfType<BattleManager>(true);
        if (battleManager == null)
        {
            turnSystem = null;
            CloseAllPopups();
            if (debugLogs) Debug.LogWarning("[BattleUI] BattleManager not found.");
            return;
        }

        turnSystem = battleManager.TurnSystem != null
            ? battleManager.TurnSystem
            : battleManager.GetComponent<TurnSystem>();

        BindLog();
        RefreshSkillLabels();

        if (debugLogs) Debug.Log($"[BattleUI] Rebind OK: BM={battleManager.name}, TS={(turnSystem != null ? "OK" : "NULL")}");
    }

    private void ApplyGateAndMode()
    {
        if (!IsGameScreenActive())
        {
            CloseAllPopups();
            SetMenuInputEnabled(false);
            return;
        }

        bool logBusy = battleLogUI != null && battleLogUI.IsBusy;

        // 로그가 뜨면 모든 팝업 닫고 입력 잠금
        if (logBusy)
        {
            SetPopupVisible(commandPopupRoot, false);
            SetPopupVisible(skillPopupRoot, false);
            SetMenuInputEnabled(false);
            return;
        }

        // 로그가 없으면 입력 가능(커맨드/스킬은 Update에서 결정)
        SetMenuInputEnabled(true);
    }

    private bool IsGameScreenActive()
    {
        if (gameScreenRoot != null)
            return gameScreenRoot.activeInHierarchy;

        // fallback
        return battleManager != null;
    }

    private void BindLog()
    {
        if (battleManager == null || battleManager.LogBuffer == null || battleLogUI == null) return;
        battleManager.LogBuffer.OnLinePushed -= battleLogUI.Push;
        battleManager.LogBuffer.OnLinePushed += battleLogUI.Push;
    }

    private void UnbindLog()
    {
        if (battleManager == null || battleManager.LogBuffer == null || battleLogUI == null) return;
        battleManager.LogBuffer.OnLinePushed -= battleLogUI.Push;
    }

    private void SetMenuInputEnabled(bool enabled)
    {
        if (menuButtonController != null)
            menuButtonController.enabled = enabled;
    }

    private void OnClickFight()
    {
        if (turnSystem == null || !turnSystem.IsWaitingForPlayerChoice) return;
        if (battleLogUI != null && battleLogUI.IsBusy) return;

        RefreshSkillLabels();
        SetPopupVisible(commandPopupRoot, false);
        SetPopupVisible(skillPopupRoot, true);
    }

    private void OnClickBall()
    {
        if (battleManager?.LogBuffer != null)
            battleManager.LogBuffer.Push("아직 볼 기능은 준비 중!");
    }

    private void OnClickPokemon()
    {
        if (battleManager?.LogBuffer != null)
            battleManager.LogBuffer.Push("아직 포켓몬 교체 기능은 준비 중!");
    }

    private void OnClickRun()
    {
        if (battleManager?.LogBuffer != null)
            battleManager.LogBuffer.Push("도망 기능은 준비 중!");
    }

    private void OnClickSkill(int slotIndex)
    {
        if (turnSystem == null || !turnSystem.IsWaitingForPlayerChoice) return;
        if (battleLogUI != null && battleLogUI.IsBusy) return;
        if (battleManager == null) return;
        if (battleManager.GetPlayerSkill(slotIndex) == null) return;

        battleManager.SelectSkillSlot(slotIndex);
        SetPopupVisible(skillPopupRoot, false);
    }

    private void RefreshSkillLabels()
    {
        if (battleManager == null) return;

        for (int i = 0; i < 4; i++)
        {
            if (skillLabels == null || i >= skillLabels.Length) break;
            if (skillLabels[i] == null) continue;
            skillLabels[i].text = battleManager.GetPlayerSkillName(i);
        }
    }

    private void SetCommandButtonsInteractable(bool interactable)
    {
        if (fightButton != null) fightButton.interactable = interactable;
        if (ballButton != null) ballButton.interactable = interactable;
        if (pokemonButton != null) pokemonButton.interactable = interactable;
        if (runButton != null) runButton.interactable = interactable;
    }

    private void CloseAllPopups()
    {
        SetPopupVisible(commandPopupRoot, false);
        SetPopupVisible(skillPopupRoot, false);
        SetCommandButtonsInteractable(false);
    }

    private bool IsPopupActive(GameObject root)
    {
        return root != null && root.activeInHierarchy;
    }

    private void SetPopupVisible(GameObject root, bool visible)
    {
        if (root == null) return;

        root.SetActive(visible);

        if (visible)
            root.transform.SetAsLastSibling();

        var cg = root.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = visible ? 1f : 0f;
            cg.interactable = visible;
            cg.blocksRaycasts = visible;
        }
    }
}
