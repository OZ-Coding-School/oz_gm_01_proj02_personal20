//Assets/_Project/Scripts/Run/RunEncounterPoolSO.cs
using System;
using System.Collections.Generic;
using UnityEngine;

/*
RunEncounterPoolSO는조우후보리스트를보관한다.
*/
[CreateAssetMenu(menuName = "PokeRogue/Run/EncounterPool")]
public sealed class RunEncounterPoolSO : ScriptableObject
{
    [SerializeField] private List<RunEncounterEntry> entries = new List<RunEncounterEntry>();

    public int Count => entries != null ? entries.Count : 0;

    //TryGetRandom은랜덤조우를선택한다
    public bool TryGetRandom(out RunEncounterEntry entry)
    {
        entry = default;

        if (entries == null || entries.Count <= 0)
        {
            return false;
        }

        int idx = UnityEngine.Random.Range(0, entries.Count);
        entry = entries[idx];
        return true;
    }
}

/*
RunEncounterEntry는한번의조우데이터다(적도감번호/레벨오프셋).
BattleSkillDataSO로타이핑하려면너프로젝트경로에맞춰using추가후Object를교체한다.
*/
[Serializable]
public struct RunEncounterEntry
{
    [Min(1)]
    [SerializeField] private int enemyPokedexNo;
    [Min(0)]
    [SerializeField] private int enemyLevelOffset;

    [SerializeField] private UnityEngine.Object enemySkill0;
    [SerializeField] private UnityEngine.Object enemySkill1;
    [SerializeField] private UnityEngine.Object enemySkill2;
    [SerializeField] private UnityEngine.Object enemySkill3;

    public int EnemyPokedexNo => enemyPokedexNo;
    public int EnemyLevelOffset => enemyLevelOffset;

    public UnityEngine.Object EnemySkill0 => enemySkill0;
    public UnityEngine.Object EnemySkill1 => enemySkill1;
    public UnityEngine.Object EnemySkill2 => enemySkill2;
    public UnityEngine.Object EnemySkill3 => enemySkill3;
}
