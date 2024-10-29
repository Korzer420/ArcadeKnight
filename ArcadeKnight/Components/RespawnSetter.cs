using System.Linq;
using UnityEngine;

namespace ArcadeKnight.Components;

public class RespawnSetter : MonoBehaviour
{
    #region Members

    private bool _activated;

    private GameObject _exitSprite;

    private GameObject _exit;

    #endregion

    #region Properties

    public float Height { get; set; }

    public float Width { get; set; }

    public bool ActivateOnce { get; set; }

    #endregion

    #region Methods

    void Start()
    {
        if (GetComponent<BoxCollider2D>() == null)
        {
            BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new(Width, Height);
            collider.enabled = gameObject.name != "minigame_start";
        }
    }

    void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.tag == "Player")
        {
            if (ActivateOnce && _activated)
                return;
            GetComponent<BoxCollider2D>().enabled = false;
            foreach (RespawnSetter respawn in Object.FindObjectsOfType<RespawnSetter>())
            {
                if (respawn == this)
                    continue;
                respawn.GetComponent<BoxCollider2D>().enabled = true;
            }
            _activated = true;
            if (_exitSprite == null)
                _exitSprite = GameObject.Find("Cancel Sprite");
            if (_exit == null)
                _exit = GameObject.Find("Cancel");
            if (gameObject.name != "minigame_start")
            {
                _exitSprite.transform.position = transform.position - new Vector3(0f, 2.4f, 0.02f);
                _exit.transform.position = transform.position - new Vector3(0f, 1f);
            }
            else
            {
                _exitSprite.transform.position = transform.position - new Vector3(0f, 1.4f);
                _exit.transform.position = transform.position;
            }
            _exit.LocateMyFSM("Door Control").FsmVariables.FindFsmGameObject("Prompt").Value.transform.position = transform.position + new Vector3(0f, 4f);
            HeroController.instance.SetHazardRespawn(transform.position - new Vector3(0f, 1f), true);
            AbilityController.AdjustCheckpoint();
        }
    }

    #endregion
}
