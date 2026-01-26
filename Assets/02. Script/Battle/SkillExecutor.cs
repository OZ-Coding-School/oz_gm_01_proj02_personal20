using UnityEngine;

/*
SkillExecutor는Battle영역에서기술실행을담당하는로직컴포넌트다.
-외부에서는Execute를호출해이기능을사용한다.
-피해/명중/상태이상/랭크변화를처리한다.
*/
public sealed class SkillExecutor : MonoBehaviour
{
    //Execute는기술을실행하고결과로그를남긴다.
    public void Execute(Battler attacker, Battler defender, BattleSkillDataSO skill, BattleLogBuffer log)
    {
        if (attacker == null || defender == null || skill == null || log == null)
        {
            Debug.LogWarning("SkillExecutor.Execute:invalid args");
            return;
        }

        if (attacker.IsFainted)
        {
            log.Push($"{attacker.DisplayName}은(는)\n기절해 행동할 수 없다!");
            return;
        }

        //1) 기술 사용(1~2줄 세트 유지)
        log.Push($"{attacker.DisplayName}의 {skill.SkillName}!\n");

        if (!Roll(skill.Accuracy))
        {
            log.Push("하지만\n빗나갔다!");
            return;
        }

        if (skill.Category == BattleTypes.SkillCategory.Status)
        {
            ResolveStatusSkill(attacker, defender, skill, log);
            return;
        }

        int damage = ComputeDamage(attacker, defender, skill);
        defender.ApplyDamage(damage);

        log.Push($"{defender.DisplayName}에게\n{damage}의 데미지!");

        if (defender.IsFainted)
        {
            log.Push($"{defender.DisplayName}이/가\n쓰러졌다!");
            return;
        }

        TryApplySecondaryEffects(attacker, defender, skill, log);
    }

    //ComputeDamage는간단한포켓몬식데미지공식을적용한다.
    private int ComputeDamage(Battler attacker, Battler defender, BattleSkillDataSO skill)
    {
        int level = Mathf.Max(1, attacker.Level);
        int power = Mathf.Max(1, skill.Power);

        int atk = skill.Category == BattleTypes.SkillCategory.Physical
            ? attacker.GetStat(BattleTypes.BattleStat.Attack)
            : attacker.GetStat(BattleTypes.BattleStat.SpAttack);

        int def = skill.Category == BattleTypes.SkillCategory.Physical
            ? defender.GetStat(BattleTypes.BattleStat.Defense)
            : defender.GetStat(BattleTypes.BattleStat.SpDefense);

        int a = (2 * level) / 5 + 2;
        int baseDamage = ((a * power * atk) / Mathf.Max(1, def)) / 50 + 2;

        float random = UnityEngine.Random.Range(0.85f, 1.01f);
        int finalDamage = Mathf.Max(1, Mathf.RoundToInt(baseDamage * random));
        return finalDamage;
    }

    //ResolveStatusSkill는상태기술의효과를처리한다.
    private void ResolveStatusSkill(Battler attacker, Battler defender, BattleSkillDataSO skill, BattleLogBuffer log)
    {
        bool any = false;

        if (skill.StageDelta != 0 && Roll(skill.StageChancePercent))
        {
            int applied = defender.ApplyStageDelta(skill.StageTargetStat, skill.StageDelta);
            if (applied != 0)
            {
                any = true;
                string dir = applied > 0 ? "올랐다!" : "내려갔다!";
                log.Push($"{defender.DisplayName}의 {GetStatName(skill.StageTargetStat)}\n랭크가 {dir}");
            }
        }

        if (skill.ApplyStatus != BattleTypes.StatusAilment.None && Roll(skill.StatusChancePercent))
        {
            if (defender.ApplyStatus(skill.ApplyStatus))
            {
                any = true;
                log.Push($"{defender.DisplayName}은(는)\n{GetStatusName(skill.ApplyStatus)} 상태가 되었다!");
            }
            else
            {
                any = true;
                log.Push("하지만\n효과가 없었다!");
            }
        }

        if (!any)
        {
            log.Push("하지만\n아무 일도 일어나지 않았다!");
        }
    }

    //TryApplySecondaryEffects는공격기술의부가효과를처리한다.
    private void TryApplySecondaryEffects(Battler attacker, Battler defender, BattleSkillDataSO skill, BattleLogBuffer log)
    {
        if (skill.StageDelta != 0 && Roll(skill.StageChancePercent))
        {
            int applied = defender.ApplyStageDelta(skill.StageTargetStat, skill.StageDelta);
            if (applied != 0)
            {
                string dir = applied > 0 ? "올랐다!" : "내려갔다!";
                log.Push($"{defender.DisplayName}의 {GetStatName(skill.StageTargetStat)}\n랭크가 {dir}");
            }
        }

        if (skill.ApplyStatus != BattleTypes.StatusAilment.None && Roll(skill.StatusChancePercent))
        {
            if (defender.ApplyStatus(skill.ApplyStatus))
            {
                log.Push($"{defender.DisplayName}은(는)\n{GetStatusName(skill.ApplyStatus)} 상태가 되었다!");
            }
        }
    }

    //Roll은확률판정을수행한다.
    private bool Roll(int chancePercent)
    {
        int c = Mathf.Clamp(chancePercent, 0, 100);
        if (c <= 0) return false;
        if (c >= 100) return true;
        return UnityEngine.Random.Range(1, 101) <= c;
    }

    private string GetStatName(BattleTypes.BattleStat stat)
    {
        switch (stat)
        {
            case BattleTypes.BattleStat.Attack: return "공격";
            case BattleTypes.BattleStat.Defense: return "방어";
            case BattleTypes.BattleStat.SpAttack: return "특수공격";
            case BattleTypes.BattleStat.SpDefense: return "특수방어";
            case BattleTypes.BattleStat.Speed: return "스피드";
            default: return "능력치";
        }
    }

    private string GetStatusName(BattleTypes.StatusAilment ailment)
    {
        switch (ailment)
        {
            case BattleTypes.StatusAilment.Poison: return "독";
            case BattleTypes.StatusAilment.Burn: return "화상";
            default: return "이상";
        }
    }
}