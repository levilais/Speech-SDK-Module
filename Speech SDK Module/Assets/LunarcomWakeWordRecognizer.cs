using UnityEngine;
using Microsoft.CognitiveServices.Speech;

public class LunarcomWakeWordRecognizer : MonoBehaviour
{
    [Header("Speech SDK Credentials")]
    public string SpeechServiceAPIKey = "febaa5534609486b852704fcffbf1d2a";
    public string SpeechServiceRegion = "westus";

    private string recognizedString = "Select a mode to begin.";
    private object threadLocker = new object();

    private SpeechRecognizer recognizer;

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
        LunarcomController.lunarcomController.HideTerminal();
        BeginRecognizing();
    }

    public void HandleOnSelectRecognitionMode(RecognitionMode recognitionMode)
    {
        if (recognitionMode == RecognitionMode.Disabled)
        {
            BeginRecognizing();
        }
        else
        {
            if (recognizer != null)
            {
                recognizer.StopContinuousRecognitionAsync();
            }
            recognizer = null;
            recognizedString = "";
        }
    }

    public async void BeginRecognizing()
    {
        if (micPermissionGranted)
        {
            CreateSpeechRecognizer();

            if (recognizer != null)
            {
                await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);
            }
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
            }
        }
    }

    private void RecognizingHandler(object sender, SpeechRecognitionEventArgs e)
    {
        Debug.Log("recognizing...");
        if (e.Result.Reason == ResultReason.RecognizingSpeech)
        {
            lock (threadLocker)
            {
                recognizedString = $"{e.Result.Text}";
            }
        }
    }

    private void Update()
    {
        if (LunarcomController.lunarcomController.speechRecognitionMode == RecognitionMode.Disabled)
        {
            if (LunarcomController.lunarcomController.Terminal.activeSelf)
            {
                if (recognizedString.ToLower().Contains(LunarcomController.lunarcomController.DismissWord.ToLower()))
                {
                    LunarcomController.lunarcomController.HideTerminal();
                }
            } else
            {
                Debug.Log("recognizing string = " + recognizedString);
                if (recognizedString.ToLower().Contains(LunarcomController.lunarcomController.WakeWord.ToLower()))
                {
                    LunarcomController.lunarcomController.ShowTerminal();
                }
            }
        }
    }
}
