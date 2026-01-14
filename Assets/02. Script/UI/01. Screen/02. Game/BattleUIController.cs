using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public sealed class BattleUIController : MonoBehaviour
{
    [Header("UI Refs (UIRoot에 있는 것만 연결)")]
    [SerializeField] private BattleLogUI battleLogUI;
    [SerializeField] private Button fightButton;

    [Header("Skill Popup Root (PopupRoot/Skill Popup 전체)")]
    [SerializeField] private GameObject skillPopupRoot;

    [Header("Skill Buttons")]
    [SerializeField] private Button[] skillButtons = new Button[4];
    [SerializeField] private TMP_Text[] skillLabels = new TMP_Text[4];

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private BattleManager battleManager;
    private TurnSystem turnSystem;

    private void Awake()
    {
        if (fightButton == null && debugLogs) Debug.LogWarning("[BattleUI] fightButton 미연결");
        if (skillPopupRoot == null && debugLogs) Debug.LogWarning("[BattleUI] skillPopupRoot 미연결");

        if (battleLogUI == null) battleLogUI = FindObjectOfType<BattleLogUI>(true);

        if (fightButton != null)
            fightButton.onClick.AddListener(OnClickFight);

        for (int i = 0; i < skillButtons.Length; i++)
        {
            int slot = i;
            if (skillButtons[i] != null)
                skillButtons[i].onClick.AddListener(() => OnClickSkill(slot));
        }

        SetSkillPopupVisible(false);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryRebindBattle();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnbindLog();
        battleManager = null;
        turnSystem = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => TryRebindBattle();

    private void Update()
    {
        bool canChoose = turnSystem != null && turnSystem.IsWaitingForPlayerChoice;

        if (fightButton != null)
            fightButton.interactable = canChoose;

        if (!canChoose && skillPopupRoot != null && skillPopupRoot.activeSelf)
            SetSkillPopupVisible(false);
    }

    private void TryRebindBattle()
    {
        UnbindLog();

        battleManager = FindObjectOfType<BattleManager>(true);
        if (battleManager == null)
        {
            if (debugLogs) Debug.LogWarning("[BattleUI] BattleManager를 씬에서 못 찾음");
            turnSystem = null;
            SetSkillPopupVisible(false);
            return;
        }

        turnSystem = battleManager.TurnSystem != null
            ? battleManager.TurnSystem
            : battleManager.GetComponent<TurnSystem>();

        if (debugLogs) Debug.Log($"[BattleUI] Rebind OK: BM={battleManager.name}, TS={(turnSystem != null ? "OK" : "NULL")}");

        BindLog();
        RefreshSkillLabels();
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

    private void OnClickFight()
    {
        bool canChoose = turnSystem != null && turnSystem.IsWaitingForPlayerChoice;

        if (debugLogs)
        {
            Debug.Log($"[BattleUI] Fight Clicked. canChoose={canChoose}, popupNull={(skillPopupRoot == null)}");
            if (skillPopupRoot != null)
                Debug.Log($"[BattleUI] popup parent active={skillPopupRoot.transform.parent?.gameObject.activeInHierarchy}");
        }

        if (!canChoose) return;

        RefreshSkillLabels();
        SetSkillPopupVisible(true);
    }

    private void OnClickSkill(int slotIndex)
    {
        if (battleManager == null) return;
        if (turnSystem == null || !turnSystem.IsWaitingForPlayerChoice) return;
        if (battleManager.GetPlayerSkill(slotIndex) == null) return;

        battleManager.SelectSkillSlot(slotIndex);
        SetSkillPopupVisible(false);
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

    private void SetSkillPopupVisible(bool visible)
    {
        if (skillPopupRoot != null)
            skillPopupRoot.SetActive(visible);
    }
}
