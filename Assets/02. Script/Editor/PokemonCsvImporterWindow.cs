using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/*
CSV(TextAsset) -> PokemonDatabase.asset(ScriptableObject) 생성/갱신.

핵심 규칙
- CSV의 No 표기는 "#0001" 같은 "표시용" 포맷을 유지한다.
- 런타임/데이터에는 No를 int(1,2,3...)로 저장한다. (#, 0-padding은 파싱 시 제거)

EvolutionCode 규칙
- > 0 : 레벨 진화(예: 16, 32)
- = 0 : 최종진화(레벨 진화 없음)
- < 0 : 특수 변형 코드(메가/거다이맥스/폼체인지 등)
*/
public class PokemonCsvImporterWindow : EditorWindow
{
    [SerializeField] private TextAsset csvFile; //프로젝트의 CSV(TextAsset)
    [SerializeField] private string outputAssetPath = "Assets/03.Database/Pokedex/PokemonDatabase.asset"; //생성/갱신될 에셋 경로

    private static readonly Regex FirstIntRegex = new Regex(@"-?\d+", RegexOptions.Compiled); //문자열에서 첫 숫자 블록 추출용

    //에디터 상단 메뉴에서 이 창을 열 수 있게 메뉴 항목 추가
    [MenuItem("Tools/Pokedex/CSV Importer")]
    public static void Open()
    {
        //창을 열거나(없으면 생성) 포커스
        GetWindow<PokemonCsvImporterWindow>("Pokemon CSV Importer");
    }

    private void OnGUI()
    {
        //창 제목/구분
        GUILayout.Label("CSV -> PokemonDatabase.asset", EditorStyles.boldLabel);
        GUILayout.Space(6);

        //CSV(TextAsset) 선택 필드(드래그 앤 드롭)
        csvFile = (TextAsset)EditorGUILayout.ObjectField("CSV File", csvFile, typeof(TextAsset), false);

        //출력 에셋 경로(프로젝트 구조에 맞게 수정 가능)
        outputAssetPath = EditorGUILayout.TextField("Output Asset Path", outputAssetPath);

        GUILayout.Space(8);

        //임포트 실행 버튼(사용자 클릭으로만 데이터 갱신)
        if (GUILayout.Button("Import CSV -> PokemonDatabase.asset", GUILayout.Height(28)))
        {
            //CSV가 비어있으면 중단
            if (csvFile == null)
            {
                Debug.LogError("CSV File이 비어있어 임포트할 수 없어.");
                return;
            }

            //파싱 -> 에셋 생성/갱신 -> 저장
            Import(csvFile.text, outputAssetPath);
        }
    }

    private void Import(string csvText, string assetPath)
    {
        //1) CSV 파싱
        List<PokemonEntry> parsed = ParseCsv(csvText);

        //2) 출력 폴더가 없으면 생성(Assets 경로 기준)
        EnsureFolderExists(Path.GetDirectoryName(assetPath));

        //3) 기존 에셋이 있으면 로드, 없으면 생성
        PokemonDatabaseSO db = AssetDatabase.LoadAssetAtPath<PokemonDatabaseSO>(assetPath);
        if (db == null)
        {
            db = CreateInstance<PokemonDatabaseSO>();
            AssetDatabase.CreateAsset(db, assetPath);
        }

        //4) 데이터 반영
        db.SetEntries(parsed);
        EditorUtility.SetDirty(db);

        //5) 저장/갱신
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        //6) 결과 확인 로그
        Debug.Log($"임포트 완료: {assetPath} / Entries={parsed.Count}");
        if (parsed.Count > 0)
        {
            Debug.Log($"샘플: No(int)={parsed[0].No}, DisplayNo={parsed[0].DisplayNo}, Name={parsed[0].Name}, EvolutionCode={parsed[0].EvolutionCode}");
        }
    }

    private List<PokemonEntry> ParseCsv(string text)
    {
        //줄바꿈 통일(윈도우/맥 혼용 방지)
        string normalized = NormalizeNewLines(text);
        string[] lines = normalized.Split('\n');

        List<PokemonEntry> result = new List<PokemonEntry>();

        if (lines.Length == 0)
        {
            return result;
        }

        //헤더가 있으면 1줄 건너뛰기
        int startIndex = 0;
        if (IsHeaderLine(lines[0]))
        {
            startIndex = 1;
        }

        for (int i = startIndex; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            //현재 CSV는 따옴표/콤마-내장 필드가 없어서 Split(',')로 충분
            //필요해지면 CSV 파서로 교체
            string[] cols = SplitCsvOrTsvLine(line);

            //최소 컬럼(번호/이름/타입1/타입2)은 필요
            if (cols.Length < 4)
            {
                continue;
            }

            //"#0001" -> 1로 변환하여 저장
            int no = ToDexNo(Get(cols, 0));
            string name = Get(cols, 1);

            string type1 = Get(cols, 2);
            string type2 = Get(cols, 3);

            //나머지는 없으면 기본값(빈 문자열/0)
            string abilities = Get(cols, 4);

            int hp = ToIntSafe(Get(cols, 5));
            int atk = ToIntSafe(Get(cols, 6));
            int def = ToIntSafe(Get(cols, 7));
            int spAtk = ToIntSafe(Get(cols, 8));
            int spDef = ToIntSafe(Get(cols, 9));
            int speed = ToIntSafe(Get(cols, 10));
            int value = ToIntSafe(Get(cols, 11));

            //EvolutionCode는 음수 규칙을 쓰므로 절대 0으로 눌러버리지 않는다
            int evolutionCode = ToIntSafe(Get(cols, 12));

            //단일 타입이면 빈칸 유지(입력 그대로)
            if (type2 == null)
            {
                type2 = "";
            }

            //No/Name 둘 다 비면 깨진 행으로 보고 스킵
            if (no == 0 && string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            result.Add(new PokemonEntry(no, name, type1, type2, abilities, hp, atk, def, spAtk, spDef, speed, value, evolutionCode));
        }

        return result;
    }

    private int ToDexNo(string s)
    {
        //"#0001" 같은 표시 포맷을 포함해 어떤 입력이 와도 '숫자만' 모아서 도감번호 int로 만든다.
        //전각 숫자(０１２３...)도 char.GetNumericValue로 처리 가능
        string t = CleanToken(s);

        int value = 0;
        bool hasDigit = false;

        for (int i = 0; i < t.Length; i++)
        {
            double n = char.GetNumericValue(t[i]);
            if (n < 0 || n > 9)
            {
                continue;
            }

            value = (value * 10) + (int)n;
            hasDigit = true;
        }

        return hasDigit ? value : 0;
    }

    private string NormalizeNewLines(string text)
    {
        if (text == null)
        {
            return "";
        }

        return text.Replace("\r\n", "\n").Replace("\r", "\n");
    }

    private bool IsHeaderLine(string firstLine)
    {
        //BOM/제로폭 문자 제거 후 비교(헤더 감지 안정화)
        string cleaned = CleanToken(firstLine);
        string lower = cleaned.ToLowerInvariant();

        //CSV 헤더: No,Name,Type1...
        return lower.Contains("no") && lower.Contains("name");
    }

    private string[] SplitCsvOrTsvLine(string line)
    {
        //TSV도 최소 대응
        if (line.Contains("\t"))
        {
            return line.Split('\t');
        }

        return line.Split(',');
    }

    private string Get(string[] cols, int index)
    {
        if (cols == null)
        {
            return "";
        }

        if (index < 0 || index >= cols.Length)
        {
            return "";
        }

        //BOM/제로폭 문자/양끝 공백 정리
        return CleanToken(cols[index]);
    }

    private string CleanToken(string s)
    {
        if (s == null)
        {
            return "";
        }

        string t = s.Trim();

        //UTF-8 BOM() / 제로폭 문자 제거
        t = t.Trim('\uFEFF', '\u200B', '\u200C', '\u200D');

        return t.Trim();
    }

    private int ToIntSafe(string s)
    {
        //"", " 32 ", "-101" 같은 케이스를 안전하게 int로 파싱
        string t = CleanToken(s);
        if (string.IsNullOrWhiteSpace(t))
        {
            return 0;
        }

        //CSV 표시용 "#" 제거(No 컬럼 외에 붙어도 안전하게 제거)
        if (t.StartsWith("#", StringComparison.Ordinal))
        {
            t = t.Substring(1);
        }

        if (int.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
        {
            return v;
        }

        Match m = FirstIntRegex.Match(t);
        if (m.Success && int.TryParse(m.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out v))
        {
            return v;
        }

        return 0;
    }

    private void EnsureFolderExists(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return;
        }

        //Assets 경로가 아니면 폴더 생성은 하지 않음
        if (!folderPath.StartsWith("Assets", StringComparison.Ordinal))
        {
            return;
        }

        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        //"Assets/A/B/C" 형태를 단계적으로 생성
        string[] parts = folderPath.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }
}
