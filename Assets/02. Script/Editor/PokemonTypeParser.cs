using System;
using System.Collections.Generic;
using UnityEngine;

/*
PokemonTypeParser는Core영역에서사용되는class다.
-이스크립트는프로젝트에서PokemonTypeParser역할을한다.
-컴포넌트참조는Awake에서캐싱하고,null을가드한다.
-외부호출지점을명확히하고중복처리를막는다.
*/
public static class PokemonTypeParser
{
    //한글타입->enum
    private static readonly Dictionary<string, PokemonType> typeMap = new Dictionary<string, PokemonType>()
    {
        {"노말", PokemonType.Normal},
        {"불꽃", PokemonType.Fire},
        {"물", PokemonType.Water},
        {"전기", PokemonType.Electric},
        {"풀", PokemonType.Grass},
        {"얼음", PokemonType.Ice},
        {"격투", PokemonType.Fighting},
        {"독", PokemonType.Poison},
        {"땅", PokemonType.Ground},
        {"비행", PokemonType.Flying},
        {"에스퍼", PokemonType.Psychic},
        {"벌레", PokemonType.Bug},
        {"바위", PokemonType.Rock},
        {"고스트", PokemonType.Ghost},
        {"드래곤", PokemonType.Dragon},
        {"악", PokemonType.Dark},
        {"강철", PokemonType.Steel},
        {"페어리", PokemonType.Fairy},
    };

    //빈값/오타는None
    public static PokemonType ParseOrNone(string rawType)
    {
        if (string.IsNullOrWhiteSpace(rawType))
        {
            return PokemonType.None;
        }

        string trimmed = rawType.Trim();

        if (typeMap.TryGetValue(trimmed, out PokemonType result))
        {
            return result;
        }

        Debug.LogWarning($"//Unknown PokemonType string:{trimmed}");
        return PokemonType.None;
    }
}
