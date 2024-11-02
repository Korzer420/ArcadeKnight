using ArcadeKnight.Enums;
using ArcadeKnight.Extensions;
using ArcadeKnight.Minigames;
using System;
using System.Collections;
using System.Threading;
using TMPro;
using UnityEngine;

namespace ArcadeKnight;

public abstract class TimeMinigame : Minigame
{
    #region Members

    protected float _passedTime = 0f;

    private int _timePenalties = 0;

    private GameObject _penaltyTimer;

    #endregion

    #region Properties

    public GameObject PenaltyTimer
    {
        get
        {
            if (_penaltyTimer == null)
            {
                GameObject hudCanvas = GameObject.Find("_GameCameras").transform.Find("HudCamera/Hud Canvas").gameObject;
                GameObject prefab = GameObject.Find("_GameCameras").transform.Find("HudCamera/Inventory/Inv/Inv_Items/Geo").transform.GetChild(0).gameObject;
                _penaltyTimer = UnityEngine.Object.Instantiate(prefab, hudCanvas.transform, true);
                _penaltyTimer.name = "Penalty Tracker";
                _penaltyTimer.transform.position = new(0f, 5.6f, 1f);
                _penaltyTimer.transform.localScale = new(2f, 2f, 1f);
                TextMeshPro textElement = _penaltyTimer.GetComponent<TextMeshPro>();
                textElement.fontSize = 3;
                textElement.alignment = TextAlignmentOptions.Center;
                textElement.text = "0";
            }
            return _penaltyTimer;
        }
    }

    #endregion

    #region Methods

    protected override void Start() =>MinigameController.CoroutineHolder.StartCoroutine(StartTimer());

    protected override void Conclude() 
    { 
        _passedTime = 0f;
        _timePenalties = 0;
    }

    internal IEnumerator StartTimer()
    {
        TextMeshPro currentCounter = MinigameController.Tracker.GetComponent<TextMeshPro>();
        TextMeshPro penaltyCounter = PenaltyTimer.GetComponent<TextMeshPro>();
        PenaltyTimer.SetActive(false);
        _timePenalties = 0;
        _passedTime = 0f;
        while (MinigameController.CurrentState == MinigameState.Active)
        {
            _passedTime += Time.deltaTime;
            currentCounter.text = TimeSpan.FromSeconds(_passedTime).ToFormat("mm:ss.ff");
            if (_timePenalties > 0 && TimePenaltyFactor() > 0)
            {
                PenaltyTimer.SetActive(true);
                TimeSpan penalty = TimeSpan.FromSeconds(_timePenalties * TimePenaltyFactor());
                if (penalty.TotalSeconds >= 60)
                    penaltyCounter.text = "<color=#de0404>+" + penalty.ToFormat("mm:ss") + " minutes</color>";
                else
                    penaltyCounter.text = "<color=#de0404>+" + penalty.ToFormat("ss") + " seconds</color>";
            }
            yield return null;
            if (GameManager.instance?.IsGamePaused() == true)
                yield return new WaitUntil(() => GameManager.instance?.IsGamePaused() == false);
        }
        _passedTime += _timePenalties * TimePenaltyFactor();
        if (MinigameController.ActiveMinigame is not XerosMirrorWorld || MinigameController.SelectedDifficulty != Difficulty.Normal)
            GameObject.Destroy(_penaltyTimer);
        currentCounter.text = TimeSpan.FromSeconds(_passedTime).ToFormat("mm:ss.ff");
    }

    public float AddTimePenalty(float seconds) 
    {
        _passedTime += seconds;
        return _passedTime;
    }

    protected virtual int TimePenaltyFactor() => 1;

    internal override void ApplyScorePenalty(int count = 1) => _timePenalties += count;
    
    #endregion
}
