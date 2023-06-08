using System;
using System.IO;
using UnityEngine;

/* TODO: Allow folders */
public class CaptionParser : ICaptionParser
{
    public enum CaptionType
    {
        SRT,
        TXT
    }

    public bool Parse(string filePath)
    {
        if (!File.Exists(filePath) && !Directory.Exists(filePath))
        {
            Debug.LogWarning("Incorrect path or it's a folder");
            return false;
        }

        if (!IsUtf8(filePath))
        {
            Debug.LogWarning("This file it's not using the UTF-8 encoding. Please save the file with UTF-8 encoding");
            return false;
        }


        string fileExtension = Path.GetExtension(filePath);

        if (string.IsNullOrEmpty(fileExtension))
        {
            Debug.LogWarning("No folders allowed");
            return false;
        }

        fileExtension = fileExtension.Substring(1); // without dot "."
        fileExtension = fileExtension.ToUpper();

        ICaptionParser parser = null;
        CaptionType captionType;

        if (Enum.TryParse(fileExtension, out captionType))
        {
            switch (captionType)
            {
                case CaptionType.TXT:
                    Debug.LogWarning("Extension is \".txt\". It is going to be treated like a \".srt\"");
                    goto case CaptionType.SRT;

                case CaptionType.SRT:
                    parser = new SrtParser();
                    break;

                default:
                    Debug.LogError("FileExtension not implemented");
                    return false;
            }
        }
        else
        {
            Debug.LogError("CaptionType not implemented");
            return false;
        }

        parser.Parse(filePath);

        return true;
    }

    private bool IsUtf8(string filePath)
    {
        /*  
            - UTF-8 has ASCII characters (1 byte characters) and also has special characters that have 2, 3 or 4 bytes.
            - This method checks if the file has 2 or more bytes.
            - It can give false positive if a file with a diferent codification meets these conditions by chance (for example a direct access). Or, the file doesn't have any special characters(´¨^)
            - Works even if the file has BOM (ByteOrderMarks)
        */

        if (Directory.Exists(filePath))
        {
            Debug.LogWarning("Can't check the encoding of a <b>folder</b>");
            return false;
        }

        try
        {
            byte[] bytes = File.ReadAllBytes(filePath);

            for (int i = 0; i < bytes.Length; i++)
            {
                byte b = bytes[i];
                if (b <= 0x7F) // ASCII
                {
                    continue;
                }

                if (b >= 0xC2 && b <= 0xDF) // 2-byte sequence
                {
                    if (i + 1 >= bytes.Length || bytes[i + 1] < 0x80 || bytes[i + 1] > 0xBF)
                    {
                        return false;
                    }
                    i++;
                }
                else if (b >= 0xE0 && b <= 0xEF) // 3-byte sequence
                {
                    if (i + 2 >= bytes.Length || bytes[i + 1] < 0x80 || bytes[i + 1] > 0xBF || bytes[i + 2] < 0x80 || bytes[i + 2] > 0xBF)
                    {
                        return false;
                    }
                    i += 2;
                }
                else if (b >= 0xF0 && b <= 0xF4) // 4-byte sequence
                {
                    if (i + 3 >= bytes.Length || bytes[i + 1] < 0x80 || bytes[i + 1] > 0xBF || bytes[i + 2] < 0x80 || bytes[i + 2] > 0xBF || bytes[i + 3] < 0x80 || bytes[i + 3] > 0xBF)
                    {
                        return false;
                    }
                    i += 3;
                }
                else
                {
                    return false;
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            Debug.LogError($"You don't have access to this file. Try to enable via file properties or your user permissions. Path: <b>{filePath}</b>");
            return false;
        }

        return true;
    }
}
