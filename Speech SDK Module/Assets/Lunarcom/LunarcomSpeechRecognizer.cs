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

public class LunarcomSpeechRecognizer : MonoBehaviour
{
    // Hook up the two properties below with a Text and Button object in your UI.
    public Text outputText;

    public enum SpeechRecognitionMode { Continuous_Recognize, One_Time_Recognize,  Translate };
    public SpeechRecognitionMode speechRecognitionMode = SpeechRecognitionMode.Continuous_Recognize;

    private string recognizedString = "Turn ON to begin speech to text.";
    private string translatedString = "";
    private object threadLocker = new object();
    public bool waitingForReco;

    public string SpeechServiceAPIKey = "febaa5534609486b852704fcffbf1d2a";
    public string SpeechServiceRegion = "westus";

    private SpeechRecognizer recognizer;
    private TranslationRecognizer translator;

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

    public void BeginRecognizing()
    {
        if (!scanning)
        {
            recognizedString = "Say something...";
            translatedString = "";
            StartRecognizer();
            scanning = true;
        } else
        {
            scanning = false;
            switch (speechRecognitionMode)
            {
                case SpeechRecognitionMode.Continuous_Recognize:
                    recognizer = null;
                    break;
                case SpeechRecognitionMode.One_Time_Recognize:
                    recognizer = null;
                    break;
                case SpeechRecognitionMode.Translate:
                    translator = null;
                    break;
                default:
                    // no mode found
                    break;
            }
            recognizer = null;
        }
    }

    public void StartRecognizer()
    {
        if (micPermissionGranted)
        {
            if (speechRecognitionMode == SpeechRecognitionMode.Translate)
            {
                StartContinuousTranslation();
            } else if (speechRecognitionMode == SpeechRecognitionMode.Continuous_Recognize)
            {
                StartContinuousRecognition();
            } else if (speechRecognitionMode == SpeechRecognitionMode.One_Time_Recognize)
            {
                StartOneTimeRecognition();
            }
        } else
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
        } else if (e.Result.Reason == ResultReason.NoMatch)
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
        //UnityEngine.Debug.Log("HandleTranslatorRecognizing called");
        ////recognizedString = $"RECOGNIZING in '{fromLanguage}': Text={e.Result.Text}";
        //foreach (var element in e.Result.Translations)
        //{
        //    translatedString = $"TRANSLATING into '{element.Key}': {element.Value}";
        //}
        UnityEngine.Debug.Log("ResultReason on Recognized: " + e.Result.Reason.ToString());
        if (e.Result.Reason == ResultReason.TranslatingSpeech)
        {
            //recognizedString = $"\nFinal result: Reason: {e.Result.Reason.ToString()}, recognized text in {fromLanguage}: {e.Result.Text}.";
            recognizedString = e.Result.Text;

            foreach (var element in e.Result.Translations)
            {
                // translatedString = $"    TRANSLATING into '{element.Key}': {element.Value}";
                translatedString = element.Value;
            }
        }
    }

    private void HandleTranslatorRecognized(object s, TranslationRecognitionEventArgs e)
    {
        UnityEngine.Debug.Log("ResultReason on Recognized: " + e.Result.Reason.ToString());
        if (e.Result.Reason == ResultReason.TranslatedSpeech)
        {
            //recognizedString = $"\nFinal result: Reason: {e.Result.Reason.ToString()}, recognized text in {fromLanguage}: {e.Result.Text}.";
            recognizedString = e.Result.Text;

            foreach (var element in e.Result.Translations)
            {
                // translatedString = $"    TRANSLATING into '{element.Key}': {element.Value}";
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
            } else
            {
                waitingForReco = false;
                recognizer = null;
            }
        } else if (speechRecognitionMode == SpeechRecognitionMode.Translate)
        {
            outputText.text = recognizedString;
            if (translatedString != "")
            {
                outputText.text += "\n\nSending...\n" + translatedString;
            }
        }
        else
        {
            outputText.text = recognizedString;
        }
    }
}


