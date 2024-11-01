using ArcadeKnight.Components;
using ArcadeKnight.Enums;
using ArcadeKnight.Minigames;
using ArcadeKnight.SaveData;
using KorzUtils.Helper;
using Modding;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    public static Dummy CoroutineHolder { get; set; }

    public static CourseData ActiveCourse => SelectedDifficulty switch
    {
        Difficulty.Normal => ActiveMinigame.Courses[SelectedLevel].NormalCourse,
        Difficulty.Hard => ActiveMinigame.Courses[SelectedLevel].HardCourse,
        _ => ActiveMinigame.Courses[SelectedLevel].EasyCourse,
    };

    public static GlobalSaveData GlobalSettings { get; set; } = new();

    #endregion

    #region Constructors

    static MinigameController()
    {
        Minigames.Add(new GorbsParkour());
        Minigames.Add(new NoEyesTrial());
        CourseLoader.Load();
    }

    #endregion

    #region Methods

    public static void Initialize()
    {
        UnityEngine.SceneManagement.SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
        On.UIManager.ReturnToMainMenu += UIManager_ReturnToMainMenu;
        On.CameraController.LockToArea += CameraController_LockToArea;
        ModHooks.LanguageGetHook += ModHooks_LanguageGetHook;
        ModHooks.TakeHealthHook += ModHooks_TakeHealthHook;
        ModHooks.GetPlayerBoolHook += ModHooks_GetPlayerBoolHook;
        On.GameManager.ReadyForRespawn += GameManager_ReadyForRespawn;
        StageBuilder.Initialize();
        AbilityController.Initialize();
        GameObject coroutineHolder = new("ArcadeKnight Dummy");
        CoroutineHolder = coroutineHolder.AddComponent<Dummy>();
        GameObject.DontDestroyOnLoad(coroutineHolder);
    }

    public static void EndMinigame()
    {
        if (CurrentState == MinigameState.Inactive)
            return;
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
        CoroutineHolder.StopAllCoroutines();
    }

    internal static IEnumerator UpdateProgression(string newProgression)
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
        Transform panel = Object.FindObjectOfType<BossChallengeUI>().transform.Find("Panel");
        panel.Find("BossName_Text").position = new Vector3(7.4f, 4f);
        panel.Find("Description_Text").position = new Vector3(7.5f, 2.9f);
        List<Text> highscoreText = [];
        List<(GameObject, GameObject)> sprites = [];
        Text textObject;
        if (panel.Find("LevelText") is Transform levelText)
        {
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
                GameObject highscoreObject = panel.Find($"Buttons/Tier{i}Button/Highscore").gameObject;
                highscoreText.Add(highscoreObject.GetComponent<Text>());
            }
            textObject = levelText.GetComponent<Text>();
        }
        else
        {
            GameObject levelObject = Object.Instantiate(panel.Find("Description_Text").gameObject, panel);
            levelObject.name = "LevelText";
            levelObject.transform.position = new Vector3(7.4f, 1.91f);
            textObject = levelObject.GetComponent<Text>();
            textObject.alignment = TextAnchor.MiddleCenter;
            textObject.fontSize++;
            textObject.text = ActiveMinigame.Courses[SelectedLevel].Name.ToUpper();

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
                GameObject highscoreObject = Object.Instantiate(levelObject, panel.Find("Buttons").Find($"Tier{i}Button"));
                highscoreObject.name = "Highscore";
                highscoreText.Add(highscoreObject.GetComponent<Text>());
                highscoreObject.transform.localPosition = new(115.55f, 0f, 0f);
            }
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
                if (ActiveMinigame.Courses[SelectedLevel].IsCustomCourse)
                    textObject.text += " (by " + (ActiveMinigame.Courses[SelectedLevel].Author ?? "Unknown") + ")";
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
        PlayingPreview = true;
        HeroController.instance.RelinquishControl();
        PDHelper.DisablePause = true;
        CameraController controller = Object.FindObjectOfType<CameraController>();
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
        while (true)
        {
            movingObject.transform.position = Vector3.MoveTowards(movingObject.transform.position, HeroController.instance.transform.position, Time.deltaTime * 40);
            if (Vector3.Distance(movingObject.transform.position, HeroController.instance.transform.position) < 1)
                break;
            yield return null;
        }
        while (vignette.localScale.x > 5.5f)
        {
            vignette.localScale -= new Vector3(200f * Time.deltaTime, 200f * Time.deltaTime, 200f * Time.deltaTime);
            if (vignette.localScale.x < 5.5f)
                vignette.localScale = new Vector3(5.5f, 5.5f, 5.5f);
            yield return null;
        }

        HeroController.instance.transform.Find("Vignette").localScale = new(5.5f, 5.5f, 5.5f);
        controller.camTarget = oldTarget;
        Object.Destroy(movingObject);
        HeroController.instance.RegainControl();
        PDHelper.DisablePause = false;
        PlayingPreview = false;
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
        //if (newScene.name == "Town")
        //{
        //    string[] abilities = 
        //    [
        //        "Broken_Mask",
        //        "Crystal_Dash",
        //        "Dream_Gate",
        //        "Monarch_Wings",
        //        "Mantis_Claw",
        //        "Vengeful_Spirit",
        //        "Dash_Slash",
        //        "Cyclone_Slash",
        //        "Great_Slash",
        //        "Desolate_Dive",
        //        "Ismas_Tear",
        //        "Howling_Wraiths",
        //        "Mothwing_Cloak",
        //        "Focus"
        //    ];
        //    for (int i = 0; i < abilities.Length; i++)
        //    {
        //        GameObject obstacleGameObject = new(abilities[i]);
        //        obstacleGameObject.SetActive(false);
        //        obstacleGameObject.transform.position = new(42.43f + (i * 4f), 12.4f, 0.02f);
        //        obstacleGameObject.transform.localScale = new(2f, 2f, 1f);
        //        obstacleGameObject.AddComponent<SpriteRenderer>().sprite = SpriteHelper.CreateSprite<ArcadeKnight>("Sprites.Imposter_Sign");
        //        obstacleGameObject.SetActive(true);

        //        GameObject abilitySprite = new("Ability Sprite");
        //        abilitySprite.transform.SetParent(obstacleGameObject.transform);
        //        abilitySprite.transform.localPosition = new(0f, -0.1f, -0.01f);
        //        // Normal sign scale
        //        //abilitySprite.transform.localScale = new(.4f, .4f);
        //        // Imposter scale
        //        //abilitySprite.transform.localScale = new(.28f, .28f);
        //        abilitySprite.AddComponent<SpriteRenderer>().sprite = SpriteHelper.CreateSprite<ArcadeKnight>("Sprites.Abilities." + abilities[i]);
        //        abilitySprite.SetActive(true);

        //        //abilitySprite = new("Block");
        //        //abilitySprite.transform.SetParent(obstacleGameObject.transform);
        //        //abilitySprite.transform.localPosition = new(0f, -0.1f, -0.011f);
        //        //abilitySprite.transform.localScale = new(.95f, .95f);
        //        //abilitySprite.AddComponent<SpriteRenderer>().sprite = SpriteHelper.CreateSprite<ArcadeKnight>("Sprites.ForbiddenSymbol");
        //        //abilitySprite.SetActive(true);
        //    }
            
        //    //GameObject dreamImpact = GameObject.Instantiate(ArcadeKnight.PreloadedObjects["Dream Impact"]);
        //    //dreamImpact.name = "Impact";
        //    //dreamImpact.SetActive(false);
        //    //dreamImpact.transform.position = new(42.43f, 11.4f);
        //    //dreamImpact.SetActive(true);
        //}
    }

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

    private static bool ModHooks_GetPlayerBoolHook(string name, bool orig)
    {
        if (name == "hasDreamGate")
            return orig && CurrentState == MinigameState.Inactive;
        return orig;
    }

    private static void GameManager_ReadyForRespawn(On.GameManager.orig_ReadyForRespawn orig, GameManager self, bool isFirstLevelForPlayer)
    {
        EndMinigame();
        orig(self, isFirstLevelForPlayer);
    }

    #endregion
}
