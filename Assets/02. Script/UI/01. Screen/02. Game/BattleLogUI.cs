using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;

public class BattleLogUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text logText;
    [SerializeField] private GameObject continueHint;//선택(없어도됨)

    [Header("Output")]
    [SerializeField] private int maxLines = 2;
    [SerializeField] private float typeSpeed = 0.02f;//0이면 즉시 출력
    [SerializeField] private bool addNewLineBetweenMessages = true;

    private readonly Queue<string> queue = new();
    private readonly List<string> lines = new();
    private Coroutine routine;

    private bool isTyping;
    private string currentMessage;

    private void Awake()
    {
        if (continueHint != null) continueHint.SetActive(false);
        RefreshText();
    }

    public void Push(string message)
    {
        if (string.IsNullOrEmpty(message)) return;

        queue.Enqueue(message);

        if (routine == null)
            routine = StartCoroutine(CoConsume());
    }

    //확인키(Z/Enter)에서 호출
    public void Confirm()
    {
        //타이핑중이면 스킵
        if (isTyping)
        {
            StopTypingAndFlush();
            return;
        }

        //다음 메시지 진행(큐가 비어있으면 힌트 끄기)
        if (queue.Count == 0)
        {
            if (continueHint != null) continueHint.SetActive(false);
        }
    }

    private IEnumerator CoConsume()
    {
        while (queue.Count > 0)
        {
            currentMessage = queue.Dequeue();

            if (typeSpeed <= 0f)
            {
                AppendMessage(currentMessage);
                yield return null;
            }
            else
            {
                yield return CoType(currentMessage);
            }

            //메시지 하나 끝나면 힌트 표시(원하면 자동 진행 안 하고 Confirm 기다리게 할 수도 있음)
            if (continueHint != null) continueHint.SetActive(queue.Count > 0);

            //자동으로 계속 진행하고 싶으면 아래 줄 유지
            //Confirm 기다리게 하고 싶으면 while로 입력 대기 넣으면 됨
            yield return null;
        }

        routine = null;
        if (continueHint != null) continueHint.SetActive(false);
    }

    private IEnumerator CoType(string message)
    {
        isTyping = true;

        //타이핑은 "마지막 줄"로 출력되게 처리
        int targetLineIndex = AddEmptyLineForTyping();
        var sb = new StringBuilder();

        for (int i = 0; i < message.Length; i++)
        {
            sb.Append(message[i]);
            lines[targetLineIndex] = sb.ToString();
            ClampLines();
            RefreshText();
            yield return new WaitForSeconds(typeSpeed);
        }

        isTyping = false;
    }

    private void StopTypingAndFlush()
    {
        //현재 타이핑중인 줄을 완성된 메시지로 확정
        isTyping = false;

        //타이핑 코루틴은 CoConsume 안에서 돌고있어서 StopCoroutine을 직접 안 해도 되지만
        //즉시 반영을 위해 현재 메시지를 강제로 확정
        ReplaceLastLine(currentMessage);
        RefreshText();
    }

    private void AppendMessage(string message)
    {
        if (addNewLineBetweenMessages && lines.Count > 0)
        {
            //빈 줄을 넣고 싶으면 여기에서 lines.Add("")
            //지금은 maxLines가 작아서 기본은 생략 추천
        }

        lines.Add(message);
        ClampLines();
        RefreshText();
    }

    private int AddEmptyLineForTyping()
    {
        lines.Add("");
        ClampLines();
        RefreshText();
        return lines.Count - 1;
    }

    private void ReplaceLastLine(string message)
    {
        if (lines.Count == 0)
        {
            lines.Add(message);
        }
        else
        {
            lines[lines.Count - 1] = message;
        }

        ClampLines();
    }

    private void ClampLines()
    {
        while (lines.Count > maxLines)
            lines.RemoveAt(0);
    }

    private void RefreshText()
    {
        if (logText == null) return;
        logText.text = string.Join("\n", lines);
    }
}
