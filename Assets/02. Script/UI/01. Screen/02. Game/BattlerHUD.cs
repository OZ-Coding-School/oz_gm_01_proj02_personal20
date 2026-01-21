using UnityEngine;
using UnityEngine.UI;
using TMPro;

/*
BattlerHUD는HP/EXP/이름/레벨을Battler데이터에바인딩한다.
-슬라이더는0~1정규화값으로세팅한다.
*/
public sealed class BattlerHUD : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;

    [SerializeField] private Slider hpSlider;
    [SerializeField] private TMP_Text hpText;

    [SerializeField] private Slider expSlider;
    [SerializeField] private TMP_Text expText;

    [Header("Debug")]
    [SerializeField] private bool debugLogs;

    private Battler bound;

    //OnDisable은구독을해제한다
    private void OnDisable()
    {
        Unbind();
    }

    //Bind는Battler를연결한다
    public void Bind(Battler battler)
    {
        if (bound == battler) return;

        Unbind();
        bound = battler;

        if (bound == null)
        {
            RenderEmpty();
            return;
        }

        bound.OnHpChanged += OnHpChanged;
        bound.OnExpChanged += OnExpChanged;
        bound.OnLevelChanged += OnLevelChanged;

        RenderStatic();
        OnHpChanged(bound.Hp, bound.MaxHp);
        OnExpChanged(bound.Exp, bound.ExpToNext);
        OnLevelChanged(bound.Level);

        LogTag("Bind");
    }

    //Unbind는현재바인딩을해제한다
    public void Unbind()
    {
        if (bound == null) return;

        bound.OnHpChanged -= OnHpChanged;
        bound.OnExpChanged -= OnExpChanged;
        bound.OnLevelChanged -= OnLevelChanged;

        bound = null;
        LogTag("Unbind");
    }

    private void RenderStatic()
    {
        if (nameText != null) nameText.text = bound != null ? bound.DisplayName : "-";
    }

    private void RenderEmpty()
    {
        if (nameText != null) nameText.text = "-";
        if (levelText != null) levelText.text = "-";

        SetSlider01(hpSlider, 0f);
        if (hpText != null) hpText.text = "0/0";

        SetSlider01(expSlider, 0f);
        if (expText != null) expText.text = "0/0";
    }

    private void OnHpChanged(int current, int max)
    {
        float v = max > 0 ? (float)current / max : 0f;
        SetSlider01(hpSlider, v);

        if (hpText != null) hpText.text = current.ToString() + "/" + max.ToString();
    }

    private void OnExpChanged(int current, int toNext)
    {
        float v = toNext > 0 ? (float)current / toNext : 0f;
        SetSlider01(expSlider, v);

        if (expText != null) expText.text = current.ToString() + "/" + toNext.ToString();
    }

    private void OnLevelChanged(int level)
    {
        if (levelText != null) levelText.text = "Lv." + level.ToString();
    }

    private void SetSlider01(Slider s, float value01)
    {
        if (s == null) return;

        if (s.minValue != 0f) s.minValue = 0f;
        if (s.maxValue != 1f) s.maxValue = 1f;

        float v = Mathf.Clamp01(value01);
        s.value = v;
    }

    private void LogTag(string tag)
    {
        if (!debugLogs) return;
        Debug.Log("[BattlerHUD]" + tag);
    }
}