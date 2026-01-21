using UnityEngine;

/*
BattleManager는Battle씬에서전투를부팅하고외부(UI/입력)와턴시스템을연결한다.
-외부에서는SelectSkillSlot을호출해플레이어기술선택을전달한다.
-턴시스템/스킬실행기/도감서비스참조를Awake에서캐싱한다.
*/
public sealed class BattleManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PokedexService pokedexService;
    [SerializeField] private TurnSystem turnSystem;
    [SerializeField] private SkillExecutor skillExecutor;

    [Header("Encounter")]
    [SerializeField] private int playerPokemonNo = 1;
    [SerializeField] private int enemyPokemonNo = 4;
    [SerializeField] private int battleLevel = 5;

    [Header("Skills")]
    [SerializeField] private BattleSkillDataSO playerSkill0;
    [SerializeField] private BattleSkillDataSO playerSkill1;
    [SerializeField] private BattleSkillDataSO playerSkill2;
    [SerializeField] private BattleSkillDataSO playerSkill3;

    [SerializeField] private BattleSkillDataSO enemySkill0;
    [SerializeField] private BattleSkillDataSO enemySkill1;
    [SerializeField] private BattleSkillDataSO enemySkill2;
    [SerializeField] private BattleSkillDataSO enemySkill3;

    private Battler player;
    private Battler enemy;
    private BattleLogBuffer log;

    public BattleLogBuffer LogBuffer => log;
    public TurnSystem TurnSystem => turnSystem;
    public Battler Player => player;
    public Battler Enemy => enemy;

    public BattleSkillDataSO GetPlayerSkill(int slotIndex)
    {
        return player?.GetSkill(slotIndex);
    }
    public string GetPlayerSkillName(int slotIndex)
    {
        var s = GetPlayerSkill(slotIndex);
        return s != null ? s.SkillName : "-";
    }

    //Awake는컴포넌트참조를캐싱한다.
    private void Awake()
    {
        if (turnSystem == null) turnSystem = GetComponent<TurnSystem>();
        if (skillExecutor == null) skillExecutor = GetComponent<SkillExecutor>();

        if (pokedexService == null)
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.Pokedex != null)
            {
                pokedexService = gm.Pokedex;
            }
            else
            {
                pokedexService = FindObjectOfType<PokedexService>(true);
            }
        }

        log = new BattleLogBuffer(32);
    }

    //Start는전투를초기화하고턴루프를시작한다.
    private void Start()
    {
        if (turnSystem == null || skillExecutor == null)
        {
            Debug.LogError("BattleManager:TurnSystem/SkillExecutor 누락");
            return;
        }

        EnsurePokedexInitialized();

        player = BuildBattler(playerPokemonNo, battleLevel, playerSkill0, playerSkill1, playerSkill2, playerSkill3);
        enemy = BuildBattler(enemyPokemonNo, battleLevel, enemySkill0, enemySkill1, enemySkill2, enemySkill3);

        turnSystem.BeginBattle(player, enemy, skillExecutor, log, OnBattleEnded);
    }

    //Update는테스트입력만처리한다.
    private void Update()
    {
        if (turnSystem == null) return;
        if (!turnSystem.IsWaitingForPlayerChoice) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSkillSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSkillSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSkillSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SelectSkillSlot(3);
    }

    //SelectSkillSlot은플레이어기술선택을턴시스템으로전달한다.
    public void SelectSkillSlot(int slotIndex)
    {
        if (turnSystem == null) return;
        turnSystem.SetPlayerChoice(slotIndex);
    }

    //EnsurePokedexInitialized는도감서비스초기화를보장한다.
    private void EnsurePokedexInitialized()
    {
        if (pokedexService == null) return;
        if (pokedexService.IsInitialized) return;

        // 1) PokedexService에 이미 DB가 연결돼 있으면 그걸 사용
        PokemonDatabaseSO db = pokedexService.Database;

        // 2) 없으면 GameManager가 쓰는 것처럼 Resources에서 로드 시도
        if (db == null)
        {
            db = Resources.Load<PokemonDatabaseSO>("PokemonDatabase");
        }

        if (db == null)
        {
            Debug.LogWarning(
                "BattleManager: PokemonDatabaseSO가 없습니다. " +
                "PokedexService Inspector에 연결하거나 Resources/PokemonDatabase.asset을 준비하세요."
            );
            return;
        }

        pokedexService.Initialize(db);
    }


    //BuildBattler는도감번호로Battler를생성한다.
    private Battler BuildBattler(int pokedexNo, int level, BattleSkillDataSO s0, BattleSkillDataSO s1, BattleSkillDataSO s2, BattleSkillDataSO s3)
    {
        var b = new Battler();

        PokemonEntry entry = null;
        if (pokedexService != null && pokedexService.TryGetDefaultByNo(pokedexNo, out PokemonEntry e))
        {
            entry = e;
        }
        else
        {
            Debug.LogWarning("BattleManager:도감번호 조회 실패:" + pokedexNo);
        }

        b.SetupFromPokedexEntry(entry, level);
        b.SetSkills(s0, s1, s2, s3);

        return b;
    }

    //OnBattleEnded는전투종료콜백이다.
    private void OnBattleEnded(bool playerWon)
    {
        //OnBattleEnded는후속(보상/씬전환)연결지점이다.
        Debug.Log("Battle End. playerWon=" + playerWon);
    }
}
