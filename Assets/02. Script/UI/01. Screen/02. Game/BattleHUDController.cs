using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/*
BattleHUDController는 BattleManager가 "준비될 때까지" 기다렸다가
Player/Enemy HUD를 안정적으로 바인딩한다.
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
    private Coroutine bindRoutine;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        StartBindRoutine();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (bindRoutine != null)
        {
            StopCoroutine(bindRoutine);
            bindRoutine = null;
        }

        UnbindAll();
        cached = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        cached = null;
        StartBindRoutine();
    }

    private bool IsGateOn()
    {
        if (gameScreenRoot != null) return gameScreenRoot.activeInHierarchy;
        return true;
    }

    private void StartBindRoutine()
    {
        if (bindRoutine != null)
        {
            StopCoroutine(bindRoutine);
            bindRoutine = null;
        }
        bindRoutine = StartCoroutine(CoBindWhenReady());
    }

    private IEnumerator CoBindWhenReady()
    {
        // 게이트가 꺼져 있으면 켜질 때까지 대기
        while (!IsGateOn())
            yield return null;

        // BattleManager 찾기
        while (cached == null)
        {
            cached = FindObjectOfType<BattleManager>(true);
            if (cached == null) yield return null;
        }

        // Player/Enemy 생성될 때까지 대기 (BattleManager.Start 이후)
        while (cached.Player == null || cached.Enemy == null)
            yield return null;

        if (playerHUD != null) playerHUD.Bind(cached.Player);
        if (enemyHUD != null) enemyHUD.Bind(cached.Enemy);

        LogTag("BindOK");
        bindRoutine = null;
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
