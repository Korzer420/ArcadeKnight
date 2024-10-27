using UnityEngine;

namespace ArcadeKnight.Components;

public class RespawnZone : MonoBehaviour
{
    #region Methods

    void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.tag == "Player")
        {
            HeroController.instance.RelinquishControl();
            GameManager.instance.HazardRespawn();
            HeroController.instance.RegainControl();
        }
    }

    #endregion
}
