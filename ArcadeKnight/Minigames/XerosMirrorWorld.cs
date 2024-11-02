using ArcadeKnight.Components;
using ArcadeKnight.Enums;
using ArcadeKnight.Extensions;
using ArcadeKnight.Obstacles;
using HutongGames.PlayMaker.Actions;
using KorzUtils.Data;
using KorzUtils.Helper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ArcadeKnight.Minigames;

public class XerosMirrorWorld : TimeMinigame
{
    #region Members

    private string[] _spriteNames =
    [
        "Crystal_Dash",
        "Cyclone_Slash",
        "Dash_Slash",
        "Desolate_Dive",
        "Focus",
        "Great_Slash",
        "Howling_Wraiths",
        "Ismas_Tear",
        "Mantis_Claw",
        "Monarch_Wings",
        "Mothwing_Cloak",
        "Vengeful_Spirit"
    ];

    private int _imposterCount = 0;

    private int _wrongPenalties = 0;

    #endregion

    #region Properties

    internal List<(string, bool)> Imposter { get; set; } = [];

    public List<bool> ImposterFlags { get; set; } = [];

    #endregion

    internal override bool CheckHighscore(CourseData courseData)
    {
        // Hours are not supported.
        if (_passedTime >= 3600)
            _passedTime = 3599;
        if (string.IsNullOrEmpty(courseData.Highscore))
        {
            courseData.Highscore = TimeSpan.FromSeconds(_passedTime).ToFormat("mm:ss.ff");
            return true;
        }
        else if (TimeSpan.TryParseExact(courseData.Highscore, @"mm\:ss\.ff", CultureInfo.InvariantCulture, out TimeSpan highscore))
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

    internal override string GetCourseFile() => "MirrorCourses";

    internal override string GetDescription() => "Remember the room and then dream nail all parts that changed.";

    internal override Vector3 GetEntryPosition() => new(98.05f, 12.4f);

    internal override string GetEntryScene() => "RestingGrounds_02";

    internal override MinigameType GetMinigameType() => MinigameType.XerosMirrorWorld;

    internal override string GetTitle() => "Xeros Mirror World";

    internal override bool HasPracticeMode() => true;

    protected override void Start()
    {
        _wrongPenalties = 0;
        base.Start();
        HeroController.instance.gameObject.LocateMyFSM("Dream Nail").GetState("Take Control").GetFirstAction<SendMessage>().Enabled = false;
        On.HeroController.CanDreamNail += HeroController_CanDreamNail;
        On.PlayMakerFSM.OnEnable += PlayMakerFSM_OnEnable;
        List<Obstacle> obstacles = MinigameController.ActiveCourse.Obstacles.Where(x => x is ImposterObstacle).ToList();
        for (int i = 0; i < obstacles.Count; i++)
        {
            GameObject dreamImpact = GameObject.Instantiate(ArcadeKnight.PreloadedObjects["Dream Impact"]);
            dreamImpact.name = "Xero Mirror Impact " + i;
            dreamImpact.SetActive(false);
            dreamImpact.transform.position = new(obstacles[i].XPosition, obstacles[i].YPosition);
            dreamImpact.SetActive(true);
        }
        ImposterFlags = Imposter.Select(x => false).ToList();
    }

    protected override void Conclude()
    {
        base.Conclude();
        HeroController.instance.gameObject.LocateMyFSM("Dream Nail").GetState("Take Control").GetFirstAction<SendMessage>().Enabled = true;
        On.HeroController.CanDreamNail -= HeroController_CanDreamNail;
        On.PlayMakerFSM.OnEnable -= PlayMakerFSM_OnEnable;
        Imposter.Clear();
    }

    protected override int TimePenaltyFactor() => MinigameController.SelectedDifficulty switch
    {
        Difficulty.Easy => 10,
        Difficulty.Hard => 30,
        _ => 20
    };

    internal void SetupImposter(ImposterObstacle obstacle)
    {
        if (MinigameController.PracticeMode)
        {
            string spriteName = _spriteNames[Random.Range(0, _spriteNames.Length)];
            GameObject obstacleGameObject = new("Sign");
            obstacleGameObject.SetActive(false);
            obstacleGameObject.transform.position = new(obstacle.XPosition, obstacle.YPosition, 0.02f);
            obstacleGameObject.transform.localScale = new(2f, 2f, 1f);
            obstacleGameObject.transform.SetRotationZ(obstacle.Rotation);
            obstacleGameObject.AddComponent<SpriteRenderer>().sprite = SpriteHelper.CreateSprite<ArcadeKnight>("Sprites.Imposter_Sign");

            GameObject abilitySprite = new("Ability Sprite");
            abilitySprite.transform.SetParent(obstacleGameObject.transform);
            abilitySprite.transform.localPosition = new(0f, -0.1f, -0.01f);
            abilitySprite.transform.localScale = new(.28f, .28f);
            abilitySprite.AddComponent<SpriteRenderer>().sprite = SpriteHelper.CreateSprite<ArcadeKnight>("Sprites.Abilities." + spriteName);
            abilitySprite.SetActive(true);
            obstacleGameObject.SetActive(true);
            Imposter.Add(new(spriteName, !obstacle.AlwaysReal));
        }
        else
        {
            string spriteName = Imposter[_imposterCount].Item1;
            GameObject obstacleGameObject = new("Sign");
            obstacleGameObject.SetActive(false);
            obstacleGameObject.transform.position = new(obstacle.XPosition, obstacle.YPosition, 0.02f);
            obstacleGameObject.transform.localScale = new(2f, 2f, 1f);
            obstacleGameObject.transform.SetRotationZ(obstacle.Rotation);
            obstacleGameObject.AddComponent<SpriteRenderer>().sprite = SpriteHelper.CreateSprite<ArcadeKnight>("Sprites.Imposter_Sign");

            GameObject abilitySprite = new("Ability Sprite");
            abilitySprite.transform.SetParent(obstacleGameObject.transform);
            abilitySprite.transform.localPosition = new(0f, -0.1f, -0.01f);
            abilitySprite.transform.localScale = new(.28f, .28f);
            abilitySprite.AddComponent<SpriteRenderer>().sprite = SpriteHelper.CreateSprite<ArcadeKnight>("Sprites.Abilities." + spriteName);
            abilitySprite.SetActive(true);

            if (Imposter[_imposterCount].Item2)
            {
                ImposterEffect imposterEffect = SelectEffect();
                switch (imposterEffect)
                {
                    case ImposterEffect.Flip:
                        if (Random.Range(0, 2) == 0 || MinigameController.SelectedDifficulty != Difficulty.Easy)
                            abilitySprite.GetComponent<SpriteRenderer>().flipX = true;
                        else
                            abilitySprite.GetComponent<SpriteRenderer>().flipY = true;
                        break;
                    case ImposterEffect.Rotation:
                        abilitySprite.transform.SetRotationZ(Random.Range(15, 46));
                        break;
                    case ImposterEffect.Color:
                        abilitySprite.GetComponent<SpriteRenderer>().color = new(Random.Range(0.1f, 0.9f), Random.Range(0.1f, 0.9f), Random.Range(0.1f, 0.9f));
                        break;
                    case ImposterEffect.WrongSprite:
                        string[] otherSprites = _spriteNames.Except([spriteName]).ToArray();
                        spriteName = otherSprites[Random.Range(0, otherSprites.Length)];
                        abilitySprite.GetComponent<SpriteRenderer>().sprite = SpriteHelper.CreateSprite<ArcadeKnight>("Sprites.Abilities." + spriteName);
                        break;
                    case ImposterEffect.Scale:
                        float xScale, yScale;
                        if (Random.Range(0, 2) == 0)
                        {
                            xScale = Random.Range(0.15f, 0.24f);
                            yScale = Random.Range(0.15f, 0.24f);
                        }
                        else
                        {
                            xScale = Random.Range(0.4f, 0.7f);
                            yScale = Random.Range(0.4f, 0.7f);
                        }
                        abilitySprite.transform.localScale = new(xScale, yScale);
                        break;
                    default:
                        obstacleGameObject.GetComponent<SpriteRenderer>().flipY = true;
                        break;
                }
            }
            obstacleGameObject.SetActive(true);
            _imposterCount++;
        }
    }

    internal void RollImposter()
    {
        _imposterCount = 0;
        List<int> viableIndex = [];
        for (int i = 0; i < Imposter.Count; i++)
        {
            if (Imposter[i].Item2)
                viableIndex.Add(i);
            Imposter[i] = new(Imposter[i].Item1, false);
        }
        int imposterAmount = MinigameController.SelectedDifficulty switch
        {
            Difficulty.Easy => Random.Range(1, 6),
            Difficulty.Hard => Random.Range(1, 11),
            _ => Random.Range(1, 8)
        };
        for (int i = 0; i < imposterAmount; i++)
        {
            int selectedIndex = Random.Range(0, viableIndex.Count);
            Imposter[viableIndex[selectedIndex]] = new(Imposter[viableIndex[selectedIndex]].Item1, true);
            viableIndex.RemoveAt(selectedIndex);
        }
    }

    private ImposterEffect SelectEffect() => (ImposterEffect)Random.Range(0, MinigameController.SelectedDifficulty == Difficulty.Easy ? 6 : 3);

    public IEnumerator EvaluteResult(FinishTrigger finishTrigger)
    {
        yield return null;
        int wrongAccusedObjects = 0;
        int missedObjects = 0;
        for (int i = 0; i < ImposterFlags.Count; i++)
            if (ImposterFlags[i] && !Imposter[i].Item2)
                wrongAccusedObjects++;
            else if (!ImposterFlags[i] && Imposter[i].Item2)
                missedObjects++;
        PenaltyTimer.transform.position -= new Vector3(0f, 2f);
        TextMeshPro textComponent = PenaltyTimer.GetComponent<TextMeshPro>();
        textComponent.text = "";
        PenaltyTimer.SetActive(true);
        yield return new WaitForSeconds(2f);
        if (wrongAccusedObjects > 0)
            textComponent.text = "<color=#de0404>Wrong accused: " + wrongAccusedObjects + " (+" + wrongAccusedObjects + " Minute(s))</color>\r\n";
        if (missedObjects > 0)
            textComponent.text += "<color=#de0404>Missed: " + missedObjects + " (+" + missedObjects + " Minute(s))</color>";
        yield return new WaitForSeconds(3f);
        AddTimePenalty(60 * wrongAccusedObjects);
        GameObject.Destroy(PenaltyTimer);
        MinigameController.Tracker.GetComponent<TextMeshPro>().text = TimeSpan.FromSeconds(AddTimePenalty(60 * missedObjects)).ToFormat("mm:ss.ff");
        yield return finishTrigger.DisplayScore();
    }

    public bool EvaluteHardModeResult()
    {
        bool allCorrect = true;
        for (int i = 0; i < ImposterFlags.Count; i++)
            if ((ImposterFlags[i] && !Imposter[i].Item2) || (!ImposterFlags[i] && Imposter[i].Item2))
            {
                ApplyScorePenalty();
                allCorrect = false;
            }
        return allCorrect;
    }

    internal override bool HasMandatoryPractice() => true;

    private bool HeroController_CanDreamNail(On.HeroController.orig_CanDreamNail orig, HeroController self) => true;

    private void PlayMakerFSM_OnEnable(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
    {
        if (self.FsmName == "npc_dream_dialogue" && self.gameObject.name.StartsWith("Xero Mirror Impact"))
        {
            int index = int.Parse(self.gameObject.name.Substring("Xero Mirror Impact ".Length));
            self.GetState("Idle").AdjustTransitions("Impact");
            if (MinigameController.SelectedDifficulty == Difficulty.Easy && !Imposter[index].Item2)
            {
                self.AddState("Punish", () =>
                {
                    HeroController.instance.TakeDamage(null, GlobalEnums.CollisionSide.top, 1, 1);
                    ApplyScorePenalty();
                }, FsmTransitionData.FromTargetState("Idle").WithEventName("FINISHED"));
                self.GetState("Impact").AdjustTransitions("Punish");
            }
            else
            {
                self.AddState("Generate Particles", () =>
                {
                    self.transform.Find("Active Pt").GetComponent<ParticleSystem>().enableEmission = !self.transform.Find("Active Pt").GetComponent<ParticleSystem>().enableEmission;
                    ImposterFlags[index] = !ImposterFlags[index];
                    if (MinigameController.SelectedDifficulty == Difficulty.Easy)
                        if (Imposter.Select(x => x.Item2).SequenceEqual(ImposterFlags))
                        {
                            GameHelper.DisplayMessage("Ending unlocked");
                            StageBuilder.FinishLine.SetActive(true);
                        }
                        else
                            StageBuilder.FinishLine.SetActive(false);
                }, FsmTransitionData.FromTargetState("Idle").WithEventName("FINISHED"));
                self.GetState("Impact").AdjustTransitions("Generate Particles");
            }
            self.GetState("Impact").RemoveFirstAction<SetParticleEmission>();
            self.GetState("Impact").RemoveFirstAction<CreateObject>();
            self.GetState("Impact").RemoveLastAction<SendEventByName>();
        }
        orig(self);
    }

    internal override void AdditionalEntranceSetup() => GameObject.Find("Inspect Region Ghost")?.SetActive(false);
}
