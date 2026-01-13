using System.Collections.Generic;
using UnityEngine;

/*
PokemonDatabaseSO는에디터에서생성되는ScriptableObject데이터다.
-데이터테이블/ScriptableObject를읽어런타임조회가가능하도록초기화한다.
-외부에서는SetEntries을호출해이기능을사용한다.
-컴포넌트참조는Awake에서캐싱하고,null을가드한다.
*/
[CreateAssetMenu(fileName = "PokemonDatabase", menuName = "Data/Pokedex/Pokemon Database")]
public class PokemonDatabaseSO : ScriptableObject
{
    [SerializeField] private List<PokemonEntry> entries = new List<PokemonEntry>(); //전체 포켓몬 목록

    public IReadOnlyList<PokemonEntry> Entries => entries;

    public void SetEntries(List<PokemonEntry> newEntries)
    {
        entries = newEntries;
    }
}
