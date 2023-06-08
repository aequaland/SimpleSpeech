using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(fileName = "Narration", menuName = "Fenikkel/Narration", order = 0)]
public class Speech : ScriptableObject
{
    public LocalizedAudioClip LocalizedAudio;
    public LocalizedString[] LocalizedCaptions; // Subtitle
    public LocalizedCaptionTimeInterval[] LocalizedTimeIntervals; 


    /*
    int _StupidCounter;
   
    private void OnValidate()
    {
        // Warning
        if (LocalizedAudio.IsEmpty)
        {
            Debug.LogWarning($"<b>LocalizedAudio</b> of {this.name} it's not asigned");
        }

        if (LocalizedCaptions != null && CaptionScreenTime != null)
        {
            // Warning
            for (int i = 0; i < LocalizedCaptions.Length; i++)
            {
                if (LocalizedCaptions[i].IsEmpty)
                {
                    Debug.LogWarning($"<b>LocalizedCaptions</b> of {this.name} it's not assigned in index {i}");
                }
            }

            // Warning
            if (CaptionScreenTime.Length == _StupidCounter - 1 || CaptionScreenTime.Length == _StupidCounter + 1)
            {
                
                _StupidCounter = CaptionScreenTime.Length < _StupidCounter ? ++_StupidCounter : --_StupidCounter;
                Debug.LogWarning($"In {this.name}, CaptionScreenTime must have the same amount of variables as LocalizedCaptions");
            }
            else if(CaptionScreenTime.Length != _StupidCounter)
            {
                // Initializing _StupidCounter
                _StupidCounter = CaptionScreenTime.Length;
            }
            else
            {
                // Equals. Everything ok
            }

            // Force the same aray size
            if (LocalizedCaptions.Length != CaptionScreenTime.Length)
            {
                Vector2[] tempVec = new Vector2[LocalizedCaptions.Length];

                for (int i = 0; i < LocalizedCaptions.Length; i++)
                {
                    if (i < CaptionScreenTime.Length)
                    {
                        tempVec[i] = CaptionScreenTime[i];
                    }
                }

                CaptionScreenTime = tempVec;
                _StupidCounter = CaptionScreenTime.Length;
            }


            // Force ascending values
            for (int i = 0; i < CaptionScreenTime.Length; i++)
            {
                // Check values
                if (CaptionScreenTime[i].y < CaptionScreenTime[i].x)
                {
                    CaptionScreenTime[i].y = CaptionScreenTime[i].x;
                }

                // check neighbour values
                if ((i + 1) < CaptionScreenTime.Length)
                {
                    if (CaptionScreenTime[i + 1].x < CaptionScreenTime[i].y)
                    {
                        CaptionScreenTime[i + 1].x = CaptionScreenTime[i].y;
                    }
                }
            }
        }
    }
    */
}
