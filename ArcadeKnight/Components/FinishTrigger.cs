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
            bool showScore = true;
            PDHelper.DisablePause = true;
            PDHelper.IsInvincible = true;
            if (MinigameController.ActiveMinigame.GetMinigameType() == MinigameType.XerosMirrorWorld)
            {
                if (MinigameController.SelectedDifficulty == Difficulty.Hard)
                {

                }
                else if (MinigameController.SelectedDifficulty == Difficulty.Normal)
                {
                    showScore = false;
                    StartCoroutine((MinigameController.ActiveMinigame as XerosMirrorWorld).EvaluteResult(this));
                }
            }
            _endingStarted = true;
            MinigameController.CurrentState = MinigameState.Finish;
            HeroController.instance.RelinquishControl();
            if (showScore)
                StartCoroutine(DisplayScore());
        }
    }

    private void AddPenalty()
    {
        _endingStarted = true;
        MinigameController.CurrentState = MinigameState.Finish;
        HeroController.instance.RelinquishControl();
    }

    public IEnumerator DisplayScore()
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
        while (timePassed < 3f)
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
