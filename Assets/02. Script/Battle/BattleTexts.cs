using UnityEngine;

public static class BattleTexts
{
    public static string WildAppeared(string enemyName) =>
        $"앗! 야생의 {enemyName}이/가\n튀어나왔다!";

    public static string GoPlayer(string playerName) =>
        $"가랏! {playerName}!";

    public static string PromptWhatWillDo(string playerName) =>
        $"[PROMPT]{playerName}은/는\n무엇을 할까?";

    public static string UseSkill(string attackerName, string skillName) =>
        $"{attackerName}의 {skillName}!\n";

    public static string Fainted(string name) =>
        $"{name}이/가 쓰러졌다!\n";

    public static string GainedExp(string playerName, int exp) =>
        $"{playerName}은/는\n{exp}의 경험치를 얻었다!";
}
