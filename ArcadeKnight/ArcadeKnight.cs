using ArcadeKnight.Components;
using ArcadeKnight.Enums;
using KorzUtils.Helper;
using Modding;
using System.Collections.Generic;
using UnityEngine;

namespace ArcadeKnight;

public class ArcadeKnight : Mod
{
    #region Members

    

    #endregion

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
        ("White_Palace_06", "White Palace Fly (3)")
    };

    public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
    {
        PreloadedObjects.Add("Start", preloadedObjects["Dream_01_False_Knight"]["Dream Entry"]);
        PreloadedObjects.Add("Tablet",preloadedObjects["GG_Workshop"]["GG_Statue_Vengefly/Inspect"]);
        PreloadedObjects.Add("ExitTrigger", preloadedObjects["Dream_01_False_Knight"]["Dream Fall Catcher"]);
        PreloadedObjects.Add("Platform", preloadedObjects["Crossroads_04"]["_Scenery/plat_float_01"]);
        PreloadedObjects.Add("CancelDoor", preloadedObjects["White_Palace_03_hub"]["doorWarp"]);
        PreloadedObjects.Add("Wingmold", preloadedObjects["White_Palace_06"]["White Palace Fly (3)"]);

        MinigameController.Initialize();
    }

    // ToDo:
    // Zwei weitere Kurse
    // Custom Kurs Support
    // Zweites Minispiel

    
}