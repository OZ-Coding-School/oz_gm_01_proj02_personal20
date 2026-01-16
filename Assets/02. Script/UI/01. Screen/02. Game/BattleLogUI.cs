using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class BattleLogUI : MonoBehaviour
{
    [Header("Single Text (필수)")]
    [SerializeField] private TMP_Text logText;

    [Header("Continue (▼) UI (표시용)")]
    [SerializeField] private GameObject continueIcon;  // ▼ 아이콘
    [SerializeField] private Button continueButton;    // 있으면 클릭 막기용(없어도 됨)

    [Header("Timing")]
    [SerializeField] private float minIntervalSeconds = 0.2f;

    [Header("Confirm Keys (▼ 있을 때만 동작)")]
    [SerializeField] private KeyCode[] confirmKeys = { KeyCode.Return, KeyCode.Z, KeyCode.Space };

    private readonly Queue<string> queue = new();
    private Coroutine routine;

    private bool waitingForConfirm;
    private bool promptActive;
    private string promptText = string.Empty;

    // 블로킹 로그(▼ 대기)가 진행 중이면 true. PROMPT는 busy 아님.
    public bool IsBusy => waitingForConfirm || queue.Count > 0;

    private void Awake()
    {
        if (logText == null)
            logText = GetComponent<TMP_Text>() ?? GetComponentInChildren<TMP_Text>(true);

        if (continueButton != null)
            continueButton.interactable = false; // 클릭 진행 금지

        SetContinueVisible(false);
        Render(string.Empty);
    }

    private void Update()
    {
        if (!waitingForConfirm) return;

        if (Input.anyKeyDown)
            Debug.Log("[BattleLogUI] anyKeyDown detected");

        for (int i = 0; i < confirmKeys.Length; i++)
        {
            if (Input.GetKeyDown(confirmKeys[i]))
            {
                Debug.Log($"[BattleLogUI] Confirm key: {confirmKeys[i]}");
                waitingForConfirm = false;
                SetContinueVisible(false);
                return;
            }
        }
    }

    private void OnDisable()
    {
        routine = null;
        waitingForConfirm = false;
    }

    // 규칙:
    // - "[PROMPT]"로 시작: 프롬프트(▼ 없이 고정)
    // - 그 외: 블로킹 메시지(▼ + 키 입력 필요)
    // - 줄바꿈(\n) 그대로 사용 (2줄 세트는 문자열에서 관리)
    public void Push(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return;

        if (raw.StartsWith("[PROMPT]"))
        {
            promptText = raw.Substring("[PROMPT]".Length);
            promptActive = true;

            if (!IsBusy)
                Render(promptText);

            return;
        }

        queue.Enqueue(raw);

        if (!isActiveAndEnabled) return;
        if (routine == null)
            routine = StartCoroutine(CoConsume());
    }

    // 외부(버튼/입력 시스템)에서 호출할 수도 있음
    public void Confirm()
    {
        if (!waitingForConfirm) return;
        waitingForConfirm = false;
        SetContinueVisible(false);
    }

    public void ClearPrompt()
    {
        promptActive = false;
        promptText = string.Empty;

        if (!IsBusy)
            Render(string.Empty);
    }

    private IEnumerator CoConsume()
    {
        while (queue.Count > 0)
        {
            string msg = queue.Dequeue();

            Render(msg);
            SetContinueVisible(false);

            if (minIntervalSeconds > 0f)
                yield return new WaitForSeconds(minIntervalSeconds);

            waitingForConfirm = true;
            SetContinueVisible(true);

            yield return new WaitUntil(() => !waitingForConfirm);
            yield return null;
        }

        routine = null;

        if (promptActive)
            Render(promptText);
        else
            Render(string.Empty);
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
}