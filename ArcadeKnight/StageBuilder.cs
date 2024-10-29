﻿using ArcadeKnight.Components;
using ArcadeKnight.Enums;
using ArcadeKnight.Obstacles;
using HutongGames.PlayMaker.Actions;
using KorzUtils.Helper;
using Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using static BossStatue;
using LogType = KorzUtils.Enums.LogType;
using Object = UnityEngine.Object;

namespace ArcadeKnight;

public static class StageBuilder
{
    #region Members

    private static GameObject _dreamGate;

    #endregion

    #region Properties

    public static Minigame ActiveMinigame => MinigameController.ActiveMinigame;

    public static Difficulty SelectedDifficulty => MinigameController.SelectedDifficulty;

    public static int SelectedLevel => MinigameController.SelectedLevel;

    public static GameObject DreamGate
    {
        get
        {
            if (_dreamGate == null)
                _dreamGate = HeroController.instance.gameObject.LocateMyFSM("Dream Nail")
                    .GetState("Spawn Gate")
                    .GetFirstAction<SpawnObjectFromGlobalPool>().gameObject.Value;
            return _dreamGate;
        }
    }

    #endregion

    #region Methods

    internal static void Initialize()
    {
        On.BossChallengeUI.LoadBoss_int_bool += BossChallengeUI_LoadBoss_int_bool;
        ModHooks.GetPlayerVariableHook += ModHooks_GetPlayerVariableHook;
        On.BossChallengeUI.Setup += BossChallengeUI_Setup;
    }

    internal static void CreateMinigameEntry()
    {
        GameObject tablet = Object.Instantiate(ArcadeKnight.PreloadedObjects["Tablet"]);
        tablet.SetActive(true);
        tablet.transform.position = MinigameController.ActiveMinigame.GetEntryPosition();
        PlayMakerFSM fsm = tablet.LocateMyFSM("GG Boss UI");
        fsm.FsmVariables.FindFsmString("Boss Name Key").Value = "MinigameTitle";
        fsm.FsmVariables.FindFsmString("Description Key").Value = "MinigameDesc";
        fsm.GetState("Reset Player").AddActions(() => MinigameController.CurrentState = MinigameState.Active);
        fsm.GetState("Open UI").AddActions(() => HeroController.instance.StartCoroutine(MinigameController.ControlSelection()));
        fsm.GetState("Close UI").AddActions(() => HeroController.instance.StopCoroutine("ControlSelection"));
        fsm.GetState("Take Control").AddActions(() => HeroController.instance.StopCoroutine("ControlSelection"));
        fsm.GetState("Change Scene").GetFirstAction<BeginSceneTransition>().entryGateName.Value = "minigame_start";

        GameObject entryPoint = new("minigame_exit");
        entryPoint.transform.position = MinigameController.ActiveMinigame.GetEntryPosition();
        TransitionPoint transitionPoint = entryPoint.AddComponent<TransitionPoint>();
        transitionPoint.isADoor = true;
        transitionPoint.dontWalkOutOfDoor = true;
        transitionPoint.entryPoint = "minigame_exit";
        transitionPoint.targetScene = "";
        transitionPoint.respawnMarker = entryPoint.AddComponent<HazardRespawnMarker>();
        CoroutineHelper.WaitFrames(MinigameController.ActiveMinigame.AdditionalEntranceSetup, false);
    }

    internal static void SetupLevel(CourseData course)
    {
        AbilityController.Enable(course.Restrictions);
        PlayerData.instance.isInvincible = true;
        CreateStart(new Vector3(course.StartPositionX, course.StartPositionY), course.StartPositionX > course.EndPositionX);
        CreateEnd(new Vector3(course.EndPositionX, course.EndPositionY));
        CreateObstacles(course.Obstacles);
        
        MinigameController.Tracker.GetComponent<TextMeshPro>().text = "0";
        MinigameController.Tracker.SetActive(false);

        CoroutineHelper.WaitFrames(() =>
        {
            if (course.ObjectsToRemove.Any())
            {
                GameObject[] gameObjects = Object.FindObjectsOfType<GameObject>();
                foreach (string objectToRemove in course.ObjectsToRemove)
                {
                    bool found = false;
                    foreach (GameObject gameObject in gameObjects)
                        if (gameObject.name == objectToRemove)
                        {
                            Object.Destroy(gameObject);
                            found = true;
                            break;
                        }
                    if (!found)
                        LogHelper.Write<ArcadeKnight>("Requested object to delete \"" + objectToRemove + "\" could not be found.", LogType.Warning, false);
                }
            }
            // Special rule
            if (ActiveMinigame.GetTitle() == "Gorbs Parkour" && ActiveMinigame.Courses[SelectedLevel].Name == "Cliffhanger" && SelectedDifficulty == Difficulty.Hard)
            {
                GameObject plaque = new("No Pogo Spike Sign");
                plaque.SetActive(false);
                plaque.transform.position = new(16.04f, 18.4f, 0.02f);
                plaque.transform.localScale = new(2f, 2f, 1f);
                plaque.AddComponent<SpriteRenderer>().sprite = SpriteHelper.CreateSprite<ArcadeKnight>("Sprites/No_Pogo_Spikes");
                plaque.SetActive(true);
            }
        }, false);

        CoroutineHelper.WaitUntil(() =>
        {
            // Special rule
            if (ActiveMinigame.GetTitle() == "Gorbs Parkour" && ActiveMinigame.Courses[SelectedLevel].Name == "Cliffhanger" && SelectedDifficulty == Difficulty.Hard)
            {
                foreach (TinkEffect spikes in Object.FindObjectsOfType<TinkEffect>().Where(obj => obj.name.Contains("Cave Spikes")))
                    spikes.gameObject.AddComponent<NonBouncer>();
            }

            // Disable transitions and replace them with hazard respawns.
            foreach (TransitionPoint transition in TransitionPoint.TransitionPoints)
                if (!transition.name.Contains("minigame") && transition.name != "Cancel" && transition.name != "Practice_Gate")
                {
                    GameObject collider = new("Entry Blocker");
                    collider.AddComponent<BoxCollider2D>().size = transition.GetComponent<BoxCollider2D>().size;
                    collider.AddComponent<BoxCollider2D>().isTrigger = true;
                    collider.AddComponent<RespawnZone>();
                    collider.transform.position = transition.transform.position;
                    collider.SetActive(true);
                    transition.gameObject.SetActive(false);
                }
            // Disable all Hazard Respawn point, so the player always spawns at the start of the minigame.
            foreach (HazardRespawnTrigger item in Object.FindObjectsOfType<HazardRespawnTrigger>())
                item.gameObject.SetActive(false);

            List<(float, float)> previewPoints = [];
            if (course.PreviewPoints.Any())
                previewPoints.AddRange(course.PreviewPoints);
            previewPoints.Add(new(course.EndPositionX, course.EndPositionY));
            HeroController.instance.StartCoroutine(MinigameController.PreviewCourse(previewPoints));
        }, HeroController.instance.CanInput, false);
    }

    private static void CreateStart(Vector3 position, bool facingLeft)
    {
        GameObject entryPoint = new("minigame_start");
        entryPoint.transform.position = position;
        TransitionPoint transitionPoint = entryPoint.AddComponent<TransitionPoint>();
        transitionPoint.isADoor = true;
        transitionPoint.entryPoint = "minigame_start";
        transitionPoint.respawnMarker = entryPoint.AddComponent<HazardRespawnMarker>();

        GameObject entry = Object.Instantiate(ArcadeKnight.PreloadedObjects["Start"]);
        entry.SetActive(false);
        entry.transform.position = position;
        entry.LocateMyFSM("Control").FsmVariables.FindFsmString("Door Entry").Value = "minigame_start";
        entry.LocateMyFSM("Control").FsmVariables.FindFsmBool("Hero Faces left").Value = facingLeft;
        entry.SetActive(true);
    }

    private static void CreateEnd(Vector3 position)
    {
        GameObject platform = Object.Instantiate(ArcadeKnight.PreloadedObjects["Platform"]);
        platform.transform.position = position;
        platform.SetActive(true);

        GameObject exit = Object.Instantiate(ArcadeKnight.PreloadedObjects["ExitTrigger"]);
        exit.transform.position = position + new Vector3(0.1f, 1.5f);
        exit.SetActive(false);

        PlayMakerFSM fsm = exit.LocateMyFSM("Control");
        fsm.AddState(new(fsm.Fsm)
        {
            Name = "Idle",
            Actions = []
        });
        fsm.GetState("Pause").AdjustTransitions("Idle");
        fsm.GetState("Idle").AddTransition("FALL", "Fade Out");
        fsm.FsmVariables.FindFsmString("Return Scene").Value = ActiveMinigame.GetEntryScene();
        fsm.FsmVariables.FindFsmString("Return Door").Value = "minigame_exit";
        PlayerData.instance.dreamReturnScene = ActiveMinigame.GetEntryScene();
        exit.AddComponent<FinishTrigger>();
        exit.SetActive(true);

        // I can't delete the child somehow... Well, then we resort to other solutions.
        exit.transform.GetChild(0).transform.position = new Vector3(-1000f, -1000f);
    }

    internal static GameObject CreateExitPoint()
    {
        float height = HeroController.instance.transform.position.y - HeroController.instance.GetComponent<BoxCollider2D>().size.y / 2;

        GameObject teleporterSprite = Object.Instantiate(DreamGate);
        teleporterSprite.transform.position = new Vector3(HeroController.instance.transform.position.x, height - 0.7f);
        teleporterSprite.SetActive(true);

        GameObject teleporter = Object.Instantiate(ArcadeKnight.PreloadedObjects["CancelDoor"]);
        teleporter.name = "Cancel";
        teleporter.transform.position = new Vector3(HeroController.instance.transform.position.x, height);
        teleporter.SetActive(true);
        teleporter.transform.localScale = new(0.5f, 1f, 1f);

        PlayMakerFSM teleportFsm = teleporter.GetComponent<PlayMakerFSM>();
        teleportFsm.FsmVariables.FindFsmString("Entry Gate").Value = "minigame_exit";
        teleportFsm.FsmVariables.FindFsmString("New Scene").Value = ActiveMinigame.GetEntryScene();
        teleportFsm.GetState("Change Scene").GetFirstAction<BeginSceneTransition>().preventCameraFadeOut = true;
        teleportFsm.GetState("Audio Snapshots").AddActions(MinigameController.EndMinigame);
        return teleporterSprite;
    }

    private static void CreateObstacles(Obstacle[] obstacles)
    {
        foreach (Obstacle obstacle in obstacles)
        {
            GameObject obstacleGameObject;
            if (obstacle is CourseObstacle courseObstacle)
                obstacleGameObject = Object.Instantiate(ArcadeKnight.PreloadedObjects[courseObstacle.ObjectName]);
            else if (obstacle is RestrictObstacle sign)
            {
                string spriteName = sign.AffectedAbility switch
                {
                    "hasDoubleJump" => "Double_Jump",
                    "hasSuperDash" => "Superdash",
                    "hasWalljump" => "Wall_Jump",
                    "canDash" => "Dash",
                    "damagePenalty" => "Damage_Penalty",
                    _ => "Error"
                };
                if (spriteName == "Error")
                {
                    LogHelper.Write<ArcadeKnight>("Couldn't assign ability \"" + sign.AffectedAbility + "\".", LogType.Error, false);
                    continue;
                }
                if (sign.SetValue)
                    spriteName = "Allow_" + spriteName;
                else
                    spriteName = "No_" + spriteName;

                obstacleGameObject = new("Ability Blocker");
                obstacleGameObject.SetActive(false);
                obstacleGameObject.transform.position = new(sign.XPosition, sign.YPosition, 0.02f);
                obstacleGameObject.transform.localScale = new(2f, 2f, 1f);
                obstacleGameObject.AddComponent<SpriteRenderer>().sprite = SpriteHelper.CreateSprite<ArcadeKnight>("Sprites/" + spriteName);
                AbilityRestrictor controller = obstacleGameObject.AddComponent<AbilityRestrictor>();
                controller.AffectedFieldName = sign.AffectedAbility;
                controller.SetValue = sign.SetValue;
                controller.RevertDirection = sign.RevertDirection;
                obstacleGameObject.SetActive(true);
                continue;
            }
            else if (obstacle is GateObstacle gateObstacle)
            {
                obstacleGameObject = Object.Instantiate(ArcadeKnight.PreloadedObjects["Switch"]);
                obstacleGameObject.SetActive(false);
                PlayMakerFSM fsm = obstacleGameObject.LocateMyFSM("Switch Control");
                fsm.GetState("Initiate").AdjustTransitions("Idle");
                fsm.GetState("Idle").AdjustTransitions("Hit");

                GameObject gate = Object.Instantiate(ArcadeKnight.PreloadedObjects["Gate"]);
                gate.transform.position = new(gateObstacle.GateXPosition, gateObstacle.GateYPosition);
                gate.transform.SetRotationZ(gateObstacle.GateRotation);
                gate.SetActive(true);
                fsm.FsmVariables.FindFsmGameObject("Target").Value = gate;
            }
            else
            {
                LogHelper.Write<ArcadeKnight>("Not a obstacle");
                continue;
            }
            obstacleGameObject.transform.position = new(obstacle.XPosition, obstacle.YPosition);
            obstacleGameObject.transform.SetRotationZ(obstacle.Rotation);
            obstacleGameObject.SetActive(true);
        }
    }

    private static void CreatePracticeDoor(Vector3 position)
    {
        GameObject door = GameObject.Instantiate(ArcadeKnight.PreloadedObjects["Door"]);
        door.name = "Practice_Gate";
        door.transform.position = position;
        door.SetActive(true);
        PlayMakerFSM fsm = door.LocateMyFSM("Door Control");
        fsm.FsmVariables.FindFsmString("New Scene").Value = MinigameController.ActiveMinigame.Courses[MinigameController.SelectedLevel].Scene;
        fsm.FsmVariables.FindFsmBool("Crossroads Ascent").Value = false;
        fsm.FsmVariables.FindFsmString("Entry Gate").Value = "minigame_start";
    }

    #endregion

    #region Eventhandler

    private static void BossChallengeUI_LoadBoss_int_bool(On.BossChallengeUI.orig_LoadBoss_int_bool orig, BossChallengeUI self, int level, bool doHideAnim)
    {
        if (!UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("GG"))
        {
            MinigameController.SelectedDifficulty = (Difficulty)level;
            FieldInfo info = typeof(BossChallengeUI).GetField("bossStatue", BindingFlags.NonPublic | BindingFlags.Instance);
            BossStatue statue = info.GetValue(self) as BossStatue;
            statue.bossScene.sceneName = ActiveMinigame.Courses[SelectedLevel].Scene;
        }
        orig(self, level, doHideAnim);
    }

    private static object ModHooks_GetPlayerVariableHook(Type type, string name, object value)
    {
        if (name == "ArcadeKnightDummy")
            return new Completion()
            {
                completedTier2 = true,
                seenTier3Unlock = true,
                isUnlocked = true,
                hasBeenSeen = true,
            };
        return value;
    }

    private static void BossChallengeUI_Setup(On.BossChallengeUI.orig_Setup orig, BossChallengeUI self, BossStatue bossStatue, string bossNameSheet, string bossNameKey, string descriptionSheet, string descriptionKey)
    {
        if (bossStatue == null)
        {
            bossStatue = new BossStatue
            {
                statueStatePD = "ArcadeKnightDummy",
                bossScene = new()
                {
                    sceneName = "Will be overwritten anyway"
                },
                hasNoTiers = false,
                dreamReturnGate = new()
            };

        }
        orig(self, bossStatue, bossNameSheet, bossNameKey, descriptionSheet, descriptionKey);
    }

    #endregion
}
