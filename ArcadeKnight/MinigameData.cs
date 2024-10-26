using UnityEngine;

namespace ArcadeKnight;

public class MinigameData
{
    #region Properties

    public Vector3 SpawnPoint { get; set; }

    public string SpawnScene { get; set; }

    public Vector3 TabletPosition { get; set; }

    public bool IsUnlocked { get; set; }

    public int EasyHighScore { get; set; }

    public int NormalHighScore { get; set; }

    public int HardHighScore { get; set; }

    public int EasyHighScoreDeveloper { get; set; }

    public int NormalHighScoreDeveloper { get; set; }

    public int HardDeveloperScore { get; set; }

    #endregion
}
