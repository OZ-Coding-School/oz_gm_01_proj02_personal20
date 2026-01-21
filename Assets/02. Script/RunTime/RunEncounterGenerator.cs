//Assets/_Project/Scripts/Run/RunEncounterGenerator.cs
using System;
using UnityEngine;

/*
RunEncounter는현재전투에필요한최소정보(적도감번호/레벨/스킬)를보관한다.
*/
[Serializable]
public struct RunEncounter
{
    [SerializeField] private int enemyPokedexNo;
    [SerializeField] private int battleLevel;

    [SerializeField] private UnityEngine.Object enemySkill0;
    [SerializeField] private UnityEngine.Object enemySkill1;
    [SerializeField] private UnityEngine.Object enemySkill2;
    [SerializeField] private UnityEngine.Object enemySkill3;

    public int EnemyPokedexNo => enemyPokedexNo;
    public int BattleLevel => battleLevel;

    public UnityEngine.Object EnemySkill0 => enemySkill0;
    public UnityEngine.Object EnemySkill1 => enemySkill1;
    public UnityEngine.Object EnemySkill2 => enemySkill2;
    public UnityEngine.Object EnemySkill3 => enemySkill3;

    //Create는엔트리+레벨로조우를생성한다
    public static RunEncounter Create(RunEncounterEntry entry, int battleLevel)
    {
        RunEncounter e = new RunEncounter();
        e.enemyPokedexNo = entry.EnemyPokedexNo;
        e.battleLevel = battleLevel;
        e.enemySkill0 = entry.EnemySkill0;
        e.enemySkill1 = entry.EnemySkill1;
        e.enemySkill2 = entry.EnemySkill2;
        e.enemySkill3 = entry.EnemySkill3;
        return e;
    }
}

/*
RunEncounterGenerator는런설정에서현재스테이지조우를생성한다.
*/
public static class RunEncounterGenerator
{
    //Generate는config/현재스테이지로조우를생성한다
    public static RunEncounter Generate(RunConfigSO config, int biomeIndex, int stageIndex)
    {
        if (config == null)
        {
            return default;
        }

        RunEncounterPoolSO pool = config.GetPool(biomeIndex, stageIndex);
        if (pool == null || !pool.TryGetRandom(out RunEncounterEntry entry))
        {
            return default;
        }

        int level = config.BaseBattleLevel + (stageIndex - 1) * config.LevelStepPerStage + entry.EnemyLevelOffset;
        if (level < 1) level = 1;

        return RunEncounter.Create(entry, level);
    }
}

/*
RunState는런플로우상태다.
*/
public enum RunState
{
    None = 0,
    InBattle = 1,
    InShopOrReward = 2,
    BiomeTransition = 3,
    GameOver = 4
}
