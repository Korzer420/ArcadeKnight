using ArcadeKnight.Enums;
using ArcadeKnight.Extensions;
using ArcadeKnight.Minigames;
using KorzUtils.Helper;
using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
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
            PDHelper.DisablePause = true;
            PDHelper.IsInvincible = true;
            if (MinigameController.ActiveMinigame.GetMinigameType() == MinigameType.XerosMirrorWorld)
            {
                if (MinigameController.SelectedDifficulty == Difficulty.Hard)
                {

                }
                else if (MinigameController.SelectedDifficulty == Difficulty.Normal)
                {
                    StartCoroutine(AddPenalty());
                    return;
                }
            }
            _endingStarted = true;
            MinigameController.CurrentState = MinigameState.Finish;
            HeroController.instance.RelinquishControl();
            StartCoroutine(DisplayScore());
        }
    }

    private IEnumerator AddPenalty()
    {
        _endingStarted = true;
        MinigameController.CurrentState = MinigameState.Finish;
        HeroController.instance.RelinquishControl();
        TextMeshPro textComponent = (MinigameController.ActiveMinigame as XerosMirrorWorld).PenaltyTimer.GetComponent<TextMeshPro>();
        textComponent.gameObject.SetActive(true);
        yield return null;
        XerosMirrorWorld xerosMirrorWorld = MinigameController.ActiveMinigame as XerosMirrorWorld;
        int wrongAccusedObjects = 0;
        int missedObjects = 0;
        for (int i = 0; i < xerosMirrorWorld.ImposterFlags.Count; i++)
            if (xerosMirrorWorld.ImposterFlags[i] && !xerosMirrorWorld.Imposter[i].Item2)
                wrongAccusedObjects++;
            else if (!xerosMirrorWorld.ImposterFlags[i] && xerosMirrorWorld.Imposter[i].Item2)
                missedObjects++;
        textComponent.text = "";
        yield return new WaitForSeconds(2f);
        if (wrongAccusedObjects > 0)
            textComponent.text = "<color=#de0404>Wrong accused: " + wrongAccusedObjects+" (+"+wrongAccusedObjects+ " Minute(s))</color>";
        yield return new WaitForSeconds(2f);
        if (missedObjects > 0)
            textComponent.text = "<color=#de0404>Missed: " + missedObjects + " (+" + missedObjects + " Minute(s))</color>";
        yield return new WaitForSeconds(3f);
        xerosMirrorWorld.AddTimePenalty(60 * wrongAccusedObjects);
        GameObject.Destroy(xerosMirrorWorld.PenaltyTimer);
        MinigameController.Tracker.GetComponent<TextMeshPro>().text = TimeSpan.FromSeconds(xerosMirrorWorld.AddTimePenalty(60 * missedObjects)).ToFormat("mm:ss.ff");
        yield return DisplayScore();
    }

    private IEnumerator DisplayScore()
    {
        // Scale 10, 10
        // Position 0,0
        float timePassed = 0f;
        while (timePassed < 3f)
        {
            float scale = MinigameController.Tracker.transform.localScale.x;
            scale = Mathf.Min(10, scale + Time.deltaTime * 4);
            MinigameController.Tracker.transform.localScale = new Vector3(scale, scale, 1f);
            float yPosition = MinigameController.Tracker.transform.position.y;
            yPosition = Mathf.Max(0, yPosition - Time.deltaTime * 4);
            MinigameController.Tracker.transform.position = new Vector3(MinigameController.Tracker.transform.position.x, yPosition);
            timePassed += Time.deltaTime;
            yield return null;
        }
        if (MinigameController.ActiveMinigame.CheckHighscore(MinigameController.ActiveCourse))
            MinigameController.Tracker.GetComponent<TextMeshPro>().text = MinigameController.Tracker.GetComponent<TextMeshPro>().text + "\r\nNew Highscore!";
        timePassed = 0f;
        while(timePassed < 3f)
        {
            yield return new WaitForSeconds(0.25f);
            timePassed += 0.25f;
            MinigameController.Tracker.SetActive(!MinigameController.Tracker.activeSelf);
        }
        MinigameController.Tracker.SetActive(true);
        yield return new WaitForSeconds(3f);
        gameObject.LocateMyFSM("Control").SendEvent("FALL");
        MinigameController.EndMinigame();
    }
}
