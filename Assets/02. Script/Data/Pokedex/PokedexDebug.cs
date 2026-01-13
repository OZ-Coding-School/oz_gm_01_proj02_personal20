using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
PokedexDebug는도감서비스의조회결과를콘솔로확인하기위한디버그컴포넌트다.
-GameManager의PokedexService초기화완료를대기한뒤조회한다.
-특정No의엔트리리스트/기본폼을출력한다.
*/
public class PokedexDebug : MonoBehaviour
{
    [SerializeField] private int testNo = 6;

    private IEnumerator Start()
    {
        while (GameManager.Instance == null)
        {
            yield return null;
        }

        while (GameManager.Instance.Pokedex == null)
        {
            yield return null;
        }

        while (!GameManager.Instance.Pokedex.IsInitialized)
        {
            yield return null;
        }

        IReadOnlyList<PokemonEntry> list;
        if (!GameManager.Instance.Pokedex.TryGetAllByNo(testNo, out list) || list == null)
        {
            Debug.LogWarning($"//PokedexDebug:No={testNo} not found");
            yield break;
        }

        Debug.Log($"//PokedexDebug:No={testNo} entries={list.Count}");

        for (int i = 0; i < list.Count; i++)
        {
            PokemonEntry e = list[i];
            if (e == null)
            {
                continue;
            }

            Debug.Log($"[{i}]No={e.No},Name={e.Name},EvolutionCode={e.EvolutionCode},Special={e.SpecialEvolutionKind},MegaVar={e.MegaVariantIndex}");
        }

        PokemonEntry def;
        if (GameManager.Instance.Pokedex.TryGetDefaultByNo(testNo, out def) && def != null)
        {
            Debug.Log($"//Default No={def.No},Name={def.Name},EvolutionCode={def.EvolutionCode}");
        }
    }
}
