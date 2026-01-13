using UnityEngine;

/// <summary>
/// BattleManager is a scene-bound MonoBehaviour.
/// - Owns battle flow orchestration (enter/exit, turn start/end, UI hooks).
/// - Keep Update free of allocations; drive logic via explicit calls / coroutines.
/// </summary>
public sealed class BattleManager : MonoBehaviour
{
    private void Start()
    {
    }

    private void Update()
    {
    }
}
