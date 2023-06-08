using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Events;


[CreateAssetMenu(fileName = "Caption Interval", menuName = "Captions/CaptionInterval", order = 1)]
[Serializable]
public class CaptionTimeInterval : ScriptableObject
{
    public CaptionInterval CaptionInterval;

    public CaptionTimeInterval(CaptionInterval interval) 
    {
        CaptionInterval = interval;
    }

    private void OnValidate()
    {
        #region Clamp time formats
        // REMEMBER: that if a structure can't be referenced, be careful to be changing the copies!
        CaptionInterval currentInterval;
        CaptionIntervalKeyframe entryTime;
        CaptionIntervalKeyframe exitTime;
        bool hasIncorrectFormat;

        hasIncorrectFormat = false;

        currentInterval = CaptionInterval;
        entryTime = currentInterval.EntryTime;
        exitTime = currentInterval.ExitTime;

        if (entryTime.Hours < 0 || CaptionIntervalKeyframe.MAX_HOURS < entryTime.Hours || exitTime.Hours < 0 || CaptionIntervalKeyframe.MAX_HOURS < exitTime.Hours)
        {
            // Debug.LogWarning($"Incorrect <b>hours format</b>. Must be in range of 0 to {CaptionIntervalKeyframe.MAX_HOURS} (inclusive). The value was clamped.");
            hasIncorrectFormat = true;
        }

        if (entryTime.Minutes < 0 || CaptionIntervalKeyframe.MAX_MINUTES < entryTime.Minutes || exitTime.Minutes < 0 || CaptionIntervalKeyframe.MAX_MINUTES < exitTime.Minutes)
        {
            // Debug.LogWarning($"Incorrect <b>minutes format</b>. Must be in range of 0 to {CaptionIntervalKeyframe.MAX_MINUTES} (inclusive). The value was clamped.");
            hasIncorrectFormat = true;
        }

        if (entryTime.Seconds < 0 || CaptionIntervalKeyframe.MAX_SECONDS < entryTime.Seconds || exitTime.Seconds < 0 || CaptionIntervalKeyframe.MAX_SECONDS < exitTime.Seconds)
        {
            // Debug.LogWarning($"Incorrect <b>seconds format</b>. Must be in range of 0 to {CaptionIntervalKeyframe.MAX_SECONDS} (inclusive). The value was clamped.");
            hasIncorrectFormat = true;
        }

        if (entryTime.Milliseconds < 0 || CaptionIntervalKeyframe.MAX_MILLISECONDS < entryTime.Milliseconds || exitTime.Milliseconds < 0 || CaptionIntervalKeyframe.MAX_MILLISECONDS < exitTime.Milliseconds)
        {
            // Debug.LogWarning($"Incorrect <b>milliseconds format</b>. Must be in range of 0 to {CaptionIntervalKeyframe.MAX_MILLISECONDS} (inclusive). The value was clamped.");
            hasIncorrectFormat = true;
        }

        if (hasIncorrectFormat)
        {
            CaptionIntervalKeyframe newEntry = new CaptionIntervalKeyframe(Mathf.Clamp(entryTime.Hours, 0, CaptionIntervalKeyframe.MAX_HOURS), Mathf.Clamp(entryTime.Minutes, 0, CaptionIntervalKeyframe.MAX_MINUTES), Mathf.Clamp(entryTime.Seconds, 0, CaptionIntervalKeyframe.MAX_SECONDS), Mathf.Clamp(entryTime.Milliseconds, 0, CaptionIntervalKeyframe.MAX_MILLISECONDS));

            CaptionIntervalKeyframe newExit = new CaptionIntervalKeyframe(Mathf.Clamp(exitTime.Hours, 0, CaptionIntervalKeyframe.MAX_HOURS), Mathf.Clamp(exitTime.Minutes, 0, CaptionIntervalKeyframe.MAX_MINUTES), Mathf.Clamp(exitTime.Seconds, 0, CaptionIntervalKeyframe.MAX_SECONDS), Mathf.Clamp(exitTime.Milliseconds, 0, CaptionIntervalKeyframe.MAX_MILLISECONDS));

            CaptionInterval newInterval = new CaptionInterval(newEntry, newExit);

            CaptionInterval = newInterval;
        }
        #endregion

        #region Check time intervals

        if (GetTotalSeconds(CaptionInterval.ExitTime) < GetTotalSeconds(CaptionInterval.EntryTime))
        {
            UnityEngine.Debug.LogWarning($"✖ EntryTime is greater than ExitTime in <b>{this.name}</b>. ExitTime({GetTotalSeconds(CaptionInterval.ExitTime)}s) < EntryTime({GetTotalSeconds(CaptionInterval.EntryTime)}s)\nPath: {GetScriptPath()}");

        }

        #endregion
    }

    private float GetTotalSeconds(CaptionIntervalKeyframe captionsTime)
    {
        return captionsTime.Hours * 3600f + captionsTime.Minutes * 60f + captionsTime.Seconds + captionsTime.Milliseconds / 1000.0f;
    }

    public float GetEntryTotalSeconds() // To know the initial wait
    {
        return GetTotalSeconds(CaptionInterval.EntryTime);
    }

    public float GetScreenTime() 
    {
        return GetTotalSeconds(CaptionInterval.ExitTime) - GetTotalSeconds(CaptionInterval.EntryTime);
    }

    // Useful to get the path of the script calling the static function. If it's not a static function we can get it via UnityEngine.Object.GetInstanceID()
    private string GetScriptPath()
    {
        // Usa StackFrame para obtener la ruta del archivo del método en ejecución
        StackFrame _Frame = new StackTrace(true).GetFrame(0);

        // Debes obtener la ruta relativa a la carpeta Assets envez de la del sistema
        return "Assets" + Path.GetFullPath(_Frame.GetFileName()).Replace(Path.GetFullPath(Application.dataPath), "");
    }
}



