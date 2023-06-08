using UnityEngine;
using UnityEditor;

/*
    TODO: Mejorar y hacer solo con estilo (_DropBoxStyle) con:
            - .onNormal
            - .onHover
            - .onActive

 */

/*
    If it doesn't work, make sure that Unity have all the Windows file managemente permissions
    
    Tener en cuenta si son utf 8 o que! Si no es utf-8 puede que pierdas accentos o datos! (Hacer check!)
    No se come los \t \n porque interpreta los reales. esos serian \\t y \\n
 */

public class CaptionsParserWindow : EditorWindow
{
    
    const int BORDER_WIDTH = 2;
    GUIStyle _DropBorderStyle;
    GUIStyle _DropOutStyle;
    GUIStyle _DropInStyle;

    GUIStyle _LastStyle = new GUIStyle();

    Rect _DropArea;
    Rect _BorderArea;
    Event _CurrentEvent;

    [MenuItem("Window/Captions parser")]
    public static void ShowWindow()
    {
        GetWindow<CaptionsParserWindow>("Captions parser");
    }

    void OnEnable() // GUI it's still not created
    {
        _DropBorderStyle = new GUIStyle();
        _DropBorderStyle.normal.background = MakeGuiTex(2, 2, Color.black);

        _DropOutStyle = new GUIStyle();
        _DropOutStyle.normal.background = MakeGuiTex(2, 2, new Color(0.2f, 0.2f, 0.2f, 1f));
        _DropOutStyle.normal.textColor = new Color(.8f, .8f, .8f, 1f);
        _DropOutStyle.fontStyle = FontStyle.Bold;
        _DropOutStyle.alignment = TextAnchor.MiddleCenter;

        _DropInStyle = new GUIStyle();
        _DropInStyle.normal.background = MakeGuiTex(2, 2, new Color(0.6f, 0.6f, 0.6f, 1f));
        _DropInStyle.normal.textColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        _DropInStyle.fontStyle = FontStyle.Bold;
        _DropInStyle.alignment = TextAnchor.MiddleCenter;

        _LastStyle = _DropOutStyle; // Aquí al final porque sino la copia es null. 
    }

    void OnGUI() // Paint GUI every time from 0
    {
        _CurrentEvent = Event.current;
        //Debug.Log(_CurrentEvent.type);

        // First paint
        GUILayout.Label("Drag and drop a .txt file here:");

        // Second paint

        GUILayout.FlexibleSpace(); // Empuja hacia abajo

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace(); // Empuja hacia la derecha

        _DropArea = GUILayoutUtility.GetRect(position.width / 1.5f, position.height / 2, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));

        // Crea el rectángulo del borde basado en el área principal
        _BorderArea = new Rect(_DropArea.x - BORDER_WIDTH, _DropArea.y - BORDER_WIDTH, _DropArea.width + 2 * BORDER_WIDTH, _DropArea.height + 2 * BORDER_WIDTH);


        GUILayout.FlexibleSpace(); // Empuja hacia la izquierda
        GUILayout.EndHorizontal();

        GUILayout.FlexibleSpace(); // Empuja hacia arriba


        switch (_CurrentEvent.type)
        {
            case EventType.DragUpdated:

                if (_DropArea.Contains(_CurrentEvent.mousePosition))
                {
                    if (_LastStyle != _DropInStyle)
                    {
                        _LastStyle = _DropInStyle;
                        Repaint();
                    }

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                }
                else
                {
                    if (_LastStyle != _DropOutStyle)
                    {
                        _LastStyle = _DropOutStyle;
                        Repaint();
                    }

                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                }

                break;

            case EventType.DragExited:
                if (_LastStyle != _DropOutStyle)
                {
                    _LastStyle = _DropOutStyle;
                    Repaint();
                }
                break;

            case EventType.DragPerform:
                if (_LastStyle != _DropOutStyle)
                {
                    _LastStyle = _DropOutStyle;
                    Repaint();
                }

                if (_DropArea.Contains(_CurrentEvent.mousePosition))
                {
                    //Debug.Log("Droped files inside _DropArea.");
                    DragAndDrop.AcceptDrag(); // Dices que aceptas los objetos que se están arrastrando y soltando.

                    CaptionParser captionParser = new CaptionParser();

                    // Only for assets inside Unity
                    /*
                    foreach (Object draggedObject in DragAndDrop.objectReferences)
                    {
                        Debug.Log("Path: " + AssetDatabase.GetAssetPath(draggedObject));
                        captionParser.Parse(AssetDatabase.GetAssetPath(draggedObject));
                    }
                    */

                    foreach (string draggedPath in DragAndDrop.paths)
                    {
                        //Debug.Log("Path: " + draggedPath);
                        captionParser.Parse(draggedPath);
                    }

                    Event.current.Use(); // Útil en situaciones en las que varios controles pueden responder al mismo evento y se desea evitar comportamientos no deseados o conflictos. Evitamos que se haga un click
                }

                break;

            case EventType.MouseDown:

                if (_CurrentEvent.button == 0 && _DropArea.Contains(_CurrentEvent.mousePosition))
                {
                    // Se hizo clic con el botón izquierdo del mouse dentro de _DropArea.
                    Debug.Log("Mouse left click detected inside _DropArea.");

                    _CurrentEvent.Use(); // Marca este evento como utilizado para que otros controles no lo procesen.
                }

                break;

            default:
                //Do nothing
                break;
        }

        // Dibuja el borde y luego el área principal
        GUI.Box(_BorderArea, GUIContent.none, _DropBorderStyle);
        GUI.Box(_DropArea, "Drop files here", _LastStyle);
    }


    private Texture2D MakeGuiTex(int width, int height, Color col) // Important to use this because Texture2D.blackTexture and similar doesn't work
    {     
        Color[] pix = new Color[width * height];

        for (int i = 0; i < pix.Length; ++i)
        {
            pix[i] = col;
        }

        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
        
    }
}
