using ArcadeKnight.Enums;

namespace ArcadeKnight.Obstacles;

public abstract class HitboxObstacle : Obstacle
{
    #region Properties

    public float Width { get; set; }

    public float Height { get; set; }

    public CheckDirection RevertDirection { get; set; }

    #endregion
}
