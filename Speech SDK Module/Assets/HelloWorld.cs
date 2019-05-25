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

public class HelloWorld : MonoBehaviour
{
    // Hook up the two properties below with a Text and Button object in your UI.
    public Text outputText;

    public enum SpeechRecognitionMode { Continuous_Recognize, One_Time_Recognize,  Translate };
    public SpeechRecognitionMode speechRecognitionMode = SpeechRecognitionMode.Continuous_Recognize;

    private string recognizedString = "";
    private object threadLocker = new object();
    public bool waitingForReco;

    public string SpeechServiceAPIKey = "febaa5534609486b852704fcffbf1d2a";
    public string SpeechServiceRegion = "westus";

    private SpeechRecognizer recognizer;
    private TranslationRecognizer translator;

    private bool micPermissionGranted = false;
    private bool scanning = false;

    private string fromLanguage = "en-us";
    private string toLanguage = "";

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

    public void StartSpeechScan()
    {
        if (!scanning)
        {
            recognizedString = "Say something...";
            StartRecognizer();
            scanning = true;
        } else
        {
            scanning = false;
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

    private async void StartContinuousTranslation()
    {
        UnityEngine.Debug.LogFormat("Starting Continuous Translation");
        CreateTranslationRecognizer();

        if (translator != null)
        {
            UnityEngine.Debug.LogFormat("Starting Speech Recognizer");
            await translator.StartContinuousRecognitionAsync().ConfigureAwait(false);

            recognizedString = "Say something...";
        }
    }

    void CreateTranslationRecognizer()
    {
        UnityEngine.Debug.LogFormat("Creating Speech Recognizer");
        recognizedString = "Initializing speech recognition, please wait...";

        if (translator == null)
        {
            SpeechTranslationConfig config = SpeechTranslationConfig.FromSubscription(SpeechServiceAPIKey, SpeechServiceRegion);
            config.SpeechRecognitionLanguage = fromLanguage;
            config.AddTargetLanguage("russian");
            translator = new TranslationRecognizer(config);
            
            if (translator != null)
            {
                
                //recognizer.Recognizing += RecognizingHandler;
                //recognizer.Recognized += RecognizedHandler;
                //recognizer.SpeechStartDetected += SpeechStartDetected;
                //recognizer.SpeechEndDetected += SpeechEndDetectedHandler;
                //recognizer.Canceled += CancelHandler;
                //recognizer.SessionStarted += SessionStartedHandler;
                //recognizer.SessionStopped += SessionStoppedHandler;
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
                recognizedString = $"HYPOTHESIS: {Environment.NewLine}{e.Result.Text}";
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
                recognizedString = $"HYPOTHESIS: {Environment.NewLine}{e.Result.Text}";
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
        } else
        {
            outputText.text = recognizedString;
        }
    }
}


