using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;
using UnityEngine.Localization.Settings;
using System.Collections.ObjectModel;
using UnityEditor.Localization.UI;
using System.Text.RegularExpressions;
using UnityEngine.Localization;

/* We choosed List<> as data structure because we don't care about the addition time but we care about the access time */
/* This ScriptableObject is used in Unity editor to check and fix the values, but it's not used as data structure for the Narration*/
/* This ScriptableObject could be inherited for add new caption formats */

[CreateAssetMenu(fileName = "Captions", menuName = "Captions/Captions", order = 0)]
public class Captions : ScriptableObject
{
    #region Constants
    const int MAX_CHARACTERS_PER_SUBTITLE_ROW = 35; // Normally it's between 35 and 42 for row. A total of 70 and 84.

    const string ID_LIMITED_CHARACTERS_PATTERN = "[^a-zA-Z0-9 ]"; // Regular expresion pattern that forbit any non alphanumeric character. Also permits white spaces.

    const string CAPTIONS_FORBIDDEN_CHARACTERS = @"{}";

    const string MAIN_FOLDER_SUGGESTION_NAME = "Speech Tables";
    const string SPEECH_TABLES_MAIN_NAME = "Speech";
    const string SPEECH_TABLE_COLLECTION_GROUP = "Speech";
    const string SUBTITLE_TABLES_NAME = "Captions";
    const string TIME_INTERVALS_TABLES_NAME = "TimeIntervals";
    const string AUDIO_CLIPS_TABLES_NAME = "AudioClips";
    const string INTERVAL_SCRIPTABLE_OBJECTS_FOLDER_NAME = "IntervalSO";
    const string SPEECH_SCRIPTABLE_OBJECTS_FOLDER_NAME = "SpeechSO";
    #endregion

    public string ID = "Subtitles ID"; // The id that goes to the Narrator Addressables
    public List<SubtitleRows> Subtitles = new List<SubtitleRows>();

    public List<CaptionInterval> TimeIntervals = new List<CaptionInterval>();

    public virtual float GetVisualizationTime(int captionIndex)
    {
        if (TimeIntervals.Count <= captionIndex)
        {
            Debug.LogWarning($"Caption index out of range: <b>{captionIndex}</b>");
            return -1f;
        }

        float totalSeconds = GetTotatSeconds(TimeIntervals[captionIndex].ExitTime) - GetTotatSeconds(TimeIntervals[captionIndex].EntryTime);

        if (totalSeconds < 0)
        {
            Debug.LogWarning($"EntryTime is greater than ExitTime. Caption index: <b>{captionIndex}</b>");
            return -2f;
        }

        return totalSeconds;
    }

    protected virtual float GetTotatSeconds(CaptionIntervalKeyframe captionsTime)
    {
        return captionsTime.Hours * 3600f + captionsTime.Minutes * 60f + captionsTime.Seconds + captionsTime.Milliseconds / 1000.0f;
    }

    public virtual bool AddSubtitle(string subtitleOne, string subtitleTwo, TimeSpan entryTime, TimeSpan exitTime)
    {

        if (string.IsNullOrWhiteSpace($"{subtitleOne}\n{subtitleTwo}"))
        {
            Debug.LogWarning("The caption is empty. It's useless add this subtitle");
            return false;
        }

        if (exitTime < entryTime)
        {
            Debug.LogWarning("Interval inconsistency. The parameter <b>exitTime</b> must be greater or equal than <b>entryTime</b>. Subtitle not added.");
            return false;
        }

        Subtitles.Add(new SubtitleRows(subtitleOne, subtitleTwo));

        TimeIntervals.Add(new CaptionInterval(new CaptionIntervalKeyframe(entryTime.Hours, entryTime.Minutes, entryTime.Seconds, entryTime.Milliseconds), new CaptionIntervalKeyframe(exitTime.Hours, exitTime.Minutes, exitTime.Seconds, exitTime.Milliseconds)));

        return true;

    }

    public virtual bool AddSubttitles(string[] firstSubtitles, string[] secondSubtitles, TimeSpan[] entryTimes, TimeSpan[] exitTimes)
    {
        if (entryTimes.Length != exitTimes.Length || entryTimes.Length != firstSubtitles.Length || entryTimes.Length != secondSubtitles.Length)
        {
            Debug.LogWarning($"Provided arrays must have the same length:\n- firstSubtitles: {firstSubtitles.Length}\n- secondSubtitles: {firstSubtitles.Length}\n- entryTimes: {entryTimes.Length}\n- exitTimes: {exitTimes.Length}");
            return false;
        }

        if (entryTimes.Length == 0)
        {
            Debug.LogWarning("Returning: Empty arrays");
            return false;
        }

        for (int i = 0; i < entryTimes.Length; i++) // Add at the end of the list
        {
            if (!AddSubtitle(firstSubtitles[i], secondSubtitles[i], entryTimes[i], exitTimes[i]))
            {
                Debug.LogWarning("Error during the adding");
                return false;
            }
        }

        return true;
    }

    private void OnValidate()
    {
        #region Check ID
        if (string.IsNullOrWhiteSpace(ID))
        {
            ID = "Speech ID";
        }

        ID = Regex.Replace(ID, ID_LIMITED_CHARACTERS_PATTERN, ""); // Takes out all the prohibited characters
        #endregion

        #region Clamp time formats
        // REMEMBER: that if a structure can't be referenced, be careful to be changing the copies!
        CaptionInterval currentInterval;
        CaptionIntervalKeyframe entryTime;
        CaptionIntervalKeyframe exitTime;
        bool hasIncorrectFormat;

        for (int i = 0; i < TimeIntervals.Count; i++)
        {
            hasIncorrectFormat = false;

            currentInterval = TimeIntervals[i];
            entryTime = currentInterval.EntryTime;
            exitTime = currentInterval.ExitTime;

            if (entryTime.Hours < 0 || CaptionIntervalKeyframe.MAX_HOURS < entryTime.Hours || exitTime.Hours < 0 || CaptionIntervalKeyframe.MAX_HOURS < exitTime.Hours)
            {
                Debug.LogWarning($"Incorrect <b>hours format</b>. Must be in range of 0 to {CaptionIntervalKeyframe.MAX_HOURS} (inclusive). The value was clamped.");
                hasIncorrectFormat = true;
            }

            if (entryTime.Minutes < 0 || CaptionIntervalKeyframe.MAX_MINUTES < entryTime.Minutes || exitTime.Minutes < 0 || CaptionIntervalKeyframe.MAX_MINUTES < exitTime.Minutes)
            {
                Debug.LogWarning($"Incorrect <b>minutes format</b>. Must be in range of 0 to {CaptionIntervalKeyframe.MAX_MINUTES} (inclusive). The value was clamped.");
                hasIncorrectFormat = true;
            }

            if (entryTime.Seconds < 0 || CaptionIntervalKeyframe.MAX_SECONDS < entryTime.Seconds || exitTime.Seconds < 0 || CaptionIntervalKeyframe.MAX_SECONDS < exitTime.Seconds)
            {
                Debug.LogWarning($"Incorrect <b>seconds format</b>. Must be in range of 0 to {CaptionIntervalKeyframe.MAX_SECONDS} (inclusive). The value was clamped.");
                hasIncorrectFormat = true;
            }

            if (entryTime.Milliseconds < 0 || CaptionIntervalKeyframe.MAX_MILLISECONDS < entryTime.Milliseconds || exitTime.Milliseconds < 0 || CaptionIntervalKeyframe.MAX_MILLISECONDS < exitTime.Milliseconds)
            {
                Debug.LogWarning($"Incorrect <b>milliseconds format</b>. Must be in range of 0 to {CaptionIntervalKeyframe.MAX_MILLISECONDS} (inclusive). The value was clamped.");
                hasIncorrectFormat = true;
            }

            if (hasIncorrectFormat)
            {
                CaptionIntervalKeyframe newEntry = new CaptionIntervalKeyframe(Mathf.Clamp(entryTime.Hours, 0, CaptionIntervalKeyframe.MAX_HOURS), Mathf.Clamp(entryTime.Minutes, 0, CaptionIntervalKeyframe.MAX_MINUTES), Mathf.Clamp(entryTime.Seconds, 0, CaptionIntervalKeyframe.MAX_SECONDS), Mathf.Clamp(entryTime.Milliseconds, 0, CaptionIntervalKeyframe.MAX_MILLISECONDS));

                CaptionIntervalKeyframe newExit = new CaptionIntervalKeyframe(Mathf.Clamp(exitTime.Hours, 0, CaptionIntervalKeyframe.MAX_HOURS), Mathf.Clamp(exitTime.Minutes, 0, CaptionIntervalKeyframe.MAX_MINUTES), Mathf.Clamp(exitTime.Seconds, 0, CaptionIntervalKeyframe.MAX_SECONDS), Mathf.Clamp(exitTime.Milliseconds, 0, CaptionIntervalKeyframe.MAX_MILLISECONDS));

                CaptionInterval newInterval = new CaptionInterval(newEntry, newExit);

                TimeIntervals[i] = newInterval;
            }

        }
        #endregion
    }

    #region Captions inspector buttons functions
    public bool ValidateCaptions()
    {
        StringBuilder reportText = new StringBuilder($"Validation of <b>{this.name}</b>:\n");
        bool success = true;

        #region Check data structure ranges 
        if (Subtitles.Count != TimeIntervals.Count)
        {
            reportText.Append("\t✖ Diferent size in his data structures\n");
            success = false;
        }
        #endregion

        #region Check maximum characters
        for (int j = 0; j < Subtitles.Count; j++)
        {
            if (MAX_CHARACTERS_PER_SUBTITLE_ROW < Subtitles[j].FirstRow.Length)
            {
                reportText.Append($"\t✖ Subtittle with index <b>{j} (first row)</b> has more than <b>{MAX_CHARACTERS_PER_SUBTITLE_ROW}</b> characters. A total of <b>{Subtitles[j].FirstRow.Length}</b>\n");
            }

            if (MAX_CHARACTERS_PER_SUBTITLE_ROW < Subtitles[j].SecondRow.Length)
            {
                reportText.Append($"\t✖ Subtittle with index <b>{j} (second row)</b> has more than <b>{MAX_CHARACTERS_PER_SUBTITLE_ROW}</b> characters. A total of <b>{Subtitles[j].SecondRow.Length}</b>\n");
            }
        }
        #endregion

        for (int i = 0; i < TimeIntervals.Count; i++)
        {
            #region Check time intervals

            if (GetTotatSeconds(TimeIntervals[i].ExitTime) < GetTotatSeconds(TimeIntervals[i].EntryTime))
            {
                reportText.Append($"\t✖ EntryTime is greater than ExitTime in index <b>{i}</b>. ExitTime({GetTotatSeconds(TimeIntervals[i].ExitTime)}s) < EntryTime({GetTotatSeconds(TimeIntervals[i].EntryTime)}s)\n");
                success = false;
            }

            #endregion

            #region Check timeline consistency

            if (i == 0)
            {
                // This check should be the last one because of this jump.
                continue;
            }

            if (GetTotatSeconds(TimeIntervals[i].EntryTime) < GetTotatSeconds(TimeIntervals[i - 1].ExitTime))
            {
                reportText.Append($"\t✖ Interval invading in the index <b>{i}</b>. Should be greater than index <b>{i - 1}</b>.\n");
                success = false;
            }

            #endregion
        }

        string firstRow;
        string firstRowProcessed;
        string secondRow;
        string secondRowProcessed;

        for (int i = 0; i < Subtitles.Count; i++)
        {
            firstRow = Subtitles[i].FirstRow;
            firstRowProcessed = Regex.Replace(firstRow, "[" + Regex.Escape(CAPTIONS_FORBIDDEN_CHARACTERS) + "]", "");

            secondRow = Subtitles[i].SecondRow;
            secondRowProcessed = Regex.Replace(secondRow, "[" + Regex.Escape(CAPTIONS_FORBIDDEN_CHARACTERS) + "]", "");

            if (!firstRow.Equals(firstRowProcessed))
            {
                reportText.Append($"\t✖ Index <b>{i}</b> of subtitles has <b>forbidden characters</b>: {firstRow}\n");
                success = false;
            }

            if (!secondRow.Equals(secondRowProcessed))
            {
                reportText.Append($"\t✖ Index <b>{i}</b> of subtitles has <b>forbidden characters</b>: {secondRow}\n");
                success = false;
            }


        }

        if (success)
        {
            reportText.Append("\t✔ SUCCESS ");
        }

        Debug.LogWarning(reportText);
        return success;
    }

    public bool ExportToLocalization() // https://docs.unity3d.com/Packages/com.unity.localization@1.4/api/UnityEditor.Localization.LocalizationEditorSettings.html
    {
        // TODO: Select the table/locale we want to save the data (so we can export other languages different to main locale)
        // TODO: Create an override report
        // TODO: Make the Override dialog button red
        // TODO: Let the user decide the default name of the tables (create new tables if doesn't exist)

        /* Check variables format */
        OnValidate(); // It can be that the ScriptableObject doesn't have done the OnValidate yet (and give ID errors)

        /* Check the active LocalizationSettings */
        LocalizationSettings localizationSettings = LocalizationEditorSettings.ActiveLocalizationSettings;

        if (localizationSettings == null) // !LocalizationSettings.HasSettings)
        {
            Debug.LogWarning("No active <b>localization settings</b> found in the project. Create one via:\n- Window -> Asset Management -> Localization Tables\n- Right click (in Project) -> Create -> Localization -> Localization Settings");
            return false;
        }


        /* Check the Locales created */
        if (localizationSettings.GetAvailableLocales().Locales.Count < 1)
        {
            Debug.LogWarning("No Locales.asset setted in the LocalizationsSettings. Remember to create at least one in the Locale Generator. Also create a StringTable.asset, asign the corresponding Locale.asset and assign this StringTable.asset to the StringTableCollection.asset in the \"Tables\" field.");
        }


        /* Get the all the Table Collections */

        // Captions
        string subtitleFilesName = $"{SPEECH_TABLES_MAIN_NAME}_{SUBTITLE_TABLES_NAME}";
        StringTableCollection stringTableCollection = LocalizationEditorSettings.GetStringTableCollection(subtitleFilesName); //Without extension

        // Time intervals
        string timeIntervalsFilesName = $"{SPEECH_TABLES_MAIN_NAME}_{TIME_INTERVALS_TABLES_NAME}";
        AssetTableCollection timeAssetTableCollection = LocalizationEditorSettings.GetAssetTableCollection(timeIntervalsFilesName);

        // Audio clips
        string audioClipsFilesName = $"{SPEECH_TABLES_MAIN_NAME}_{AUDIO_CLIPS_TABLES_NAME}";
        AssetTableCollection audioAssetTableCollection = LocalizationEditorSettings.GetAssetTableCollection(audioClipsFilesName);


        /* Get the permission to override */
        bool overrideCaptions = true;

        if (stringTableCollection != null || timeIntervalsFilesName != null || audioAssetTableCollection != null)
        {
            overrideCaptions = EditorUtility.DisplayDialog(
                "Subtitle override alert",
                $"Do you want override if there is any existing values for {ID}?",
                "OVERRIDE",
                "Keep the old"
                );

            if (overrideCaptions)
            {
                Debug.LogWarning($"Overriding the values of <b>{ID}</b> if there is any collision");
            }
        }

        /* Request the path to save the all the TableCollections */
        string saveFolderAbsolutePath = EditorUtility.SaveFolderPanel("Choose a folder path to save the StringTableCollection & company", Application.dataPath, MAIN_FOLDER_SUGGESTION_NAME); 

        if (string.IsNullOrEmpty(saveFolderAbsolutePath))
        {
            Debug.LogWarning($"Captions creation canceled");
            return false;
        }

        #region Speech Scriptable Objects
        /* Create a directory for the scriptable objects */
        string speechScriptableObjectsFolder = Path.Combine(saveFolderAbsolutePath, SPEECH_SCRIPTABLE_OBJECTS_FOLDER_NAME);
        speechScriptableObjectsFolder = speechScriptableObjectsFolder.Replace(Application.dataPath, "Assets"); // Convert to relative path

        if (!Directory.Exists(speechScriptableObjectsFolder))
        {
            Directory.CreateDirectory(speechScriptableObjectsFolder);
        }

        string speechPath = Path.Combine(speechScriptableObjectsFolder, $"{ID}.asset");

        Speech speechSO;

        if (!File.Exists(speechPath))
        {
            speechSO = ScriptableObject.CreateInstance<Speech>();
            AssetDatabase.CreateAsset(speechSO, speechPath);
        }
        else
        {
            speechSO = AssetDatabase.LoadAssetAtPath<Speech>(speechPath);
        }
        #endregion

        #region Subtitles

        /* Create a new StringTableCollection & his related */
        if (stringTableCollection == null)
        {
            string captionsNewFolder = Path.Combine(saveFolderAbsolutePath, SUBTITLE_TABLES_NAME);

            if (File.Exists(Path.Combine(captionsNewFolder, $"{subtitleFilesName}.asset"))) // Should never happen because we are creating our files inside a new folder
            {
                Debug.LogWarning($"Trying to override a StringTableCollection with the same name:\n- {subtitleFilesName}.asset\n- {subtitleFilesName} shared data.asset\nIn the folder: <b>{captionsNewFolder}</b>\nABORTING: delete manually the files if you want override it");
                return false;
            }

            stringTableCollection = LocalizationEditorSettings.CreateStringTableCollection(subtitleFilesName, captionsNewFolder);
            stringTableCollection.Group = SPEECH_TABLE_COLLECTION_GROUP;

            Debug.Log($"<b>{subtitleFilesName}</b> was created and saved to the folder: {captionsNewFolder}");
        }


        /* Get the StringTable with the main Locale */
        ReadOnlyCollection<StringTable> stringTables = stringTableCollection.StringTables; // Warning: The LocalizationTables windows may have the tables sorted by the LocalizationSettings.asset in the field "Availiable Locales". The index of the ReadOnlyCollection<StringTable> is equal to the order of the StringTable.asset assigned in the field "Tables" in the StringTableCollection.asset

        if (stringTables.Count < 1)
        {
            Debug.LogWarning("Add at least one StringTable to the StringTableCollection.\nABORTING: No string values added.");
            return false;
        }

        StringTable stringTable = stringTables[0];

        //Debug.Log($"The main locale in <b>{stringTableCollection.name}</b> is <b>{stringTable.LocaleIdentifier}</b>. Selecting this one for save the captions.");


        /* Set the entries: Add, override or keep */
        string entryID;
        string textEntryValue;
        string safeTextEntryValue;
        StringTableEntry stringTableEntry;

        speechSO.LocalizedCaptions = new LocalizedString[Subtitles.Count];
        string firstRow;
        string secondRow;

        for (int subtitleNumber = 0; subtitleNumber < Subtitles.Count; subtitleNumber++)
        {
            entryID = $"{ID} {subtitleNumber}";
            firstRow = Subtitles[subtitleNumber].FirstRow;
            secondRow = Subtitles[subtitleNumber].SecondRow;

            // Set the reference for Speech Scriptable Object
            speechSO.LocalizedCaptions[subtitleNumber] = new LocalizedString(stringTableCollection.TableCollectionNameReference, (TableEntryReference)entryID);


            // Get the values for the entry
            textEntryValue = $"{firstRow}\n{secondRow}";
            safeTextEntryValue = Regex.Replace(textEntryValue, "[" + Regex.Escape(CAPTIONS_FORBIDDEN_CHARACTERS) + "]", "");

            if (!textEntryValue.Equals(safeTextEntryValue))
            {
                Debug.LogError($"<b>{entryID}</b> has forbidden characters: {CAPTIONS_FORBIDDEN_CHARACTERS}\nTaking out (overriding or not)");
            }

            stringTableEntry = stringTable.GetEntry(entryID); // If there is no entry with the "entryID" key, it will return a null

            if (stringTableEntry != null && !string.IsNullOrEmpty(stringTableEntry.Value)) // If the entry it's not empty...
            {
                if (!stringTableEntry.Value.Equals(textEntryValue)) // and the new value is different...
                {
                    if (overrideCaptions)
                    {
                        Debug.LogWarning($"Overriding the value <b>{stringTableEntry.Value.Replace("\n", " ")}</b> for <b>{textEntryValue.Replace("\n", " ")}</b>\n" +
                       $"It is stored in the key <b>{stringTableEntry.Key}</b> and the table <b>{stringTable.name}</b> in the collection <b>{stringTableCollection.name}</b>");
                    }
                    else
                    {
                        Debug.Log($"Keeping the old value: <b>{stringTableEntry.Value.Replace("\n", " ")}</b>\n" + 
                            $"It is stored in the key <b>{stringTableEntry.Key}</b> and the table <b>{stringTable.name}</b> in the collection <b>{stringTableCollection.name}</b>");
                        
                        
                        continue; // Don't override
                    }
                }
            }

            // Add a new or override
            stringTable.AddEntry(entryID, safeTextEntryValue); // Warning: It doesn't complain if it overrides the value that is already in the table (with the same key)
        }


        /* Save the changes */
        if (Application.isEditor)
        {
            EditorUtility.SetDirty(stringTableCollection);
            EditorUtility.SetDirty(stringTable); // Marks the StringTable as 'dirty' and then saves all 'dirty' assets with SaveAssets()
            EditorUtility.SetDirty(stringTable.SharedData); // Important to save also permanently the changes in the SharedTableData
            EditorUtility.SetDirty(speechSO); // To save permanently the changes

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            /* Refresh the Localization window (Not working properly) */
            stringTableCollection.RefreshAddressables();
        }
        #endregion

        #region Caption Time Intervals

        /* Create a new AssetTableCollection & his related */
        string timeIntervalsNewFolder = Path.Combine(saveFolderAbsolutePath, TIME_INTERVALS_TABLES_NAME);

        if (timeAssetTableCollection == null)
        {    
            if (File.Exists(Path.Combine(timeIntervalsNewFolder, $"{timeIntervalsFilesName}.asset"))) // Should never happen because we are creating our files inside a new folder
            {
                Debug.LogWarning($"Trying to override a AssetTableCollection with the same name:\n- {timeIntervalsFilesName}.asset\n- {timeIntervalsFilesName} shared data.asset\nIn the folder: <b>{timeIntervalsNewFolder}</b>\nABORTING: delete manually the files if you want override it");
                return false;
            }

            /* Create a new AssetTableCollection & his related */
            timeAssetTableCollection = LocalizationEditorSettings.CreateAssetTableCollection(timeIntervalsFilesName, timeIntervalsNewFolder);
            timeAssetTableCollection.Group = SPEECH_TABLE_COLLECTION_GROUP;


            Debug.Log($"<b>{timeIntervalsFilesName}</b> was created and saved to the folder: {timeIntervalsNewFolder}");
        }

        /* Get the AssetTable with the main Locale */
        ReadOnlyCollection<AssetTable> timeAssetTables = timeAssetTableCollection.AssetTables; // Warning: The LocalizationTables windows may have the tables sorted by the LocalizationSettings.asset in the field "Availiable Locales". The index of the ReadOnlyCollection<AssetTable> is equal to the order of the AssetTable.asset assigned in the field "Tables" in the AssetTableCollection.asset

        if (timeAssetTables.Count < 1)
        {
            Debug.LogWarning("Add at least one AssetTable to the AssetTableCollection.\nABORTING: No captions time intervals values added.");
            return false;
        }

        AssetTable timeAssetTable = timeAssetTables[0];

        //Debug.Log($"The main locale in <b>{assetTableCollection.name}</b> is <b>{assetTable.LocaleIdentifier}</b>. Selecting this one to save the time intervals.");


        /* Create a directory for the scriptable objects */
        string scriptableObjectsNewFolder = Path.Combine(timeIntervalsNewFolder, INTERVAL_SCRIPTABLE_OBJECTS_FOLDER_NAME, ID);
        scriptableObjectsNewFolder = scriptableObjectsNewFolder.Replace(Application.dataPath, "Assets"); // Convert to relative path

        if (!Directory.Exists(scriptableObjectsNewFolder))
        {
            Directory.CreateDirectory(scriptableObjectsNewFolder);
        }

        /* Set the entries: Add, override or keep */
        CaptionTimeInterval timeAssetEntryValue;
        AssetTableEntry timeAssetTableEntry;
        string scriptableObjectsPath;

        speechSO.LocalizedTimeIntervals = new LocalizedCaptionTimeInterval[TimeIntervals.Count];

        for (int intervalNumber = 0; intervalNumber < TimeIntervals.Count; intervalNumber++)
        {
            entryID = $"{ID} {intervalNumber}";

            // Set the reference for Speech Scriptable Object
            speechSO.LocalizedTimeIntervals[intervalNumber] = new LocalizedCaptionTimeInterval();
            speechSO.LocalizedTimeIntervals[intervalNumber].SetReference(timeAssetTableCollection.TableCollectionNameReference, entryID);

            // Get the values for the entry
            timeAssetEntryValue = ScriptableObject.CreateInstance<CaptionTimeInterval>();
            timeAssetEntryValue.CaptionInterval = TimeIntervals[intervalNumber];

            scriptableObjectsPath = Path.Combine(scriptableObjectsNewFolder, $"{entryID}.asset");

            AssetDatabase.CreateAsset(timeAssetEntryValue, scriptableObjectsPath);

            timeAssetTableEntry = timeAssetTable.GetEntry(entryID); // If there is no entry with the "entryID" key, it will return a null

            if (timeAssetTableEntry == null)
            {
                /* TODO: Discover how add assets values to the entries. For the moment seems that Unity restricted that via code because it causes a lot of troubles. Maybe it can be possible if we learn to create the assets, make them addressables, asign into a addresable group, and obtain his ¿LocalizeID? */
                timeAssetTable.AddEntry(entryID, string.Empty);
            }
            else if(!timeAssetTableEntry.IsEmpty) // If there is a value
            {
                if (overrideCaptions)
                {

                    Debug.LogWarning($"Overriding the value with the key <b>{timeAssetTableEntry.Key}</b> and the table <b>{timeAssetTable.name}</b> in the collection <b>{timeAssetTableCollection.name}</b>");
                }
                else
                {
                    Debug.Log($"Keeping the old value with the key <b>{timeAssetTableEntry.Key}</b> and the table <b>{timeAssetTable.name}</b> in the collection <b>{timeAssetTableCollection.name}</b>");

                    continue; // Don't override
                }
            }
            else
            {
                // There is no value assigned in the entry. We can add the value
            }
        }

        /* Save the changes */
        if (Application.isEditor)
        {
            EditorUtility.SetDirty(stringTableCollection);
            EditorUtility.SetDirty(timeAssetTable);
            EditorUtility.SetDirty(timeAssetTable.SharedData);
            EditorUtility.SetDirty(speechSO);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            timeAssetTableCollection.RefreshAddressables();
        }
        #endregion

        #region Speech AudioClips

        /* Create a new AssetTableCollection & his related */
        string audioClipNewFolder = Path.Combine(saveFolderAbsolutePath, AUDIO_CLIPS_TABLES_NAME);

        if (audioAssetTableCollection == null)
        {
            if (File.Exists(Path.Combine(audioClipNewFolder, $"{audioClipsFilesName}.asset"))) // Should never happen because we are creating our files inside a new folder
            {
                Debug.LogWarning($"Trying to override a AssetTableCollection with the same name:\n- {audioClipsFilesName}.asset\n- {audioClipsFilesName} shared data.asset\nIn the folder: <b>{audioClipNewFolder}</b>\nABORTING: delete manually the files if you want override it");
                return false;
            }

            /* Create a new AssetTableCollection & his related */
            audioAssetTableCollection = LocalizationEditorSettings.CreateAssetTableCollection(audioClipsFilesName, audioClipNewFolder);
            audioAssetTableCollection.Group = SPEECH_TABLE_COLLECTION_GROUP;

            Debug.Log($"<b>{audioClipsFilesName}</b> was created and saved to the folder: {audioClipNewFolder}");
        }

        /* Get the AssetTable with the main Locale */
        ReadOnlyCollection<AssetTable> audioAssetTables = audioAssetTableCollection.AssetTables; // Warning: The LocalizationTables windows may have the tables sorted by the LocalizationSettings.asset in the field "Availiable Locales". The index of the ReadOnlyCollection<AssetTable> is equal to the order of the AssetTable.asset assigned in the field "Tables" in the AssetTableCollection.asset


        if (audioAssetTables.Count < 1)
        {
            Debug.LogWarning("Add at least one AssetTable to the AssetTableCollection.\nABORTING: No captions time intervals values added.");
            return false;
        }

        AssetTable audioAssetTable = audioAssetTables[0];

        //Debug.Log($"The main locale in <b>{assetTableCollection.name}</b> is <b>{assetTable.LocaleIdentifier}</b>. Selecting this one to save the time intervals.");


        /* Create the entry */

        AssetTableEntry audioAssetTableEntry;

        entryID = ID;

        audioAssetTableEntry = audioAssetTable.GetEntry(entryID); // If there is no entry with the "entryID" key, it will return a null

        if (audioAssetTableEntry == null)
        {
            audioAssetTableEntry = audioAssetTable.AddEntry(entryID, string.Empty);
        }
        else if (!audioAssetTableEntry.IsEmpty) // If there is a value
        {
            Debug.Log($"Audio already have an entry");

            if (overrideCaptions)
            {
            }
            else
            {
            }
        }
        else
        {
            // There is no value assigned in the entry. We can add the value
        }

        // Set the Speech SO reference
        speechSO.LocalizedAudio.SetReference(audioAssetTableCollection.TableCollectionNameReference, (TableEntryReference)audioAssetTableEntry.Key);


        /* Save the changes */
        if (Application.isEditor)
        {
            EditorUtility.SetDirty(stringTableCollection);
            EditorUtility.SetDirty(audioAssetTable);
            EditorUtility.SetDirty(audioAssetTable.SharedData);
            EditorUtility.SetDirty(speechSO);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            audioAssetTableCollection.RefreshAddressables();
        }
        #endregion


        /* Show the Localization Tables windows */
        LocalizationTablesWindow localizationTablesWindow = EditorWindow.GetWindow<LocalizationTablesWindow>(); // It opens the windows if it's closed

        if (localizationTablesWindow != null)
        {
            localizationTablesWindow.Focus(); // Focus to use the keyboard in that window
            localizationTablesWindow.EditCollection(timeAssetTableCollection); // Open the CaptionIntervalTable view
            localizationTablesWindow.Repaint();
        }

        //EditorApplication.RepaintHierarchyWindow();
        //EditorApplication.RepaintProjectWindow();


        /* Show a report dialog */
        EditorUtility.DisplayDialog("Export done succesfully",
            $"Please asign the {TimeIntervals[0].GetType()} scriptable objects to the AssetTableCollection named {timeAssetTableCollection.name}\n\n" +
            $"Save info:\n" +
            $"  ‣ Main locale in {stringTableCollection.name} is {stringTable.LocaleIdentifier}.\n" +
            $"  ‣ Main locale in {timeAssetTableCollection.name} is {timeAssetTable.LocaleIdentifier}."
            , "OK");

        return true;
    }
    #endregion
}



