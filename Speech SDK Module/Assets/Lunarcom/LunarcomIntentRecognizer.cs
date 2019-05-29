using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.Windows.Speech;

public class LunarcomIntentRecognizer : MonoBehaviour
{
    [Header("LUIS Credentials")]
    //public string LUISKey = "fa2db4721c3344ef9b98f62b808782f3";
    //public string LUISRegion = "westus";
    //public string LUISAppID = "6a1bc995-6b04-4831-83b7-430fae70f7df";

    string luisEndpoint = "https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/6a1bc995-6b04-4831-83b7-430fae70f7df?verbose=true&timezoneOffset=-360&subscription-key=a6efc7b1f54e479494feaa57e9dc07f8&q=";
    DictationRecognizer dictationRecognizer;  //Component converting speech to text
    LunarcomController lunarcomController;
    bool micPermissionGranted = false;
    string recognizedString;

    void Start()
    {
        lunarcomController = LunarcomController.lunarcomController;

        if (lunarcomController.outputText == null)
        {
            Debug.LogError("outputText property is null! Assign a UI Text element to it.");
        }
        else
        {
            micPermissionGranted = true;
        }

        lunarcomController.onSelectRecognitionMode += HandleOnSelectRecognitionMode;
    }

    public void HandleOnSelectRecognitionMode(RecognitionMode recognitionMode)
    {
        if (recognitionMode == RecognitionMode.Intent_Recognizer)
        {
            BeginRecognizing();
        }
        else
        {
            StopCapturingAudio(); // this may not be right.
            recognizedString = "";
        }
    }

    private void BeginRecognizing()
    {
        if (Microphone.devices.Length > 0)
        {
            if (dictationRecognizer == null)
            {
                dictationRecognizer = new DictationRecognizer
                {
                    InitialSilenceTimeoutSeconds = 60,
                    AutoSilenceTimeoutSeconds = 5
                };

                dictationRecognizer.DictationResult += DictationRecognizer_DictationResult;
                dictationRecognizer.DictationError += DictationRecognizer_DictationError;
            }
            dictationRecognizer.Start();
            Debug.Log("Capturing Audio...");
        }
    }

    public void StopCapturingAudio()
    {
        dictationRecognizer.Stop();
        Debug.Log("Stop Capturing Audio...");
    }

    private void DictationRecognizer_DictationResult(string dictationCaptured, ConfidenceLevel confidence)
    {
        StopCapturingAudio();
        StartCoroutine(SubmitRequestToLuis(dictationCaptured, BeginRecognizing));
        Debug.Log("Dictation: " + dictationCaptured);
        recognizedString = dictationCaptured;
    }

    private void DictationRecognizer_DictationError(string error, int hresult)
    {
        Debug.Log("Dictation exception: " + error);
    }

    [Serializable] //this class represents the LUIS response
    class AnalysedQuery
    {
        public TopScoringIntentData topScoringIntent;
        public EntityData[] entities;
        public string query;
    }

    [Serializable]
    class TopScoringIntentData
    {
        public string intent;
        public float score;
    }

    [Serializable]
    class EntityData
    {
        public string entity;
        public string type;
        public int startIndex;
        public int endIndex;
        public float score;
    }

    public IEnumerator SubmitRequestToLuis(string dictationResult, Action done)
    {
        string queryString = string.Concat(Uri.EscapeDataString(dictationResult));

        using (UnityWebRequest unityWebRequest = UnityWebRequest.Get(luisEndpoint + queryString))
        {
            yield return unityWebRequest.SendWebRequest();

            if (unityWebRequest.isNetworkError || unityWebRequest.isHttpError)
            {
                Debug.Log(unityWebRequest.error);
            }
            else
            {
                try
                {
                    AnalysedQuery analysedQuery = JsonUtility.FromJson<AnalysedQuery>(unityWebRequest.downloadHandler.text);
 
                    UnpackResults(analysedQuery);
                }
                catch (Exception exception)
                {
                    Debug.Log("Luis Request Exception Message: " + exception.Message);
                }
            }

            done();
            yield return null;
        }
    }

    private void UnpackResults(AnalysedQuery aQuery)
    {
        string topIntent = aQuery.topScoringIntent.intent;

        Dictionary<string, string> entityDic = new Dictionary<string, string>();

        foreach (EntityData ed in aQuery.entities)
        {
            entityDic.Add(ed.type, ed.entity);
        }

        switch (aQuery.topScoringIntent.intent)
        {
            case "PressButton":
                string actionToTake = null;
                string targetButton = null;

                foreach (var pair in entityDic)
                {
                    if (pair.Key == "Target")
                    {
                        targetButton = pair.Value;
                    }
                    else if (pair.Key == "Action")
                    {
                        actionToTake = pair.Value;
                    }
                }
                ProcessResults(targetButton, actionToTake);
                break;
        }
    }

    public void ProcessResults(string targetButton, string actionToTake)
    {
        Debug.Log("Pressing the " + targetButton + " button because I was told to " + actionToTake);

        switch (targetButton)
        {
            case "launch":
                recognizedString += "\n\nCommand Recognized:\nPushing the Launch button.";
                break;
            case "reset":
                recognizedString += "\n\nCommand Recognized:\nPushing the Reset button.";
                break;
            case "hint":
                recognizedString += "\n\nCommand Recognized:\nPushing the Hint button.";
                break;
        }
    }

    private void Update()
    {
        if (lunarcomController.CurrentRecognitionMode() == RecognitionMode.Intent_Recognizer)
        {
            lunarcomController.UpdateLunarcomText(recognizedString);
        }
    }
}