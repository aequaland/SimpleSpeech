using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;

/* 
 * Tenemos un LocalizeStringEvent y un LocalizeAudioClipEvent, que cuando cambia el asset relacionado, lanza el evento vinculado que reconfigura todo lo necesario para actializar el juego
 */

[RequireComponent(typeof(LocalizeAudioClipEvent), typeof(LocalizeStringEvent), typeof(AudioSource))] // It can't require more than 3 components
public class SpeechController : MonoBehaviour
{
    [Header("Speech")]
    [SerializeField] int _MaxSpeechInQueue = 2;

    [Header("Captions")]
    [Range(0.5f, 1.5f)]
    [SerializeField] float _Size = 1.0f;
    [SerializeField] Color _HighlightColor = Color.clear; // Alpha to 0 disable it
    [SerializeField] bool _FadeTransition = false;
    [Range(0.1f, 0.75f)]
    [SerializeField] float _FadeSpeed = 0.25f; // Seconds we spend to do a fade (in or out)
    [Space]
    public UnityEvent OnStartSpeech;
    public UnityEvent OnEndSpeech;

    LocalizeAudioClipEvent _LocalizeAudioClipEvent; // Speech
    LocalizeStringEvent _LocalizeStringEvent; // Captions
    LocalizeCaptionTimeIntervalEvent _LocalizeTimeIntervalEvent; // TimeInterval
    AudioSource _AudioSource;
    TextMeshProUGUI _CaptionText;

    Queue<Speech> _SpeechQueue = new Queue<Speech>();

    Coroutine _SpeechCoroutine;

    public bool FadeTransition { get { return _FadeTransition; } }
    public int SpeechQueueCount { get { return _SpeechQueue.Count; } }

    #region Singleton
    private static SpeechController _Instance = null; // This value is shared for all DiscoManager instances
    public static SpeechController Instance
    {
        get
        {
            return _Instance;
        }
    }
    #endregion

    private void Awake()
    {
        CheckSingleton();

        // Config Localization
        LocalizationSettings.StringDatabase.MissingTranslationState = MissingTranslationBehavior.PrintWarning; // LocalizationSettings (asset) -> String Database -> Missing Translation State
        LocalizationSettings.StringDatabase.NoTranslationFoundMessage = "No translation found for <b>'{key}'</b> in <b>{table.TableCollectionName}</b> in <b>{locale.name}</b> language";

        Init();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        //Debug.Log("OnSceneLoaded: " + scene.name);
        //Debug.Log(mode);

        RemoveEventListeners();
    }
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void PlaySpeech(Speech speech)  // scriptable onbject
    {
        if (speech == null) 
        {
            Debug.LogWarning("Speech is null");
            return;
        }

        if (speech.LocalizedAudio.IsEmpty)
        {
            Debug.LogWarning($"Please fill the localized variable of <b>{speech.LocalizedAudio.GetType()}</b> of the {speech.GetType()} with name <b>{speech.name}</b>\nAborting");
            return;
        }

        if (speech.LocalizedCaptions.Length != speech.LocalizedTimeIntervals.Length)
        {
            Debug.LogWarning($"Please make the arrays of <b>{speech.LocalizedCaptions.GetType()}</b> and <b>{speech.LocalizedTimeIntervals.GetType()}</b> have the same amount of elements. Search in the {speech.GetType()} with name {speech.name}\nAborting");
            return;
        }

        if (speech.LocalizedCaptions.Length < 1)
        {
            Debug.LogWarning($"Please fill the arrays of <b>{speech.LocalizedCaptions.GetType()}</b> and <b>{speech.LocalizedTimeIntervals.GetType()}</b> of the {speech.GetType()} with name {speech.name}\nAborting");
            return;
        }

        if (_SpeechCoroutine != null)
        {
            Debug.LogWarning("Already speeching. Enqueueing the speech");
            EnqueueSpeech(speech);
            return;
        }


        _SpeechCoroutine = StartCoroutine(PlaySpeechCoroutine(speech));

    }

    private CaptionTimeInterval LoadCaptionTimeInterval(LocalizedCaptionTimeInterval localizedCaptionTimeInterval)
    {
        if (localizedCaptionTimeInterval == null)
        {
            Debug.LogWarning($"<b>{localizedCaptionTimeInterval.GetType()}</b> is null");
            return null;
        }

        if (localizedCaptionTimeInterval.IsEmpty) // Nothing assigned in the variable LocalizedCaptionTimeInterval
        {
            Debug.LogWarning($"Please fill the localized variable of <b>{localizedCaptionTimeInterval.GetType()}</b>\nReturning an empty {localizedCaptionTimeInterval.GetType()}");
            return ScriptableObject.CreateInstance<CaptionTimeInterval>();
        }

        CaptionTimeInterval loaded = localizedCaptionTimeInterval.LoadAsset();

        if (loaded == null) // Have a LocalizedCaptionTimeInterval but the current Table of the Locale doesn't have any value assigned
        {
            Debug.LogWarning($"Please fill the table collection <b>{localizedCaptionTimeInterval.TableReference.TableCollectionName}</b> in the entry <b>{localizedCaptionTimeInterval.TableEntryReference.Key}</b> with a <b>{localizedCaptionTimeInterval.GetType()}</b> type\nReturning an empty {localizedCaptionTimeInterval.GetType()}");
            return ScriptableObject.CreateInstance<CaptionTimeInterval>();
        }
        
        return localizedCaptionTimeInterval.LoadAsset();
    }

    IEnumerator PlaySpeechCoroutine(Speech speech)
    {
        // Init
        SetSpeechAudio(speech.LocalizedAudio);
        _AudioSource.Play();

        OnStartSpeech.Invoke();

        // Wait until speech start
        CaptionTimeInterval currentTimeInterval = LoadCaptionTimeInterval(speech.LocalizedTimeIntervals[0]);

        yield return new WaitForSeconds(currentTimeInterval.GetEntryTotalSeconds());

        // Blink
        float onScreenTime;
        float fullOpacityTime;

        for (int i = 0; i < speech.LocalizedTimeIntervals.Length; i++)
        {
            currentTimeInterval = LoadCaptionTimeInterval(speech.LocalizedTimeIntervals[i]);

            // Calculate the next on screen time for the caption
            onScreenTime = currentTimeInterval.GetScreenTime() - _FadeSpeed * 2f;
            onScreenTime = Mathf.Max(0f, onScreenTime);

            // Set the next caption text
            SetCaptionsText(speech.LocalizedCaptions[i]);

            // Show the text during the CaptionScreenTime
            if (_FadeTransition)
            {
                fullOpacityTime = onScreenTime - _FadeSpeed * 2.0f;
                //yield return _CaptionText.Blink(BlinkMode.InOut, 1, _FadeSpeed, _FadeSpeed, onScreenTime, 0f, Interpolation.EaseInOut, Interpolation.EaseInOut);
                yield return FadeInOutCoroutine( _CaptionText, _FadeSpeed, _FadeSpeed, onScreenTime, 0f);
            }
            else
            {
                _CaptionText.alpha = 1.0f;
                yield return new WaitForSeconds(onScreenTime);
                _CaptionText.alpha = 0.0f;
            }
        }

        // Wait audio
        yield return new WaitWhile(() => _AudioSource.isPlaying);

        OnEndSpeech.Invoke();

        if (0 < _SpeechQueue.Count)
        {
            yield return new WaitForSeconds(0.5f); // Space between queued audios

            _SpeechCoroutine = null;
            // Play next in queue
            PlaySpeech(_SpeechQueue.Dequeue());
        }
        else
        {
            _SpeechCoroutine = null;
        }
    }

    private void EnqueueSpeech(Speech speech) 
    {
        if (_MaxSpeechInQueue == 0)
        {
            return;
        }

        if ( _MaxSpeechInQueue <= _SpeechQueue.Count)
        {
            // Dequeue the oldest
            Speech dequeued = _SpeechQueue.Dequeue();
            Debug.LogWarning($"Speech <b>{dequeued.name}</b> was dequeued");
        }

        _SpeechQueue.Enqueue(speech);
    }

    private void SetSpeechAudio(LocalizedAudioClip localizedAudioClip) // Audio asset reference (key) of the Speech Audio Table
    {
        if (localizedAudioClip.IsEmpty)
        {
            Debug.LogWarning("Please fill the <b>LocalizedAudioClip</b> in the <b>Speech</b> scriptable object");
            return;
        }

        _LocalizeAudioClipEvent.AssetReference = localizedAudioClip;
    }

    private void SetCaptionsText(LocalizedString localizedString) // Subtitle string reference (key) of the Speech Caption Table
    {
        if (localizedString.IsEmpty)
        {
            Debug.LogWarning("Please fill the <b>LocalizedString</b> in the <b>Speech</b> scriptable object");
            return;
        }

        // Da un nuevo valor que triggerea el LocalizeStringEvent.OnUpdateString
        _LocalizeStringEvent.StringReference = localizedString;
    }

    #region Update events

    private void ChangeAudioClip(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("This language doesn't have an audio asigned");
            return;
        }

        if (_AudioSource.isPlaying && _AudioSource.time < clip.length)
        {
            float currentTime = _AudioSource.time;
            _AudioSource.clip = clip;
            _AudioSource.time = currentTime;
            _AudioSource.Play();
        }
        else
        {
            _AudioSource.clip = clip;
            _AudioSource.time = 0; // Important to do the time reset
        }
    }

    private void ChangeCaptionsText(string captionString)
    {
        if (captionString == string.Empty)
        {
            //Debug.LogWarning("This language doesn't have an string asigned");
            _CaptionText.text = "<b>EMPTY</b>";
            return;
        }

        // Set some invisible letters at the start and end of the string to extend the highlight
        //captionString = $"<alpha=#00>a<alpha=#FF>{captionString}<alpha=#00>a<alpha=#FF>";

        /*
         * Put your asset and material preset in: Resources/Fonts & Materials/
         * Or in the custom path configured in: ProjectSettings -> TextMesh Pro -> Settings (http://digitalnativestudios.com/textmeshpro/docs/settings/#font)
         */
        _CaptionText.text = $"<mark=#{ColorUtility.ToHtmlStringRGBA(_HighlightColor)}>{captionString}</mark></font>"; // $ -> to save memory

        // <font=\"{_CaptionText.font.name}\">
        // Example: <font="LiberationSans SDF">  

        //Debug.Log($"Caption changed to: {_CaptionText.text}");
    }

    private void ChangeCaptionTimeInterval(CaptionTimeInterval captionTimeInterval)
    {
        /*
        if (captionTimeInterval == null)
        {
            Debug.LogWarning("This language doesn't have a CaptionTimeInterval scriptable object asigned");
            return;
        }

        Debug.Log($"Entry time: {captionTimeInterval.CaptionInterval.EntryTime.Seconds}  Exit time {captionTimeInterval.CaptionInterval.ExitTime.Seconds}");
        */
    }

    #endregion

    private void Init()
    {
        _LocalizeAudioClipEvent = GetComponent<LocalizeAudioClipEvent>();
        _LocalizeStringEvent = GetComponent<LocalizeStringEvent>();
        _LocalizeTimeIntervalEvent = GetComponent<LocalizeCaptionTimeIntervalEvent>();

        _AudioSource = GetComponent<AudioSource>();
        _CaptionText = GetComponentInChildren<TextMeshProUGUI>();

        // Size
        _CaptionText.rectTransform.localScale = Vector3.one * _Size;

        /* Automatization */
        if (_LocalizeAudioClipEvent.OnUpdateAsset.GetPersistentEventCount() != 0)
        {
            Debug.LogWarning("Be careful, you added a event to OnUpdateAsset, we ALREADY update the audio asset via script. Count: " + _LocalizeAudioClipEvent.OnUpdateAsset.GetPersistentEventCount());
        }

        if (_LocalizeStringEvent.OnUpdateString.GetPersistentEventCount() != 0)
        {
            Debug.LogWarning("Be careful, you added a event to OnUpdateString, we ALREADY update the text via script. Count: " + _LocalizeStringEvent.OnUpdateString.GetPersistentEventCount());
        }

        /* Update events */
        _LocalizeAudioClipEvent.OnUpdateAsset.AddListener((audioClip) => ChangeAudioClip(audioClip)); // alternative if it doesn't work on a certain platform -> new UnityAction<AudioClip>((audioClip) => ChangeAudioClip(audioClip))
        _LocalizeStringEvent.OnUpdateString.AddListener((captionString) => ChangeCaptionsText(captionString));
        _LocalizeTimeIntervalEvent.OnUpdateAsset.AddListener((captionTimeInterval) => ChangeCaptionTimeInterval(captionTimeInterval));

        /* Hide Text */
        _CaptionText.alpha = 0f;
    }

    private void RemoveEventListeners()
    {
        OnStartSpeech.RemoveAllListeners();
        OnEndSpeech.RemoveAllListeners();
    }

    private void CheckSingleton()
    {
        if (_Instance != null && _Instance != this) //If the instance we got is from another
        {
            Destroy(this.gameObject);
            return;
        }
        else
        {
            _Instance = this;
        }

        
        /*
        this.transform.parent = null;   //Unparent for the sake of the DontDestroyOnLoad

        DontDestroyOnLoad(this.gameObject);
        */
    }

    private void OnValidate()
    {
        // It's not running in editor mode because it's not serializable and we get the value on start
        if (_CaptionText != null)
        {
            _CaptionText.rectTransform.localScale = Vector3.one * _Size;
        }

        _MaxSpeechInQueue = Mathf.Max(_MaxSpeechInQueue, 0);
    }

    #region Fade
    private IEnumerator FadeInOutCoroutine(TextMeshProUGUI text, float goTime, float backTime, float middleWait, float loopWait)
    {
        WaitForSeconds betweenWait = new WaitForSeconds(middleWait);
        WaitForSeconds endWait = new WaitForSeconds(loopWait);

        Color originalColor = text.color;
        Color goColor = new Color(originalColor.r, originalColor.g, originalColor.b, 1f);
        Color backColor = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);


        float progress = 0f;
        float step;
        Color newColor;

        /* Go */
        do
        {
            // Add the new tiny extra amount
            progress += Time.deltaTime / goTime;
            progress = Mathf.Clamp01(progress);

            // Apply changes
            step = -(Mathf.Cos(Mathf.PI * progress) - 1.0f) / 2.0f; // Ease in out
            newColor = Color.Lerp(originalColor, goColor, step);

            text.color = newColor;

            // Wait for the next frame
            yield return null;
        }
        while (progress < 1f); // Keep moving while we don't reach any goal

        // Wait
        yield return betweenWait;

        progress = 0f; // Reset

        do
        {
            // Add the new tiny extra amount
            progress += Time.deltaTime / backTime;
            progress = Mathf.Clamp01(progress);

            // Apply changes
            step = -(Mathf.Cos(Mathf.PI * progress) - 1.0f) / 2.0f; // Ease in out
            newColor = Color.Lerp(goColor, backColor, step);

            text.color = newColor;

            // Wait for the next frame
            yield return null;
        }
        while (progress < 1f); // Keep moving while we don't reach any goal

        // Wait
        yield return endWait;
    }

    #endregion
}
