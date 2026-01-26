using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/*
BattleUIController는전투UI의입력,게이트,팝업상태를관리한다
-로그가busy면팝업을닫고Confirm입력만허용한다
-로그가끝나고플레이어선택대기상태면커맨드팝업을자동으로연다
-팝업은SetActive방식으로평소OFF,필요시ON한다
-Confirm입력은ScreenGate와무관하게항상처리한다
*/
public sealed class BattleUIController : MonoBehaviour
{
    [Header("Screen Gate")]
    [SerializeField] private GameObject gameScreenRoot;//게임화면활성여부게이트

    [Header("HUD")]
    [SerializeField] private BattleLogUI battleLogUI;//배틀로그UI참조

    [Header("Command Popup")]
    [SerializeField] private GameObject commandPopupRoot;//커맨드팝업루트
    [SerializeField] private Button fightButton;//싸우기버튼
    [SerializeField] private Button ballButton;//볼버튼
    [SerializeField] private Button pokemonButton;//포켓몬버튼
    [SerializeField] private Button runButton;//도망버튼

    [Header("Skill Popup")]
    [SerializeField] private GameObject skillPopupRoot;//스킬팝업루트
    [SerializeField] private Button[] skillButtons = new Button[4];//스킬버튼배열
    [SerializeField] private TMP_Text[] skillLabels = new TMP_Text[4];//스킬라벨배열

    [Header("Optional Input Owner")]
    [SerializeField] private MonoBehaviour menuButtonController;//외부메뉴입력차단용

    [Header("Keys")]
    [SerializeField] private KeyCode confirmKey1 = KeyCode.Return;//확정키1
    [SerializeField] private KeyCode confirmKey2 = KeyCode.Z;//확정키2

    [Header("Debug")]
    [SerializeField] private bool debugLogs;//디버그로그출력여부

    private BattleManager battleManager;//현재배틀매니저
    private TurnSystem turnSystem;//턴시스템참조

    private bool lastGate;//이전게이트상태
    private bool lastLogBusy;//이전로그busy상태
    private bool lastWaitingChoice;//이전플레이어선택대기상태

    //Awake는필수참조캐싱과버튼리스너를설정한다
    private void Awake()
    {
        if (battleLogUI == null)
        {
            battleLogUI = FindObjectOfType<BattleLogUI>(true);
        }

        if (fightButton != null)
        {
            fightButton.onClick.AddListener(OnClickFight);
        }

        if (ballButton != null)
        {
            ballButton.onClick.AddListener(OnClickBall);
        }

        if (pokemonButton != null)
        {
            pokemonButton.onClick.AddListener(OnClickPokemon);
        }

        if (runButton != null)
        {
            runButton.onClick.AddListener(OnClickRun);
        }

        for (int i = 0; i < skillButtons.Length; i++)
        {
            int slot = i;
            if (skillButtons[i] != null)
            {
                skillButtons[i].onClick.AddListener(() => OnClickSkill(slot));
            }
        }

        CloseAllPopups();
        CacheState(true);
        LogTag("Awake");
    }

    //OnEnable은씬로드이벤트등록과배틀재바인딩을수행한다
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryRebindBattle();
        CacheState(true);
        ApplyState(true);
        LogTag("OnEnable");
    }

    //OnDisable은이벤트해제와상태정리를수행한다
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnbindLog();
        battleManager = null;
        turnSystem = null;

        //OnDisable중엔SetPopupVisible을피하고직접끄기
        if (commandPopupRoot != null) commandPopupRoot.SetActive(false);
        if (skillPopupRoot != null) skillPopupRoot.SetActive(false);

        LogTag("OnDisable");
    }

    //OnSceneLoaded는게임씬전환시배틀을다시찾는다
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryRebindBattle();
        CacheState(true);
        ApplyState(true);
        LogTag("SceneLoaded");
    }

    //Update는입력처리와상태변화를감지한다
    private void Update()
    {
        //Confirm입력은게이트와무관하게항상처리한다
        if (battleLogUI != null)
        {
            if (Input.GetKeyDown(confirmKey1) || Input.GetKeyDown(confirmKey2) || Input.GetKeyDown(KeyCode.Space))
            {
                battleLogUI.Confirm();
                LogTag("ConfirmKey");
            }
        }

        bool gate = IsGameScreenActive();
        if (!gate)
        {
            return;
        }

        bool logBusy = battleLogUI != null && battleLogUI.IsBusy;
        bool waitingChoice = turnSystem != null && turnSystem.IsWaitingForPlayerChoice;

        bool changed = gate != lastGate || logBusy != lastLogBusy || waitingChoice != lastWaitingChoice;
        if (changed)
        {
            lastGate = gate;
            lastLogBusy = logBusy;
            lastWaitingChoice = waitingChoice;
            ApplyState(false);
        }

        if (logBusy)
        {
            return;
        }
    }

    //CacheState는현재상태를기록한다
    private void CacheState(bool force)
    {
        if (!force)
        {
            return;
        }

        lastGate = IsGameScreenActive();
        lastLogBusy = battleLogUI != null && battleLogUI.IsBusy;
        lastWaitingChoice = turnSystem != null && turnSystem.IsWaitingForPlayerChoice;
    }

    //ApplyState는UI상태를일괄적용한다
    private void ApplyState(bool force)
    {
        bool gate = IsGameScreenActive();
        if (!gate)
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

    //TryRebindBattle는현재씬에서배틀관련컴포넌트를다시찾는다
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

        turnSystem = battleManager.TurnSystem != null ? battleManager.TurnSystem : battleManager.GetComponent<TurnSystem>();

        BindLog();
        RefreshSkillLabels();
        LogTag("RebindOK");
    }

    //IsGameScreenActive는게임화면활성여부를판단한다
    private bool IsGameScreenActive()
    {
        if (gameScreenRoot != null)
        {
            return gameScreenRoot.activeInHierarchy;
        }

        return battleManager != null;
    }

    //BindLog는로그버퍼와UI를연결한다
    private void BindLog()
    {
        if (battleManager == null || battleManager.LogBuffer == null || battleLogUI == null)
        {
            return;
        }

        battleManager.LogBuffer.OnLinePushed -= battleLogUI.Push;
        battleManager.LogBuffer.OnLinePushed += battleLogUI.Push;
    }

    //UnbindLog는로그연결을해제한다
    private void UnbindLog()
    {
        if (battleManager == null || battleManager.LogBuffer == null || battleLogUI == null)
        {
            return;
        }

        battleManager.LogBuffer.OnLinePushed -= battleLogUI.Push;
    }

    //OnClickFight는싸우기버튼처리이다
    private void OnClickFight()
    {
        if (turnSystem == null || !turnSystem.IsWaitingForPlayerChoice)
        {
            return;
        }

        if (battleLogUI != null && battleLogUI.IsBusy)
        {
            return;
        }

        RefreshSkillLabels();
        SetPopupVisible(commandPopupRoot, false);
        SetPopupVisible(skillPopupRoot, true);
        LogTag("FightClicked");
    }

    //OnClickBall은볼버튼처리이다
    private void OnClickBall()
    {
        if (battleManager != null)
        {
            battleManager.LogBuffer.Push("아직볼기능은준비중!");
        }

        LogTag("BallClicked");
    }

    //OnClickPokemon은포켓몬버튼처리이다
    private void OnClickPokemon()
    {
        if (battleManager != null)
        {
            battleManager.LogBuffer.Push("아직포켓몬교체기능은준비중!");
        }

        LogTag("PokemonClicked");
    }

    //OnClickRun은도망버튼처리이다
    private void OnClickRun()
    {
        if (battleManager != null)
        {
            battleManager.LogBuffer.Push("도망기능은준비중!");
        }

        LogTag("RunClicked");
    }

    //OnClickSkill은스킬선택처리이다
    private void OnClickSkill(int slotIndex)
    {
        if (turnSystem == null || !turnSystem.IsWaitingForPlayerChoice)
        {
            return;
        }

        if (battleLogUI != null && battleLogUI.IsBusy)
        {
            return;
        }

        if (battleManager == null)
        {
            return;
        }

        if (battleManager.GetPlayerSkill(slotIndex) == null)
        {
            return;
        }

        battleManager.SelectSkillSlot(slotIndex);
        SetPopupVisible(skillPopupRoot, false);
        LogTag("SkillClicked");
    }

    //RefreshSkillLabels는현재포켓몬스킬명을갱신한다
    private void RefreshSkillLabels()
    {
        if (battleManager == null)
        {
            return;
        }

        for (int i = 0; i < 4; i++)
        {
            if (skillLabels == null || i >= skillLabels.Length)
            {
                break;
            }

            if (skillLabels[i] == null)
            {
                continue;
            }

            skillLabels[i].text = battleManager.GetPlayerSkillName(i);
        }
    }

    //SetCommandButtonsInteractable은커맨드버튼입력을제어한다
    private void SetCommandButtonsInteractable(bool interactable)
    {
        if (fightButton != null)
        {
            fightButton.interactable = interactable;
        }

        if (ballButton != null)
        {
            ballButton.interactable = interactable;
        }

        if (pokemonButton != null)
        {
            pokemonButton.interactable = interactable;
        }

        if (runButton != null)
        {
            runButton.interactable = interactable;
        }
    }

    //CloseAllPopups는모든팝업을닫는다
    private void CloseAllPopups()
    {
        SetPopupVisible(commandPopupRoot, false);
        SetPopupVisible(skillPopupRoot, false);
        SetCommandButtonsInteractable(false);
    }

    //IsPopupSelfActive는팝업자체활성상태를반환한다
    private bool IsPopupSelfActive(GameObject root)
    {
        if (root == null)
        {
            return false;
        }

        return root.activeSelf;
    }

    //SetPopupVisible은팝업루트를켜거나끈다
    private void SetPopupVisible(GameObject popupRoot, bool visible)
    {
        if (popupRoot == null)
        {
            return;
        }

        //끄는경우:부모체인을절대건드리지않고자기자신만끈다
        if (!visible)
        {
            if (popupRoot.activeSelf)
            {
                popupRoot.SetActive(false);
            }
            return;
        }

        //켜는경우:부모가꺼져있으면자식이켜져도안보이므로부모체인을보장한다
        Transform p = popupRoot.transform.parent;
        while (p != null)
        {
            if (!p.gameObject.activeSelf)
            {
                p.gameObject.SetActive(true);
            }
            p = p.parent;
        }

        if (!popupRoot.activeSelf)
        {
            popupRoot.SetActive(true);
        }

        popupRoot.transform.SetAsLastSibling();
    }


    //SetMenuInputEnabled는외부메뉴입력을제어한다
    private void SetMenuInputEnabled(bool enabled)
    {
        if (menuButtonController != null)
        {
            menuButtonController.enabled = enabled;
        }
    }

    //LogTag는디버그로그를출력한다
    private void LogTag(string tag)
    {
        if (!debugLogs)
        {
            return;
        }

        Debug.Log("[BattleUI]" + tag);
    }
}
