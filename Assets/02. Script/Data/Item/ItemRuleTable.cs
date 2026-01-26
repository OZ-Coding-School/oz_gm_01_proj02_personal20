using System;

/*스프라이트 이름→아이템 효과/값/가격을 정의하는 룰 테이블*/
public static class ItemRuleTable
{
    //정의 성공 시 true
    public static bool TryGet(string spriteName, out ItemEffectType effectType, out int value, out int price)
    {
        effectType = ItemEffectType.None;
        value = 0;
        price = 0;

        if (string.IsNullOrEmpty(spriteName))
        {
            return false;
        }

        //정확히 일치 매핑(가장 안전)
        switch (spriteName)
        {
            //회복류 예시
            case "Good Potion":
                effectType = ItemEffectType.HealHpPercent;
                value = 25;//HP 25% 회복(퍼센트)
                price = 250;
                return true;

            //골드 관련 예시
            case "Gold Orb":
                effectType = ItemEffectType.GainGold;
                value = 100;//골드 +100
                price = 300;
                return true;

            //경험치 버프 예시
            case "Golden Egg":
                effectType = ItemEffectType.ExpBoostPercent;
                value = 20;//획득 경험치 +20%
                price = 400;
                return true;

            case "Good Experience Charm":
                effectType = ItemEffectType.ExpBoostPercent;
                value = 10;//획득 경험치 +10%
                price = 200;
                return true;

            //리롤(예시)
            case "Reroll Ticket":
                effectType = ItemEffectType.RerollShop;
                value = 1;
                price = 150;
                return true;
        }

        //규칙 기반(옵션): 이름에 단어가 들어가면 기본 룰 적용
        //대량 PNG를 일단 굴리고 싶을 때 유용함.
        if (Contains(spriteName, "Potion"))
        {
            effectType = ItemEffectType.HealHpPercent;
            value = 20;
            price = 200;
            return true;
        }

        if (Contains(spriteName, "Egg"))
        {
            effectType = ItemEffectType.ExpBoostPercent;
            value = 15;
            price = 300;
            return true;
        }

        return false;
    }

    private static bool Contains(string s, string token)
    {
        return s.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
