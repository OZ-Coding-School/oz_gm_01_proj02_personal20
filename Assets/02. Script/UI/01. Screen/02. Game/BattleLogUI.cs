using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/*
BattleLogUI는전투로그출력과다음진행(Confirm)대기를담당한다.
-문자열규칙:[PROMPT]는고정프롬프트(▼없음),그외는블로킹메시지(▼+Confirm필요)
-Confirm은대기중이면즉시소모,대기전이면pending으로저장후대기진입시자동소모
-▼는별도UI없이로그텍스트끝에자동으로붙인다(waitingForConfirm일때만)
-디버그로그는상태변화지점에서만출력한다(Update에서GC유발로그금지)
*/
public sealed class BattleLogUI : MonoBehaviour
{
    [Header("Single Text")]
    [SerializeField] private TMP_Text logText;

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

        waitingForConfirm = false;
        pendingAdvance = false;

        Render(string.Empty);

        LogTag("Awake");
    }

    //OnEnable은Hide됐다가Show될때남은큐를다시소비시작한다
    private void OnEnable()
    {
        if (routine == null && queue.Count > 0)
        {
            routine = StartCoroutine(CoConsume());
            LogTag("ResumeConsume");
        }

        //현재상태에맞춰▼표시복구
        Render(GetCurrentDisplayText());

        LogTag("OnEnable");
    }

    //OnDisable은큐/상태는유지하고코루틴참조만끊는다
    private void OnDisable()
    {
        //코루틴은Disable시자동중지됨→참조만끊고큐/상태는유지
        routine = null;
        LogTag("OnDisable");
    }

    //Push는로그라인을큐에추가한다
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

    //Confirm은대기중이면즉시소모하고아니면pending으로저장한다
    public void Confirm()
    {
        if (waitingForConfirm)
        {
            waitingForConfirm = false;
            pendingAdvance = false;

            //대기해제되면▼가사라지도록즉시갱신
            Render(GetCurrentDisplayText());

            LogTag("ConfirmConsumedNow");
            return;
        }

        pendingAdvance = true;
        LogTag("ConfirmBuffered");
    }

    //ClearPrompt는프롬프트를해제한다
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

            waitingForConfirm = false;
            Render(msg);
            LogTag("ShowMsg");

            if (minIntervalSeconds > 0f)
                yield return new WaitForSecondsRealtime(minIntervalSeconds);

            waitingForConfirm = true;
            Render(msg);
            LogTag("WaitEnter");

            if (pendingAdvance)
            {
                pendingAdvance = false;
                waitingForConfirm = false;
                Render(msg);
                LogTag("PendingConsumed");
                yield return null;
                continue;
            }

            yield return new WaitUntil(() => !waitingForConfirm);
            LogTag("WaitExit");
            yield return null;
        }

        routine = null;
        waitingForConfirm = false;
        pendingAdvance = false;

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

    private string GetCurrentDisplayText()
    {
        if (logText == null) return string.Empty;

        //현재화면에표시중인텍스트에서마지막▼만제거해원본을복구한다
        string t = logText.text ?? string.Empty;
        if (t.EndsWith("\n▼")) t = t.Substring(0, t.Length - 2);
        return t;
    }

    //Render는현재대기상태면텍스트끝에▼를붙인다
    private void Render(string text)
    {
        if (logText == null) return;

        string baseText = text ?? string.Empty;

        if (waitingForConfirm)
        {
            logText.text = baseText + "\n▼";
        }
        else
        {
            logText.text = baseText;
        }
    }

    private void LogTag(string tag)
    {
        if (!debugLogs) return;
        Debug.Log("[BattleLogUI]" + tag);
    }
}
