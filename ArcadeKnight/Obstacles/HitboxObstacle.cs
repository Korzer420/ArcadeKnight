using ArcadeKnight.Enums;

namespace ArcadeKnight.Obstacles;

public abstract class HitboxObstacle : Obstacle
{
    #region Properties

    public float Width { get; set; }

    public float Height { get; set; }

    public float HorizontalOffset { get; set; }

    public float VerticalOffset { get; set; }

    #endregion
}
