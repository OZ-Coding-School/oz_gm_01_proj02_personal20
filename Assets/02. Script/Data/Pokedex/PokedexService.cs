using System;
using System.Collections.Generic;
using UnityEngine;

/*
PokedexService는씬/오브젝트에붙는MonoBehaviour컴포넌트다.
-매니저와데이터사이에서기능단위를제공하는서비스레이어다.
-외부에서는Initialize을호출해이기능을사용한다.
-외부에서는RebuildCache을호출해이기능을사용한다.
-외부에서는GetAll을호출해이기능을사용한다.
-외부에서는TryGetAllByNo을호출해이기능을사용한다.
*/
public class PokedexService : MonoBehaviour
{
    [SerializeField] private PokemonDatabaseSO database;//도감 DB(SO) 런타임 참조

    //No->같은 도감번호를 가진 모든 엔트리(기본폼+특수폼)를 묶어서 저장
    private readonly Dictionary<int, List<PokemonEntry>> byNo = new Dictionary<int, List<PokemonEntry>>();

    //Name->빠른 접근용(중복 이름이 있으면 최초 1개만 저장)
    private readonly Dictionary<string, PokemonEntry> byName = new Dictionary<string, PokemonEntry>(StringComparer.OrdinalIgnoreCase);

    public bool IsInitialized { get; private set; }//초기화 완료 여부
    public PokemonDatabaseSO Database => database;//원본 DB 접근(디버그/툴링용)

    public void Initialize(PokemonDatabaseSO db)
    {
        //런타임에서 DB 참조가 없으면 이후 모든 조회가 무의미하므로 즉시 실패 처리
        if (db == null)
        {
            Debug.LogError("PokedexService.Initialize:db가 null이야.");
            IsInitialized = false;
            return;
        }

        database = db;
        RebuildCache();
    }

    public void RebuildCache()
    {
        //임포트/재로드/테스트 등에서 여러 번 호출될 수 있으니 항상 초기화 후 재구축
        byNo.Clear();
        byName.Clear();

        if (database == null)
        {
            Debug.LogError("PokedexService.RebuildCache:database가 null이야.");
            IsInitialized = false;
            return;
        }

        IReadOnlyList<PokemonEntry> entries = database.Entries;
        if (entries == null || entries.Count == 0)
        {
            //비어있는 DB도 서비스는 동작할 수 있어야 하므로 Initialized는 true로 둔다
            Debug.LogWarning("PokemonDatabaseSO에 entries가 없거나 비어있어.");
            IsInitialized = true;
            return;
        }

        //캐시 구축:No->List,Name->Entry
        for (int i = 0; i < entries.Count; i++)
        {
            PokemonEntry e = entries[i];
            if (e == null)
            {
                continue;
            }

            //No->List 캐시(같은 No는 같은 리스트에 쌓인다)
            if (!byNo.TryGetValue(e.No, out List<PokemonEntry> list))
            {
                list = new List<PokemonEntry>();
                byNo.Add(e.No, list);
            }

            list.Add(e);

            //Name->Entry 캐시(동명이인/중복이 있으면 최초 항목을 유지)
            if (!string.IsNullOrWhiteSpace(e.Name) && !byName.ContainsKey(e.Name))
            {
                byName.Add(e.Name, e);
            }
        }

        IsInitialized = true;
    }

    public IReadOnlyList<PokemonEntry> GetAll()
    {
        //디버그/리스트 UI 등에서 전체 엔트리가 필요할 때 사용
        if (database == null || database.Entries == null)
        {
            return Array.Empty<PokemonEntry>();
        }

        return database.Entries;
    }

    public bool TryGetAllByNo(int no, out IReadOnlyList<PokemonEntry> entries)
    {
        //같은 도감번호의 기본폼/특수폼을 전부 받고 싶을 때 사용
        if (byNo.TryGetValue(no, out List<PokemonEntry> list) && list != null && list.Count > 0)
        {
            entries = list;
            return true;
        }

        entries = Array.Empty<PokemonEntry>();
        return false;
    }

    public bool TryGetDefaultByNo(int no, out PokemonEntry entry)
    {
        //대표 1개(기본폼 우선)만 뽑고 싶을 때 사용
        if (!TryGetAllByNo(no, out IReadOnlyList<PokemonEntry> entries))
        {
            entry = null;
            return false;
        }

        entry = SelectDefaultEntry(entries);
        return entry != null;
    }

    public bool TryGetByNo(int no, out PokemonEntry entry)
    {
        //기존 코드 호환용(내부적으로 Default 규칙을 사용)
        return TryGetDefaultByNo(no, out entry);
    }

    public bool TryGetByName(string name, out PokemonEntry entry)
    {
        //UI 검색/디버그에서 이름으로 바로 찾고 싶을 때 사용
        if (string.IsNullOrWhiteSpace(name))
        {
            entry = null;
            return false;
        }

        return byName.TryGetValue(name.Trim(), out entry);
    }

    public PokemonEntry GetByNoOrThrow(int no)
    {
        //테스트/필수 데이터 구간에서 실패를 강하게 드러내고 싶을 때 사용
        if (!TryGetDefaultByNo(no, out PokemonEntry entry))
        {
            throw new KeyNotFoundException($"도감 번호를 찾지 못했어:{no}");
        }

        return entry;
    }

    public IReadOnlyList<PokemonEntry> GetAllByNoOrThrow(int no)
    {
        if (!TryGetAllByNo(no, out IReadOnlyList<PokemonEntry> entries))
        {
            throw new KeyNotFoundException($"도감 번호를 찾지 못했어:{no}");
        }

        return entries;
    }

    public PokemonEntry GetByNameOrThrow(string name)
    {
        if (!TryGetByName(name, out PokemonEntry entry))
        {
            throw new KeyNotFoundException($"도감 이름을 찾지 못했어:{name}");
        }

        return entry;
    }

    private PokemonEntry SelectDefaultEntry(IReadOnlyList<PokemonEntry> entries)
    {
        //같은 No에 여러 폼이 있을 수 있으므로 대표 1개를 고르는 규칙을 고정한다.
        //1순위:EvolutionCode>=0(기본폼/일반폼)
        //2순위:EvolutionCode<0(특수폼)
        //동률이면 이름 키워드/이름 길이로 타이브레이크한다.

        if (entries == null || entries.Count == 0)
        {
            return null;
        }

        PokemonEntry best = null;

        for (int i = 0; i < entries.Count; i++)
        {
            PokemonEntry e = entries[i];
            if (e == null)
            {
                continue;
            }

            //첫 유효 엔트리를 초기값으로 둔다
            if (best == null)
            {
                best = e;
                continue;
            }

            //특수폼 여부를 EvolutionCode로 1차 판정한다
            //메가X/Y(-101/-102),폼체인지(-301...),거다이맥스(-2) 등은 모두 음수로 들어오므로 기본폼보다 뒤로 밀린다
            bool bestIsSpecial = best.EvolutionCode < 0;
            bool curIsSpecial = e.EvolutionCode < 0;

            //best가 특수폼이고 현재가 기본폼이면 즉시 교체
            if (bestIsSpecial && !curIsSpecial)
            {
                best = e;
                continue;
            }

            //둘 다 같은 범주면(둘 다 기본폼이거나 둘 다 특수폼) 추가 규칙으로 안정적인 대표를 고른다
            if (bestIsSpecial == curIsSpecial)
            {
                //이름 키워드 기반 보조 판정(데이터가 불완전한 시기에 대응)
                //예:EvolutionCode가 잘못 들어가도 "메가/폼/리전" 같은 키워드가 있으면 특수로 보는 보정 역할
                bool bestNameSpecial = IsSpecialFormName(best.Name);
                bool curNameSpecial = IsSpecialFormName(e.Name);

                if (bestNameSpecial && !curNameSpecial)
                {
                    best = e;
                    continue;
                }

                if (bestNameSpecial == curNameSpecial)
                {
                    //마지막 타이브레이커:짧은 이름 우선
                    //예:리자몽 vs 메가리자몽X/Y 처럼 기본폼이 보통 더 짧게 유지되는 편
                    int bestLen = string.IsNullOrWhiteSpace(best.Name) ? int.MaxValue : best.Name.Length;
                    int curLen = string.IsNullOrWhiteSpace(e.Name) ? int.MaxValue : e.Name.Length;

                    if (curLen < bestLen)
                    {
                        best = e;
                    }
                }
            }
        }

        //모든 항목이 null인 비정상 상황을 대비해 폴백
        return best ?? entries[0];
    }

    private bool IsSpecialFormName(string name)
    {
        //EvolutionCode가 아직 완벽히 정리되지 않은 상태에서도 폼 여부를 어느 정도 감지하기 위한 보조 규칙
        //최종적으로는 EvolutionCode가 정답이 되도록 데이터를 맞추는 것이 목표다

        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        string n = name.Trim();

        //메가
        if (n.Contains("메가", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        //리전폼(프로젝트에서 쓰는 표기 기준으로 키워드만 확장 가능)
        if (n.Contains("가라르", StringComparison.OrdinalIgnoreCase) ||
           n.Contains("히스이", StringComparison.OrdinalIgnoreCase) ||
           n.Contains("알로라", StringComparison.OrdinalIgnoreCase) ||
           n.Contains("팔데아", StringComparison.OrdinalIgnoreCase) ||
           n.Contains("리전", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        //기타 폼/특수 표기
        if (n.Contains("오리진", StringComparison.OrdinalIgnoreCase) ||
           n.Contains("폼", StringComparison.OrdinalIgnoreCase) ||
           n.Contains("(", StringComparison.OrdinalIgnoreCase) ||
           n.Contains(")", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}