using ArcadeKnight.Components;
using ArcadeKnight.Enums;
using HutongGames.PlayMaker.Actions;
using KorzUtils.Helper;
using Modding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static BossStatue;

namespace ArcadeKnight;

public class ArcadeKnight : Mod
{
    #region Members

    protected static GameObject _tracker;
    private static GameObject _dreamGate;
    private static GameObject _trackerContainer;

    #endregion

    #region Properties

    public GameObject Tablet { get; set; }

    public GameObject Control { get; set; }

    public GameObject Exit { get; set; }

    public GameObject Platform { get; set; }

    public GameObject CancelDoor { get; set; }

    public GameObject Wingmold { get; set; }

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

    public static MinigameState State { get; set; }

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
        Tablet = preloadedObjects["GG_Workshop"]["GG_Statue_Vengefly/Inspect"];
        Control = preloadedObjects["Dream_01_False_Knight"]["Dream Entry"];
        Exit = preloadedObjects["Dream_01_False_Knight"]["Dream Fall Catcher"];
        Platform = preloadedObjects["Crossroads_04"]["_Scenery/plat_float_01"];
        CancelDoor = preloadedObjects["White_Palace_03_hub"]["doorWarp"];
        Wingmold = preloadedObjects["White_Palace_06"]["White Palace Fly (3)"];

        UnityEngine.SceneManagement.SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
        On.BossChallengeUI.Setup += BossChallengeUI_Setup;
        On.CameraController.LockToArea += CameraController_LockToArea;
        ModHooks.LanguageGetHook += ModHooks_LanguageGetHook;
        ModHooks.GetPlayerBoolHook += ModHooks_GetPlayerBoolHook;
        ModHooks.GetPlayerIntHook += ModHooks_GetPlayerIntHook;
        On.HeroController.FixedUpdate += HeroController_FixedUpdate;
        ModHooks.GetPlayerVariableHook += ModHooks_GetPlayerVariableHook;
    }

    private object ModHooks_GetPlayerVariableHook(Type type, string name, object value)
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

    private int ModHooks_GetPlayerIntHook(string name, int orig)
    {
        if (name == "MinigameCounter")
            return 20;
        return orig;
    }

    private bool ModHooks_GetPlayerBoolHook(string name, bool orig)
    {
        if (name == "canDash")
            return orig && (int)HeroController.instance.hero_state > 2;
        return orig;
    }

    private int lastState = -1;

    private void HeroController_FixedUpdate(On.HeroController.orig_FixedUpdate orig, HeroController self)
    {
        if ((int)HeroController.instance.hero_state < 3 && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Cliffs_02"
            && HeroController.instance.acceptingInput && State == MinigameState.Active)
        {

            if (lastState > 2)
                HeroController.instance.StartCoroutine(UpdateProgression());
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
        lastState = (int)self.hero_state;
    }

    private void CameraController_LockToArea(On.CameraController.orig_LockToArea orig, CameraController self, CameraLockArea lockArea)
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Cliffs_02")
            orig(self, lockArea);
    }

    private string ModHooks_LanguageGetHook(string key, string sheetTitle, string orig)
    {
        if (key == "GorbChallenge")
            orig = "Gorbs Parcour";
        else if (key == "GorbChallengeDesc")
            orig = "Reach the other side while touching the ground as few as possible.";
        return orig;
    }

    private void BossChallengeUI_Setup(On.BossChallengeUI.orig_Setup orig, BossChallengeUI self, BossStatue bossStatue, string bossNameSheet, string bossNameKey, string descriptionSheet, string descriptionKey)
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

    private void SceneManager_activeSceneChanged(UnityEngine.SceneManagement.Scene arg0, UnityEngine.SceneManagement.Scene arg1)
    {
        if (arg1.name == "Cliffs_02")
        {
            Tracker.SetActive(true);
            GameObject entryPoint = new("door_dreamEnter");
            entryPoint.transform.position = new(4.0938f, 24.4081f);
            TransitionPoint transitionPoint = entryPoint.AddComponent<TransitionPoint>();
            transitionPoint.isADoor = true;
            transitionPoint.entryPoint = "door_dreamEnter";
            transitionPoint.respawnMarker = entryPoint.AddComponent<HazardRespawnMarker>();

            GameObject entry = GameObject.Instantiate(Control);
            entry.SetActive(false);
            entry.transform.position = new(4.0938f, 24.4081f);
            entry.LocateMyFSM("Control").FsmVariables.FindFsmString("Door Entry").Value = "door_dreamEnter";
            entry.SetActive(true);
            entryPoint = new("door_dreamExit");
            entryPoint.transform.position = new(55.07f, 34.91f);
            transitionPoint = entryPoint.AddComponent<TransitionPoint>();
            transitionPoint.isADoor = true;
            transitionPoint.entryPoint = "door_dreamExit";
            transitionPoint.targetScene = "";
            transitionPoint.respawnMarker = entryPoint.AddComponent<HazardRespawnMarker>();

            //212.89 28.4

            GameObject platform = GameObject.Instantiate(Platform);
            platform.transform.position = new(222.89f, 28.4f);
            platform.SetActive(true);

            GameObject exit = GameObject.Instantiate(Exit);
            exit.transform.position = new(222.9f, 29.8f);
            exit.SetActive(false);

            PlayMakerFSM fsm = exit.LocateMyFSM("Control");
            fsm.AddState(new(fsm.Fsm)
            {
                Name = "Idle",
                Actions = []
            });
            fsm.GetState("Pause").AdjustTransitions("Idle");
            fsm.GetState("Idle").AddTransition("FALL", "Fade Out");
            fsm.FsmVariables.FindFsmString("Return Scene").Value = "Cliffs_02";
            fsm.FsmVariables.FindFsmString("Return Door").Value = "door_dreamExit";
            PlayerData.instance.dreamReturnScene = "Cliffs_02";
            exit.AddComponent<FinishTrigger>();
            exit.SetActive(true);

            // I can't delete the child somehow... Well, then we resort to other solutions.
            exit.transform.GetChild(0).transform.position = new Vector3(-1000f, -1000f);

            GameObject teleporterSprite = GameObject.Instantiate(DreamGate);
            teleporterSprite.transform.position = new(2.5f, 23.1f);
            GameObject teleporter = GameObject.Instantiate(CancelDoor);
            teleporter.name = "Cancel";
            teleporter.transform.position = new(2.5f, 23.8f);
            teleporter.SetActive(true);
            teleporter.transform.localScale = new(0.5f, 1f, 1f);

            PlayMakerFSM teleportFsm = teleporter.GetComponent<PlayMakerFSM>();
            teleportFsm.FsmVariables.FindFsmString("Entry Gate").Value = "right1";
            teleportFsm.FsmVariables.FindFsmString("New Scene").Value = "Cliffs_01";
            teleportFsm.GetState("Change Scene").GetFirstAction<BeginSceneTransition>().preventCameraFadeOut = true;

            CoroutineHelper.WaitUntil(() =>
            {
                // Easy only
                //GameObject.Destroy(GameObject.Find("plat_float_01"));
                //GameObject.Destroy(GameObject.Find("plat_float_01 (1)"));
                //GameObject.Destroy(GameObject.Find("plat_float_02"));
                //GameObject.Destroy(GameObject.Find("plat_float_03"));
                //GameObject.Destroy(GameObject.Find("plat_float_05"));
                // Hard only
                foreach (TinkEffect spikes in GameObject.FindObjectsOfType<TinkEffect>().Where(obj => obj.name.Contains("Cave Spikes")))
                {
                    spikes.gameObject.AddComponent<NonBouncer>();
                }
                foreach (TransitionPoint transition in TransitionPoint.TransitionPoints)
                    if (!transition.name.Contains("dream") && transition.name != "Cancel")
                    {
                        GameObject collider = new("Entry Blocker");
                        collider.AddComponent<BoxCollider2D>().size = transition.GetComponent<BoxCollider2D>().size;
                        collider.AddComponent<BoxCollider2D>().isTrigger = true;
                        collider.AddComponent<RespawnZone>();
                        collider.transform.position = transition.transform.position;
                        collider.SetActive(true);
                        transition.gameObject.SetActive(false);
                    }
                foreach (HazardRespawnTrigger item in Component.FindObjectsOfType<HazardRespawnTrigger>())
                    item.gameObject.SetActive(false);
            }, HeroController.instance.CanInput, false);

            //HeroController.instance.StartCoroutine(PreviewCourse());

            //GameObject courseObject = GameObject.Instantiate(Wingmold);
            //courseObject.transform.position = new(56.66f, 6.4f);
            //courseObject.SetActive(true);

            //courseObject = GameObject.Instantiate(Wingmold);
            //courseObject.transform.position = new(116.04f, 31.4f);
            //courseObject.SetActive(true);

            // Normal only
            //courseObject = GameObject.Instantiate(Wingmold);
            //courseObject.transform.position = new(199.25f, 28.41f);
            //courseObject.SetActive(true);

            //courseObject = GameObject.Instantiate(Wingmold);
            //courseObject.transform.position = new(178.02f, 23.41f);
            //courseObject.SetActive(true);

            //courseObject = GameObject.Instantiate(Wingmold);
            //courseObject.transform.position = new(215.43f, 28.41f);
            //courseObject.SetActive(true);

            // Easy only

            //courseObject = GameObject.Instantiate(Wingmold);
            //courseObject.transform.position = new(35.52f, 11.4f);
            //courseObject.SetActive(true);

            //courseObject = GameObject.Instantiate(Wingmold);
            //courseObject.transform.position = new(89.64f, 6.4f);
            //courseObject.SetActive(true);

            //courseObject = GameObject.Instantiate(Wingmold);
            //courseObject.transform.position = new(129.59f, 32.86f);
            //courseObject.SetActive(true);

            //courseObject = GameObject.Instantiate(Platform);
            //courseObject.transform.position = new(124.49f, 36f);
            //courseObject.transform.SetRotationZ(270f);
            //courseObject.SetActive(true);

            //courseObject = GameObject.Instantiate(Platform);
            //courseObject.transform.position = new(224.49f, 35.3f);
            //courseObject.transform.SetRotationZ(90f);
            //courseObject.SetActive(true);

            //GameObject plaque = new("Ability Blocker");
            //plaque.transform.position = new(121.2908f, 32.5081f, 0.02f);
            //plaque.transform.localScale = new(2f, 2f, 1f);
            //plaque.AddComponent<SpriteRenderer>().sprite = SpriteHelper.CreateSprite<ArcadeKnight>("Sprites/Allow_Superdash");
            //plaque.SetActive(true);

            // Hard only
            GameObject plaque = new("Ability Blocker 1");
            plaque.SetActive(false);
            plaque.transform.position = new(11.06f, 22.41f, 0.02f);
            plaque.transform.localScale = new(2f, 2f, 1f);
            plaque.AddComponent<SpriteRenderer>().sprite = SpriteHelper.CreateSprite<ArcadeKnight>("Sprites/No_Double_Jump");
            AbilityController controller = plaque.AddComponent<AbilityController>();
            controller.AffectedFieldName = "hasDoubleJump";
            controller.SetValue = false;
            controller.RevertDirection = CheckDirection.Left;
            plaque.SetActive(true);

            plaque = new("Ability Blocker 2");
            plaque.SetActive(false);
            plaque.transform.position = new(80.15f, 10.3f, 0.02f);
            plaque.transform.localScale = new(2f, 2f, 1f);
            plaque.AddComponent<SpriteRenderer>().sprite = SpriteHelper.CreateSprite<ArcadeKnight>("Sprites/No_Double_Jump");
            controller = plaque.AddComponent<AbilityController>();
            controller.AffectedFieldName = "hasDoubleJump";
            controller.SetValue = false;
            controller.RevertDirection = CheckDirection.Left;
            plaque.SetActive(true);

            plaque = new("Ability Blocker 3");
            plaque.SetActive(false);
            plaque.transform.position = new(61.54f, 7.4f, 0.02f);
            plaque.transform.localScale = new(2f, 2f, 1f);
            plaque.AddComponent<SpriteRenderer>().sprite = SpriteHelper.CreateSprite<ArcadeKnight>("Sprites/Allow_Double_Jump");
            controller = plaque.AddComponent<AbilityController>();
            controller.AffectedFieldName = "hasDoubleJump";
            controller.SetValue = true;
            controller.RevertDirection = CheckDirection.Left;
            plaque.SetActive(true);

            plaque = new("Ability Blocker 4");
            plaque.SetActive(false);
            plaque.transform.position = new(198.79f, 29.4f, 0.02f);
            plaque.transform.localScale = new(2f, 2f, 1f);
            plaque.AddComponent<SpriteRenderer>().sprite = SpriteHelper.CreateSprite<ArcadeKnight>("Sprites/Allow_Double_Jump");
            controller = plaque.AddComponent<AbilityController>();
            controller.AffectedFieldName = "hasDoubleJump";
            controller.SetValue = true;
            controller.RevertDirection = CheckDirection.Left;
            plaque.SetActive(true);

            plaque = new("Ability Blocker 4");
            plaque.SetActive(false);
            plaque.transform.position = new(16.04f, 18.4f, 0.02f);
            plaque.transform.localScale = new(2f, 2f, 1f);
            plaque.AddComponent<SpriteRenderer>().sprite = SpriteHelper.CreateSprite<ArcadeKnight>("Sprites/No_Pogo_Spikes");
            plaque.SetActive(true);

            plaque = new("Ability Blocker 5");
            plaque.SetActive(false);
            plaque.transform.position = new(6.77f, 25.4f, 0.02f);
            plaque.transform.localScale = new(2f, 2f, 1f);
            plaque.AddComponent<SpriteRenderer>().sprite = SpriteHelper.CreateSprite<ArcadeKnight>("Sprites/No_Superdash");
            plaque.SetActive(true);

            // Allow double jump at: 61.54f, 6.4f and 198.79f 28.4f
            // Block double jump at 11.06f, 21.41f and 79.15f, 9.3f
            // Spike sign 16.04f, 17.4f
        }
        else if (arg1.name == "Tutorial_01")
        {
            GameObject tablet = GameObject.Instantiate(Tablet);
            tablet.SetActive(true);
            tablet.transform.position = new(42.26f, 11.9f);
            PlayMakerFSM fsm = tablet.LocateMyFSM("GG Boss UI");
            fsm.FsmVariables.FindFsmString("Boss Name Key").Value = "GorbChallenge";
            fsm.FsmVariables.FindFsmString("Description Key").Value = "GorbChallengeDesc";
            fsm.GetState("Reset Player").AddActions(() => State = MinigameState.Active);
            fsm.GetState("Open UI").AddActions(() => HeroController.instance.StartCoroutine(ControlSelection()));
            fsm.GetState("Close UI").AddActions(() => HeroController.instance.StopCoroutine("ControlSelection"));
            fsm.GetState("Take Control").AddActions(() => HeroController.instance.StopCoroutine("ControlSelection"));


        }
    }

    private IEnumerator ControlSelection()
    {
        int level = 1;
        Transform panel = Component.FindObjectOfType<BossChallengeUI>().transform.Find("Panel");
        panel.Find("BossName_Text").position = new Vector3(7.4f, 4f);
        panel.Find("Description_Text").position = new Vector3(7.5f, 2.9f);
        GameObject levelObject = GameObject.Instantiate(panel.Find("Description_Text").gameObject, panel);
        levelObject.transform.position = new Vector3(10.6f, 1.51f);
        Text textObject = levelObject.GetComponent<Text>();
        textObject.fontSize++;
        textObject.text = "COURSE " + level;
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
            item.text = "HIGHSCORE:\r\n01:20:20.20";
        bool changed = false;
        while (true)
        {
            if (panel == null)
                yield break;

            if (InputHandler.Instance.inputActions.left.WasPressed)
            {
                changed = true;
                level--;
                textObject.text = "COURSE " + level;
            }
            else if (InputHandler.Instance.inputActions.right.WasPressed)
            {
                changed = true;
                level++;
                textObject.text = "COURSE " + level;
            }
            if (changed)
            {
                changed = false;
                foreach ((GameObject, GameObject) images in sprites)
                {
                    images.Item2.SetActive(level % 2 == 0);
                    images.Item1.SetActive(level % 2 == 1);
                }
            }
            yield return null;
        }
    }

    private IEnumerator PreviewCourse()
    {
        yield return new WaitUntil(HeroController.instance.CanInput);

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
        Vector3[] positions = [
            new Vector3(49.58f, 9.6f, z),
            new Vector3(86.45f, 9.6f, z),
            new Vector3(115.78f, 32.76f, z),
            new Vector3(137.54f, 30.9f, z),
            new Vector3(161.9f, 22.24f, z),
            new Vector3(174.03f, 28.95f, z),
            new Vector3(222.89f, 28.4f, z),
        ];
        HeroController.instance.RelinquishControl();

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
    }

    private int i = 0;

    internal IEnumerator UpdateProgression()
    {
        i++;
        if (GameManager.instance?.IsGameplayScene() == true)
        {
            TextMeshPro currentCounter = _tracker.GetComponent<TextMeshPro>();
            currentCounter.text = i.ToString();
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

    // ToDo:
    // Zwei weitere Kurse
    // Custom Kurs Support
    // Zweites Minispiel

    
}