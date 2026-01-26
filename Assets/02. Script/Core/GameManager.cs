using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*전반적인 게임흐름을 관리하는 매니저입니다
[UI 애니메이션 관련]
 게임 일시정지시 timeScale이 0으로 설정하여 관리합니다
 게임이 정지되었을때 실행되는 UI의 애니메이션은 멈추면 안되기때문에
 해당 UI의 animator컴포넌트 inspector창에서 Update Mode를 [Normal] -> [Unscaled Time]으로 변경해주세요
 UI만 해당하는 사항이며 다른 오브젝트들은 따로 설정하실 필요 없습니다
*/
public class GameManager : Singleton<GameManager>
{

    public event Action OnGameStart;  // 게임시작시 호출 할 함수
    public event Action OnGameOver;   // 게임오버시 호출 할 함수
    public event Action OnGameClear;  // 스테이지 클리어시 호출 할 함수
    public event Action OnGamePause;  // 게임일시정지시 호출 할 함수
    public event Action OnGameResume; // 게임재개시 호출 할 함수
    public event Action OnLevelUp;    // 레벨업시 호출 할 함수

    public float gameGold { get; private set; }
    public bool isPlay { get; private set; } // 게임이 진행중인지 확인 변수

    [Header("Data")]
    [SerializeField] private PokemonDatabaseSO pokemonDatabase;

    [Header("Managers")]
    [SerializeField] private SceneLoader sceneLoader;
    [SerializeField] private PokedexService pokedexService;

    public SceneLoader SceneLoader => sceneLoader != null ? sceneLoader : SceneLoader.Instance;
    public PokedexService Pokedex => pokedexService;

    protected override void Init()
    {
        base.Init();
        isPlay = false;
        gameGold = 1000f;
    }

    private IEnumerator Start()
    {
        yield return null;
        EnsureSceneLoaderReady();
        EnsurePokedexReady();
    }

    private void EnsureSceneLoaderReady()
    {
        if (sceneLoader != null) return;

        sceneLoader = SceneLoader.Instance;
        if (sceneLoader != null) return;

        sceneLoader = FindObjectOfType<SceneLoader>(true);
    }

    private void EnsurePokedexReady()
    {
        if (pokedexService == null)
        {
            pokedexService = FindObjectOfType<PokedexService>(true);
        }

        if (pokedexService == null)
        {
            var go = new GameObject("PokedexService");
            pokedexService = go.AddComponent<PokedexService>();
            DontDestroyOnLoad(go);
        }

        if (pokemonDatabase == null)
        {
            pokemonDatabase = Resources.Load<PokemonDatabaseSO>("PokemonDatabase");
        }

        if (pokemonDatabase == null)
        {
            Debug.LogWarning("GameManager: PokemonDatabaseSO가 없습니다. (Inspector 연결 또는 Resources/PokemonDatabase.asset 필요)");
            return;
        }

        if (!pokedexService.IsInitialized)
        {
            pokedexService.Initialize(pokemonDatabase);
        }
    }

    public void GameStart()
    {
        Time.timeScale = 1f;
        isPlay = true;
        OnGameStart?.Invoke();
    }

    public void GameOver()
    {
        Time.timeScale = 0f;
        isPlay = false;
        OnGameOver?.Invoke();
    }

    public void GameClear()
    {
        Time.timeScale = 0f;
        isPlay = false;
        OnGameClear?.Invoke();
    }

    public void GamePause()
    {
        Time.timeScale = 0f;
        isPlay = false;
        OnGamePause?.Invoke();
    }

    public void GameResume()
    {
        Time.timeScale = 1f;
        isPlay = true;
        OnGameResume?.Invoke();
    }

    public void LevelUp()
    {
        Time.timeScale = 0f;
        isPlay = false;
        OnLevelUp?.Invoke();
    }

    public void AddGold(float num)
    {
        gameGold += num;
    }
}