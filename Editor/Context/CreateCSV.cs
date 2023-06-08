using System.Text;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;

// https://docs.unity3d.com/ScriptReference/MenuCommand-context.html

public class CreateCSV //: EditorWindow
{

    /* 
        You can use this function going to any StringTableCollection.asset, click it, and in the inspector, you can click at the three dots (or kog) and select "Print CSV"
        - CONTEXT means the cog o the three points when you select the asset
        - StringTableCollection is the type of the asset
        - Print CSV is the name of the option
    */

    [MenuItem("CONTEXT/StringTableCollection/Print CSV")]
    public static void CreateTheCSV(MenuCommand command)
    {
        StringTableCollection collection = command.context as StringTableCollection;

        StringBuilder stringBuilder = new StringBuilder();

        // Header
        stringBuilder.Append("Key,");
        foreach (StringTable table in collection.StringTables)
        {
            stringBuilder.Append(table.LocaleIdentifier);
            stringBuilder.Append(",");
        }
        stringBuilder.Append("\n");

        // Add each row   
        foreach (LocalizationTableCollection.Row<StringTableEntry> row in collection.GetRowEnumerator())
        {
            // Key column
            stringBuilder.Append(row.KeyEntry.Key);
            stringBuilder.Append(",");

            foreach (StringTableEntry tableEntry in row.TableEntries)
            {
                // The table entry will be null if no entry exists for this key
                stringBuilder.Append(tableEntry == null ? string.Empty : tableEntry.Value);
                stringBuilder.Append(",");
            }
            stringBuilder.Append("\n");
        }

        // Print the contents. You could save it to a file here.
        Debug.Log(stringBuilder.ToString());
    }
}

