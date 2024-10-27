using ArcadeKnight.SaveData;
using KorzUtils.Helper;
using Modding;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ArcadeKnight;

public class ArcadeKnight : Mod, ILocalSettings<LocalSaveData>
{
    #region Properties

    public static Dictionary<string, GameObject> PreloadedObjects { get; set; } = [];

    #endregion

    public override List<(string, string)> GetPreloadNames() => new()
    {
        ("GG_Workshop", "GG_Statue_Vengefly/Inspect"),
        ("Dream_01_False_Knight", "Dream Entry"),
        ("Dream_01_False_Knight", "Dream Fall Catcher"),
        ("Crossroads_04", "_Scenery/plat_float_01"),
        ("White_Palace_03_hub", "doorWarp"),
        ("White_Palace_06", "White Palace Fly (3)"),
        ("Fungus1_31", "_Scenery/fung_plat_float_02"),
        ("Fungus1_22", "Gate Switch"),
        ("Fungus1_22", "Metal Gate")
    };

    public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
    {
        PreloadedObjects.Add("Start", preloadedObjects["Dream_01_False_Knight"]["Dream Entry"]);
        PreloadedObjects.Add("Tablet",preloadedObjects["GG_Workshop"]["GG_Statue_Vengefly/Inspect"]);
        PreloadedObjects.Add("ExitTrigger", preloadedObjects["Dream_01_False_Knight"]["Dream Fall Catcher"]);
        PreloadedObjects.Add("Platform", preloadedObjects["Crossroads_04"]["_Scenery/plat_float_01"]);
        PreloadedObjects.Add("CancelDoor", preloadedObjects["White_Palace_03_hub"]["doorWarp"]);
        PreloadedObjects.Add("Wingmould", preloadedObjects["White_Palace_06"]["White Palace Fly (3)"]);
        PreloadedObjects.Add("Block", preloadedObjects["Fungus1_31"]["_Scenery/fung_plat_float_02"]);
        PreloadedObjects.Add("Gate", preloadedObjects["Fungus1_22"]["Metal Gate"]);
        PreloadedObjects.Add("Switch", preloadedObjects["Fungus1_22"]["Gate Switch"]);
        MinigameController.Initialize();
    }

    public void OnLoadLocal(LocalSaveData localSaveData)
    {
        if (localSaveData?.RecordData == null)
            return;
        MinigameController.UnassignableRecordData.Clear();
        foreach (RecordData recordData in localSaveData.RecordData)
        {
            Minigame minigame = MinigameController.Minigames.FirstOrDefault(x => x.GetMinigameType() == recordData.Minigame);
            if (minigame == null)
            {
                LogHelper.Write("Record data contains unknown minigame. Entry will be skipped.", KorzUtils.Enums.LogType.Warning, false);
                continue;
            }
            CourseMetaData courseMetaData = minigame.Courses.FirstOrDefault(x => x.Name == recordData.CourseName);
            if (courseMetaData == null)
            {
                MinigameController.UnassignableRecordData.Add(recordData);
                LogHelper.Write("Couldn't find course \""+recordData.CourseName+"\" in minigame \""+minigame.GetMinigameType().ToString()+"\". Entry will still be saved.", KorzUtils.Enums.LogType.Normal, false);
                continue;
            }
            courseMetaData.EasyCourse.Highscore = recordData.EasyHighscore;
            courseMetaData.NormalCourse.Highscore = recordData.NormalHighscore;
            courseMetaData.HardCourse.Highscore = recordData.HardHighscore;
        }
    }

    public LocalSaveData OnSaveLocal()
    {
        List<RecordData> records = [];
        foreach (Minigame minigame in MinigameController.Minigames)
            foreach (CourseMetaData course in minigame.Courses)
                records.Add(new()
                {
                    CourseName = course.Name,
                    Minigame = minigame.GetMinigameType(),
                    EasyHighscore = course.EasyCourse.Highscore,
                    NormalHighscore = course.NormalCourse.Highscore,
                    HardHighscore = course.HardCourse.Highscore
                });
        records.AddRange(MinigameController.UnassignableRecordData);
        return new() { RecordData = records };
    }

    // ToDo:
    // Ein weiterer Kurs
    // Invincibility in Finish Sequenz fixen.
    // Zweites Minispiel.
    
}