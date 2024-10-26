using ArcadeKnight.Enums;
using UnityEngine;

namespace ArcadeKnight.Components;

public class AbilityRestrictor : MonoBehaviour
{
    private bool _activated = false;

    #region Properties

    public string AffectedFieldName { get; set; }

    public bool SetValue { get; set; }

    public CheckDirection RevertDirection { get; set; }

    #endregion

    #region Methods

    void Start()
    {
        if (gameObject.GetComponent<BoxCollider2D>() == null)
        {
            BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;

            if (RevertDirection == CheckDirection.Left || RevertDirection == CheckDirection.Right)
                collider.size = new(1f, 600f);
            else
                collider.size = new(600f, 1f);
            collider.offset = new(0.175f, 0f);
        }
    }

    void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.tag == "Player")
        {
            if (PlayerData.instance.GetBool(AffectedFieldName) == SetValue)
                return;
            PlayerData.instance.SetBool(AffectedFieldName, SetValue);
            _activated = true;
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
                    if (!_activated)
                        return;
                    PlayerData.instance.SetBool(AffectedFieldName, !SetValue);
                    _activated = false;
                    break;
                default:
                    break;
            }
    }

    #endregion
}
