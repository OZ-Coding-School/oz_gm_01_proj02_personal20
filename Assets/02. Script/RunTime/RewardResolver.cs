using System;
using UnityEngine;

/*
RewardResolver는보상선택을적용하고RunManager로다음전투를진행시키는컴포넌트다.
-UI는SelectReward/ConfirmSelected만호출한다.
-RunManager상태가InShopOrReward일때만동작한다.
*/
public sealed class RewardResolver : MonoBehaviour
{
    [Serializable]
    private struct RewardOption
    {
        [SerializeField] private RewardType type;
        [Min(0)]
        [SerializeField] private int amount;

        public RewardType Type => type;
        public int Amount => amount;
    }

    private enum RewardType
    {
        None = 0,
        Gold = 1
        //Heal=2, Item=3, SkillPP=4... 확장
    }

    [Header("Options (MVP)")]
    [SerializeField] private RewardOption[] options = Array.Empty<RewardOption>();

    [Header("Debug")]
    [SerializeField] private bool debugLogs;

    private int selectedIndex = -1;

    //OnEnable은RunManager이벤트를구독한다
    private void OnEnable()
    {
        if (RunManager.Instance != null)
        {
            RunManager.Instance.OnStateChanged += OnRunStateChanged;
        }
    }

    //OnDisable은구독을해제한다
    private void OnDisable()
    {
        if (RunManager.Instance != null)
        {
            RunManager.Instance.OnStateChanged -= OnRunStateChanged;
        }
    }

    //SelectReward는UI에서보상슬롯을선택할때호출한다
    public void SelectReward(int index)
    {
        if (!IsRewardState()) return;
        if (options == null || options.Length == 0) return;
        if (index < 0 || index >= options.Length) return;

        selectedIndex = index;
        LogTag("SelectReward");
    }

    //ConfirmSelected는UI에서확정버튼을눌렀을때호출한다
    public void ConfirmSelected()
    {
        if (!IsRewardState()) return;
        if (options == null || options.Length == 0) return;
        if (selectedIndex < 0 || selectedIndex >= options.Length) return;

        ApplyReward(options[selectedIndex]);
        selectedIndex = -1;

        RunManager.Instance.CommitRewardAndContinue();
        LogTag("ConfirmSelected");
    }

    //OnRunStateChanged는상태진입시선택을리셋한다
    private void OnRunStateChanged(RunState state)
    {
        if (state == RunState.InShopOrReward)
        {
            selectedIndex = -1;
            LogTag("EnterRewardState");
        }
    }

    //ApplyReward는선택보상을적용한다(MVP:골드만)
    private void ApplyReward(RewardOption opt)
    {
        if (RunManager.Instance == null) return;

        switch (opt.Type)
        {
            case RewardType.Gold:
                RunManager.Instance.AddGold(opt.Amount);
                break;
        }

        LogTag("ApplyReward");
    }

    //IsRewardState는현재보상상태인지판정한다
    private bool IsRewardState()
    {
        return RunManager.Instance != null && RunManager.Instance.State == RunState.InShopOrReward;
    }

    //LogTag는추적용로그다(Update에서호출금지)
    private void LogTag(string tag)
    {
        if (!debugLogs) return;
        Debug.Log("[RewardResolver]" + tag);
    }
}
