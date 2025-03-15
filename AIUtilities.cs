using UnityEngine;

/// <summary>
/// Shared utilities and enums for the AI systems
/// </summary>
public static class AIUtilities
{
    /// <summary>
    /// AI thinking state enum - defined in a central location for all scripts to use
    /// </summary>
    public enum ThinkingState
    {
        Idle,
        ReadyToThink,
        Thinking,
        ThinkingComplete,
        ReadyToExecute,
        Executing
    }
}