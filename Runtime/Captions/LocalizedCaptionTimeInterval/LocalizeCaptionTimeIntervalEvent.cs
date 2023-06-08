using System;
using UnityEngine;
using UnityEngine.Localization.Components;

/*
    This script it's linked with LocalizedCaptionTimeInterval and UnityEventCaptionTimeInterval
    Everyone has to be [Serializable] to work. Also every script has to be an independent script (.cs)
 */

[AddComponentMenu("Localization/Asset/Localize CaptionTimeInterval Event")]
[Serializable] // It's mandatory
public class LocalizeCaptionTimeIntervalEvent : LocalizedAssetEvent<CaptionTimeInterval, LocalizedCaptionTimeInterval, UnityEventCaptionTimeInterval> { }

