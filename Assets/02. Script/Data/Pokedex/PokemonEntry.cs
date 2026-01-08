using System;
using UnityEngine;

/*
포켓몬 1개 데이터.

규칙
- No는 런타임에서 int(1,2,3...)로 사용한다.
- CSV는 "#0001" 같은 표기 포맷을 유지하므로, UI 표기는 DisplayNo로 통일한다.

EvolutionCode 규칙(한 컬럼으로 진화/특수폼 의미를 관리)
- > 0 : 레벨 진화(예: 16, 32)
- = 0 : 최종진화(레벨 진화 없음)
- < 0 : 특수 변형 코드

특수 변형 코드(확장형)
- -1                : 메가진화(단일)
- -101, -102 ...    : 메가진화 세부 폼(예: X/Y)  (-100 ~ -199 범위 사용)
- -2                : 거다이맥스(단일)
- -301, -302, ...   : 폼체인지 세부 폼(여러 개)  (-300 이하 범위 사용)
*/
[Serializable]
public class PokemonEntry
{
    [SerializeField] private int no;              //전국도감 번호(int)
    [SerializeField] private string name;         //한글 이름

    [SerializeField] private string type1;        //타입1
    [SerializeField] private string type2;        //타입2(단일타입이면 빈칸)
    [SerializeField] private string abilities;    //특성 문자열

    [SerializeField] private int hp;              //체력
    [SerializeField] private int atk;             //공격력
    [SerializeField] private int def;             //방어력
    [SerializeField] private int spAtk;           //특공
    [SerializeField] private int spDef;           //특방
    [SerializeField] private int speed;           //스피드

    [SerializeField] private int value;           //종족값
    [SerializeField] private int evolutionCode;   //진화 레벨/특수코드

    //데이터는 SO/CSV에서 들어오므로 런타임에서 값 변경은 막고, 조회만 하도록 한다.
    public int No => no;
    public string Name => name;

    public string Type1 => type1;
    public string Type2 => type2;

    public string Abilities => abilities;

    public int HP => hp;
    public int Atk => atk;
    public int Def => def;
    public int SpAtk => spAtk;
    public int SpDef => spDef;
    public int Speed => speed;
    public int Value => value;

    public int EvolutionCode => evolutionCode;            //음수 규칙을 사용하므로 0으로 클램프하지 않는다.
    public string DisplayNo => $"#{No:0000}";             //UI 표기용(예: #0001)
    public bool HasLevelEvolution => evolutionCode > 0;   //레벨 진화 대상(예: 16, 32)
    public bool IsFinalEvolution => evolutionCode == 0;   //최종진화(레벨 진화 없음 고정)
    public bool HasSpecialEvolution => evolutionCode < 0; //특수 변형(메가/거다이맥스/폼체인지 등)

    //특수 변형 타입(음수 값 해석 결과)
    public SpecialEvolutionKind SpecialEvolutionKind => GetSpecialEvolutionKind(evolutionCode);

    //-101 -> 1, -102 -> 2 (메가 세부 폼이 아닐 경우 0)
    public int MegaVariantIndex => GetMegaVariantIndex(evolutionCode);

    //-301 -> 1, -302 -> 2 (폼체인지 세부 폼이 아닐 경우 0)
    public int FormChangeVariantIndex => GetFormChangeVariantIndex(evolutionCode);

    //CSV/임포터에서 값을 받아 직렬화 가능한 형태로 저장한다.
    //No만 음수 방지하고, evolutionCode는 음수 규칙이 있으므로 그대로 저장한다.
    public PokemonEntry(
        int no,
        string name,
        string type1,
        string type2,
        string abilities,
        int hp,
        int atk,
        int def,
        int spAtk,
        int spDef,
        int speed,
        int value,
        int evolutionCode
    )
    {
        //No는 도감 조회 키이므로 음수는 막는다(0은 “미정/오류”로 남겨도 됨)
        this.no = Mathf.Max(0, no);

        this.name = name;
        this.type1 = type1;
        this.type2 = type2;
        this.abilities = abilities;

        this.hp = hp;
        this.atk = atk;
        this.def = def;
        this.spAtk = spAtk;
        this.spDef = spDef;
        this.speed = speed;
        this.value = value;

        //0은 최종진화, 음수는 특수 변형 코드이므로 그대로 보존한다
        this.evolutionCode = evolutionCode;
    }

    private SpecialEvolutionKind GetSpecialEvolutionKind(int v)
    {
        //양수/0은 특수 변형이 아니다
        if (v >= 0)
        {
            return SpecialEvolutionKind.None;
        }

        //메가 세부 폼: -101, -102 ... (범위를 제한해서 폼체인지(-301...)와 충돌 방지)
        if (v <= -100 && v > -200)
        {
            return SpecialEvolutionKind.MegaEvolution;
        }

        //폼체인지 세부 폼: -301, -302 ...
        if (v <= -300)
        {
            return SpecialEvolutionKind.FormChange;
        }

        //단일 코드 처리
        if (v == -1)
        {
            return SpecialEvolutionKind.MegaEvolution;
        }

        if (v == -2)
        {
            return SpecialEvolutionKind.Gigantamax;
        }

        //폼체인지 단일(-3)은 “폼체인지 있음”만 표시하는 최소 표현으로 남겨둔다
        if (v == -3)
        {
            return SpecialEvolutionKind.FormChange;
        }

        //규칙에 없는 값은 Unknown으로 둬서 디버깅 포인트로 삼는다
        return SpecialEvolutionKind.Unknown;
    }

    private int GetMegaVariantIndex(int v)
    {
        //-101 -> 1, -102 -> 2 ...
        if (v <= -100 && v > -200)
        {
            return (-v) - 100;
        }

        return 0;
    }

    private int GetFormChangeVariantIndex(int v)
    {
        //-301 -> 1, -302 -> 2, -303 -> 3 ...
        if (v <= -300)
        {
            return (-v) - 300;
        }

        return 0;
    }
}

//특수 변형 타입(EvolutionCode 음수 해석 결과)
public enum SpecialEvolutionKind
{
    None = 0,
    MegaEvolution = 1,
    Gigantamax = 2,
    FormChange = 3,
    Unknown = 99,
}
