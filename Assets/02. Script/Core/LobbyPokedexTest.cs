using UnityEngine;

/*
LobbyScene에서 도감 DB 연결 상태를 빠르게 확인하기 위한 스모크 테스트.
-GameManager→PokedexService가 초기화 되었는지 확인한다.
-초기화 됐다면 엔트리 일부를 콘솔에 출력한다.
*/
public class LobbyPokedexTest : MonoBehaviour
{
    [SerializeField] private int printCount = 5;//콘솔에 출력할 개수

    private void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance가 null이야.BootScene에서 GameManager가 생성되는지 확인해줘.");
            return;
        }

        if (GameManager.Instance.Pokedex == null)
        {
            Debug.LogError("PokedexService가 null이야.GameManager가 PokedexService를 생성/참조하는지 확인해줘.");
            return;
        }

        if (!GameManager.Instance.Pokedex.IsInitialized)
        {
            Debug.LogError("PokedexService가 초기화되지 않았어.GameManager에 PokemonDatabaseSO가 할당됐는지 확인해줘.");
            return;
        }

        var list = GameManager.Instance.Pokedex.GetAll();
        Debug.Log($"Pokedex OK.Entries:{list.Count}");

        int count = Mathf.Clamp(printCount, 0, list.Count);
        for (int i = 0; i < count; i++)
        {
            var e = list[i];
            if (e == null)
            {
                continue;
            }

            Debug.Log($"[{i}]No={e.No},Name={e.Name},EvolutionCode={e.EvolutionCode},Special={e.SpecialEvolutionKind},MegaVar={e.MegaVariantIndex}");
        }
    }
}