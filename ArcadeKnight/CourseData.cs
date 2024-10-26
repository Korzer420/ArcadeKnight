namespace ArcadeKnight;

public class CourseData
{
    #region Properties

    public string[] ObjectsToRemove { get; set; } = [];

    public Obstacle[] Obstacles { get; set; } = [];

    public string[] InitialRules { get; set; } = [];

    public string Highscore { get; set; }

    public float StartPositionX { get; set; }

    public float StartPositionY { get; set; }

    public float EndPositionX { get; set; }

    public float EndPositionY { get; set; }

    public (float, float)[] PreviewPoints { get; set; } = [];

    #endregion
}
