using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
LobbyPokedexTest는도감DB연결상태를빠르게확인하기위한스모크테스트다.
-GameManager의PokedexService가초기화되었는지확인한다.
-초기화되었다면특정No의엔트리일부를콘솔에출력한다.
*/
public class LobbyPokedexTest : MonoBehaviour
{
    [SerializeField] private int testNo = 6;
    [SerializeField] private int printCount = 5;

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
            Debug.LogWarning($"//LobbyPokedexTest:No={testNo} not found");
            yield break;
        }

        int n = Mathf.Min(printCount, list.Count);
        Debug.Log($"//LobbyPokedexTest:No={testNo} entries={list.Count} print={n}");

        for (int i = 0; i < n; i++)
        {
            PokemonEntry e = list[i];
            if (e == null)
            {
                continue;
            }

            Debug.Log($"[{i}]No={e.No},Name={e.Name},EvolutionCode={e.EvolutionCode},Special={e.SpecialEvolutionKind},MegaVar={e.MegaVariantIndex}");
        }
    }
}
