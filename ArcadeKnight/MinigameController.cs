using ArcadeKnight.Components;
using ArcadeKnight.Enums;
using ArcadeKnight.Minigames;
using HutongGames.PlayMaker.Actions;
using KorzUtils.Helper;
using Modding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static BossStatue;

namespace ArcadeKnight;

public static class MinigameController
{
    #region Members

    private static GameObject _tracker;

    private static GameObject _dreamGate;

    #endregion

    #region Properties

    public static MinigameState CurrentState { get; set; }

    public static GameObject Tracker
    {
        get
        {
            if (_tracker == null)
            {
                GameObject hudCanvas = GameObject.Find("_GameCameras").transform.Find("HudCamera/Hud Canvas").gameObject;
                GameObject prefab = GameObject.Find("_GameCameras").transform.Find("HudCamera/Inventory/Inv/Inv_Items/Geo").transform.GetChild(0).gameObject;
                _tracker = GameObject.Instantiate(prefab, hudCanvas.transform, true);
                _tracker.name = "Tracker";
                _tracker.transform.position = new(0f, 6.5f, 1f);
                _tracker.transform.localScale = new(4f, 4f, 1f);
                TextMeshPro textElement = _tracker.GetComponent<TextMeshPro>();
                textElement.fontSize = 3;
                textElement.alignment = TextAlignmentOptions.Center;
                textElement.text = "0";
            }
            return _tracker;
        }
    }

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

    public static int SelectedLevel { get; set; } = 0;

    public static Difficulty SelectedDifficulty { get; set; }

    public static Minigame ActiveMinigame { get; set; }

    public static List<Minigame> Minigames { get; set; } = [];

    public static List<RecordData> UnassignableRecordData { get; set; } = [];

    #endregion

    #region Constructors

    static MinigameController()
    {
        Minigames.Add(new GorbsParkour()
        {
            Courses = NormalCourses.GorbCourses
        });

        //string customCourseDirectory = Path.Combine(Path.GetDirectoryName(typeof(ArcadeKnight).Assembly.Location), "CustomCourses");
    }

    #endregion

    #region Methods

    public static void Initialize()
    {
        UnityEngine.SceneManagement.SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
        ModHooks.AfterPlayerDeadHook += ModHooks_AfterPlayerDeadHook;
        On.UIManager.ReturnToMainMenu += UIManager_ReturnToMainMenu;
        On.BossChallengeUI.LoadBoss_int_bool += BossChallengeUI_LoadBoss_int_bool;
        ModHooks.GetPlayerVariableHook += ModHooks_GetPlayerVariableHook;
        On.CameraController.LockToArea += CameraController_LockToArea;
        On.BossChallengeUI.Setup += BossChallengeUI_Setup;
        ModHooks.LanguageGetHook += ModHooks_LanguageGetHook;
    }

    public static void EndMinigame()
    {
        CurrentState = MinigameState.Inactive;
        SelectedLevel = 0;
        ActiveMinigame?.Conclude();
        ActiveMinigame = null;
        Tracker.SetActive(false);
        _tracker.transform.position = new(0f, 6.5f, 1f);
        _tracker.transform.localScale = new(4f, 4f, 1f);
        if (PlayerData.instance != null)
        { 
            PDHelper.DisablePause = false;
            PDHelper.IsInvincible = false;
        }
    }

    private static void SetupLevel(CourseData course)
    {
        CreateStart(new Vector3(course.StartPositionX, course.StartPositionY), course.StartPositionX > course.EndPositionX);
        CreateEnd(new Vector3(course.EndPositionX, course.EndPositionY));
        CreateObstacles(course.Obstacles);
        Tracker.GetComponent<TextMeshPro>().text = "0";
        Tracker.SetActive(false);

        CoroutineHelper.WaitUntil(() =>
        {
            if (course.ObjectsToRemove.Any())
            {
                GameObject[] gameObjects = GameObject.FindObjectsOfType<GameObject>();
                foreach (string objectToRemove in course.ObjectsToRemove)
                {
                    bool found = false;
                    foreach (GameObject gameObject in gameObjects)
                        if (gameObject.name == objectToRemove)
                        {
                            GameObject.Destroy(gameObject);
                            found = true;
                            break;
                        }
                    if (!found)
                        LogHelper.Write<ArcadeKnight>("Requested object to delete \"" + objectToRemove + "\" could not be found.", KorzUtils.Enums.LogType.Warning, false);
                }
            }

            // Special rule
            if (ActiveMinigame.GetTitle() == "Gorbs Parkour" && ActiveMinigame.Courses[SelectedLevel].Name == "Cliffhanger" && SelectedDifficulty == Difficulty.Hard)
            {
                foreach (TinkEffect spikes in GameObject.FindObjectsOfType<TinkEffect>().Where(obj => obj.name.Contains("Cave Spikes")))
                    spikes.gameObject.AddComponent<NonBouncer>();

                GameObject plaque = new("No Pogo Spike Sign");
                plaque.SetActive(false);
                plaque.transform.position = new(16.04f, 18.4f, 0.02f);
                plaque.transform.localScale = new(2f, 2f, 1f);
                plaque.AddComponent<SpriteRenderer>().sprite = SpriteHelper.CreateSprite<ArcadeKnight>("Sprites/No_Pogo_Spikes");
                plaque.SetActive(true);
            }

            // Disable transitions and replace them with hazard respawns.
            foreach (TransitionPoint transition in TransitionPoint.TransitionPoints)
                if (!transition.name.Contains("minigame") && transition.name != "Cancel")
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
            foreach (HazardRespawnTrigger item in Component.FindObjectsOfType<HazardRespawnTrigger>())
                item.gameObject.SetActive(false);

            List<(float, float)> previewPoints = [];
            if (course.PreviewPoints.Any())
                previewPoints.AddRange(course.PreviewPoints);
            previewPoints.Add(new(course.EndPositionX, course.EndPositionY));
            HeroController.instance.StartCoroutine(PreviewCourse(previewPoints));
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

        GameObject entry = GameObject.Instantiate(ArcadeKnight.PreloadedObjects["Start"]);
        entry.SetActive(false);
        entry.transform.position = position;
        entry.LocateMyFSM("Control").FsmVariables.FindFsmString("Door Entry").Value = "minigame_start";
        entry.LocateMyFSM("Control").FsmVariables.FindFsmBool("Hero Faces left").Value = facingLeft;
        entry.SetActive(true);
    }

    private static void CreateEnd(Vector3 position)
    {
        GameObject platform = GameObject.Instantiate(ArcadeKnight.PreloadedObjects["Platform"]);
        platform.transform.position = position;
        platform.SetActive(true);

        GameObject exit = GameObject.Instantiate(ArcadeKnight.PreloadedObjects["ExitTrigger"]);
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

    private static GameObject CreateExitPoint()
    {
        float height = HeroController.instance.transform.position.y - HeroController.instance.GetComponent<BoxCollider2D>().size.y / 2;

        GameObject teleporterSprite = GameObject.Instantiate(DreamGate);
        teleporterSprite.transform.position = new Vector3(HeroController.instance.transform.position.x, height - 0.7f);
        teleporterSprite.SetActive(true);

        GameObject teleporter = GameObject.Instantiate(ArcadeKnight.PreloadedObjects["CancelDoor"]);
        teleporter.name = "Cancel";
        teleporter.transform.position = new Vector3(HeroController.instance.transform.position.x, height);
        teleporter.SetActive(true);
        teleporter.transform.localScale = new(0.5f, 1f, 1f);

        PlayMakerFSM teleportFsm = teleporter.GetComponent<PlayMakerFSM>();
        teleportFsm.FsmVariables.FindFsmString("Entry Gate").Value = "minigame_exit";
        teleportFsm.FsmVariables.FindFsmString("New Scene").Value = ActiveMinigame.GetEntryScene();
        teleportFsm.GetState("Change Scene").GetFirstAction<BeginSceneTransition>().preventCameraFadeOut = true;
        teleportFsm.GetState("Audio Snapshots").AddActions(EndMinigame);
        return teleporterSprite;
    }

    private static void CreateObstacles(Obstacle[] obstacles)
    {
        foreach (Obstacle obstacle in obstacles)
            if (obstacle is CourseObstacle courseObstacle)
            {
                GameObject courseObject = GameObject.Instantiate(courseObstacle.ObjectName == "Platform" ? ArcadeKnight.PreloadedObjects["Platform"] : ArcadeKnight.PreloadedObjects["Wingmold"]);
                courseObject.transform.position = new(courseObstacle.XPosition, courseObstacle.YPosition);
                courseObject.transform.SetRotationZ(courseObstacle.Rotation);
                courseObject.SetActive(true);
            }
            else if (obstacle is AbilityModifier sign)
            {
                string spriteName = sign.AffectedAbility switch
                {
                    "hasDoubleJump" => "Double_Jump",
                    "hasSuperDash" => "Superdash",
                    _ => "Error"
                };
                if (spriteName == "Error")
                {
                    LogHelper.Write<ArcadeKnight>("Couldn't assign ability \"" + sign.AffectedAbility + "\".", KorzUtils.Enums.LogType.Error, false);
                    continue;
                }
                if (sign.SetValue)
                    spriteName = "Allow_" + spriteName;
                else
                    spriteName = "No_" + spriteName;

                GameObject plaque = new("Ability Blocker");
                plaque.SetActive(false);
                plaque.transform.position = new(sign.XPosition, sign.YPosition, 0.02f);
                plaque.transform.localScale = new(2f, 2f, 1f);
                plaque.transform.SetRotationZ(sign.Rotation);
                plaque.AddComponent<SpriteRenderer>().sprite = SpriteHelper.CreateSprite<ArcadeKnight>("Sprites/" + spriteName);
                AbilityRestrictor controller = plaque.AddComponent<AbilityRestrictor>();
                controller.AffectedFieldName = sign.AffectedAbility;
                controller.SetValue = sign.SetValue;
                controller.RevertDirection = sign.RevertDirection;
                plaque.SetActive(true);
            }
    }

    internal static IEnumerator UpdateProgression(int newProgression)
    {
        if (GameManager.instance?.IsGameplayScene() == true)
        {
            TextMeshPro currentCounter = _tracker.GetComponent<TextMeshPro>();
            currentCounter.text = newProgression.ToString();
            Vector3 normalScale = currentCounter.transform.localScale;
            float currentColorLevel = 1f;
            float passedTime = 0f;
            while (passedTime < 0.25f)
            {
                yield return null;
                passedTime += Time.deltaTime;
                currentColorLevel = Mathf.Max(0, currentColorLevel - Time.deltaTime * 4);
                currentCounter.transform.localScale += new Vector3(Time.deltaTime * 2, Time.deltaTime * 2);
                currentCounter.color = new(1f, currentColorLevel, currentColorLevel);
            }
            passedTime = 0f;
            currentColorLevel = 0f;
            while (passedTime < 0.25f)
            {
                yield return null;
                currentColorLevel = Mathf.Max(1, currentColorLevel + Time.deltaTime * 4);
                passedTime += Time.deltaTime;
                currentCounter.transform.localScale -= new Vector3(Time.deltaTime * 2, Time.deltaTime * 2);
                currentCounter.color = new(1f, currentColorLevel, currentColorLevel);
            }
            currentCounter.color = new(1f, 1f, 1f);
            currentCounter.transform.localScale = normalScale;
        }
    }

    private static IEnumerator ControlSelection()
    {
        Transform panel = Component.FindObjectOfType<BossChallengeUI>().transform.Find("Panel");
        panel.Find("BossName_Text").position = new Vector3(7.4f, 4f);
        panel.Find("Description_Text").position = new Vector3(7.5f, 2.9f);
        GameObject levelObject = GameObject.Instantiate(panel.Find("Description_Text").gameObject, panel);
        levelObject.transform.position = new Vector3(10.6f, 1.51f);
        Text textObject = levelObject.GetComponent<Text>();
        textObject.fontSize++;
        textObject.text = "CLIFFHANGER"; // Name of the first normal course.
        List<Text> highscoreText = [];
        List<(GameObject, GameObject)> sprites = [];
        for (int i = 1; i < 4; i++)
        {
            Transform parent = panel.Find("Buttons").Find($"Tier{i}Button");
            parent.Find("Text").GetComponent<Text>().text = i switch
            {
                1 => "EASY",
                2 => "NORMAL",
                _ => "HARD"
            };
            sprites.Add(new(parent.Find("NotchImage").gameObject, parent.Find("SymbolImage").gameObject));
            GameObject highscoreObject = GameObject.Instantiate(levelObject, panel.Find("Buttons").Find($"Tier{i}Button"));
            highscoreText.Add(highscoreObject.GetComponent<Text>());
            highscoreObject.transform.localPosition = new(243.3334f, 0f, 0f);
        }
        foreach (Text item in highscoreText)
            item.text = "";
        bool changed = true;
        while (true)
        {
            if (panel == null)
                yield break;

            if (InputHandler.Instance.inputActions.left.WasPressed)
            {
                changed = true;
                SelectedLevel--;
                if (SelectedLevel < 0)
                    SelectedLevel = ActiveMinigame.Courses.Count - 1;
            }
            else if (InputHandler.Instance.inputActions.right.WasPressed)
            {
                changed = true;
                SelectedLevel++;
                if (SelectedLevel >= ActiveMinigame.Courses.Count)
                    SelectedLevel = 0;
            }
            if (changed)
            {
                changed = false;
                textObject.text = ActiveMinigame.Courses[SelectedLevel].Name?.ToUpper();
                sprites[0].Item1.SetActive(string.IsNullOrEmpty(ActiveMinigame.Courses[SelectedLevel].EasyCourse.Highscore));
                sprites[0].Item2.SetActive(!string.IsNullOrEmpty(ActiveMinigame.Courses[SelectedLevel].EasyCourse.Highscore));
                sprites[1].Item1.SetActive(string.IsNullOrEmpty(ActiveMinigame.Courses[SelectedLevel].NormalCourse.Highscore));
                sprites[1].Item2.SetActive(!string.IsNullOrEmpty(ActiveMinigame.Courses[SelectedLevel].NormalCourse.Highscore));
                sprites[2].Item1.SetActive(string.IsNullOrEmpty(ActiveMinigame.Courses[SelectedLevel].HardCourse.Highscore));
                sprites[2].Item2.SetActive(!string.IsNullOrEmpty(ActiveMinigame.Courses[SelectedLevel].HardCourse.Highscore));
                if (!string.IsNullOrEmpty(ActiveMinigame.Courses[SelectedLevel].EasyCourse.Highscore))
                    highscoreText[0].text = "HIGHSCORE:\r\n" + ActiveMinigame.Courses[SelectedLevel].EasyCourse.Highscore;
                else
                    highscoreText[0].text = "";

                if (!string.IsNullOrEmpty(ActiveMinigame.Courses[SelectedLevel].NormalCourse.Highscore))
                    highscoreText[1].text = "HIGHSCORE:\r\n" + ActiveMinigame.Courses[SelectedLevel].NormalCourse.Highscore;
                else
                    highscoreText[1].text = "";

                if (!string.IsNullOrEmpty(ActiveMinigame.Courses[SelectedLevel].HardCourse.Highscore))
                    highscoreText[2].text = "HIGHSCORE:\r\n" + ActiveMinigame.Courses[SelectedLevel].HardCourse.Highscore;
                else
                    highscoreText[2].text = "";
            }
            yield return null;
        }
    }

    private static IEnumerator PreviewCourse(List<(float, float)> coordinates)
    {
        HeroController.instance.RelinquishControl();
        PDHelper.DisablePause = true;
        CameraController controller = GameObject.FindObjectOfType<CameraController>();
        CameraTarget oldTarget = controller.camTarget;
        GameObject movingObject = new("Preview");
        movingObject.transform.position = HeroController.instance.transform.position;
        CameraTarget target = movingObject.AddComponent<CameraTarget>();
        target.mode = CameraTarget.TargetMode.FREE;
        target.cameraCtrl = controller;
        controller.camTarget = target;

        float z = controller.transform.position.z;
        Vector3[] positions = [.. coordinates.Select(x => new Vector3(x.Item1, x.Item2, z))];

        Transform vignette = HeroController.instance.transform.Find("Vignette");
        int currentPositionIndex = 0;
        while (true)
        {
            vignette.localScale += new Vector3(21f * Time.deltaTime, 21f * Time.deltaTime, 21f * Time.deltaTime);
            movingObject.transform.position = Vector3.MoveTowards(movingObject.transform.position, positions[currentPositionIndex], Time.deltaTime * 20);
            if (Vector3.Distance(movingObject.transform.position, positions[currentPositionIndex]) < 1)
            {
                currentPositionIndex++;
                if (currentPositionIndex == positions.Length)
                    break;
            }
            yield return null;
        }
        yield return new WaitForSeconds(2f);
        CreateExitPoint();
        while (true)
        {
            vignette.localScale -= new Vector3(50f * Time.deltaTime, 50f * Time.deltaTime, 50f * Time.deltaTime);
            if (vignette.localScale.x < 5.5f)
                vignette.localScale = new Vector3(5.5f, 5.5f, 5.5f);
            movingObject.transform.position = Vector3.MoveTowards(movingObject.transform.position, HeroController.instance.transform.position, Time.deltaTime * 40);
            if (Vector3.Distance(movingObject.transform.position, HeroController.instance.transform.position) < 1)
                break;

            yield return null;
        }

        HeroController.instance.transform.Find("Vignette").localScale = new(5.5f, 5.5f, 5.5f);
        controller.camTarget = oldTarget;
        GameObject.Destroy(movingObject);
        HeroController.instance.RegainControl();
        PDHelper.DisablePause = false;
        Tracker.SetActive(true);
        ActiveMinigame.Start();
    }

    #endregion

    #region Eventhandler

    private static void SceneManager_activeSceneChanged(UnityEngine.SceneManagement.Scene oldScene, UnityEngine.SceneManagement.Scene newScene)
    {
        if (Minigames.FirstOrDefault(x => x.GetEntryScene() == newScene.name) is Minigame minigame && CurrentState == MinigameState.Inactive)
        {
            ActiveMinigame = minigame;
            GameObject tablet = GameObject.Instantiate(ArcadeKnight.PreloadedObjects["Tablet"]);
            tablet.SetActive(true);
            tablet.transform.position = minigame.GetEntryPosition();
            PlayMakerFSM fsm = tablet.LocateMyFSM("GG Boss UI");
            fsm.FsmVariables.FindFsmString("Boss Name Key").Value = "MinigameTitle";
            fsm.FsmVariables.FindFsmString("Description Key").Value = "MinigameDesc";
            fsm.GetState("Reset Player").AddActions(() => CurrentState = MinigameState.Active);
            fsm.GetState("Open UI").AddActions(() => HeroController.instance.StartCoroutine(ControlSelection()));
            fsm.GetState("Close UI").AddActions(() => HeroController.instance.StopCoroutine("ControlSelection"));
            fsm.GetState("Take Control").AddActions(() => HeroController.instance.StopCoroutine("ControlSelection"));
            fsm.GetState("Change Scene").GetFirstAction<BeginSceneTransition>().entryGateName.Value = "minigame_start";

            GameObject entryPoint = new("minigame_exit");
            entryPoint.transform.position = minigame.GetEntryPosition();
            TransitionPoint transitionPoint = entryPoint.AddComponent<TransitionPoint>();
            transitionPoint.isADoor = true;
            transitionPoint.entryPoint = "minigame_exit";
            transitionPoint.targetScene = "";
            transitionPoint.respawnMarker = entryPoint.AddComponent<HazardRespawnMarker>();

        }
        else if (CurrentState == MinigameState.Active)
            SetupLevel(SelectedDifficulty switch
            {
                Difficulty.Easy => ActiveMinigame.Courses[SelectedLevel].EasyCourse,
                Difficulty.Hard => ActiveMinigame.Courses[SelectedLevel].HardCourse,
                _ => ActiveMinigame.Courses[SelectedLevel].NormalCourse
            });
        else
            ActiveMinigame = null;
        if (newScene.name == "Cliffs_02")
            GameObject.Find("Inspect Region Ghost").SetActive(false);
    }

    private static void ModHooks_AfterPlayerDeadHook() => EndMinigame();

    private static IEnumerator UIManager_ReturnToMainMenu(On.UIManager.orig_ReturnToMainMenu orig, UIManager self)
    {
        EndMinigame();
        return orig(self);
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

    private static void CameraController_LockToArea(On.CameraController.orig_LockToArea orig, CameraController self, CameraLockArea lockArea)
    {
        if (CurrentState != MinigameState.Active)
            orig(self, lockArea);
    }

    private static void BossChallengeUI_LoadBoss_int_bool(On.BossChallengeUI.orig_LoadBoss_int_bool orig, BossChallengeUI self, int level, bool doHideAnim)
    {
        if (!UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("GG"))
        {
            SelectedDifficulty = (Difficulty)level;
            FieldInfo info = typeof(BossChallengeUI).GetField("bossStatue", BindingFlags.NonPublic | BindingFlags.Instance);
            BossStatue statue = info.GetValue(self) as BossStatue;
            statue.bossScene.sceneName = ActiveMinigame.Courses[SelectedLevel].Scene;
            CurrentState = MinigameState.Active;
        }
        orig(self, level, doHideAnim);
    }

    private static string ModHooks_LanguageGetHook(string key, string sheetTitle, string orig)
    {
        if (key == "MinigameTitle")
            orig = ActiveMinigame?.GetTitle()?.ToUpper();
        else if (key == "MinigameDesc")
            orig = ActiveMinigame?.GetDescription();
        return orig;
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
                    sceneName = "Cliffs_02"
                },
                hasNoTiers = false,
                dreamReturnGate = new()
            };

        }
        orig(self, bossStatue, bossNameSheet, bossNameKey, descriptionSheet, descriptionKey);
    }

    #endregion
}
