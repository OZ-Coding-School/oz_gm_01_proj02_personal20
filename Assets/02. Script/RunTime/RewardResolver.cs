using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/*
RewardResolver는 전투종료 후 정산로그를 표시하고
정산이 끝나면 RunManager에게 결과만 전달한다.
※ 화면 전환(Game <-> ShopReward)은 UIManager가 RunState를 보고 처리한다.
*/
public sealed class RewardResolver : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private BattleLogUI battleLogUI;

    [Header("Rewards")]
    [SerializeField, Min(1)] private int expGainTest = 12;

    [Header("Debug")]
    [SerializeField] private bool debugLogs;

    private BattleManager battleManager;
    private RunManager runManager;

    private Coroutine routine;
    private Coroutine bindRoutine;
    private bool bindRequested;

    private void Awake()
    {
        if (battleLogUI == null) battleLogUI = FindObjectOfType<BattleLogUI>(true);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        RequestBindNextFrame();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        UnbindBattle();

        if (bindRoutine != null)
        {
            StopCoroutine(bindRoutine);
            bindRoutine = null;
        }
        bindRequested = false;

        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RequestBindNextFrame();
    }

    private void RequestBindNextFrame()
    {
        if (bindRequested) return;
        bindRequested = true;

        if (bindRoutine != null)
        {
            StopCoroutine(bindRoutine);
            bindRoutine = null;
        }

        bindRoutine = StartCoroutine(CoBindNextFrame());
    }

    private IEnumerator CoBindNextFrame()
    {
        yield return null;

        bindRequested = false;

        TryRebindBattle();
        CacheRunManager();
    }

    private void CacheRunManager()
    {
        runManager = RunManager.Instance;
        if (runManager == null) LogTag("RunManagerMissing");
    }

    private void TryRebindBattle()
    {
        UnbindBattle();

        battleManager = FindObjectOfType<BattleManager>(true);
        if (battleManager == null)
        {
            LogTag("RebindFail");
            return;
        }

        battleManager.BattleEnded -= OnBattleEnded;
        battleManager.BattleEnded += OnBattleEnded;

        LogTag("RebindOK");
    }

    private void UnbindBattle()
    {
        if (battleManager != null)
        {
            battleManager.BattleEnded -= OnBattleEnded;
            battleManager = null;
        }

        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
    }

    private void OnBattleEnded(bool playerWon, Battler player, Battler enemy, BattleLogBuffer log)
    {
        if (routine != null) return;
        routine = StartCoroutine(CoResolve(playerWon, player, enemy, log));
    }

    private IEnumerator CoResolve(bool playerWon, Battler player, Battler enemy, BattleLogBuffer log)
    {
        LogTag("ResolveStart");

        if (playerWon)
        {
            log.Push(enemy.DisplayName + "이/가 쓰러졌다!");
            yield return WaitLogIdle();

            player.GainExp(expGainTest);
            log.Push(player.DisplayName + "은/는\n" + expGainTest + "의 경험치를 얻었다!");
            yield return WaitLogIdle();

            NotifyRunBattleEnded(true);
        }
        else
        {
            log.Push(player.DisplayName + "은/는 쓰러졌다!");
            yield return WaitLogIdle();

            NotifyRunBattleEnded(false);
        }

        routine = null;
        LogTag("ResolveEnd");
    }

    private IEnumerator WaitLogIdle()
    {
        if (battleLogUI == null) yield break;
        yield return new WaitUntil(() => !battleLogUI.IsBusy);
    }

    private void NotifyRunBattleEnded(bool playerWon)
    {
        if (runManager == null) runManager = RunManager.Instance;
        if (runManager == null)
        {
            LogTag("RunManagerMissing");
            return;
        }

        // 여기서 상태가 InShopOrReward로 바뀌면
        // UIManager가 자동으로 ScreenId.ShopReward를 켠다.
        runManager.ReportBattleEnded(playerWon);
        LogTag("ReportBattleEnded");
    }

    private void LogTag(string tag)
    {
        if (!debugLogs) return;
        Debug.Log("[RewardResolver]" + tag);
    }
}
