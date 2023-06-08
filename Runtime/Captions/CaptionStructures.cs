using System;
using UnityEngine;

#region Structures
[Serializable]
public struct CaptionInterval
{
    public CaptionIntervalKeyframe EntryTime;
    public CaptionIntervalKeyframe ExitTime;

    public CaptionInterval(CaptionIntervalKeyframe entryTime, CaptionIntervalKeyframe exitTime)
    {
        EntryTime = entryTime;
        ExitTime = exitTime;
    }
}

[Serializable]
public struct CaptionIntervalKeyframe
{
    #region Constants
    public const int MAX_HOURS = 23;
    public const int MAX_MINUTES = 59;
    public const int MAX_SECONDS = 59;
    public const int MAX_MILLISECONDS = 999;
    #endregion

    public int Hours;
    public int Minutes;
    public int Seconds;
    public int Milliseconds;

    public CaptionIntervalKeyframe(int hours, int minutes, int seconds, int milliseconds)
    {
        Hours = Mathf.Clamp(hours, 0, MAX_HOURS);
        Minutes = Mathf.Clamp(minutes, 0, MAX_MINUTES);
        Seconds = Mathf.Clamp(seconds, 0, MAX_SECONDS);
        Milliseconds = Mathf.Clamp(milliseconds, 0, MAX_MILLISECONDS);

        if (hours < 0 || MAX_HOURS < hours)
        {
            Debug.LogWarning($"Incorrect hours format. Must be in range of 0 to {MAX_HOURS} (inclusive). The value was clamped.");
        }

        if (hours < 0 || MAX_MINUTES < hours)
        {
            Debug.LogWarning($"Incorrect hours format. Must be in range of 0 to {MAX_MINUTES} (inclusive). The value was clamped.");
        }

        if (hours < 0 || MAX_SECONDS < hours)
        {
            Debug.LogWarning($"Incorrect hours format. Must be in range of 0 to {MAX_SECONDS} (inclusive). The value was clamped.");
        }

        if (hours < 0 || MAX_MILLISECONDS < hours)
        {
            Debug.LogWarning($"Incorrect hours format. Must be in range of 0 to {MAX_MILLISECONDS} (inclusive). The value was clamped.");
        }
    }
}

[Serializable]
public struct SubtitleRows
{
    public string FirstRow;
    public string SecondRow;

    public SubtitleRows(string subtitleOne, string subtitleTwo)
    {
        FirstRow = subtitleOne;
        SecondRow = subtitleTwo;
    }
}
#endregion
