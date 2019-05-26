using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Globalization;
using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Translation;

#if PLATFORM_ANDROID
using UnityEngine.Android;
#endif

public enum SpeechRecognitionMode { Continuous_Recognize, One_Time_Recognize, Translate, Disabled };
public enum SpecificityRequired { Exact, Intent };
public enum EnableDeviceAsBackup { Enabled, Disabled };

public class LunarcomSpeechRecognizer : MonoBehaviour
{
    [Header("Speech SDK Credentials")]
    public string SpeechServiceAPIKey = "febaa5534609486b852704fcffbf1d2a";
    public string SpeechServiceRegion = "westus";

    [Space(6)]
    [Header("Reference Objects")]
    public Text outputText;

    [Space(6)]
    [Header("Lunarcom Settings")]
    public SpecificityRequired SpecificityRequired = SpecificityRequired.Exact;
    public EnableDeviceAsBackup EnableDeviceAsBackup = EnableDeviceAsBackup.Disabled;
    public bool KeywordLaunchEnabled = false;

    private string recognizedString = "Select a mode to begin.";
    private string translatedString = "";
    private object threadLocker = new object();
    private bool waitingForReco;
    private LunarcomButtonController activeButton = null;

    private SpeechRecognizer recognizer;
    private TranslationRecognizer translator;
    private SpeechRecognitionMode speechRecognitionMode = SpeechRecognitionMode.Disabled;

    private bool micPermissionGranted = false;
    private bool scanning = false;

    private string fromLanguage = "en-US";
    private string toLanguage = "ru-RU";

#if PLATFORM_ANDROID
    // Required to manifest microphone permission, cf.
    // https://docs.unity3d.com/Manual/android-manifest.html
    private Microphone mic;
#endif

    void Start()
    {
        if (outputText == null)
        {
            UnityEngine.Debug.LogError("outputText property is null! Assign a UI Text element to it.");
        }
        else
        {
            // Continue with normal initialization, Text and Button objects are present.

#if PLATFORM_ANDROID
            // Request to use the microphone, cf.
            // https://docs.unity3d.com/Manual/android-RequestingPermissions.html
            //message = "Waiting for mic permission";
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                Permission.RequestUserPermission(Permission.Microphone);
            }
#else
            micPermissionGranted = true;
            //message = "Click button to recognize speech";
#endif
        }
    }

    public void SetActiveButton(LunarcomButtonController buttonToSetActive)
    {
        activeButton = buttonToSetActive;
    }

    public void SelectMode(SpeechRecognitionMode speechRecognitionModeToSet)
    {
        speechRecognitionMode = speechRecognitionModeToSet;

        if (speechRecognitionMode != SpeechRecognitionMode.Disabled)
        {
            recognizedString = "Say something...";
            translatedString = "";
            BeginScanning();
        }
        else
        {
            recognizer = null;
            translator = null;
            activeButton = null;
        }
    }

    private void BeginScanning()
    {
        if (micPermissionGranted)
        {
            if (speechRecognitionMode == SpeechRecognitionMode.Translate)
            {
                StartContinuousTranslation();
            }
            else if (speechRecognitionMode == SpeechRecognitionMode.Continuous_Recognize)
            {
                StartContinuousRecognition();
            }
            else if (speechRecognitionMode == SpeechRecognitionMode.One_Time_Recognize)
            {
                StartOneTimeRecognition();
            }
        }
        else
        {
            recognizedString = "This app cannot function without access to the microphone.";
        }
    }

    public async void StartContinuousTranslation()
    {
        UnityEngine.Debug.LogFormat("Starting Continuous Translation Recognition");
        CreateTranslationRecognizer();

        if (translator != null)
        {
            await translator.StartContinuousRecognitionAsync().ConfigureAwait(false);
            recognizedString = "Say something...";
        }
    }

    void CreateTranslationRecognizer()
    {
        UnityEngine.Debug.LogFormat("Creating Translation Recognizer");
        recognizedString = "Initializing speech recognition, please wait...";

        if (translator == null)
        {
            SpeechTranslationConfig config = SpeechTranslationConfig.FromSubscription(SpeechServiceAPIKey, SpeechServiceRegion);
            config.SpeechRecognitionLanguage = fromLanguage;
            config.AddTargetLanguage(toLanguage);

            translator = new TranslationRecognizer(config);

            if (translator != null)
            {
                translator.Recognizing += HandleTranslatorRecognizing;
                translator.Recognized += HandleTranslatorRecognized;
                translator.Canceled += HandleTranslatorCanceled;
                translator.SessionStarted += HandleTranslatorSessionStarted;
                translator.SessionStopped += HandleTranslatorSessionStopped;
            }
        }
    }

    private async void StartOneTimeRecognition()
    {
        CreateSpeechRecognizer();

        if (recognizer != null)
        {
            waitingForReco = true;
            UnityEngine.Debug.LogFormat("Starting Speech Recognizer");
            await recognizer.RecognizeOnceAsync().ConfigureAwait(false);
            recognizedString = "Say something...";
        }
    }

    private async void StartContinuousRecognition()
    {
        UnityEngine.Debug.LogFormat("Starting Continuous Speech Recognition");
        CreateSpeechRecognizer();

        if (recognizer != null)
        {
            await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);
            recognizedString = "Say something...";
        }
    }

    void CreateSpeechRecognizer()
    {
        if (recognizer == null)
        {
            SpeechConfig config = SpeechConfig.FromSubscription(SpeechServiceAPIKey, SpeechServiceRegion);
            config.SpeechRecognitionLanguage = fromLanguage;
            recognizer = new SpeechRecognizer(config);
            if (recognizer != null)
            {
                recognizer.Recognizing += RecognizingHandler;
                recognizer.Recognized += RecognizedHandler;
                recognizer.SpeechStartDetected += SpeechStartDetected;
                recognizer.SpeechEndDetected += SpeechEndDetectedHandler;
                recognizer.Canceled += CancelHandler;
                recognizer.SessionStarted += SessionStartedHandler;
                recognizer.SessionStopped += SessionStoppedHandler;
            }
        }
    }

    #region Speech Recognition Event Handlers
    private void SessionStartedHandler(object sender, SessionEventArgs e)
    {
        UnityEngine.Debug.Log("SessionStartedHandler called");
    }
    private void SessionStoppedHandler(object sender, SessionEventArgs e)
    {
        UnityEngine.Debug.Log("SessionStoppedHandler called");
    }

    private void RecognizingHandler(object sender, SpeechRecognitionEventArgs e)
    {
        if (e.Result.Reason == ResultReason.RecognizingSpeech)
        {
            lock (threadLocker)
            {
                recognizedString = $"{e.Result.Text}";
            }
        }
        UnityEngine.Debug.Log("Recognizing Handler called");
    }

    private void RecognizedHandler(object sender, SpeechRecognitionEventArgs e)
    {
        UnityEngine.Debug.Log("Recognized Handler called");
        if (e.Result.Reason == ResultReason.RecognizingSpeech)
        {
            lock (threadLocker)
            {
                recognizedString = $"{e.Result.Text}";
            }
        }
        else if (e.Result.Reason == ResultReason.NoMatch)
        {
            UnityEngine.Debug.Log("No Match Found");
        }

        waitingForReco = false;
    }

    private void SpeechStartDetected(object sender, RecognitionEventArgs e)
    {
        UnityEngine.Debug.Log("SpeechStart Handler called");
    }

    private void SpeechEndDetectedHandler(object sender, RecognitionEventArgs e)
    {
        UnityEngine.Debug.Log("SpeechStart Handler called");
    }

    private void CancelHandler(object sender, RecognitionEventArgs e)
    {
        UnityEngine.Debug.Log("SpeechEndDetectedHandler called");
    }
    #endregion
    #region Translation Recognition Event Handlers
    private void HandleTranslatorRecognizing(object s, TranslationRecognitionEventArgs e)
    {
        if (e.Result.Reason == ResultReason.TranslatingSpeech)
        {
            if (e.Result.Text != "")
            {
                recognizedString = e.Result.Text;

                foreach (var element in e.Result.Translations)
                {
                    translatedString = element.Value;
                }
            }
        }
    }

    private void HandleTranslatorRecognized(object s, TranslationRecognitionEventArgs e)
    {
        if (e.Result.Reason == ResultReason.TranslatedSpeech)
        {
            recognizedString = e.Result.Text;

            foreach (var element in e.Result.Translations)
            {
                translatedString = element.Value;
            }
        }
    }

    private void HandleTranslatorCanceled(object s, TranslationRecognitionEventArgs e)
    {

        UnityEngine.Debug.Log("HandleTranslatorCanceled called");
    }

    private void HandleTranslatorSessionStarted(object s, SessionEventArgs e)
    {
        UnityEngine.Debug.Log("HandleTranslatorSessionStarted called");
    }

    public void HandleTranslatorSessionStopped(object s, SessionEventArgs e)
    {
        UnityEngine.Debug.Log("HandleTranslatorSessionStopped called");
    }
    #endregion

    private void Update()
    {
        if (speechRecognitionMode == SpeechRecognitionMode.One_Time_Recognize)
        {
            if (waitingForReco)
            {
                outputText.text = recognizedString;
            }
            else
            {
                waitingForReco = false;
                recognizer = null;
                activeButton.ToggleSelected();
            }
        }
        else if (speechRecognitionMode == SpeechRecognitionMode.Translate)
        {
            outputText.text = recognizedString;
            if (translatedString != "")
            {
                outputText.text += "\n\nSending...\n" + translatedString;
            }
        }
        else
        {
            if (speechRecognitionMode != SpeechRecognitionMode.Disabled) {
                outputText.text = recognizedString;
            }
        }
    }
}


