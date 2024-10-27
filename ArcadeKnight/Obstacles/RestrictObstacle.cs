using ArcadeKnight.Enums;

namespace ArcadeKnight.Obstacles;

public class RestrictObstacle : Obstacle
{
    #region Properties

    public string AffectedAbility { get; set; }

    public bool SetValue { get; set; }

    public CheckDirection RevertDirection { get; set; }

    #endregion
}
