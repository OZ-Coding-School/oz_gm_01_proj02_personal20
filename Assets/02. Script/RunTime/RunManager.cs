using System;
using UnityEngine;

/*
RunManager는로그라이크런전체흐름(전투→정산→상점/보상→다음전투)을단일책임으로관리한다.
-씬전환최소화전제(GameScene내Screen전환),UI는이벤트만구독한다.
-10라운드클리어시상점/보상없이즉시지역전환후다음전투로간다.
*/
public sealed class RunManager : MonoBehaviour
{
    private const int StagePerBiome = 10;

    [Header("Config")]
    [SerializeField] private RunConfigSO config;

    [Header("Debug")]
    [SerializeField] private bool debugLogs;

    private RunState state = RunState.None;
    private int biomeIndex = 1;
    private int stageIndex = 0;
    private int gold = 0;

    private RunEncounter currentEncounter;

    private bool rewardLocked;//보상선택후락(즉시다음전투로넘어가므로안전가드)

    public static RunManager Instance { get; private set; }

    public RunState State => state;
    public int BiomeIndex => biomeIndex;
    public int StageIndex => stageIndex;
    public int Gold => gold;
    public RunEncounter CurrentEncounter => currentEncounter;

    public event Action<RunState> OnStateChanged;
    public event Action<RunEncounter> OnEncounterPrepared;

    //Awake는싱글톤과DontDestroy를설정한다
    private void Awake()
    {
        // 싱글톤 처리(이미 있으면 파괴)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 루트가 아니면 루트로 떼기
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }

        DontDestroyOnLoad(gameObject);
        LogTag("Awake");
    }

    //StartNewRun은런을초기화하고첫전투를준비한다
    public void StartNewRun(int starterPokemonNo)
    {
        if (config == null)
        {
            Debug.LogError("RunManager:config missing");
            return;
        }

        biomeIndex = 1;
        stageIndex = 1;
        gold = config.StartGold;

        rewardLocked = false;

        PrepareNextBattle();
        LogTag("StartNewRun");
    }

    //PrepareNextBattle는다음전투조우를결정하고InBattle로전환한다
    public void PrepareNextBattle()
    {
        if (config == null)
        {
            Debug.LogError("RunManager:config missing");
            return;
        }

        rewardLocked = false;

        currentEncounter = RunEncounterGenerator.Generate(config, biomeIndex, stageIndex);
        SetState(RunState.InBattle);

        OnEncounterPrepared?.Invoke(currentEncounter);
        LogTag("PrepareNextBattle");
    }

    //ReportBattleEnded는전투결과를받아다음상태로전환한다
    public void ReportBattleEnded(bool playerWon)
    {
        Debug.Log("[Run]ReportBattleEnded playerWon=" + playerWon);
        if (state != RunState.InBattle)
        {
            Debug.LogWarning("RunManager:ReportBattleEnded ignored(not InBattle)");
            return;
        }

        if (!playerWon)
        {
            SetState(RunState.GameOver);
            LogTag("GameOver");
            return;
        }

        stageIndex++;

        if (IsBiomeTransitionStage(stageIndex - 1))
        {
            biomeIndex++;
            SetState(RunState.BiomeTransition);
            LogTag("BiomeTransition");

            PrepareNextBattle();
            return;
        }

        SetState(RunState.InShopOrReward);
        LogTag("ToShopOrReward");
    }

    //TryBuyShopItem은상점아이템구매를시도한다(여러번가능)
    public bool TryBuyShopItem(int price, Action applyEffect)
    {
        if (state != RunState.InShopOrReward)
        {
            Debug.LogWarning("RunManager:TryBuyShopItem ignored(not InShopOrReward)");
            return false;
        }

        if (rewardLocked)
        {
            Debug.LogWarning("RunManager:TryBuyShopItem ignored(rewardLocked)");
            return false;
        }

        if (!TrySpendGold(price))
        {
            Debug.Log("RunManager:NoGold");
            return false;
        }

        if (applyEffect != null)
        {
            applyEffect.Invoke();
        }

        LogTag("BuyShopItem");
        return true;
    }

    //PickRewardAndContinue는보상선택을확정하고즉시다음전투로간다
    public void PickRewardAndContinue(Action applyEffect)
    {
        if (state != RunState.InShopOrReward)
        {
            Debug.LogWarning("RunManager:PickReward ignored");
            return;
        }

        if (rewardLocked)
        {
            Debug.LogWarning("RunManager:PickReward ignored(already picked)");
            return;
        }

        rewardLocked = true;

        if (applyEffect != null)
        {
            applyEffect.Invoke();
        }

        PrepareNextBattle();
        LogTag("PickRewardAndContinue");
    }

    //AddGold는골드를증가시킨다
    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        gold += amount;
        LogTag("AddGold");
    }

    //TrySpendGold는골드소모를시도한다
    public bool TrySpendGold(int amount)
    {
        if (amount <= 0) return true;
        if (gold < amount) return false;
        gold -= amount;
        LogTag("SpendGold");
        return true;
    }

    //IsBiomeTransitionStage는지역전환판정이다(10,20,30...)
    private bool IsBiomeTransitionStage(int clearedStage)
    {
        if (clearedStage <= 0) return false;
        return (clearedStage % StagePerBiome) == 0;
    }

    //SetState는상태를변경하고이벤트를발행한다
    private void SetState(RunState next)
    {
        if (state == next) return;
        state = next;
        OnStateChanged?.Invoke(state);
        LogTag("StateChanged");
    }

    //LogTag는흐름추적용로그를출력한다(Update에서호출금지)
    private void LogTag(string tag)
    {
        if (!debugLogs) return;
        Debug.Log("[Run]" + tag);
    }
}
