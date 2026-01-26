using System;
using UnityEngine;

/*
PokemonEntry는Data영역에서사용되는class다.
-데이터테이블/ScriptableObject를읽어런타임조회가가능하도록초기화한다.
-컴포넌트참조는Awake에서캐싱하고,null을가드한다.
-Update에서GC유발패턴을피한다.
*/
[Serializable]
public class PokemonEntry
{
    [SerializeField] private int no;//도감번호키
    [SerializeField] private string name;//표시이름
    [SerializeField] private string type1;//타입1문자열
    [SerializeField] private string type2;//타입2문자열
    [SerializeField] private string abilities;//특성문자열
    [SerializeField] private int hp;//hp
    [SerializeField] private int atk;//atk
    [SerializeField] private int def;//def
    [SerializeField] private int spAtk;//spAtk
    [SerializeField] private int spDef;//spDef
    [SerializeField] private int speed;//speed
    [SerializeField] private int value;//종족값합
    [SerializeField] private int evolutionCode;//진화레벨/특수변형코드

    public int No => no;//도감번호
    public string Name => name;//이름
    public string Type1 => type1;//타입1
    public string Type2 => type2;//타입2
    public string Abilities => abilities;//특성
    public int HP => hp;//hp
    public int Atk => atk;//atk
    public int Def => def;//def
    public int SpAtk => spAtk;//spAtk
    public int SpDef => spDef;//spDef
    public int Speed => speed;//speed
    public int Value => value;//종족값합
    public int EvolutionCode => evolutionCode;//원본코드
    public string DisplayNo => $"#{No:0000}";//UI표기

    public bool HasLevelEvolution => evolutionCode > 0;//레벨진화대상
    public bool IsFinalEvolution => evolutionCode == 0;//최종진화
    public bool HasSpecialEvolution => evolutionCode < 0;//특수변형

    public SpecialEvolutionKind SpecialEvolutionKind => GetSpecialEvolutionKind(evolutionCode);//특수변형타입
    public int MegaVariantIndex => GetMegaVariantIndex(evolutionCode);//-101->1,-102->2
    public int FormChangeVariantIndex => GetFormChangeVariantIndex(evolutionCode);//-3->1,-4->2,-5->3

    public PokemonEntry(int no, string name, string type1, string type2, string abilities, int hp, int atk, int def, int spAtk, int spDef, int speed, int value, int evolutionCode)
    {
        this.no = Mathf.Max(0, no);//키는음수방지
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

        this.evolutionCode = evolutionCode;//0은최종진화,음수는특수코드라그대로보존
    }

    //코드해석
    private static SpecialEvolutionKind GetSpecialEvolutionKind(int v)
    {
        if (v >= 0)
        {
            return SpecialEvolutionKind.None;
        }

        if (v <= -100)
        {
            return SpecialEvolutionKind.MegaEvolution;
        }

        if (v == -1)
        {
            return SpecialEvolutionKind.MegaEvolution;
        }

        if (v == -2)
        {
            return SpecialEvolutionKind.Gigantamax;
        }

        if (v == -3)
        {
            return SpecialEvolutionKind.FormChange1;
        }

        if (v == -4)
        {
            return SpecialEvolutionKind.FormChange2;
        }

        if (v == -5)
        {
            return SpecialEvolutionKind.FormChange3;
        }

        return SpecialEvolutionKind.Unknown;
    }

    //메가세부폼인덱스
    private static int GetMegaVariantIndex(int v)
    {
        if (v <= -100)
        {
            return (-v) - 100;
        }

        return 0;
    }

    //폼체인지인덱스
    private static int GetFormChangeVariantIndex(int v)
    {
        if (v == -3)
        {
            return 1;
        }

        if (v == -4)
        {
            return 2;
        }

        if (v == -5)
        {
            return 3;
        }

        return 0;
    }
}
