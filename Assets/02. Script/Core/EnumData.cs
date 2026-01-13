using UnityEngine;

/*
UISceneCanvasKind는Core영역에서사용되는enum다.
-씬전환을비동기로처리하고,전환중중복호출을가드한다.
-UI상태전환/페이드/버튼입력등화면흐름을담당한다.
-컴포넌트참조는Awake에서캐싱하고,null을가드한다.
-씬이름에숫자/점이포함되면enum.ToString()로직접로드할수없으므로SceneName매핑을사용한다.
*/
public static class EnumData
{
    public enum PopupId
    {
        None = 0,
        Shop = 1,
        LevelUp = 2,
        Settings = 100,
    }
    public enum GameState
    {
        None = 0,
        Boot = 1,
        Lobby = 2,
        Battle = 3,
        Result = 4,
        Settings = 10,
    }

    //Screen UI 식별 전용 ID
    public enum ScreenId
    {
        None = 0,      //없음
        Boot = 1,      //부트
        Lobby = 2,     //로비
        Game = 3,      //게임/배틀
        PartyList = 4, //파티리스트
        Pokedex = 5,   //도감
    }

    //씬 종류
    public enum sceneType
    {
        BootScene = 0,
        LobbyScene = 1,
        GameScene = 2,
    }

    public static string GetSceneName(sceneType type)
    {
        return type switch
        {
            sceneType.BootScene => "01. BootScene",
            sceneType.LobbyScene => "02. LobbyScene",
            sceneType.GameScene => "03. GameScene",
            _ => string.Empty
        };
    }
}

//UICanvas역할구분용
public enum UISceneCanvasKind
{
    Boot,    //부트UI
    Lobby,   //로비UI
    Game,    //게임UI
    Loading, //로딩UI
}

//EvolutionCode음수해석결과용
public enum SpecialEvolutionKind
{
    None = 0,          //특수변형아님
    MegaEvolution = 1, //메가진화(-1,-101,-102...)
    Gigantamax = 2,    //거다이맥스(-2)
    FormChange1 = 31,  //폼체인지1(-3)
    FormChange2 = 32,  //폼체인지2(-4)
    FormChange3 = 33,  //폼체인지3(-5)
    Unknown = 99,      //규칙밖값
}

//타입공용정의용
public enum PokemonType
{
    None = 0, //타입2없음
    Normal,   //노말
    Fire,     //불꽃
    Water,    //물
    Electric, //전기
    Grass,    //풀
    Ice,      //얼음
    Fighting, //격투
    Poison,   //독
    Ground,   //땅
    Flying,   //비행
    Psychic,  //에스퍼
    Bug,      //벌레
    Rock,     //바위
    Ghost,    //고스트
    Dragon,   //드래곤
    Dark,     //악
    Steel,    //강철
    Fairy,    //페어리
}
