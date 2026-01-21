using UnityEngine;
using UnityEngine.SceneManagement;

/*
BattleHUDController는씬의BattleManager를찾아Player/Enemy HUD를바인딩한다.
-UIRoot에두고BattleScene에서만gate로활성되게쓰는것을권장한다.
*/
public sealed class BattleHUDController : MonoBehaviour
{
    [Header("Gate (Optional)")]
    [SerializeField] private GameObject gameScreenRoot;

    [Header("HUDs")]
    [SerializeField] private BattlerHUD playerHUD;
    [SerializeField] private BattlerHUD enemyHUD;

    [Header("Debug")]
    [SerializeField] private bool debugLogs;

    private BattleManager cached;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryBind();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnbindAll();
        cached = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        cached = null;
        TryBind();
    }

    private void Update()
    {
        if (!IsGateOn()) return;

        if (cached == null || cached.Player == null || cached.Enemy == null)
        {
            TryBind();
        }
    }

    private bool IsGateOn()
    {
        if (gameScreenRoot != null) return gameScreenRoot.activeInHierarchy;
        return true;
    }

    private void TryBind()
    {
        if (!IsGateOn()) return;

        cached = FindObjectOfType<BattleManager>(true);
        if (cached == null) return;

        if (cached.Player != null && playerHUD != null) playerHUD.Bind(cached.Player);
        if (cached.Enemy != null && enemyHUD != null) enemyHUD.Bind(cached.Enemy);

        LogTag("TryBind");
    }

    private void UnbindAll()
    {
        if (playerHUD != null) playerHUD.Unbind();
        if (enemyHUD != null) enemyHUD.Unbind();
    }

    private void LogTag(string tag)
    {
        if (!debugLogs) return;
        Debug.Log("[BattleHUDController]" + tag);
    }
}