using ArcadeKnight.Components;
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
        {"damagePenalty", false}
    };

    private static bool _damagePenalty = false;

    private static List<AbilityRestrictor> _signs = [];

    #endregion

    #region Methods

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
    }

    public static void Disable()
    {
        ModHooks.SetPlayerBoolHook -= ModHooks_SetPlayerBoolHook;
        ModHooks.AfterTakeDamageHook -= ModHooks_AfterTakeDamageHook;
        On.GameManager.HazardRespawn -= GameManager_HazardRespawn;
        foreach (string key in _initialRules.Keys.ToList())
            _initialRules[key] = true;
        _initialRules["damagePenalty"] = false;
        _damagePenalty = false;
        foreach (string key in _originalValues.Keys)
            PlayerData.instance.SetBool(key, _originalValues[key]);
    }

    public static void ResetToCurrentRules()
    {
        foreach (string key in _initialRules.Keys)
            PlayerData.instance.SetBool(key, _initialRules[key]);
        _signs.ForEach(x => x.Activated = false);
    }

    #endregion

    #region Event handler

    private static bool ModHooks_SetPlayerBoolHook(string name, bool orig)
    {
        if (name == "damagePenalty")
            _damagePenalty = orig;
        return orig;
    }

    private static int ModHooks_AfterTakeDamageHook(int hazardType, int damageAmount)
    {
        if (_damagePenalty)
            MinigameController.ActiveMinigame?.ApplyScorePenalty();
        return damageAmount;
    }

    private static void GameManager_HazardRespawn(On.GameManager.orig_HazardRespawn orig, GameManager self)
    {
        ResetToCurrentRules();
        orig(self);
    }

    #endregion
}
