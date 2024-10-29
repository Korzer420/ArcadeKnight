using ArcadeKnight.Components;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using KorzUtils.Data;
using KorzUtils.Helper;
using Modding;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ArcadeKnight;

public static class AbilityController
{
    #region Members

    private static Dictionary<string, bool> _originalValues = [];

    private static Dictionary<string, bool> _initialRules = new()
    {
        {nameof(PlayerData.instance.canDash), true},
        {nameof(PlayerData.instance.hasWalljump), true},
        {nameof(PlayerData.instance.hasSuperDash), true},
        {nameof(PlayerData.instance.hasDoubleJump), true},
        {nameof(PlayerData.instance.hasAcidArmour), true},
        {"canFocus", true },
        {"damagePenalty", false}
    };

    private static bool _damagePenalty = false;

    private static bool _canFocus = true;

    private static List<AbilityRestrictor> _signs = [];

    #endregion

    #region Methods

    public static void Initialize() => On.PlayMakerFSM.OnEnable += PlayMakerFSM_OnEnable;
    
    public static void Enable(string[] restrictions)
    {
        _signs.Clear();
        _signs.AddRange(Object.FindObjectsOfType<AbilityRestrictor>());
        _originalValues.Clear();
        foreach (string key in _initialRules.Keys)
            _originalValues.Add(key, PlayerData.instance.GetBool(key));
        
        foreach (string rule in restrictions)
            if (_initialRules.ContainsKey(rule))
                _initialRules[rule] = false;
            else
                LogHelper.Write<ArcadeKnight>("Restriction " + rule + " could not be established.", KorzUtils.Enums.LogType.Warning, false);
        
        ResetToCurrentRules();
        ModHooks.SetPlayerBoolHook += ModHooks_SetPlayerBoolHook;
        ModHooks.AfterTakeDamageHook += ModHooks_AfterTakeDamageHook;
        On.GameManager.HazardRespawn += GameManager_HazardRespawn;
        On.HeroController.CanFocus += HeroController_CanFocus;
    }

    public static void Disable()
    {
        ModHooks.SetPlayerBoolHook -= ModHooks_SetPlayerBoolHook;
        ModHooks.AfterTakeDamageHook -= ModHooks_AfterTakeDamageHook;
        On.GameManager.HazardRespawn -= GameManager_HazardRespawn;
        On.HeroController.CanFocus -= HeroController_CanFocus;
        foreach (string key in _initialRules.Keys.ToList())
            _initialRules[key] = true;
        _initialRules["damagePenalty"] = false;
        _damagePenalty = false;
        _canFocus = true;
        foreach (string key in _originalValues.Keys)
            PlayerData.instance.SetBool(key, _originalValues[key]);
    }

    public static void ResetToCurrentRules()
    {
        foreach (string key in _initialRules.Keys)
            PlayerData.instance.SetBool(key, _initialRules[key]);
        _signs.ForEach(x => x.Activated = false);
        if (PDHelper.HasAcidArmour)
            PlayMakerFSM.BroadcastEvent("GET ACID ARMOUR");
        else
            PlayMakerFSM.BroadcastEvent("REMOVE ACID ARMOUR");
    }

    #endregion

    #region Event handler

    private static bool ModHooks_SetPlayerBoolHook(string name, bool orig)
    {
        if (name == "damagePenalty")
            _damagePenalty = orig;
        else if (name == "canFocus")
            _canFocus = orig;
        return orig;
    }

    private static int ModHooks_AfterTakeDamageHook(int hazardType, int damageAmount)
    {
        if (_damagePenalty && !MinigameController.PracticeMode)
            MinigameController.ActiveMinigame?.ApplyScorePenalty();
        return damageAmount;
    }

    private static void GameManager_HazardRespawn(On.GameManager.orig_HazardRespawn orig, GameManager self)
    {
        ResetToCurrentRules();
        orig(self);
    }

    private static bool HeroController_CanFocus(On.HeroController.orig_CanFocus orig, HeroController self) => orig(self) && _canFocus;

    private static void PlayMakerFSM_OnEnable(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
    {
        if (self.FsmName == "Acid Armour Check" && MinigameController.CurrentState == Enums.MinigameState.Active)
        {
            if (self.name == "Acid Box")
            {
                self.AddState("Enable", () => self.GetComponent<DamageHero>().damageDealt = 1, 
                    FsmTransitionData.FromTargetState("Disable").WithEventName("GET ACID ARMOUR"));
                self.GetState("Disable").AddTransition("REMOVE ACID ARMOUR", "Enable");
            }
            else if (self.name == "Surface Water Region")
            {
                self.AddState("Disable", () => self.GetComponent<BoxCollider2D>().enabled = false);
                self.GetState("Disable").AddTransition("GET ACID ARMOUR", "Enable");
                self.GetState("Enable").AddTransition("REMOVE ACID ARMOUR", "Disable");
            }
        }
        orig(self);
    }

    #endregion
}
