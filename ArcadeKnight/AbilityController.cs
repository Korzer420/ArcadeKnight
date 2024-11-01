using ArcadeKnight.Components;
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

    private static Dictionary<string, int> _originalSpellStates = new()
    {
        {nameof(PlayerData.fireballLevel), 1 },
        {nameof(PlayerData.screamLevel), 1 },
        {nameof(PlayerData.quakeLevel), 1 }
    };

    private static Dictionary<string, bool> _initialRules = new()
    {
        {nameof(PlayerData.canDash), true},
        {nameof(PlayerData.hasWalljump), true},
        {nameof(PlayerData.hasSuperDash), true},
        {nameof(PlayerData.hasDoubleJump), true},
        {nameof(PlayerData.hasAcidArmour), true},
        {"canFocus", true },
        {nameof(PlayerData.hasDashSlash), true}, // Great slash
        {nameof(PlayerData.hasUpwardSlash), true}, // Dash Slash
        {nameof(PlayerData.hasCyclone), true},
        {nameof(PlayerData.hasDreamNail), true },
        {"canFireball", true },
        {"canDive", true },
        {"canScream", true },
        {"damagePenalty", false}
    };

    private static bool _damagePenalty = false;

    private static bool _canFocus = true;

    private static bool _active = false;

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
        if (_active)
            return;
        _active = true;

        _originalValues.Clear();
        foreach (string key in _initialRules.Keys)
            _originalValues.Add(key, PlayerData.instance.GetBool(key));

        foreach (string key in _originalSpellStates.Keys.ToList())
            _originalSpellStates[key] = PlayerData.instance.GetInt(key);

        foreach (string rule in restrictions)
        {
            string realRule = rule;
            // The nail arts have other names internally. To not bother the user with this, we convert them here from "normal" names.
            if (realRule == "hasDashSlash")
                realRule = "hasUpwardSlash";
            else if (realRule == "hasGreatSlash")
                realRule = "hasDashSlash";
            else if (realRule == "hasCycloneSlash")
                realRule = "hasCyclone";

            if (_initialRules.ContainsKey(rule))
                _initialRules[rule] = false;
            else
                LogHelper.Write<ArcadeKnight>("Restriction " + rule + " could not be established.", KorzUtils.Enums.LogType.Warning, false);
        }

        ModHooks.SetPlayerBoolHook += ModHooks_SetPlayerBoolHook;
        ModHooks.AfterTakeDamageHook += ModHooks_AfterTakeDamageHook;
        On.GameManager.HazardRespawn += GameManager_HazardRespawn;
        On.HeroController.CanFocus += HeroController_CanFocus;
        ResetToCurrentRules();
    }

    public static void Disable()
    {
        foreach (string key in _initialRules.Keys.ToList())
            _initialRules[key] = true;
        _initialRules["damagePenalty"] = false;
        _damagePenalty = false;
        _canFocus = true;
        foreach (string key in _originalValues.Keys)
            PlayerData.instance.SetBool(key, _originalValues[key]);
        foreach (string key in _originalSpellStates.Keys.ToList())
            _originalSpellStates[key] = PlayerData.instance.GetInt(key);
        if (!_active)
            return;
        _active = false;
        CurrentRestrictions.Clear();
        DisabledRestrictions.Clear();
        EstablishedRestrictions.Clear();
        ModHooks.SetPlayerBoolHook -= ModHooks_SetPlayerBoolHook;
        ModHooks.AfterTakeDamageHook -= ModHooks_AfterTakeDamageHook;
        On.GameManager.HazardRespawn -= GameManager_HazardRespawn;
        On.HeroController.CanFocus -= HeroController_CanFocus;
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

        PDHelper.HasNailArt = PDHelper.HasCyclone || PDHelper.HasDashSlash || PDHelper.HasUpwardSlash;
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

    public static bool CheckState(string name)
    {
        if (name == "damagePenalty")
            return _damagePenalty;
        else if (name == "canFocus")
            return _canFocus;
        else if (name == "canFireball")
            return PDHelper.FireballLevel == 1;
        else if (name == "canQuake")
            return PDHelper.QuakeLevel == 1;
        else if (name == "canScream")
            return PDHelper.ScreamLevel == 1;
        else
            return PlayerData.instance.GetBool(name);
    }

    #endregion

    #region Event handler

    private static bool ModHooks_SetPlayerBoolHook(string name, bool orig)
    {
        if (name == "damagePenalty")
            _damagePenalty = orig;
        else if (name == "canFocus")
            _canFocus = orig;
        else if (name == "canFireball")
            PDHelper.FireballLevel = orig ? 1 : 0;
        else if (name == "canScream")
            PDHelper.ScreamLevel = orig ? 1 : 0;
        else if (name == "canDive")
            PDHelper.QuakeLevel = orig ? 1 : 0;
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
        //else if (self.FsmName == "npc_dream_dialogue" && self.gameObject.name == "Impact")
        //{
        //    self.GetState("Idle").AdjustTransitions("Impact");
        //    self.AddState("Generate Particles", () =>
        //    {
        //        GameObject hint = Object.Instantiate(ArcadeKnight.PreloadedObjects["Dream Effect"], self.transform);
        //        hint.name = "Dream Hint";
        //        hint.SetActive(true);
        //        hint.GetComponent<ParticleSystem>().enableEmission = true;
        //        hint.transform.position = self.transform.position;
        //        hint.transform.position -= new Vector3(0f, 0f, 3f);
        //    }, FsmTransitionData.FromTargetState("Idle").WithEventName("FINISHED"));
        //    self.GetState("Impact").AdjustTransitions("Generate Particles");
        //    self.GetState("Impact").RemoveActions(5);
        //    self.GetState("Impact").RemoveActions(0);
        //}
        //else if (self.FsmName == "Dream Nail" && self.gameObject.name == "Knight")
        //    self.GetState("Take Control").GetFirstAction<SendMessage>().Enabled = false;
        orig(self);
    }

    #endregion
}
