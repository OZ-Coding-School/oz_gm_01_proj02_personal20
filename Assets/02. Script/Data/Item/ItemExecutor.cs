using UnityEngine;

/*아이템 효과 실행기*/
public sealed class ItemExecutor : MonoBehaviour
{
    //아이템을 실제로 적용한다
    public bool TryUse(ItemRuntimeData item, RunContext ctx)
    {
        if (item == null || ctx == null)
        {
            return false;
        }

        switch (item.EffectType)
        {
            case ItemEffectType.HealHpPercent:
                return UseHealHpPercent(item, ctx);
            case ItemEffectType.HealHpFlat:
                return UseHealHpFlat(item, ctx);
            case ItemEffectType.RestorePPFlat:
                return UseRestorePP(item, ctx);
            case ItemEffectType.GainGold:
                return UseGainGold(item, ctx);
            case ItemEffectType.ExpBoostPercent:
                return UseExpBoost(item, ctx);
            case ItemEffectType.RerollShop:
                return UseReroll(item, ctx);
        }

        return false;
    }

    private bool UseHealHpPercent(ItemRuntimeData item, RunContext ctx)
    {
        Debug.Log($"ItemUse:HealHpPercent name={item.DisplayName} value={item.Value}");
        //TODO:현재 선택 포켓몬 HP를 MaxHP 기준 value%만큼 회복
        return true;
    }

    private bool UseHealHpFlat(ItemRuntimeData item, RunContext ctx)
    {
        Debug.Log($"ItemUse:HealHpFlat name={item.DisplayName} value={item.Value}");
        //TODO:현재 선택 포켓몬 HP를 value만큼 회복
        return true;
    }

    private bool UseRestorePP(ItemRuntimeData item, RunContext ctx)
    {
        Debug.Log($"ItemUse:RestorePPFlat name={item.DisplayName} value={item.Value}");
        //TODO:선택 스킬 PP 회복
        return true;
    }

    private bool UseGainGold(ItemRuntimeData item, RunContext ctx)
    {
        Debug.Log($"ItemUse:GainGold name={item.DisplayName} value={item.Value}");
        //예:RunManager.Instance.AddGold(item.Value);
        return true;
    }

    private bool UseExpBoost(ItemRuntimeData item, RunContext ctx)
    {
        Debug.Log($"ItemUse:ExpBoostPercent name={item.DisplayName} value={item.Value}");
        //TODO:런 버프(경험치 배율) 같은 곳에 등록
        return true;
    }

    private bool UseReroll(ItemRuntimeData item, RunContext ctx)
    {
        Debug.Log($"ItemUse:RerollShop name={item.DisplayName}");
        //TODO:OfferGenerator 재생성 호출
        return true;
    }
}

/*런/전투/상점에서 공유할 컨텍스트(프로젝트에 맞춰 채우기)*/
public sealed class RunContext
{
}
