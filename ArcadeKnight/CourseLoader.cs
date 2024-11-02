using ArcadeKnight.Obstacles;
using KorzUtils.Enums;
using KorzUtils.Helper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ArcadeKnight;

public static class CourseLoader
{
    internal static void Load()
    {
        LoadNormalCourses();
        LoadCustomCourses();
    }

    private static void LoadNormalCourses()
    {
        foreach (Minigame minigame in MinigameController.Minigames)
        {
            List<CourseMetaData> metaData = ResourceHelper.LoadJsonResource<ArcadeKnight, List<CourseMetaData>>($"Data.{minigame.GetCourseFile()}.json");
            AssignCourseData(metaData);
        }
    }

    private static void LoadCustomCourses()
    {
        string customCourseDirectory = Path.Combine(Path.GetDirectoryName(typeof(ArcadeKnight).Assembly.Location), "CustomCourses");
        if (!Directory.Exists(customCourseDirectory))
            return;
        string[] files = Directory.GetFiles(customCourseDirectory, "*.json");
        if (files.Length == 0)
            return;
        LogHelper.Write<ArcadeKnight>("Found custom courses files.", includeScene: false);
        foreach (string file in files)
        {
            string fileContent = File.ReadAllText(file);
            try
            {
                List<CourseMetaData> courseData = [];
                try
                {
                    CourseMetaData singleCourseData = JsonConvert.DeserializeObject<CourseMetaData>(fileContent, new JsonSerializerSettings()
                    {
                        TypeNameHandling = TypeNameHandling.Auto,
                    });
                    courseData.Add(singleCourseData);
                }
                catch (Exception)
                {
                    courseData = JsonConvert.DeserializeObject<List<CourseMetaData>>(fileContent, new JsonSerializerSettings()
                    {
                        TypeNameHandling = TypeNameHandling.Auto,
                    });
                }
                LogHelper.Write<ArcadeKnight>($"Found {courseData.Count} course(s) in file {Path.GetFileName(file)}.", includeScene: false);
                AssignCourseData(courseData, true);
            }
            catch (Exception exception)
            {
                LogHelper.Write<ArcadeKnight>($"Couldn't process file {Path.GetFileName(file)}: ", exception, false);
            }
        }
    }

    private static void AssignCourseData(List<CourseMetaData> courseData, bool isCustom = false)
    {
        foreach (CourseMetaData metaData in courseData)
        {
            Minigame minigame = MinigameController.Minigames.FirstOrDefault(x => x.GetMinigameType() == metaData.Minigame);
            if (minigame == null)
            {
                LogHelper.Write<ArcadeKnight>($"Couldn't find minigame of entry {metaData.Name}.", LogType.Error, false);
                continue;
            }

            string validationMessages = ValidateCourseData(metaData);
            if (!string.IsNullOrEmpty(validationMessages))
            {
                LogHelper.Write<ArcadeKnight>($"Course {metaData.Name} has an invalid configuration. Validation messages: {validationMessages}", LogType.Error, false);
                continue;
            }
            if (minigame.Courses.Any(x => x.Name.ToUpper() == metaData.Name.ToUpper()))
            {
                LogHelper.Write<ArcadeKnight>($"A course with the name {metaData.Name} already exists in the minigame {metaData.Minigame}. This entry will be skipped.", LogType.Warning, false);
                continue;
            }
            // Highscores will be saved in the save data and are not taken from the initial file.
            metaData.EasyCourse.Highscore = "";
            metaData.NormalCourse.Highscore = "";
            metaData.HardCourse.Highscore = "";
            metaData.IsCustomCourse = isCustom;
            minigame.Courses.Add(metaData);
            if (isCustom)
                LogHelper.Write<ArcadeKnight>($"Added course {metaData.Name}.", includeScene: false);
        }
    }

    private static string ValidateCourseData(CourseMetaData courseMetaData)
    {
        string errorMessage = string.Empty;
        if (string.IsNullOrEmpty(courseMetaData.Name))
            errorMessage += "Course needs a name.\r\n";
        if (string.IsNullOrEmpty(courseMetaData.Scene))
            errorMessage += "No scene provided.\r\n";
        if (courseMetaData.EasyCourse == null || courseMetaData.NormalCourse == null || courseMetaData.HardCourse == null)
            errorMessage += "Not all three difficulties provided.\r\n";
        else
        {
            // Ensure that we don't have null values.
            courseMetaData.EasyCourse.ObjectsToRemove ??= [];
            courseMetaData.NormalCourse.ObjectsToRemove ??= [];
            courseMetaData.HardCourse.ObjectsToRemove ??= [];
            courseMetaData.EasyCourse.Obstacles ??= [];
            courseMetaData.NormalCourse.Obstacles ??= [];
            courseMetaData.HardCourse.Obstacles ??= [];
            courseMetaData.EasyCourse.Restrictions ??= [];
            courseMetaData.NormalCourse.Restrictions ??= [];
            courseMetaData.HardCourse.Restrictions ??= [];
        }
        if (courseMetaData.EasyCourse.StartPositionX == 0 || courseMetaData.EasyCourse.StartPositionY == 0
            || courseMetaData.EasyCourse.EndPositionX == 0 || courseMetaData.EasyCourse.EndPositionY == 0)
            errorMessage += "Easy course requires the start and end position to be assigned.\r\n";
        if (courseMetaData.NormalCourse.StartPositionX == 0 || courseMetaData.NormalCourse.StartPositionY == 0
            || courseMetaData.NormalCourse.EndPositionX == 0 || courseMetaData.NormalCourse.EndPositionY == 0)
            errorMessage += "Normal course requires the start and end position to be assigned.\r\n";
        if (courseMetaData.HardCourse.StartPositionX == 0 || courseMetaData.HardCourse.StartPositionY == 0
            || courseMetaData.HardCourse.EndPositionX == 0 || courseMetaData.HardCourse.EndPositionY == 0)
            errorMessage += "Hard course requires the start and end position to be assigned.\r\n";
        if (courseMetaData.Minigame == Enums.MinigameType.XerosMirrorWorld)
        {
            if (courseMetaData.EasyCourse.Obstacles.Count(x => x is ImposterObstacle imposter && !imposter.AlwaysReal) < 5)
                errorMessage += "Easy courses of Xeros Mirror world require at least 5 viable imposter obstacles.\r\n";
            if (courseMetaData.NormalCourse.Obstacles.Count(x => x is ImposterObstacle imposter && !imposter.AlwaysReal) < 7)
                errorMessage += "Normal courses of Xeros Mirror world require at least 7 viable imposter obstacles.\r\n";
            if (courseMetaData.HardCourse.Obstacles.Count(x => x is ImposterObstacle imposter && !imposter.AlwaysReal) < 10)
                errorMessage += "Hard courses of Xeros Mirror world require at least 10 viable imposter obstacles.\r\n";
        }

        return errorMessage.TrimStart();
    }
}
