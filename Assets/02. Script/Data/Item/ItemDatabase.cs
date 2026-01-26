using System.Collections.Generic;
using UnityEngine;

/*Resources/Items의 ItemData를 자동 등록해서 ID로 조회하는 DB*/
public sealed class ItemDatabase : MonoBehaviour
{
    [SerializeField] private string resourcesPath = "Items";//Resources/Items
    private readonly Dictionary<string, ItemRuntimeData> byId = new Dictionary<string, ItemRuntimeData>(128);

    private void Awake()
    {
        Build();
    }

    //Resources에서 아이템 스프라이트를 읽고 룰테이블로 데이터화한다
    private void Build()
    {
        byId.Clear();

        Sprite[] sprites = Resources.LoadAll<Sprite>(resourcesPath);
        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogWarning($"ItemSpriteDatabase:NoSprites path={resourcesPath}");
            return;
        }

        for (int i = 0; i < sprites.Length; i++)
        {
            Sprite sprite = sprites[i];
            if (sprite == null)
            {
                continue;
            }

            string id = sprite.name;
            if (byId.ContainsKey(id))
            {
                Debug.LogError($"ItemSpriteDatabase:DuplicateId id={id}");
                continue;
            }

            ItemEffectType effectType;
            int value;
            int price;

            bool ok = ItemRuleTable.TryGet(sprite.name, out effectType, out value, out price);
            if (!ok)
            {
                effectType = ItemEffectType.None;
                value = 0;
                price = 0;
                Debug.LogWarning($"ItemSpriteDatabase:MissingRule name={sprite.name}");
            }

            ItemRuntimeData data = new ItemRuntimeData(id, sprite.name, sprite, effectType, value, price);
            byId.Add(id, data);
        }

        Debug.Log($"ItemSpriteDatabase:BuildDone count={byId.Count}");
    }

    //ID로 조회(없으면 null)
    public ItemRuntimeData Get(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        if (byId.TryGetValue(id, out ItemRuntimeData data))
        {
            return data;
        }

        return null;
    }

    //랜덤 1개(상점/보상용)
    public ItemRuntimeData GetRandom()
    {
        if (byId.Count == 0)
        {
            return null;
        }

        int index = Random.Range(0, byId.Count);
        int i = 0;
        foreach (KeyValuePair<string, ItemRuntimeData> kv in byId)
        {
            if (i == index)
            {
                return kv.Value;
            }
            i++;
        }

        return null;
    }
}