namespace ArcadeKnight;

public class CourseMetaData
{
    #region Properties

    public string Name { get; set; }

    public string Author { get; set; }

    public CourseData EasyCourse { get; set; }

    public CourseData NormalCourse { get; set; }

    public CourseData HardCourse { get; set; }

    public string Scene { get; set; }

    #endregion
}
