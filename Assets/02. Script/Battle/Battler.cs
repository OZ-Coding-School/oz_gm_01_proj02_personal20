using System;
using UnityEngine;

/*
Battler는Battle영역에서전투중인개체(포켓몬)를표현하는runtime class다.
-외부에서는SetupFromPokedexEntry를호출해초기화한다.
-외부에서는SetSkills을호출해기술슬롯을설정한다.
-외부에서는ApplyDamage/Heal/ApplyStatus/ApplyStageDelta/GainExp를호출해상태를변경한다.
-UI바인딩을위해HP/EXP/레벨변경이벤트를제공한다.
*/
public sealed class Battler
{
    private const int StageMin = -6;
    private const int StageMax = 6;

    private string displayName;
    private int level;

    private int maxHp;
    private int hp;

    private int exp;
    private int expToNext;

    private int baseAtk;
    private int baseDef;
    private int baseSpAtk;
    private int baseSpDef;
    private int baseSpeed;

    private int stageAtk;
    private int stageDef;
    private int stageSpAtk;
    private int stageSpDef;
    private int stageSpeed;

    private BattleTypes.StatusAilment status = BattleTypes.StatusAilment.None;

    private readonly BattleSkillDataSO[] skillSlots = new BattleSkillDataSO[4];

    public string DisplayName => displayName;
    public int Level => level;
    public int MaxHp => maxHp;
    public int Hp => hp;

    public int Exp => exp;
    public int ExpToNext => expToNext;

    public BattleTypes.StatusAilment Status => status;
    public bool IsFainted => hp <= 0;

    public event Action<int, int> OnHpChanged;
    public event Action<int, int> OnExpChanged;
    public event Action<int> OnLevelChanged;

    //GetSkill은슬롯기술을반환한다.
    public BattleSkillDataSO GetSkill(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= skillSlots.Length) return null;
        return skillSlots[slotIndex];
    }

    //SetSkills는기술슬롯을설정한다.
    public void SetSkills(BattleSkillDataSO s0, BattleSkillDataSO s1, BattleSkillDataSO s2, BattleSkillDataSO s3)
    {
        skillSlots[0] = s0;
        skillSlots[1] = s1;
        skillSlots[2] = s2;
        skillSlots[3] = s3;
    }

    //SetupFromPokedexEntry는도감엔트리기반으로능력치를세팅한다.
    public void SetupFromPokedexEntry(PokemonEntry entry, int levelValue)
    {
        if (entry == null)
        {
            displayName = "Unknown";
            level = Mathf.Max(1, levelValue);
            maxHp = 10 + level * 2;
            hp = maxHp;

            baseAtk = 5 + level;
            baseDef = 5 + level;
            baseSpAtk = 5 + level;
            baseSpDef = 5 + level;
            baseSpeed = 5 + level;

            ResetStagesAndStatus();
            ResetExpState();
            RaiseAllVitals();
            return;
        }

        displayName = entry.Name;
        level = Mathf.Max(1, levelValue);

        maxHp = Mathf.Max(1, entry.HP + level * 2);
        hp = maxHp;

        baseAtk = Mathf.Max(1, entry.Atk + level);
        baseDef = Mathf.Max(1, entry.Def + level);
        baseSpAtk = Mathf.Max(1, entry.SpAtk + level);
        baseSpDef = Mathf.Max(1, entry.SpDef + level);
        baseSpeed = Mathf.Max(1, entry.Speed + level);

        ResetStagesAndStatus();
        ResetExpState();
        RaiseAllVitals();
    }

    //GetStat은현재스탯(랭크적용)을반환한다.
    public int GetStat(BattleTypes.BattleStat stat)
    {
        switch (stat)
        {
            case BattleTypes.BattleStat.Attack:
                return Mathf.Max(1, Mathf.RoundToInt(baseAtk * BattleTypes.GetStageMultiplier(stageAtk)));
            case BattleTypes.BattleStat.Defense:
                return Mathf.Max(1, Mathf.RoundToInt(baseDef * BattleTypes.GetStageMultiplier(stageDef)));
            case BattleTypes.BattleStat.SpAttack:
                return Mathf.Max(1, Mathf.RoundToInt(baseSpAtk * BattleTypes.GetStageMultiplier(stageSpAtk)));
            case BattleTypes.BattleStat.SpDefense:
                return Mathf.Max(1, Mathf.RoundToInt(baseSpDef * BattleTypes.GetStageMultiplier(stageSpDef)));
            case BattleTypes.BattleStat.Speed:
                return Mathf.Max(1, Mathf.RoundToInt(baseSpeed * BattleTypes.GetStageMultiplier(stageSpeed)));
            default:
                return 1;
        }
    }

    //ApplyDamage는현재HP를감소시키고0이하로내리지않는다.
    public void ApplyDamage(int amount)
    {
        if (amount <= 0) return;
        SetHp(hp - amount);
    }

    //Heal은현재HP를회복시키고최대치를넘기지않는다.
    public void Heal(int amount)
    {
        if (amount <= 0) return;
        SetHp(hp + amount);
    }

    //GainExp는경험치를추가한다(MVP:간단한레벨업만지원).
    public void GainExp(int amount)
    {
        if (amount <= 0) return;

        exp += amount;
        RaiseExp();

        while (expToNext > 0 && exp >= expToNext)
        {
            exp -= expToNext;
            LevelUpOnce();
            RaiseExp();
        }
    }

    //ApplyStatus는상태이상을적용한다(이미있으면무시).
    public bool ApplyStatus(BattleTypes.StatusAilment ailment)
    {
        if (ailment == BattleTypes.StatusAilment.None) return false;
        if (status != BattleTypes.StatusAilment.None) return false;

        status = ailment;
        return true;
    }

    //ClearStatus는상태이상을해제한다.
    public void ClearStatus()
    {
        status = BattleTypes.StatusAilment.None;
    }

    //ApplyStageDelta는능력치랭크를변경한다.
    public int ApplyStageDelta(BattleTypes.BattleStat stat, int delta)
    {
        if (delta == 0) return 0;

        int before;

        switch (stat)
        {
            case BattleTypes.BattleStat.Attack:
                before = stageAtk;
                stageAtk = Mathf.Clamp(stageAtk + delta, StageMin, StageMax);
                return stageAtk - before;

            case BattleTypes.BattleStat.Defense:
                before = stageDef;
                stageDef = Mathf.Clamp(stageDef + delta, StageMin, StageMax);
                return stageDef - before;

            case BattleTypes.BattleStat.SpAttack:
                before = stageSpAtk;
                stageSpAtk = Mathf.Clamp(stageSpAtk + delta, StageMin, StageMax);
                return stageSpAtk - before;

            case BattleTypes.BattleStat.SpDefense:
                before = stageSpDef;
                stageSpDef = Mathf.Clamp(stageSpDef + delta, StageMin, StageMax);
                return stageSpDef - before;

            case BattleTypes.BattleStat.Speed:
                before = stageSpeed;
                stageSpeed = Mathf.Clamp(stageSpeed + delta, StageMin, StageMax);
                return stageSpeed - before;

            default:
                return 0;
        }
    }

    //ResetStagesAndStatus는전투시작시랭크/상태를초기화한다.
    private void ResetStagesAndStatus()
    {
        stageAtk = 0;
        stageDef = 0;
        stageSpAtk = 0;
        stageSpDef = 0;
        stageSpeed = 0;
        status = BattleTypes.StatusAilment.None;
    }

    //SetHp는HP를클램프하고변경이벤트를발행한다.
    private void SetHp(int value)
    {
        int next = Mathf.Clamp(value, 0, Mathf.Max(1, maxHp));
        if (next == hp) return;

        hp = next;
        RaiseHp();
    }

    //ResetExpState는경험치상태를초기화한다.
    private void ResetExpState()
    {
        exp = 0;
        expToNext = ComputeExpToNext(level);
    }

    //LevelUpOnce는레벨업을1회수행한다(MVP:스탯간단증가).
    private void LevelUpOnce()
    {
        level = Mathf.Max(1, level + 1);

        maxHp = Mathf.Max(1, maxHp + 2);
        hp = Mathf.Min(maxHp, hp + 2);

        baseAtk = Mathf.Max(1, baseAtk + 1);
        baseDef = Mathf.Max(1, baseDef + 1);
        baseSpAtk = Mathf.Max(1, baseSpAtk + 1);
        baseSpDef = Mathf.Max(1, baseSpDef + 1);
        baseSpeed = Mathf.Max(1, baseSpeed + 1);

        expToNext = ComputeExpToNext(level);

        OnLevelChanged?.Invoke(level);
        RaiseHp();
    }

    //ComputeExpToNext는다음레벨까지필요경험치를반환한다(MVP).
    private int ComputeExpToNext(int lv)
    {
        int l = Mathf.Max(1, lv);
        return 10 + l * 5;
    }

    //RaiseAllVitals는초기렌더용이벤트를발행한다.
    private void RaiseAllVitals()
    {
        RaiseHp();
        RaiseExp();
        OnLevelChanged?.Invoke(level);
    }

    private void RaiseHp()
    {
        Debug.Log($"[HP] {displayName} {hp}/{maxHp}");
        OnHpChanged?.Invoke(hp, maxHp);
    }

    private void RaiseExp()
    {
        OnExpChanged?.Invoke(exp, expToNext);
    }
}