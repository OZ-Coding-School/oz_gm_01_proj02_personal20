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
    private Action<bool> onBattleEnded;

    private Coroutine battleRoutine;

    private bool waitingForPlayerChoice;
    private int chosenPlayerSlot = -1;

    public bool IsWaitingForPlayerChoice => waitingForPlayerChoice;

    public void BeginBattle(Battler player, Battler enemy, SkillExecutor executor, BattleLogBuffer log, Action<bool> onBattleEnded)
    {
        this.player = player;
        this.enemy = enemy;
        this.executor = executor;
        this.log = log;
        this.onBattleEnded = onBattleEnded;

        if (battleRoutine != null)
            StopCoroutine(battleRoutine);

        waitingForPlayerChoice = false;
        chosenPlayerSlot = -1;

        //0) 전투 시작 연출 (2줄 세트)
        log.Push(BattleTexts.WildAppeared(enemy.DisplayName));
        log.Push(BattleTexts.GoPlayer(player.DisplayName));

        //프롬프트(고정)
        log.Push(BattleTexts.PromptWhatWillDo(player.DisplayName));

        battleRoutine = StartCoroutine(BattleLoop());
    }

    public void SetPlayerChoice(int slotIndex)
    {
        if (!waitingForPlayerChoice) return;
        if (slotIndex < 0 || slotIndex >= SkillSlots) return;
        if (player == null) return;
        if (player.GetSkill(slotIndex) == null) return;

        chosenPlayerSlot = slotIndex;
        waitingForPlayerChoice = false;
    }

    private IEnumerator BattleLoop()
    {
        while (true)
        {
            //매 턴 시작: 프롬프트 갱신(고정)
            log.Push(BattleTexts.PromptWhatWillDo(player.DisplayName));

            waitingForPlayerChoice = true;
            chosenPlayerSlot = -1;

            yield return new WaitUntil(() => chosenPlayerSlot >= 0);

            //1) 싸운다 → 스킬 선택 후 라운드 실행(여기서부터 기존 전투 로직 유지)
            var playerSkill = player.GetSkill(chosenPlayerSlot);

            //예시: 선공/후공 판정 후 각각 스킬 실행 전에 문구 Push
            //(실제 데미지/효과 문구는 SkillExecutor에서 Push하거나, 여기서 추가로 Push하면 됨)
            log.Push(BattleTexts.UseSkill(player.DisplayName, playerSkill.SkillName));
            executor.Execute(player, enemy, playerSkill, log);

            if (enemy.IsFainted)
            {
                log.Push(BattleTexts.Fainted(enemy.DisplayName));
                log.Push(BattleTexts.GainedExp(player.DisplayName, 12)); // exp는 너의 계산값으로 교체
                onBattleEnded?.Invoke(true);
                yield break;
            }

            //적 턴 예시(기존 AI 선택으로 교체)
            var enemySkill = enemy.GetSkill(0);
            log.Push(BattleTexts.UseSkill(enemy.DisplayName, enemySkill.SkillName));
            executor.Execute(enemy, player, enemySkill, log);

            if (player.IsFainted)
            {
                log.Push(BattleTexts.Fainted(player.DisplayName));
                onBattleEnded?.Invoke(false);
                yield break;
            }

            yield return null;
        }
    }
}
