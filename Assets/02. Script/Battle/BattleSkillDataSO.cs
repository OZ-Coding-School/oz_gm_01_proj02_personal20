using UnityEngine;

/*
BattleSkillDataSO는에디터에서생성되는ScriptableObject데이터다.
-전투기술의이름/위력/명중/카테고리/부가효과를정의한다.
*/
[CreateAssetMenu(fileName = "BattleSkill", menuName = "Data/Battle/Battle Skill")]
public class BattleSkillDataSO : ScriptableObject
{
    [SerializeField] private string skillName;
    [SerializeField] private BattleTypes.SkillCategory category = BattleTypes.SkillCategory.Physical;

    [SerializeField] private int power = 40;
    [SerializeField] private int accuracy = 100;

    [SerializeField] private int pp = 20;

    [Header("Status Effect")]
    [SerializeField] private BattleTypes.StatusAilment applyStatus = BattleTypes.StatusAilment.None;
    [SerializeField] private int statusChancePercent = 0;

    [Header("Stage Change")]
    [SerializeField] private BattleTypes.BattleStat stageTargetStat = BattleTypes.BattleStat.Attack;
    [SerializeField] private int stageDelta = 0;
    [SerializeField] private int stageChancePercent = 0;

    public string SkillName => string.IsNullOrEmpty(skillName) ? name : skillName;
    public BattleTypes.SkillCategory Category => category;
    public int Power => Mathf.Max(0, power);
    public int Accuracy => Mathf.Clamp(accuracy, 1, 100);
    public int Pp => Mathf.Max(0, pp);

    public BattleTypes.StatusAilment ApplyStatus => applyStatus;
    public int StatusChancePercent => Mathf.Clamp(statusChancePercent, 0, 100);

    public BattleTypes.BattleStat StageTargetStat => stageTargetStat;
    public int StageDelta => Mathf.Clamp(stageDelta, -6, 6);
    public int StageChancePercent => Mathf.Clamp(stageChancePercent, 0, 100);
}
