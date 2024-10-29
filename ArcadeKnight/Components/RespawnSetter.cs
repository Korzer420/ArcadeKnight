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
        }
    }

    void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.tag == "Player")
        {
            if (ActivateOnce && _activated)
                return;
            _activated = true;
            if (_exitSprite == null)
                _exitSprite = GameObject.Find("Cancel Sprite");
            if (_exit == null)
                _exit = GameObject.Find("Cancel");
            _exitSprite.transform.position = transform.position - new Vector3(0f, 2.2f, 0.02f);
            _exit.transform.position = transform.position - new Vector3(0f, 1f);
            _exit.LocateMyFSM("Door Control").FsmVariables.FindFsmGameObject("Prompt").Value.transform.position = transform.position + new Vector3(0f, 4f);
            HeroController.instance.SetHazardRespawn(transform.position - new Vector3(0f, 1f), true);
        }
    }

    #endregion
}
