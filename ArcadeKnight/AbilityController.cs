using ArcadeKnight.Components;
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

    #endregion

    #region Properties

    /// <summary>
    /// Alle Regeln die etabliert wurden (Wenn eine deaktiviert wird, muss hier geschaut werden)
    /// </summary>
    public static List<AbilityRestrictor> CurrentRestrictions { get; set; } = [];

    /// <summary>
    /// Alle Regelns aus <see cref="CurrentRestrictions"/> werden beim Berühren eines Checkpoints übertragen.
    /// </summary>
    public static List<AbilityRestrictor> EstablishedRestrictions { get; set; } = [];

    /// <summary>
    /// Alle Regeln die deaktiviert wurden, aber nicht in <see cref="CurrentRestrictions"/> auftauchen.
    /// </summary>
    public static List<AbilityRestrictor> DisabledRestrictions { get; set; } = [];

    #endregion

    #region Methods

    public static void Initialize() => On.PlayMakerFSM.OnEnable += PlayMakerFSM_OnEnable;

    public static void Enable(string[] restrictions)
    {
        CurrentRestrictions.Clear();
        EstablishedRestrictions.Clear();
        DisabledRestrictions.Clear();
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
        CurrentRestrictions.ForEach(x => x.Activated = false);
        CurrentRestrictions.Clear();
        DisabledRestrictions.ForEach(x => x.Activated = true);
        DisabledRestrictions.Clear();

        if (PDHelper.HasAcidArmour)
            PlayMakerFSM.BroadcastEvent("GET ACID ARMOUR");
        else
            PlayMakerFSM.BroadcastEvent("REMOVE ACID ARMOUR");
    }

    public static void AdjustCheckpoint()
    {
        // Snapshot current rules.
        foreach (string key in _initialRules.Keys.ToList())
            _initialRules[key] = PlayerData.instance.GetBool(key);
        _initialRules["damagePenalty"] = _damagePenalty;
        _initialRules["canFocus"] = _canFocus;

        foreach (AbilityRestrictor restriction in DisabledRestrictions)
            EstablishedRestrictions.Remove(restriction);
        DisabledRestrictions.Clear();
        EstablishedRestrictions.AddRange(CurrentRestrictions);
        CurrentRestrictions.Clear();
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
