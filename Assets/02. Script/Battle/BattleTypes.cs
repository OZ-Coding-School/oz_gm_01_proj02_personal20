using UnityEngine;

/*
BattleTypes는Battle영역에서사용되는enum/상수집합이다.
-전투에서공통으로쓰는타입/스탯/상태/스테이지테이블을제공한다.
-외부에서는GetStageMultiplier를호출해이기능을사용한다.
*/
public static class BattleTypes
{
    public enum SkillCategory
    {
        Physical = 0,
        Special = 1,
        Status = 2,
    }

    public enum BattleStat
    {
        Attack = 0,
        Defense = 1,
        SpAttack = 2,
        SpDefense = 3,
        Speed = 4,
    }

    public enum StatusAilment
    {
        None = 0,
        Poison = 1,
        Burn = 2,
    }

    //GetStageMultiplier는능력치랭크(-6~+6)의배율을반환한다.
    public static float GetStageMultiplier(int stage)
    {
        int s = Mathf.Clamp(stage, -6, 6);

        if (s >= 0)
        {
            return (2f + s) / 2f;
        }

        return 2f / (2f + Mathf.Abs(s));
    }
}
