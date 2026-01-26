using UnityEngine;

/*PNG 기반 런타임 아이템 데이터(룰테이블로 효과/값/가격을 세팅한다)*/
public sealed class ItemRuntimeData
{
    private readonly string id;//고유ID(여기선 sprite.name)
    private readonly string displayName;//표시이름(여기선 sprite.name)
    private readonly Sprite icon;//아이콘
    private readonly ItemEffectType effectType;//효과 타입
    private readonly int value;//효과 값
    private readonly int price;//상점 가격

    public ItemRuntimeData(string id, string displayName, Sprite icon, ItemEffectType effectType, int value, int price)
    {
        this.id = id;
        this.displayName = displayName;
        this.icon = icon;
        this.effectType = effectType;
        this.value = value;
        this.price = price;
    }

    public string Id => id;
    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public ItemEffectType EffectType => effectType;
    public int Value => value;
    public int Price => price;
}

public enum ItemEffectType
{
    None = 0,
    HealHpPercent = 1,
    HealHpFlat = 2,
    RestorePPFlat = 3,
    GainGold = 4,
    ExpBoostPercent = 5,
    RerollShop = 6
}
