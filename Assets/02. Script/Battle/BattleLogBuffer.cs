using System;
using System.Collections.Generic;
using UnityEngine;

/*
BattleLogBuffer는전투로그를버퍼링하고이벤트로전달한다.
-외부에서는Push를호출해이기능을사용한다.
-외부에서는GetSnapshot을호출해이기능을사용한다.
*/
public sealed class BattleLogBuffer
{
    private readonly List<string> lines;
    private readonly int capacity;

    public event Action<string> OnLinePushed;

    //BattleLogBuffer는로그버퍼크기를설정한다.
    public BattleLogBuffer(int capacityValue)
    {
        capacity = Mathf.Max(8, capacityValue);
        lines = new List<string>(capacity);
    }

    //Push는한줄로그를추가한다.
    public void Push(string line)
    {
        if (string.IsNullOrEmpty(line)) return;

        if (lines.Count >= capacity)
        {
            lines.RemoveAt(0);
        }

        lines.Add(line);

        Debug.Log(line);
        OnLinePushed?.Invoke(line);
    }

    //GetSnapshot은현재버퍼를복사없이열람가능한형태로제공한다.
    public IReadOnlyList<string> GetSnapshot()
    {
        return lines;
    }
}
