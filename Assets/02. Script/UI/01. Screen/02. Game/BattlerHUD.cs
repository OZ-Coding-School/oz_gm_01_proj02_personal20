using UnityEngine;
using UnityEngine.UI;
using TMPro;

/*
BattlerHUD는HP/EXP/이름/레벨을Battler데이터에바인딩한다.
-EXP UI가없는적HUD는expSlider/expText를비워두면자동으로무시한다.
-플레이어는expText만비워두면슬라이더만표시된다.
-HP바는체력비율(2/3,1/3)기준으로색이변한다.
*/
public sealed class BattlerHUD : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text hpText;

    [Header("HP")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Image hpFillImage; // 비우면 hpSlider.fillRect에서 자동탐색

    [Header("EXP (Optional)")]
    [SerializeField] private Slider expSlider;  // 적 HUD면 null 가능
    [SerializeField] private TMP_Text expText;  // 플레이어 HUD는 null 가능

    [Header("HP Colors (Optional)")]
    [SerializeField] private bool useHpColor = true;
    [SerializeField] private Color hpHighColor = new Color(0.25f, 0.9f, 0.25f, 1f);   // 초록
    [SerializeField] private Color hpMidColor = new Color(0.95f, 0.85f, 0.2f, 1f);    // 노랑
    [SerializeField] private Color hpLowColor = new Color(0.95f, 0.25f, 0.25f, 1f);   // 빨강

    [Header("Debug")]
    [SerializeField] private bool debugLogs;

    private Battler bound;

    private const float HighThreshold = 2f / 3f;
    private const float MidThreshold = 1f / 3f;

    private void Awake()
    {
        AutoWireFillImage();
    }

    private void OnDisable()
    {
        Unbind();
    }

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
        OnLevelChanged(bound.Level);
        OnHpChanged(bound.Hp, bound.MaxHp);
        OnExpChanged(bound.Exp, bound.ExpToNext);

        LogTag("Bind");
    }

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
        if (hpText != null) hpText.text = "0/0";

        SetSlider01(hpSlider, 0f);
        ApplyHpColor(0f);

        if (expSlider != null) SetSlider01(expSlider, 0f);
        if (expText != null) expText.text = "0/0";
    }

    private void OnHpChanged(int current, int max)
    {
        float ratio = (max > 0) ? (float)current / max : 0f;

        SetSlider01(hpSlider, ratio);

        if (hpText != null)
        {
            hpText.text = current.ToString() + "/" + max.ToString();
        }

        ApplyHpColor(ratio);
    }

    private void OnExpChanged(int current, int toNext)
    {
        if (expSlider != null)
        {
            float ratio = (toNext > 0) ? (float)current / toNext : 0f;
            SetSlider01(expSlider, ratio);
        }

        if (expText != null)
        {
            expText.text = current.ToString() + "/" + toNext.ToString();
        }
    }

    private void OnLevelChanged(int level)
    {
        if (levelText != null)
        {
            levelText.text = "Lv." + level.ToString();
        }
    }

    private void SetSlider01(Slider s, float value01)
    {
        if (s == null) return;

        if (s.minValue != 0f) s.minValue = 0f;
        if (s.maxValue != 1f) s.maxValue = 1f;

        s.value = Mathf.Clamp01(value01);
    }

    private void AutoWireFillImage()
    {
        if (hpFillImage != null) return;
        if (hpSlider == null) return;

        if (hpSlider.fillRect != null)
        {
            hpFillImage = hpSlider.fillRect.GetComponent<Image>();
        }
    }

    private void ApplyHpColor(float ratio01)
    {
        if (!useHpColor) return;

        if (hpFillImage == null)
        {
            AutoWireFillImage();
            if (hpFillImage == null) return;
        }

        float r = Mathf.Clamp01(ratio01);

        if (r > HighThreshold) hpFillImage.color = hpHighColor;
        else if (r > MidThreshold) hpFillImage.color = hpMidColor;
        else hpFillImage.color = hpLowColor;
    }

    private void LogTag(string tag)
    {
        if (!debugLogs) return;
        Debug.Log("[BattlerHUD]" + tag);
    }
}
