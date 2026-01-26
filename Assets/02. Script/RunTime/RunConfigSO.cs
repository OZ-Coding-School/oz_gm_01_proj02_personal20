using UnityEngine;

/*
RunConfigSO는런난이도/스테이지/조우풀설정을보관한다.
-Resources/RunConfig.asset로두면RunManager가자동로드한다.
*/
[CreateAssetMenu(menuName = "PokeRogue/Run/RunConfig")]
public sealed class RunConfigSO : ScriptableObject
{
    [Header("Economy")]
    [Min(0)]
    [SerializeField] private int startGold = 0;

    [Header("Battle Level")]
    [Min(1)]
    [SerializeField] private int baseBattleLevel = 5;
    [Min(0)]
    [SerializeField] private int levelStepPerStage = 1;

    [Header("Encounter Pools")]
    [SerializeField] private RunEncounterPoolSO earlyPool;
    [SerializeField] private RunEncounterPoolSO midPool;
    [SerializeField] private RunEncounterPoolSO latePool;

    public int StartGold => startGold;
    public int BaseBattleLevel => baseBattleLevel;
    public int LevelStepPerStage => levelStepPerStage;

    public RunEncounterPoolSO EarlyPool => earlyPool;
    public RunEncounterPoolSO MidPool => midPool;
    public RunEncounterPoolSO LatePool => latePool;

    //GetPool은바이옴/스테이지에맞는풀을반환한다
    public RunEncounterPoolSO GetPool(int biomeIndex, int stageIndex)
    {
        if (stageIndex <= 3 && earlyPool != null) return earlyPool;
        if (stageIndex <= 7 && midPool != null) return midPool;
        if (latePool != null) return latePool;
        return earlyPool != null ? earlyPool : midPool != null ? midPool : latePool;
    }
}
