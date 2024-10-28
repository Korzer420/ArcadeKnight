using ArcadeKnight.Enums;
using KorzUtils.Helper;
using System;
using UnityEngine;

namespace ArcadeKnight.Minigames;

public class NoEyesTrial : Minigame
{
    private TimeSpan passedTime;

    private int _penalties;

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

    internal override void ApplyScorePenalty()
    {
        _penalties++;
    }

    internal override bool CheckHighscore(Difficulty difficulty, int level)
    {
        return false;
    }

    internal override void Conclude()
    {
        if (_viewBlocker != null)
            GameObject.Destroy(_viewBlocker);
        On.HutongGames.PlayMaker.Actions.SetVector3XYZ.DoSetVector3XYZ -= SetVector3XYZ_DoSetVector3XYZ;
        passedTime = TimeSpan.FromMilliseconds(0);
        _penalties = 0;
    }

    internal override string GetDescription() => "Reach the goal while the room is shrouded in darkness.";

    internal override Vector3 GetEntryPosition() => new(39.29f, 12.4f);

    internal override string GetEntryScene() => "Tutorial_01";

    internal override MinigameType GetMinigameType() => MinigameType.NoEyesTrial;

    internal override string GetTitle() => "No Eyes Trial";

    internal override void Start()
    {
        On.HutongGames.PlayMaker.Actions.SetVector3XYZ.DoSetVector3XYZ += SetVector3XYZ_DoSetVector3XYZ;
        MinigameController.Tracker.SetActive(false);
        GameHelper.DisplayMessage("Enter the dream gate to quit practice.");
    }

    private void SetVector3XYZ_DoSetVector3XYZ(On.HutongGames.PlayMaker.Actions.SetVector3XYZ.orig_DoSetVector3XYZ orig, HutongGames.PlayMaker.Actions.SetVector3XYZ self)
    {
        orig(self);
        //if (!MinigameController.PracticeMode && self.IsCorrectContext("Darkness Control", "Vignette", null))
        //{
        //    float scale = MinigameController.SelectedDifficulty switch
        //    {
        //        Difficulty.Easy => 1.1f,
        //        Difficulty.Normal => 0.8f,
        //        _ => self.x.Value
        //    };
        //    self.vector3Variable.Value = new(scale, scale, scale);
        //}
    }

    internal override bool HasPracticeMode() => true;
}
