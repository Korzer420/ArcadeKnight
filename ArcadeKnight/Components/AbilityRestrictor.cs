using ArcadeKnight.Enums;
using KorzUtils.Helper;
using UnityEngine;

namespace ArcadeKnight.Components;

public class AbilityRestrictor : MonoBehaviour
{
    #region Properties

    public string AffectedFieldName { get; set; }

    public bool SetValue { get; set; }

    public CheckDirection RevertDirection { get; set; }

    public bool Activated { get; set; } = false;

    public float Height { get; set; }

    public float Width { get; set; }

    #endregion

    #region Methods

    void Start()
    {
        if (gameObject.GetComponent<BoxCollider2D>() == null)
        {
            BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new(Width, Height);
        }
    }

    void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.tag == "Player")
        {
            if (PlayerData.instance.GetBool(AffectedFieldName) == SetValue)
                return;
            if (AbilityController.CurrentRestrictions.Contains(this))
                AbilityController.CurrentRestrictions.Remove(this);
            else if (AbilityController.EstablishedRestrictions.Contains(this))
                AbilityController.DisabledRestrictions.Add(this);
            else if (!Activated)
                AbilityController.CurrentRestrictions.Add(this);
            else
                LogHelper.Write<ArcadeKnight>("An error occured. The restrict sign has an invalid state. Please report this to the mod developer." + name, KorzUtils.Enums.LogType.Error);
            PlayerData.instance.SetBool(AffectedFieldName, SetValue);
            if (AffectedFieldName == nameof(PlayerData.hasAcidArmour))
                PlayMakerFSM.BroadcastEvent(SetValue ? "GET ACID ARMOUR" : "REMOVE ACID ARMOUR");
            Activated = true;
        }
    }

    void OnTriggerExit2D(Collider2D collider)
    {
        if (collider.tag == "Player")
            switch (RevertDirection)
            {
                case CheckDirection.Left when collider.transform.position.x < transform.position.x:
                case CheckDirection.Right when collider.transform.position.x > transform.position.x:
                case CheckDirection.Up when collider.transform.position.y > transform.position.y:
                case CheckDirection.Down when collider.transform.position.y < transform.position.y:
                    if (!Activated)
                        return;
                    PlayerData.instance.SetBool(AffectedFieldName, !SetValue);
                    Activated = false;
                    if (AffectedFieldName == nameof(PlayerData.hasAcidArmour))
                        PlayMakerFSM.BroadcastEvent(SetValue ? "REMOVE ACID ARMOUR" : "GET ACID ARMOUR");
                    if (AbilityController.CurrentRestrictions.Contains(this))
                        AbilityController.CurrentRestrictions.Remove(this);
                    else if (AbilityController.DisabledRestrictions.Contains(this))
                        AbilityController.DisabledRestrictions.Remove(this);
                    break;
                default:
                    break;
            }
    }

    #endregion
}
