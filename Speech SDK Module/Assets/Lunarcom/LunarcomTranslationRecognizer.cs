﻿using UnityEngine;
using UnityEngine.UI;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Translation;

public class LunarcomTranslationRecognizer : MonoBehaviour
{
    [Header("Speech SDK Credentials")]
    public string SpeechServiceAPIKey = "febaa5534609486b852704fcffbf1d2a";
    public string SpeechServiceRegion = "westus";

    private Text outputText;
    private string recognizedString = "Select a mode to begin.";
    private string translatedString = "";
    private object threadLocker = new object();

    private TranslationRecognizer translator;

    private bool micPermissionGranted = false;
    private bool scanning = false;

    private string fromLanguage = "en-US";
    private string toLanguage = "ru-RU";

    void Start()
    {
        if (LunarcomController.lunarcomController.outputText == null)
        {
            Debug.LogError("outputText property is null! Assign a UI Text element to it.");
        }
        else
        {
            outputText = LunarcomController.lunarcomController.outputText;
            micPermissionGranted = true;
        }

        LunarcomController.lunarcomController.onSelectRecognitionMode += HandleOnSelectRecognitionMode;
    }

    public void HandleOnSelectRecognitionMode(RecognitionMode recognitionMode)
    {
        if (recognitionMode == RecognitionMode.Tralation_Recognizer)
        {
            recognizedString = "Say something...";
            translatedString = "";
            BeginTranslating();
        } else
        {
            translator = null;
        }
    }

    public async void BeginTranslating()
    {
        if (micPermissionGranted)
        {
            CreateTranslationRecognizer();

            if (translator != null)
            {
                await translator.StartContinuousRecognitionAsync().ConfigureAwait(false);
                recognizedString = "Say something...";
            }
        }
        else
        {
            recognizedString = "This app cannot function without access to the microphone.";
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
        if (LunarcomController.lunarcomController.speechRecognitionMode == RecognitionMode.Tralation_Recognizer)
        {
            outputText.text = recognizedString;
            if (translatedString != "")
            {
                outputText.text += "\n\nSending...\n" + translatedString;
            }
        }
        else
        {
            if (LunarcomController.lunarcomController.speechRecognitionMode != RecognitionMode.Disabled)
            {
                outputText.text = recognizedString;
            }
        }
    }
}


