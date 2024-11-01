using ArcadeKnight.SaveData;
using KorzUtils.Helper;
using Modding;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ArcadeKnight;

public class ArcadeKnight : Mod, ILocalSettings<LocalSaveData>, IGlobalSettings<GlobalSaveData>, IMenuMod
{
    #region Properties

    public static Dictionary<string, GameObject> PreloadedObjects { get; set; } = [];

    public bool ToggleButtonInsideMenu => false;

    public List<IMenuMod.MenuEntry> GetMenuData(IMenuMod.MenuEntry? toggleButtonEntry)
        =>
        [
            new("Disable Preview", ["False", "True"], "Disables the preview before the minigame.", x => MinigameController.GlobalSettings.DisablePreview = x == 1, () =>
                MinigameController.GlobalSettings.DisablePreview ? 1 : 0),
            new("Disable Practice", ["False", "True"], "Disables the practice before the minigame.", x => MinigameController.GlobalSettings.DisablePractice = x == 1, () =>
                MinigameController.GlobalSettings.DisablePractice ? 1 : 0)
        ];

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
        ("Fungus1_22", "Metal Gate"),
        ("Crossroads_01", "_Transition Gates/door1"),
        ("Cliffs_02", "Cave Spikes (14)"),
        ("Abyss_20", "Dream Dialogue"),
        ("Ruins_Bathhouse", "Ghost NPC/Idle Pt"),
    };

    public override string GetVersion() => "0.2.0.0";

    public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
    {
        PreloadedObjects.Add("Start", preloadedObjects["Dream_01_False_Knight"]["Dream Entry"]);
        PreloadedObjects.Add("Tablet", preloadedObjects["GG_Workshop"]["GG_Statue_Vengefly/Inspect"]);
        PreloadedObjects.Add("ExitTrigger", preloadedObjects["Dream_01_False_Knight"]["Dream Fall Catcher"]);
        PreloadedObjects.Add("Platform", preloadedObjects["Crossroads_04"]["_Scenery/plat_float_01"]);
        PreloadedObjects.Add("CancelDoor", preloadedObjects["White_Palace_03_hub"]["doorWarp"]);
        PreloadedObjects.Add("Wingmould", preloadedObjects["White_Palace_06"]["White Palace Fly (3)"]);
        PreloadedObjects.Add("Block", preloadedObjects["Fungus1_31"]["_Scenery/fung_plat_float_02"]);
        PreloadedObjects.Add("Gate", preloadedObjects["Fungus1_22"]["Metal Gate"]);
        PreloadedObjects.Add("Switch", preloadedObjects["Fungus1_22"]["Gate Switch"]);
        PreloadedObjects.Add("Door", preloadedObjects["Crossroads_01"]["_Transition Gates/door1"]);
        PreloadedObjects.Add("Dream Impact", preloadedObjects["Abyss_20"]["Dream Dialogue"]);
        PreloadedObjects.Add("Dream Effect", preloadedObjects["Ruins_Bathhouse"]["Ghost NPC/Idle Pt"]);
        PreloadedObjects["Door"].transform.position = new(0f, 0f);
        GameObject spikes = new("Spikes");
        GameObject.DontDestroyOnLoad(spikes);
        spikes.SetActive(false);
        spikes.AddComponent<TinkEffect>().blockEffect = preloadedObjects["Cliffs_02"]["Cave Spikes (14)"].GetComponent<TinkEffect>().blockEffect;
        spikes.AddComponent<SpriteRenderer>().sprite = SpriteHelper.CreateSprite<ArcadeKnight>("Sprites.Spikes");
        DamageHero damageHero = spikes.AddComponent<DamageHero>();
        damageHero.damageDealt = 1;
        damageHero.hazardType = 2;
        spikes.layer = 17;
        spikes.AddComponent<BoxCollider2D>().size = new(1.3f, 1f);
        spikes.GetComponent<BoxCollider2D>().isTrigger = true;
        PreloadedObjects.Add("Spikes", spikes);
        GameObject.Destroy(preloadedObjects["Cliffs_02"]["Cave Spikes (14)"]);
        MinigameController.Initialize();
        //On.HeroController.CanDreamNail += (x, y) => true;
    }

    public void OnLoadGlobal(GlobalSaveData globalSaveData) => MinigameController.GlobalSettings = globalSaveData ?? new();

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
                LogHelper.Write("Couldn't find course \"" + recordData.CourseName + "\" in minigame \"" + minigame.GetMinigameType().ToString() + "\". Entry will still be saved.", KorzUtils.Enums.LogType.Normal, false);
                continue;
            }
            courseMetaData.EasyCourse.Highscore = recordData.EasyHighscore;
            courseMetaData.NormalCourse.Highscore = recordData.NormalHighscore;
            courseMetaData.HardCourse.Highscore = recordData.HardHighscore;
        }
    }

    public GlobalSaveData OnSaveGlobal() => MinigameController.GlobalSettings ?? new();

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
    // Preview und Übung überspringbar machen

    // Readme schreiben
}