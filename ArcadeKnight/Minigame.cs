using ArcadeKnight.Enums;
using Modding;
using System.Collections.Generic;
using UnityEngine;

namespace ArcadeKnight;

public abstract class Minigame
{
    #region Properties

    public List<CourseMetaData> Courses { get; set; } = [];

    #endregion

    #region Constructors

    protected Minigame() => ModHooks.LanguageGetHook += ModHooks_LanguageGetHook;

    #endregion

    #region Methods

    internal abstract string GetTitle();
    
    internal abstract string GetDescription();

    internal abstract string GetEntryScene();

    internal abstract Vector3 GetEntryPosition();

    internal abstract void Start();

    internal abstract void Conclude();

    internal abstract MinigameType GetMinigameType();

    internal abstract bool CheckHighscore(Difficulty difficulty, int level);

    internal abstract void ApplyScorePenalty();

    #endregion

    #region Eventhandler

    private string ModHooks_LanguageGetHook(string key, string sheetTitle, string orig)
    {
        if (key == GetType().Name)
            return GetTitle();
        else if (key == $"{GetType().FullName}_Desc")
            return GetDescription();
        return orig;
    }

    #endregion
}
