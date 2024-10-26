using ArcadeKnight.Enums;
using KorzUtils.Helper;
using Modding;
using System;
using UnityEngine;

namespace ArcadeKnight.Minigames;

internal class GorbsParkour : Minigame
{
    #region Members

    private int _lastState = -1;
    private int _score = 0;

    #endregion

    #region Methods

    internal override Vector3 GetEntryPosition() => new(55.04f, 34.4f);

    internal override string GetDescription() => "Reach the goal while touching the ground as few times as possible.";

    internal override string GetEntryScene() => "Cliffs_02";

    internal override string GetTitle() => "Gorbs Parkour";

    internal override void Start()
    {
        _score = 0;
        _lastState = -1;
        ModHooks.GetPlayerBoolHook += ModHooks_GetPlayerBoolHook;
        On.HeroController.FixedUpdate += HeroController_FixedUpdate;
    }

    internal override void Conclude()
    {
        HeroController.instance.RUN_SPEED = 8.3f;
        HeroController.instance.WALK_SPEED = 6f;
        HeroController.instance.RUN_SPEED_CH = 10f;
        HeroController.instance.RUN_SPEED_CH_COMBO = 11.5f;
        _score = 0;
        _lastState = -1;
        ModHooks.GetPlayerBoolHook -= ModHooks_GetPlayerBoolHook;
        On.HeroController.FixedUpdate -= HeroController_FixedUpdate;
    }

    internal override bool CheckHighscore(Difficulty difficulty, int level)
    {
        if (_score >= 0)
        {
            CourseData runCourse = difficulty switch
            {
                Difficulty.Easy => Courses[level].EasyCourse,
                Difficulty.Hard => Courses[level].HardCourse,
                _ => Courses[level].NormalCourse,
            };
            if (string.IsNullOrEmpty(runCourse.Highscore) || Convert.ToInt32(runCourse.Highscore) > Convert.ToInt32(_score))
            { 
                runCourse.Highscore = _score.ToString();
                return true;
            }
        }
        return false;
    }

    #endregion

    private bool ModHooks_GetPlayerBoolHook(string name, bool orig)
    {
        if (name == "canDash")
            return (int)HeroController.instance.hero_state > 2;
        return orig;
    }

    private void HeroController_FixedUpdate(On.HeroController.orig_FixedUpdate orig, HeroController self)
    {
        if ((int)HeroController.instance.hero_state < 3 && HeroController.instance.acceptingInput 
            && MinigameController.CurrentState == MinigameState.Active)
        {
            if (_lastState > 2)
            {
                _score++;
                HeroController.instance.StartCoroutine(MinigameController.UpdateProgression(_score));
            }
            self.RUN_SPEED = 0f;
            self.WALK_SPEED = 0f;
            self.RUN_SPEED_CH = 0f;
            self.RUN_SPEED_CH_COMBO = 0f;
        }
        else
        {
            self.RUN_SPEED = 8.3f;
            self.WALK_SPEED = 6f;
            self.RUN_SPEED_CH = 10f;
            self.RUN_SPEED_CH_COMBO = 11.5f;
        }

        orig(self);
        _lastState = (int)self.hero_state;
    }
}
