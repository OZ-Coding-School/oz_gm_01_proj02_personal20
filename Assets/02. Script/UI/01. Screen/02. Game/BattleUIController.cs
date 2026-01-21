using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/*
BattleUIController는전투UI게이트/입력/팝업상태를관리한다.
-로그가busy면팝업을닫고Confirm키만받는다
-로그가없고플레이어선택대기면커맨드팝업을자동오픈한다
-Update에서문자열생성/로그출력금지(GC규칙),상태변화시점에서만로그출력
*/
public sealed class BattleUIController : MonoBehaviour
{
    [Header("Screen Gate")]
    [SerializeField] private GameObject gameScreenRoot;

    [Header("HUD")]
    [SerializeField] private BattleLogUI battleLogUI;

    [Header("Command Popup (4버튼 팝업 루트)")]
    [SerializeField] private GameObject commandPopupRoot;
    [SerializeField] private Button fightButton;
    [SerializeField] private Button ballButton;
    [SerializeField] private Button pokemonButton;
    [SerializeField] private Button runButton;

    [Header("Skill Popup")]
    [SerializeField] private GameObject skillPopupRoot;
    [SerializeField] private Button[] skillButtons = new Button[4];
    [SerializeField] private TMP_Text[] skillLabels = new TMP_Text[4];

    [Header("Optional Input Owner")]
    [SerializeField] private MonoBehaviour menuButtonController;

    [Header("Keys")]
    [SerializeField] private KeyCode confirmKey1 = KeyCode.Return;
    [SerializeField] private KeyCode confirmKey2 = KeyCode.Z;

    [Header("Debug")]
    [SerializeField] private bool debugLogs;

    private BattleManager battleManager;
    private TurnSystem turnSystem;

    private bool lastGate;
    private bool lastLogBusy;
    private bool lastWaitingChoice;

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
        CacheState(force: true);
        LogTag("Awake");
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryRebindBattle();
        CacheState(force: true);
        ApplyState(force: true);
        LogTag("OnEnable");
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnbindLog();
        battleManager = null;
        turnSystem = null;
        CloseAllPopups();
        LogTag("OnDisable");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryRebindBattle();
        CacheState(force: true);
        ApplyState(force: true);
        LogTag("SceneLoaded");
    }

    private void Update()
    {
        bool gate = IsGameScreenActive();
        bool logBusy = battleLogUI != null && battleLogUI.IsBusy;
        bool waitingChoice = turnSystem != null && turnSystem.IsWaitingForPlayerChoice;

        bool changed = (gate != lastGate) || (logBusy != lastLogBusy) || (waitingChoice != lastWaitingChoice);
        if (changed)
        {
            lastGate = gate;
            lastLogBusy = logBusy;
            lastWaitingChoice = waitingChoice;

            ApplyState(force: false);
        }

        if (!gate) return;

        if (logBusy)
        {
            if (Input.GetKeyDown(confirmKey1) || Input.GetKeyDown(confirmKey2) || Input.GetKeyDown(KeyCode.Space))
            {
                battleLogUI.Confirm();
                LogTag("ConfirmKey");
            }

            return;
        }
    }

    private void CacheState(bool force)
    {
        if (!force) return;

        lastGate = IsGameScreenActive();
        lastLogBusy = battleLogUI != null && battleLogUI.IsBusy;
        lastWaitingChoice = turnSystem != null && turnSystem.IsWaitingForPlayerChoice;
    }

    private void ApplyState(bool force)
    {
        if (!lastGate)
        {
            CloseAllPopups();
            SetMenuInputEnabled(false);
            LogTag("GateOff");
            return;
        }

        if (lastLogBusy)
        {
            SetPopupVisible(commandPopupRoot, false);
            SetPopupVisible(skillPopupRoot, false);
            SetCommandButtonsInteractable(false);
            SetMenuInputEnabled(false);
            LogTag("LogBusy");
            return;
        }

        SetMenuInputEnabled(true);

        if (lastWaitingChoice)
        {
            SetCommandButtonsInteractable(true);

            if (!IsPopupSelfActive(commandPopupRoot) && !IsPopupSelfActive(skillPopupRoot))
            {
                SetPopupVisible(commandPopupRoot, true);
                LogTag("OpenCommandPopup");
            }

            return;
        }

        SetPopupVisible(commandPopupRoot, false);
        SetPopupVisible(skillPopupRoot, false);
        SetCommandButtonsInteractable(false);
        LogTag("NotMyTurn");
    }

    private void TryRebindBattle()
    {
        UnbindLog();

        battleManager = FindObjectOfType<BattleManager>(true);
        if (battleManager == null)
        {
            turnSystem = null;
            CloseAllPopups();
            LogTag("RebindFail");
            return;
        }

        turnSystem = battleManager.TurnSystem != null
            ? battleManager.TurnSystem
            : battleManager.GetComponent<TurnSystem>();

        BindLog();
        RefreshSkillLabels();
        LogTag("RebindOK");
    }

    private bool IsGameScreenActive()
    {
        if (gameScreenRoot != null)
            return gameScreenRoot.activeInHierarchy;

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
        LogTag("FightClicked");
    }

    private void OnClickBall()
    {
        battleManager?.LogBuffer?.Push("아직 볼 기능은 준비 중!");
        LogTag("BallClicked");
    }

    private void OnClickPokemon()
    {
        battleManager?.LogBuffer?.Push("아직 포켓몬 교체 기능은 준비 중!");
        LogTag("PokemonClicked");
    }

    private void OnClickRun()
    {
        battleManager?.LogBuffer?.Push("도망 기능은 준비 중!");
        LogTag("RunClicked");
    }

    private void OnClickSkill(int slotIndex)
    {
        if (turnSystem == null || !turnSystem.IsWaitingForPlayerChoice) return;
        if (battleLogUI != null && battleLogUI.IsBusy) return;
        if (battleManager == null) return;
        if (battleManager.GetPlayerSkill(slotIndex) == null) return;

        battleManager.SelectSkillSlot(slotIndex);
        SetPopupVisible(skillPopupRoot, false);
        LogTag("SkillClicked");
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

    private bool IsPopupSelfActive(GameObject root)
    {
        return root != null && root.activeSelf;
    }

    private void SetPopupVisible(GameObject root, bool visible)
    {
        if (root == null) return;

        if (root.activeSelf == visible) return;

        root.SetActive(visible);

        if (visible)
            root.transform.SetAsLastSibling();

        CanvasGroup cg = root.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = visible ? 1f : 0f;
            cg.interactable = visible;
            cg.blocksRaycasts = visible;
        }
    }

    private void LogTag(string tag)
    {
        if (!debugLogs) return;
        Debug.Log("[BattleUI]" + tag);
    }
}