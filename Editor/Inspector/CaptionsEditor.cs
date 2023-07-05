using UnityEngine;
using UnityEditor;
using System.IO;
using System.Diagnostics;

/*
    Adds a button on Captions scriptables objects for validate his data and upload the captions in Localization tables.
    Also adds an extra icon to the scriptable objects icon. This icon is named CAPTIONS_ICON_NAME itmust be in the same folder as this script.
*/

[CustomEditor(typeof(Captions))]
public class CaptionsEditor : Editor
{
    const string CAPTIONS_ICON_NAME = "CaptionsIcon.png";

    static string _IconPath;
    static Texture2D _IconTexture;
    static StackFrame _Frame;

    [InitializeOnLoadMethod]
    static void Init()
    {
        EditorApplication.projectWindowItemOnGUI += ProjectWindowItemOnGUI;
    }

    public override void OnInspectorGUI()
    {
        Captions captionsScript = (Captions)target;

        if (GUILayout.Button("Validate captions")) // If the button is pressed
        {
            captionsScript.ValidateCaptions(); // Call ValidateCaptions from the same script
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Export to Localization Tables"))
        {
            captionsScript.ExportToLocalization();
        }

        EditorGUILayout.Space();

        DrawDefaultInspector(); // Draw the default inspector
    }

    static void ProjectWindowItemOnGUI(string guid, Rect selectionRect)
    {
        if (AssetDatabase.GetMainAssetTypeAtPath(AssetDatabase.GUIDToAssetPath(guid)) == typeof(Captions))
        {
            selectionRect.width = 18;
            selectionRect.height = 18;
            //selectionRect.x = selectionRect.x + 8;
            //selectionRect.y = selectionRect.y + 25;

            _IconPath = GetRelativePath("Packages", GetScriptAbsolutePath());

            if (string.IsNullOrWhiteSpace(_IconPath))
            {
                _IconPath = GetRelativePath("Assets", GetScriptAbsolutePath());

                if (string.IsNullOrWhiteSpace(_IconPath))
                {
                    UnityEngine.Debug.LogError($"This script it's not neither in 'Assets' nor in 'Packages': {_IconPath}");

                    _IconPath = GetRelativePath("Editor", GetScriptAbsolutePath());

                    if (string.IsNullOrWhiteSpace(_IconPath))
                    {
                        
                        UnityEngine.Debug.LogError($"This script an extrange path: {GetScriptAbsolutePath()}");
                        return;
                    }
                    else
                    {
                        UnityEngine.Debug.Log($"TODO: Find the logo in this temp folder: {GetScriptAbsolutePath()}");
                        return;
                    }
                }
            }
            
            _IconPath = Path.Combine(Path.GetDirectoryName(_IconPath), CAPTIONS_ICON_NAME);

            if (!File.Exists(_IconPath))
            {
                UnityEngine.Debug.LogError("File not found: " + _IconPath);
                return;
            }

            _IconTexture = new Texture2D(2, 2);

            if (!_IconTexture.LoadImage(File.ReadAllBytes(_IconPath)))
            {
                UnityEngine.Debug.LogError("Failed to load texture from file: " + _IconPath);
                return;
            }

            //GUI.DrawTexture(selectionRect, AssetDatabase.LoadAssetAtPath<Texture>(_IconPath), ScaleMode.ScaleToFit);
            GUI.DrawTexture(selectionRect, _IconTexture, ScaleMode.ScaleToFit);
        }
    }

    // Useful to get the path of the script calling the static function. If it's not a static function we can get it via UnityEngine.Object.GetInstanceID()
    static string GetScriptAbsolutePath()
    {
        // Use StackFrame to obtain the path to the file that is executing the method
        return Path.GetFullPath(new StackTrace(true).GetFrame(0).GetFileName());
    }

    static string GetRelativePath(string newRootFolderName, string absolutePath)
    {
        // Input:   Absolute path -> C:\Users\Fenikkel\AequalandUtils\Packages\Simple Speech\CaptionsEditor.cs
        // Output:  Relative path -> Packages/Simple Speech/CaptionsEditor.cs

        absolutePath = absolutePath.Replace('\\', '/'); // Normalize the path to the standard. Folders and file names cant have a slash or back slash, so this wont change any name.

        int index = absolutePath.IndexOf($"/{newRootFolderName}/");

        if (index < 0)
        {
            UnityEngine.Debug.LogWarning($"The specified path is not located within a '<b>{newRootFolderName}</b>' directory.");
            return null;
        }

        return absolutePath.Substring(index + 1); // we take out the first slash character
    }
}
