using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/*
BattleLogUI는전투로그출력과다음진행(Confirm)대기를담당한다.
-문자열규칙:[PROMPT]는고정프롬프트(▼없음),그외는블로킹메시지(▼+Confirm필요)
-Confirm은대기중이면즉시소모,대기전이면pending으로저장후대기진입시자동소모
-디버그로그는상태변화지점에서만출력한다(Update에서GC유발로그금지)
*/
public sealed class BattleLogUI : MonoBehaviour
{
    [Header("Single Text")]
    [SerializeField] private TMP_Text logText;

    [Header("Continue UI")]
    [SerializeField] private GameObject continueIcon;
    [SerializeField] private Button continueButton;

    [Header("Timing")]
    [Min(0f)]
    [SerializeField] private float minIntervalSeconds = 0.2f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs;

    private readonly Queue<string> queue = new Queue<string>();
    private Coroutine routine;

    private bool waitingForConfirm;
    private bool pendingAdvance;

    private bool promptActive;
    private string promptText = string.Empty;

    public bool IsBusy => waitingForConfirm || queue.Count > 0;

    private void Awake()
    {
        if (logText == null)
        {
            logText = GetComponent<TMP_Text>();
            if (logText == null) logText = GetComponentInChildren<TMP_Text>(true);
        }

        if (continueButton != null) continueButton.interactable = false;

        SetContinueVisible(false);
        Render(string.Empty);

        LogTag("Awake");
    }

    // ✅ 핵심: Screen이 Hide됐다가 Show될 때, 남은 큐를 다시 소비 시작
    private void OnEnable()
    {
        if (routine == null && queue.Count > 0)
        {
            routine = StartCoroutine(CoConsume());
            LogTag("ResumeConsume");
        }

        // 현재 상태에 맞춰 ▼ 표시 복구
        SetContinueVisible(waitingForConfirm);

        LogTag("OnEnable");
    }

    // ✅ Disable될 때 큐를 날리면 안 됨(Show/Hide 사이클에서 멈춤 발생)
    private void OnDisable()
    {
        // 코루틴은 Disable 시 자동 중지됨 → 참조만 끊고, 큐/상태는 유지
        routine = null;
        LogTag("OnDisable");
    }

    public void Push(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return;

        if (raw.StartsWith("[PROMPT]"))
        {
            promptText = raw.Substring("[PROMPT]".Length);
            promptActive = true;

            if (!IsBusy) Render(promptText);

            LogTag("PushPrompt");
            return;
        }

        queue.Enqueue(raw);
        LogTag("EnqueueMsg");

        if (!isActiveAndEnabled) return;

        if (routine == null)
        {
            routine = StartCoroutine(CoConsume());
            LogTag("ConsumeStart");
        }
    }

    public void Confirm()
    {
        if (waitingForConfirm)
        {
            waitingForConfirm = false;
            pendingAdvance = false;
            SetContinueVisible(false);
            LogTag("ConfirmConsumedNow");
            return;
        }

        pendingAdvance = true;
        LogTag("ConfirmBuffered");
    }

    public void ClearPrompt()
    {
        promptActive = false;
        promptText = string.Empty;

        if (!IsBusy) Render(string.Empty);

        LogTag("ClearPrompt");
    }

    private IEnumerator CoConsume()
    {
        while (queue.Count > 0)
        {
            string msg = queue.Dequeue();

            Render(msg);
            SetContinueVisible(false);
            LogTag("ShowMsg");

            if (minIntervalSeconds > 0f)
                yield return new WaitForSecondsRealtime(minIntervalSeconds);

            waitingForConfirm = true;
            SetContinueVisible(true);
            LogTag("WaitEnter");

            if (pendingAdvance)
            {
                pendingAdvance = false;
                waitingForConfirm = false;
                SetContinueVisible(false);
                LogTag("PendingConsumed");
                yield return null;
                continue;
            }

            yield return new WaitUntil(() => !waitingForConfirm);
            LogTag("WaitExit");
            yield return null;
        }

        routine = null;

        if (promptActive)
        {
            Render(promptText);
            LogTag("RestorePrompt");
        }
        else
        {
            Render(string.Empty);
            LogTag("ClearText");
        }

        LogTag("ConsumeEnd");
    }

    private void Render(string text)
    {
        if (logText == null) return;
        logText.text = text ?? string.Empty;
    }

    private void SetContinueVisible(bool visible)
    {
        if (continueIcon != null) continueIcon.SetActive(visible);
        if (continueButton != null) continueButton.interactable = false;
    }

    private void LogTag(string tag)
    {
        if (!debugLogs) return;
        Debug.Log("[BattleLogUI]" + tag);
    }
}