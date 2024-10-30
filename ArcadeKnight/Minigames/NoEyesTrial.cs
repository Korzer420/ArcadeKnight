using ArcadeKnight.Enums;
using ArcadeKnight.Extensions;
using KorzUtils.Helper;
using System;
using UnityEngine;

namespace ArcadeKnight.Minigames;

public class NoEyesTrial : TimeMinigame
{
    private GameObject _viewBlocker;

    public GameObject ViewBlocker
    {
        get
        {
            if (_viewBlocker == null)
            {
                Sprite darknessSprite = HeroController.instance.transform.Find("Vignette/Darkness Border/black_solid").GetComponent<SpriteRenderer>().sprite;
                _viewBlocker = new("View Blocker");
                _viewBlocker.AddComponent<SpriteRenderer>().sprite = darknessSprite;
                _viewBlocker.GetComponent<SpriteRenderer>().sortingOrder = 1;
                _viewBlocker.transform.localScale = new Vector3(5000f, 5000f, 5000f);
            }
            return _viewBlocker;
        }
    }
    
    internal override bool CheckHighscore(CourseData courseData)
    {
        if (string.IsNullOrEmpty(courseData.Highscore))
        {
            courseData.Highscore = TimeSpan.FromSeconds(_passedTime).ToFormat("mm:ss.ff");
            return true;
        }
        else if (TimeSpan.TryParse(courseData.Highscore, out TimeSpan highscore))
        {
            TimeSpan currentScore = TimeSpan.FromSeconds(_passedTime);
            if (currentScore < highscore)
            {
                courseData.Highscore = currentScore.ToFormat("mm:ss.ff");
                return true;
            }
        }
        else
            LogHelper.Write<ArcadeKnight>("Highscore data corrupted.", KorzUtils.Enums.LogType.Error);
        return false;
    }

    protected override void Conclude()
    {
        if (_viewBlocker != null)
            GameObject.Destroy(_viewBlocker);
        On.HutongGames.PlayMaker.Actions.SetVector3XYZ.DoSetVector3XYZ -= SetVector3XYZ_DoSetVector3XYZ;
        _passedTime = 0f;
    }

    internal override string GetDescription() => "Reach the goal while the room is shrouded in darkness.";

    internal override Vector3 GetEntryPosition() => new(55.04f, 34.4f);

    internal override string GetEntryScene() => "Cliffs_02";

    internal override MinigameType GetMinigameType() => MinigameType.NoEyesTrial;

    internal override string GetTitle() => "No Eyes Trial";

    protected override void Start()
    {
        On.HutongGames.PlayMaker.Actions.SetVector3XYZ.DoSetVector3XYZ += SetVector3XYZ_DoSetVector3XYZ;
        if (MinigameController.SelectedDifficulty == Difficulty.Hard)
            ViewBlocker.SetActive(true);
        HeroController.instance.vignetteFSM.SendEvent("SCENE RESET");
        base.Start();
    }

    internal override string GetCourseFile() => "TrialCourses";

    internal override void AdditionalEntranceSetup() => GameObject.Find("Inspect Region Ghost").SetActive(false);

    private void SetVector3XYZ_DoSetVector3XYZ(On.HutongGames.PlayMaker.Actions.SetVector3XYZ.orig_DoSetVector3XYZ orig, HutongGames.PlayMaker.Actions.SetVector3XYZ self)
    {
        orig(self);
        if (self.IsCorrectContext("Darkness Control", "Vignette", null))
        {
            float scale = MinigameController.SelectedDifficulty switch
            {
                Difficulty.Easy => 1.1f,
                Difficulty.Normal => 0.3f,
                _ => self.x.Value
            };
            self.vector3Variable.Value = new(scale, scale, scale);
        }
    }

    internal override bool HasPracticeMode() => true;

    protected override int TimePenaltyFactor() => MinigameController.SelectedDifficulty switch
    {
        Difficulty.Easy => 5,
        Difficulty.Hard => 15,
        _ => 10,
    };
}
