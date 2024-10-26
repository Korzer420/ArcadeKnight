using KorzUtils.Helper;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using LogType = KorzUtils.Enums.LogType;

namespace ArcadeKnight.Components;

public class FinishTrigger : MonoBehaviour
{
    private bool _endingStarted = false;

    void Start()
    {
        if (gameObject.LocateMyFSM("Control") == null)
        {
            LogHelper.Write<ArcadeKnight>("Couldn't find finish sequence fsm.", LogType.Error);
            return;
        }
        if (GetComponent<BoxCollider2D>() == null)
        {
            BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new(2.1f, 2);
        }
    }

    void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.tag == "Player" && !_endingStarted)
        {
            _endingStarted = true;
            ArcadeKnight.State = Enums.MinigameState.Finish;
            HeroController.instance.RelinquishControl();
            StartCoroutine(DisplayScore());
        }
    }

    private IEnumerator DisplayScore()
    {
        // Scale 10, 10
        // Position 0,0
        float timePassed = 0f;
        while (timePassed < 3f)
        {
            float scale = ArcadeKnight.Tracker.transform.localScale.x;
            scale = Mathf.Min(10, scale + Time.deltaTime * 4);
            ArcadeKnight.Tracker.transform.localScale = new Vector3(scale, scale, 1f);
            float yPosition = ArcadeKnight.Tracker.transform.position.y;
            yPosition = Mathf.Max(0, yPosition - Time.deltaTime * 4);
            ArcadeKnight.Tracker.transform.position = new Vector3(ArcadeKnight.Tracker.transform.position.x, yPosition);
            timePassed += Time.deltaTime;
            yield return null;
        }

        timePassed = 0f;
        while(timePassed < 3f)
        {
            yield return new WaitForSeconds(0.25f);
            timePassed += 0.25f;
            ArcadeKnight.Tracker.SetActive(!ArcadeKnight.Tracker.activeSelf);
        }
        ArcadeKnight.Tracker.SetActive(true);
        yield return new WaitForSeconds(3f);
        gameObject.LocateMyFSM("Control").SendEvent("FALL");
    }
}
