using ArcadeKnight.Enums;
using ArcadeKnight.Extensions;
using KorzUtils.Helper;
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
            if (_timePenalties > 0)
            {
                PenaltyTimer.SetActive(true);
                TimeSpan penalty = TimeSpan.FromSeconds(_timePenalties * TimePenaltyFactor());
                if (penalty.TotalSeconds >= 60)
                    penaltyCounter.text = "<color=#de0404>+" + penalty.ToFormat("mm:ss") + " minutes</color>";
                else
                    penaltyCounter.text = "<color=#de0404>+" + penalty.ToFormat("ss") + " seconds</color>";
            }
            yield return null;
        }
        _passedTime += _timePenalties * TimePenaltyFactor();
        GameObject.Destroy(_penaltyTimer);
        currentCounter.text = TimeSpan.FromSeconds(_passedTime).ToFormat("mm:ss.ff");
    }

    protected virtual int TimePenaltyFactor() => 1;

    internal override void ApplyScorePenalty() => Interlocked.Increment(ref _timePenalties);

    #endregion
}
