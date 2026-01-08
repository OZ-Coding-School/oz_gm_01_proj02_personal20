using UnityEditor;
using UnityEngine;

/*
프로젝트 내 PokemonDatabaseSO 에셋을 전부 찾아 경로/샘플 값을 콘솔에 출력한다.
-임포터가 갱신한 에셋과 런타임에서 참조하는 에셋이 같은지 확인할 때 사용한다.
-문자열 "t:PokemonDatabaseSO"를 그대로 쓰면 클래스명 변경/오타에 취약하므로 typeof 기반으로 검색한다.
-반드시 Editor 폴더 아래에 있어야 AssetDatabase를 정상적으로 사용할 수 있다.
*/
public static class PokemonDatabaseLocator
{
    [MenuItem("Tools/Pokedex/Debug/List All PokemonDatabaseSO")]
    public static void ListAllPokemonDatabaseAssets()
    {
        //FindAssets는 "t:타입명"으로 필터링한다.
        //typeof로 타입명을 가져오면 클래스명 변경 시에도 코드가 같이 따라간다.
        string typeName = typeof(PokemonDatabaseSO).Name;
        string[] guids = AssetDatabase.FindAssets($"t:{typeName}");

        Debug.Log($"PokemonDatabaseSO assets found:{guids.Length}");

        //에셋이 0이면 타입명이 다르거나(네임스페이스/클래스 변경),파일이 아직 임포트되지 않았거나,
        //스크립트가 Editor 밖에서 실행되는 상태일 수 있다.
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var db = AssetDatabase.LoadAssetAtPath<PokemonDatabaseSO>(path);

            //db가 null이면 에셋 타입이 기대와 다르거나 로드 실패 상황이다.
            int count = db != null && db.Entries != null ? db.Entries.Count : 0;

            //샘플은 실제 데이터가 들어갔는지(특히 No=0 문제 같은 것) 빠르게 확인하기 위함이다.
            string sample = "(empty)";
            if (db != null && db.Entries != null && db.Entries.Count > 0 && db.Entries[0] != null)
            {
                sample = $"Sample:No={db.Entries[0].No},Name={db.Entries[0].Name},EvolutionCode={db.Entries[0].EvolutionCode}";
            }

            Debug.Log($"[{i}]{path}/Entries={count}/{sample}");
        }
    }
}
