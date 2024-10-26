namespace ArcadeKnight.Enums;

/// <summary>
/// Defines the state the game is in currently.
/// </summary>
public enum MinigameState
{
    /// <summary>
    /// No minigame is active, the game should work normal.
    /// </summary>
    Inactive,

    /// <summary>
    /// A minigame is active.
    /// </summary>
    Active,

    /// <summary>
    /// The minigame can be transitioned to inactive and save the score.
    /// </summary>
    Finish
}
