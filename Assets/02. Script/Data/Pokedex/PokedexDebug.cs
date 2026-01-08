using System.Collections.Generic;
using UnityEngine;

/*
특정 도감번호의 폼(중복 No) 목록을 한 번에 확인하는 디버그 스크립트.
-FormChangeVariantIndex가 아직 없을 수 있어서 호출하지 않는다(컴파일 에러 방지).
*/
public class PokedexDebug : MonoBehaviour
{
    [SerializeField] private int testNo = 6;//확인할 도감번호

    private void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance가 없어.");
            return;
        }

        IReadOnlyList<PokemonEntry> list;
        if (!GameManager.Instance.Pokedex.TryGetAllByNo(testNo, out list))
        {
            Debug.LogWarning($"No={testNo}를 찾지 못했어.");
            return;
        }

        Debug.Log($"No={testNo}entries={list.Count}");
        for (int i = 0; i < list.Count; i++)
        {
            var e = list[i];
            Debug.Log($"[{i}]No={e.No},Name={e.Name},EvolutionCode={e.EvolutionCode},Special={e.SpecialEvolutionKind},MegaVar={e.MegaVariantIndex}");
        }

        PokemonEntry def;
        if (GameManager.Instance.Pokedex.TryGetDefaultByNo(testNo, out def))
        {
            Debug.Log($"Default->No={def.No},Name={def.Name},EvolutionCode={def.EvolutionCode}");
        }
    }
}