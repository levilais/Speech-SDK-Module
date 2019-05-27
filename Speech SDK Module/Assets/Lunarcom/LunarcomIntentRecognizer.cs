using UnityEngine;
using UnityEngine.UI;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Intent;

public class LunarcomIntentRecognizer : MonoBehaviour
{
    [Header("LUIS Credentials")]
    public string LUISKey = "fa2db4721c3344ef9b98f62b808782f3";
    public string LUISRegion = "westus";
    public string LUISAppID = "6a1bc995-6b04-4831-83b7-430fae70f7df";

    private Text outputText;
    private string recognizedString = "Select a mode to begin.";
    private object threadLocker = new object();

    private IntentRecognizer recognizer;

    private bool micPermissionGranted = false;
    private bool scanning = false;

    private string fromLanguage = "en-US";

    void Start()
    {
        if (LunarcomController.lunarcomController.outputText == null)
        {
            Debug.LogError("outputText property is null! Assign a UI Text element to it.");
        }
        else
        {
            micPermissionGranted = true;
        }

        LunarcomController.lunarcomController.onSelectRecognitionMode += HandleOnSelectRecognitionMode;
    }

    public void HandleOnSelectRecognitionMode(RecognitionMode recognitionMode)
    {
        Debug.Log("handle on select called");
        if (recognitionMode == RecognitionMode.Intent_Recognizer)
        {
            BeginRecognizing();
            //recognizedString = "Say something...";
        }
        else
        {
            Debug.Log("intent recognizer being set to null");
            recognizer = null;
        }
    }

    public async void BeginRecognizing()
    {
        if (micPermissionGranted)
        {
            Debug.LogFormat("Starting Continuous Speech Recognition");
            CreateSpeechRecognizer();

            if (recognizer != null)
            {
                await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);
                recognizedString = "Say something...";
            }
        }
        else
        {
            recognizedString = "This app cannot function without access to the microphone.";
        }
    }

    void CreateSpeechRecognizer()
    {
        if (recognizer == null)
        {
            SpeechConfig config = SpeechConfig.FromSubscription(LUISKey, LUISRegion);
            recognizer = new IntentRecognizer(config);

            var model = LanguageUnderstandingModel.FromAppId(LUISAppID);
            recognizer.AddIntent(model, "PressButton", "button");
            recognizer.AddIntent(model, "None", "none");

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
    private void RecognizingHandler(object sender, IntentRecognitionEventArgs e)
    {

        Debug.Log("Recognizing is being called");
        //if (e.Result.Reason == ResultReason.RecognizingIntent)
        //{
        //    lock (threadLocker)
        //    {
        //        recognizedString = $"{e.Result.Text}";
        //    }
        //}
       
    }

    private void RecognizedHandler(object sender, IntentRecognitionEventArgs e)
    {
        Debug.Log("Recognized Handler called");

        //if (e.Result.Reason == ResultReason.RecognizedIntent)
        //{
        //    //lock (threadLocker)
        //    //{
        //        recognizedString = $"{e.Result.Text}";
        //    //}
        //}
        //else if (e.Result.Reason == ResultReason.NoMatch)
        //{
        //    Debug.Log("No Match Found");
        //}
    }

    private void SpeechStartDetected(object sender, RecognitionEventArgs e)
    {
        UnityEngine.Debug.Log("SpeechStart Handler called");
    }

    private void SpeechEndDetectedHandler(object sender, RecognitionEventArgs e)
    {
        
        UnityEngine.Debug.Log("SpeechStart Handler called");
    }

    private void CancelHandler(object sender, IntentRecognitionCanceledEventArgs e)
    {
        UnityEngine.Debug.Log("SpeechEndDetectedHandler called");
    }
    private void SessionStartedHandler(object sender, SessionEventArgs e)
    {
        UnityEngine.Debug.Log("SessionStartedHandler called");
    }
    private void SessionStoppedHandler(object sender, SessionEventArgs e)
    {
        UnityEngine.Debug.Log("SessionStoppedHandler called");

    }
    #endregion

    private void Update()
    {
        if (LunarcomController.lunarcomController.speechRecognitionMode == RecognitionMode.Intent_Recognizer)
        {
            LunarcomController.lunarcomController.outputText.text = recognizedString;
        }
    }
}