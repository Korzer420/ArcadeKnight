﻿using ArcadeKnight.Enums;
using Modding;
using System.Collections.Generic;
using UnityEngine;

namespace ArcadeKnight;

public abstract class Minigame
{
    protected bool _active;

    #region Properties

    public List<CourseMetaData> Courses { get; set; } = [];

    #endregion

    #region Constructors

    protected Minigame() => ModHooks.LanguageGetHook += ModHooks_LanguageGetHook;

    #endregion

    #region Methods

    internal void Begin()
    {
        if (_active)
            return;
        _active = true;
        MinigameController.Tracker.SetActive(true);
        Start();
    }

    internal void End()
    {
        if (!_active)
            return;
        _active = false;
        Conclude();
        // Safety check.
        MinigameController.PracticeMode = false;
    }

    internal abstract string GetTitle();

    internal abstract string GetDescription();

    internal abstract string GetEntryScene();

    internal abstract string GetCourseFile();

    internal abstract Vector3 GetEntryPosition();

    protected abstract void Start();

    protected abstract void Conclude();

    internal abstract MinigameType GetMinigameType();

    internal abstract bool CheckHighscore(CourseData runCourse);

    internal abstract void ApplyScorePenalty(int count = 1);

    internal virtual bool HasPracticeMode() => false;

    internal virtual void AdditionalEntranceSetup() { }

    internal virtual bool HasMandatoryPractice() => false;

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
