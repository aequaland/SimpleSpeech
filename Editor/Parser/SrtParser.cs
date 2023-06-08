using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class SrtParser : ICaptionParser
{
    const int TIME_FORMAT_LENGTH = 12;

    enum SrtFilePointer
    {
        Index,
        Time,
        RowOneText,
        RowTwoText,
        CaptionEndSpace
    }

    public bool Parse(string filePath)
    {
        /* Request the path to save the asset (or replace an old one) */
        string newAssetPath = EditorUtility.SaveFilePanelInProject("Save Captions Asset", Path.GetFileNameWithoutExtension(filePath), "asset", "It's a good practice to save this file inside a folder named Editor. This way this data won't be added in the project builds."); // The message it's only showed in MAC.

        if (string.IsNullOrEmpty(newAssetPath))
        {
            Debug.LogWarning($"Captions asset creation aborted for <b>{Path.GetFileName(filePath)}</b>");
            return false;
        }


        /* Parse the .srt to a .asset */
        Captions captionsSO = ScriptableObject.CreateInstance<Captions>();
        captionsSO.ID = Path.GetFileNameWithoutExtension(filePath);

        try
        {
            using (StreamReader reader = new StreamReader(filePath, Encoding.UTF8))
            {
                SrtFilePointer filePointer = SrtFilePointer.Index;
                string readedLine;
                int fileLineIndex = 0;
                int tmpIndex;
                int srtIndex = 1; // .srt starts with index 1
                TimeSpan entryTime = new TimeSpan();
                TimeSpan exitTime = new TimeSpan();
                string subtitleOne = "";
                string subtitleTwo = "";

                while ((readedLine = reader.ReadLine()) != null)
                {
                    fileLineIndex++;

                    switch (filePointer)
                    {
                        case SrtFilePointer.Index:

                            if (string.IsNullOrWhiteSpace(readedLine))
                            {
                                Debug.LogWarning($"Extra line break detected on line <b>{fileLineIndex}</b>");
                                continue;
                            }


                            if (int.TryParse(readedLine, out tmpIndex))
                            {
                                if (srtIndex == tmpIndex)
                                {
                                    subtitleOne = "";
                                    subtitleTwo = "";
                                    srtIndex++;
                                    filePointer = SrtFilePointer.Time;
                                }
                                else
                                {
                                    Debug.LogWarning($"Line {fileLineIndex} has an incorrect <b>srt index</b>: It's {readedLine} and it should be {srtIndex}");
                                    return false;
                                }
                            }
                            else
                            {
                                Debug.LogWarning($"Line {fileLineIndex} it should be a <b>index</b>: {readedLine}\nIf the previous line is a line break, please, delete it");
                                return false;
                            }

                            break;

                        case SrtFilePointer.Time:

                            if (string.IsNullOrWhiteSpace(readedLine))
                            {
                                Debug.LogWarning($"Extra line break detected on line <b>{fileLineIndex}</b>");
                                continue;
                            }

                            readedLine = readedLine.Replace(',', '.'); // To TimeSpan format

                            string[] intervalKeyframes = readedLine.Split(new[] { "-->" }, StringSplitOptions.RemoveEmptyEntries);

                            if (intervalKeyframes.Length != 2)
                            {
                                Debug.LogWarning($"Line {fileLineIndex} it should be a <b>time interval</b>: {readedLine}\nIf it's a time interval, make sure that has the correct format.");
                                return false;
                            }

                            // Delete the white spaces
                            intervalKeyframes[0] = intervalKeyframes[0].Trim();
                            intervalKeyframes[1] = intervalKeyframes[1].Trim();

                            if (TIME_FORMAT_LENGTH < intervalKeyframes[0].Length)
                            {
                                Debug.LogWarning($"Line {fileLineIndex} in entry time has a too large milliseconds number or extra parameters. If it's a large milliseconds number, we gonna get only the three first digits. If it's because a extra parameters, we can't recognize them.");
                                intervalKeyframes[0] = intervalKeyframes[0].Substring(0, 12);
                            }

                            if (TIME_FORMAT_LENGTH < intervalKeyframes[1].Length)
                            {
                                Debug.LogWarning($"Line {fileLineIndex} in exit time has a too large milliseconds number or extra parameters. If it's a large milliseconds number, we gonna get only the <b>three first digits</b>. If it's because a extra parameters, we can't recognize them.");
                                intervalKeyframes[1] = intervalKeyframes[1].Substring(0, 12);
                            }

                            if (!TimeSpan.TryParse(intervalKeyframes[0], out entryTime))
                            {
                                Debug.LogWarning($"Line {fileLineIndex}: incorrect <b>entry time format</b>: {readedLine}");
                                return false;
                            }

                            if (!TimeSpan.TryParse(intervalKeyframes[1], out exitTime))
                            {
                                Debug.LogWarning($"Line {fileLineIndex}: incorrect <b>exit time format</b>: {readedLine}");
                                return false;
                            }

                            filePointer = SrtFilePointer.RowOneText;
                            break;

                        case SrtFilePointer.RowOneText:

                            if (string.IsNullOrWhiteSpace(readedLine))
                            {
                                Debug.LogWarning($"Subtitle row one in line {fileLineIndex} can't be emptyLine. If it's a line break, delete it please.");
                                return false;
                            }

                            subtitleOne = readedLine;
                            filePointer = SrtFilePointer.RowTwoText;
                            break;

                        case SrtFilePointer.RowTwoText:
                            subtitleTwo = readedLine;

                            filePointer = string.IsNullOrWhiteSpace(readedLine) ? SrtFilePointer.Index : SrtFilePointer.CaptionEndSpace; // Jump directly to Index if the second row is white space

                            if (!captionsSO.AddSubtitle(subtitleOne, subtitleTwo, entryTime, exitTime))
                            {
                                Debug.LogWarning($"Error adding the subtitle <b>{srtIndex - 1}</b>. Cancelling the parse.");
                                return false;
                            }

                            //Debug.Log($"Adding:\n- Subtitle one: <b>{subtitleOne}</b>\n- Subtitle two: <b>{subtitleTwo}</b>\n- Entry time: <b>{entryTime}</b>\n- Exit time: <b>{exitTime}</b>");

                            break;

                        case SrtFilePointer.CaptionEndSpace:

                            if (!string.IsNullOrWhiteSpace(readedLine))
                            {
                                Debug.LogWarning($"Invalid srt format. Line <b>{fileLineIndex}</b> should be a white space.\nCancelling the parse.");
                                return false;
                            }

                            filePointer = SrtFilePointer.Index;
                            break;

                        default:
                            Debug.LogWarning($"Pointer type not implemented: <b>{filePointer}</b>");
                            return false;
                    }
                }

                if (filePointer == SrtFilePointer.RowTwoText) // add if the last caption if just have one row
                {
                    captionsSO.AddSubtitle(subtitleOne, subtitleTwo, entryTime, exitTime);
                    //Debug.Log($"Adding LAST:\n- Subtitle one: <b>{subtitleOne}</b>\n- Subtitle two: <b>{subtitleTwo}</b>\n- Entry time: <b>{entryTime}</b>\n- Exit time: <b>{exitTime}</b>");
                }
                else if (filePointer != SrtFilePointer.Index && filePointer != SrtFilePointer.CaptionEndSpace)
                {
                    Debug.LogWarning($"Can't add the last caption with the srt index: <b>{srtIndex - 1}</b>");
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            Debug.LogWarning($"You don't have access to this file. Try to enable via file properties or your user permissions. Path: <b>{filePath}</b>");
            return false;
        }


        /* Create the .asset */
        if (File.Exists(newAssetPath))
        {
            Debug.LogWarning($"These captions were overrided: <b>{newAssetPath}</b>");
        }

        try
        {
            AssetDatabase.CreateAsset(captionsSO, newAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        catch
        {
            Debug.LogWarning("Error during the creation of the asset");
            return false;
        }

        Debug.Log($"<b>{Path.GetFileName(filePath)}</b> was parsed to <b>{Path.GetFileName(newAssetPath)}</b>.\nSaved to the folder: {newAssetPath}");
        return true;
    }



}
