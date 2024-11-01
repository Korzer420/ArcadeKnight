using ArcadeKnight.Enums;
using HutongGames.PlayMaker.Actions;
using KorzUtils.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ArcadeKnight.Minigames;

public class XerosMirrorWorld : TimeMinigame
{
    internal override bool CheckHighscore(CourseData runCourse)
    {
        throw new NotImplementedException();
    }

    internal override string GetCourseFile() => "MirrorCourses";

    internal override string GetDescription() => "Remember the room and then dream nail all parts that changed.";

    internal override Vector3 GetEntryPosition() => new(2f, 2f);

    internal override string GetEntryScene() => "RestingGround_02";

    internal override MinigameType GetMinigameType() => MinigameType.XerosMirrorWorld;

    internal override string GetTitle() => "Xeros Mirror World";

    internal override bool HasPracticeMode() => true;

    protected override void Start()
    {
        base.Start();
        HeroController.instance.gameObject.LocateMyFSM("Dream Nail").GetState("Take Control").GetFirstAction<SendMessage>().Enabled = false;
        On.HeroController.CanDreamNail += HeroController_CanDreamNail;
    }

    protected override void Conclude()
    {
        base.Conclude();
        HeroController.instance.gameObject.LocateMyFSM("Dream Nail").GetState("Take Control").GetFirstAction<SendMessage>().Enabled = true;
        On.HeroController.CanDreamNail -= HeroController_CanDreamNail;
    }

    protected override int TimePenaltyFactor() => MinigameController.SelectedDifficulty switch
    {
        Difficulty.Easy => 5,
        Difficulty.Hard => 30,
        _ => 15
    };

    private bool HeroController_CanDreamNail(On.HeroController.orig_CanDreamNail orig, HeroController self) => HeroController.instance.CanInput();

    /*
     if (self.FsmName == "npc_dream_dialogue" && self.gameObject.name == "Impact")
        {
            self.GetState("Idle").AdjustTransitions("Impact");
            self.AddState("Generate Particles", () =>
            {
                GameObject hint = Object.Instantiate(ArcadeKnight.PreloadedObjects["Dream Effect"], self.transform);
                hint.name = "Dream Hint";
                hint.SetActive(true);
                hint.GetComponent<ParticleSystem>().enableEmission = true;
                hint.transform.position = self.transform.position;
                hint.transform.position -= new Vector3(0f, 0f, 3f);
            }, FsmTransitionData.FromTargetState("Idle").WithEventName("FINISHED"));
            self.GetState("Impact").AdjustTransitions("Generate Particles");
            self.GetState("Impact").RemoveActions(5);
            self.GetState("Impact").RemoveActions(0);
        }
     */
}
