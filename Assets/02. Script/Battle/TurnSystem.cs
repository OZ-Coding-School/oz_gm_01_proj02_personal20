using System;
using System.Collections;
using UnityEngine;

/*
TurnSystem는Battle영역에서턴진행상태머신을담당하는컴포넌트다.
-외부에서는BeginBattle을호출해전투루프를시작한다.
-외부에서는SetPlayerChoice을호출해플레이어기술선택을전달한다.
*/
public sealed class TurnSystem : MonoBehaviour
{
    private const int SkillSlots = 4;

    private Battler player;
    private Battler enemy;
    private SkillExecutor executor;
    private BattleLogBuffer log;

    private Coroutine battleRoutine;

    private bool waitingForPlayerChoice;
    private int chosenPlayerSlot = -1;

    private Action<bool> onBattleEnded;

    public bool IsWaitingForPlayerChoice => waitingForPlayerChoice;

    //BeginBattle는전투루프를시작한다.
    public void BeginBattle(Battler playerBattler, Battler enemyBattler, SkillExecutor skillExecutor, BattleLogBuffer logBuffer, Action<bool> onBattleEndedCallback)
    {
        if (battleRoutine != null)
        {
            StopCoroutine(battleRoutine);
            battleRoutine = null;
        }

        player = playerBattler;
        enemy = enemyBattler;
        executor = skillExecutor;
        log = logBuffer;
        onBattleEnded = onBattleEndedCallback;

        if (player == null || enemy == null || executor == null || log == null)
        {
            Debug.LogWarning("TurnSystem.BeginBattle:invalid refs");
            return;
        }

        chosenPlayerSlot = -1;
        waitingForPlayerChoice = false;

        log.Push("전투 시작!");
        log.Push(player.DisplayName + " VS " + enemy.DisplayName);

        battleRoutine = StartCoroutine(BattleLoop());
    }

    //SetPlayerChoice는플레이어선택을턴시스템에전달한다.
    public void SetPlayerChoice(int slotIndex)
    {
        if (!waitingForPlayerChoice) return;
        if (slotIndex < 0 || slotIndex >= SkillSlots) return;
        if (player == null) return;
        if (player.GetSkill(slotIndex) == null) return;

        chosenPlayerSlot = slotIndex;
        waitingForPlayerChoice = false;
    }

    //BattleLoop는턴진행코루틴이다.
    private IEnumerator BattleLoop()
    {
        while (true)
        {
            if (player.IsFainted || enemy.IsFainted)
            {
                EndBattle();
                yield break;
            }

            //BattleLoop는플레이어입력을기다린다.
            waitingForPlayerChoice = true;
            chosenPlayerSlot = -1;
            log.Push("기술을 선택해라(1~4).");

            yield return new WaitUntil(() => !waitingForPlayerChoice);

            int enemySlot = ChooseEnemySkillSlot(enemy);
            int firstActor = DecideFirstActor(player, enemy);

            if (firstActor == 0)
            {
                yield return ExecuteAction(player, enemy, chosenPlayerSlot);
                if (enemy.IsFainted) continue;

                yield return ExecuteAction(enemy, player, enemySlot);
            }
            else
            {
                yield return ExecuteAction(enemy, player, enemySlot);
                if (player.IsFainted) continue;

                yield return ExecuteAction(player, enemy, chosenPlayerSlot);
            }

            if (player.IsFainted || enemy.IsFainted) continue;

            ApplyEndTurnEffects(player);
            if (player.IsFainted) continue;

            ApplyEndTurnEffects(enemy);
        }
    }

    //ExecuteAction은한번의행동을실행한다.
    private IEnumerator ExecuteAction(Battler attacker, Battler defender, int slotIndex)
    {
        BattleSkillDataSO skill = attacker.GetSkill(slotIndex);
        if (skill == null)
        {
            log.Push(attacker.DisplayName + "은(는) 사용할 기술이 없다!");
            yield return null;
            yield break;
        }

        executor.Execute(attacker, defender, skill, log);
        yield return null;
    }

    //ChooseEnemySkillSlot는간단AI로기술슬롯을선택한다.
    private int ChooseEnemySkillSlot(Battler b)
    {
        //ChooseEnemySkillSlot는최고위력기술을우선선택한다.
        int bestSlot = -1;
        int bestPower = -1;

        for (int i = 0; i < SkillSlots; i++)
        {
            BattleSkillDataSO s = b.GetSkill(i);
            if (s == null) continue;

            int p = s.Power;
            if (p > bestPower)
            {
                bestPower = p;
                bestSlot = i;
            }
        }

        if (bestSlot >= 0) return bestSlot;

        //ChooseEnemySkillSlot는모두없으면0을반환한다.
        return 0;
    }

    //DecideFirstActor는스피드비교로선공을결정한다.
    private int DecideFirstActor(Battler a, Battler b)
    {
        int spA = a.GetStat(BattleTypes.BattleStat.Speed);
        int spB = b.GetStat(BattleTypes.BattleStat.Speed);

        if (spA > spB) return 0;
        if (spB > spA) return 1;

        return UnityEngine.Random.Range(0, 2);
    }

    //ApplyEndTurnEffects는상태이상지속효과를적용한다.
    private void ApplyEndTurnEffects(Battler b)
    {
        if (b == null || b.IsFainted) return;

        int dot = b.ComputeEndTurnDot();
        if (dot <= 0) return;

        b.ApplyDamage(dot);
        log.Push(b.DisplayName + "은(는) " + GetStatusName(b.Status) + "로 " + dot + "의 피해!");

        if (b.IsFainted)
        {
            log.Push(b.DisplayName + "은(는) 쓰러졌다!");
        }
    }

    //EndBattle는전투종료를처리한다.
    private void EndBattle()
    {
        bool playerWon = enemy != null && enemy.IsFainted && player != null && !player.IsFainted;
        log.Push(playerWon ? "승리!" : "패배...");

        onBattleEnded?.Invoke(playerWon);

        if (battleRoutine != null)
        {
            StopCoroutine(battleRoutine);
            battleRoutine = null;
        }
    }

    //GetStatusName은표시용이름을반환한다.
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
