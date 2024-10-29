using ArcadeKnight.Components;
using ArcadeKnight.Enums;
using ArcadeKnight.Minigames;
using HutongGames.PlayMaker.Actions;
using KorzUtils.Helper;
using Modding;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static BossStatue;
using LogType = KorzUtils.Enums.LogType;

namespace ArcadeKnight;

public static class MinigameController
{
    #region Members

    private static GameObject _tracker;

    #endregion

    #region Properties

    public static MinigameState CurrentState { get; set; }

    public static bool PlayingPreview { get; set; }

    public static GameObject Tracker
    {
        get
        {
            if (_tracker == null)
            {
                GameObject hudCanvas = GameObject.Find("_GameCameras").transform.Find("HudCamera/Hud Canvas").gameObject;
                GameObject prefab = GameObject.Find("_GameCameras").transform.Find("HudCamera/Inventory/Inv/Inv_Items/Geo").transform.GetChild(0).gameObject;
                _tracker = UnityEngine.Object.Instantiate(prefab, hudCanvas.transform, true);
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

    public static int SelectedLevel { get; set; } = 0;

    public static Difficulty SelectedDifficulty { get; set; }

    public static Minigame ActiveMinigame { get; set; }

    public static List<Minigame> Minigames { get; set; } = [];

    public static List<RecordData> UnassignableRecordData { get; set; } = [];

    public static bool DamagePenaltyActive { get; set; }

    public static bool PracticeMode { get; set; }

    #endregion

    #region Constructors

    static MinigameController()
    {
        Minigames.Add(new GorbsParkour());
        CourseLoader.Load();
    }

    #endregion

    #region Methods

    public static void Initialize()
    {
        UnityEngine.SceneManagement.SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
        ModHooks.AfterPlayerDeadHook += ModHooks_AfterPlayerDeadHook;
        On.UIManager.ReturnToMainMenu += UIManager_ReturnToMainMenu;
        On.CameraController.LockToArea += CameraController_LockToArea;
        ModHooks.LanguageGetHook += ModHooks_LanguageGetHook;
        ModHooks.TakeHealthHook += ModHooks_TakeHealthHook;
        StageBuilder.Initialize();
    }

    public static void EndMinigame()
    {
        CurrentState = MinigameState.Inactive;
        AbilityController.Disable();
        SelectedLevel = 0;
        DamagePenaltyActive = false;
        ActiveMinigame?.End();
        ActiveMinigame = null;
        Tracker.SetActive(false);
        _tracker.transform.position = new(0f, 6.5f, 1f);
        _tracker.transform.localScale = new(4f, 4f, 1f);
        if (PlayerData.instance != null)
            PDHelper.DisablePause = false;
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

    internal static IEnumerator ControlSelection()
    {
        Transform panel = UnityEngine.Object.FindObjectOfType<BossChallengeUI>().transform.Find("Panel");
        panel.Find("BossName_Text").position = new Vector3(7.4f, 4f);
        panel.Find("Description_Text").position = new Vector3(7.5f, 2.9f);
        GameObject levelObject = UnityEngine.Object.Instantiate(panel.Find("Description_Text").gameObject, panel);
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
            GameObject highscoreObject = UnityEngine.Object.Instantiate(levelObject, panel.Find("Buttons").Find($"Tier{i}Button"));
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

    internal static IEnumerator PreviewCourse(List<(float, float)> coordinates)
    {
        HeroController.instance.RelinquishControl();
        PlayerData.instance.isInvincible = true;
        PDHelper.DisablePause = true;
        CameraController controller = UnityEngine.Object.FindObjectOfType<CameraController>();
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
            vignette.localScale += new Vector3(20f * Time.deltaTime, 20f * Time.deltaTime, 20 * Time.deltaTime);
            if (vignette.localScale.x > 600f)
                vignette.localScale = new Vector3(600f, 600f, 600f);
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
        StageBuilder.CreateExitPoint();
        while (true)
        {
            movingObject.transform.position = Vector3.MoveTowards(movingObject.transform.position, HeroController.instance.transform.position, Time.deltaTime * 40);
            if (Vector3.Distance(movingObject.transform.position, HeroController.instance.transform.position) < 1)
                break;
            yield return null;
        }
        while(vignette.localScale.x > 5.5f)
        {
            vignette.localScale -= new Vector3(200f * Time.deltaTime, 200f * Time.deltaTime, 200f * Time.deltaTime);
            if (vignette.localScale.x < 5.5f)
                vignette.localScale = new Vector3(5.5f, 5.5f, 5.5f);
            yield return null;
        }
        
        HeroController.instance.transform.Find("Vignette").localScale = new(5.5f, 5.5f, 5.5f);
        controller.camTarget = oldTarget;
        UnityEngine.Object.Destroy(movingObject);
        HeroController.instance.RegainControl();
        PDHelper.DisablePause = false;
        PlayingPreview = false;
        ActiveMinigame.Begin();
    }

    #endregion

    #region Eventhandler

    private static void SceneManager_activeSceneChanged(UnityEngine.SceneManagement.Scene oldScene, UnityEngine.SceneManagement.Scene newScene)
    {
        if (Minigames.FirstOrDefault(x => x.GetEntryScene() == newScene.name) is Minigame minigame && CurrentState == MinigameState.Inactive)
        {
            ActiveMinigame = minigame;
            StageBuilder.CreateMinigameEntry();
        }
        else if (CurrentState == MinigameState.Active)
            StageBuilder.SetupLevel(SelectedDifficulty switch
            {
                Difficulty.Easy => ActiveMinigame.Courses[SelectedLevel].EasyCourse,
                Difficulty.Hard => ActiveMinigame.Courses[SelectedLevel].HardCourse,
                _ => ActiveMinigame.Courses[SelectedLevel].NormalCourse
            });
        else
            ActiveMinigame = null;
    }

    private static void ModHooks_AfterPlayerDeadHook() => EndMinigame();

    private static IEnumerator UIManager_ReturnToMainMenu(On.UIManager.orig_ReturnToMainMenu orig, UIManager self)
    {
        EndMinigame();
        return orig(self);
    }

    private static void CameraController_LockToArea(On.CameraController.orig_LockToArea orig, CameraController self, CameraLockArea lockArea)
    {
        if (!PlayingPreview)
            orig(self, lockArea);
    }

    private static string ModHooks_LanguageGetHook(string key, string sheetTitle, string orig)
    {
        if (key == "MinigameTitle")
            orig = ActiveMinigame?.GetTitle()?.ToUpper();
        else if (key == "MinigameDesc")
            orig = ActiveMinigame?.GetDescription();
        return orig;
    }

    private static int ModHooks_TakeHealthHook(int damage)
    {
        if (PracticeMode)
            damage = 0;
        return damage;
    }

    #endregion
}
