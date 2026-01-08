using System.Collections;
using UnityEngine;

/*
BootScene에서 1회 실행되는 코어 엔트리 포인트.
- 매니저를 BootScene에서만 생성하고 DontDestroyOnLoad로 유지한다.
- 데이터(SO)를 초기화한 뒤 LobbyScene으로 이동한다.
- 런타임에서 GameManager를 통해 SceneLoader/PokedexService에 단일 접근한다.
- 씬 이름은 GameManager가 관리하고, UI는 GameManager의 LoadLobby/LoadGame만 호출한다.
*/
[DefaultExecutionOrder(-1000)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }            //전역 접근 지점(중복 생성 방지)

    [Header("Scenes")]
    [SerializeField] private string lobbySceneName = "02. LobbyScene";//Boot 이후 로드할 로비 씬 이름(실제 이름)
    [SerializeField] private string gameSceneName = "03. GameScene";//로비에서 진입할 게임 씬 이름(실제 이름)

    [Header("Data")]
    [SerializeField] private PokemonDatabaseSO pokemonDatabase;//도감 DB(SO) 레퍼런스

    [Header("Managers")]
    [SerializeField] private SceneLoader sceneLoader;//씬 로더(전환 단일 책임)
    [SerializeField] private PokedexService pokedexService;//도감 서비스(조회/캐시 단일 책임)

    public SceneLoader SceneLoader => sceneLoader;//코어 서비스 접근용
    public PokedexService Pokedex => pokedexService;//코어 서비스 접근용

    public string LobbySceneName => lobbySceneName;//디버그/툴링 확인용
    public string GameSceneName => gameSceneName;//디버그/툴링 확인용

    private bool isBootstrapped;//Start 중복 실행 방지

    private void Awake()
    {
        //싱글톤: BootScene에서 실수로 프리팹/씬 중복 배치돼도 1개만 유지하기 위함
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        //씬이 바뀌어도 코어 매니저는 유지되어야 런 루프/데이터 캐시가 끊기지 않음
        DontDestroyOnLoad(gameObject);

        //인스펙터 누락/프리팹 변경에도 실행되도록 자동 보정
        EnsureCoreComponents();
    }

    private void Start()
    {
        //씬 로드/리로드 등으로 Start가 다시 타는 상황 방지
        if (isBootstrapped)
        {
            return;
        }

        isBootstrapped = true;

        //코어 초기화 순서(데이터→씬)를 유지하기 위해 코루틴 사용
        StartCoroutine(BootstrapRoutine());
    }

    //UI에서 바로 호출하는 진입점(씬 이름은 GameManager가 가진다)
    public void LoadLobby()
    {
        StartCoroutine(LoadSceneRoutine(lobbySceneName));
    }

    //UI에서 바로 호출하는 진입점(씬 이름은 GameManager가 가진다)
    public void LoadGame()
    {
        StartCoroutine(LoadSceneRoutine(gameSceneName));
    }

    private void EnsureCoreComponents()
    {
        //컴포넌트 참조 누락 시 런타임에서 즉시 복구해 실행 자체는 되게 만든다.
        if (sceneLoader == null)
        {
            sceneLoader = GetComponent<SceneLoader>();
        }

        if (sceneLoader == null)
        {
            sceneLoader = gameObject.AddComponent<SceneLoader>();
        }

        if (pokedexService == null)
        {
            pokedexService = GetComponent<PokedexService>();
        }

        if (pokedexService == null)
        {
            pokedexService = gameObject.AddComponent<PokedexService>();
        }
    }

    private IEnumerator BootstrapRoutine()
    {
        //데이터 초기화는 씬 전환 전에 끝내야 Lobby에서 즉시 참조 가능
        if (pokemonDatabase == null)
        {
            Debug.LogError("PokemonDatabaseSO가 GameManager에 연결되지 않았어.BootScene의 GameManager 인스펙터에 할당해줘.");
        }
        else
        {
            pokedexService.Initialize(pokemonDatabase);
        }

        //Initialize 직후 프레임을 한 번 넘겨 Editor/Runtime의 호출 순서 충돌 가능성을 줄임
        yield return null;

        //BootScene의 역할은 '코어 준비 + 로비 이동'까지만
        yield return LoadSceneRoutine(lobbySceneName);
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        if (sceneLoader == null)
        {
            Debug.LogError("SceneLoader가 없어.GameManager에 SceneLoader가 붙어있는지 확인해줘.");
            yield break;
        }

        if (sceneLoader.IsLoading)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("씬 이름이 비어있어.GameManager 인스펙터에서 씬 이름을 설정해줘.");
            yield break;
        }

        yield return sceneLoader.LoadSceneAsync(sceneName);
    }
}