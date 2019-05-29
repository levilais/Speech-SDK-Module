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
    private DictationRecognizer dictationRecognizer;  //Component converting speech to text
    public Text outputText; //a UI object used to debug dictation result

    void Start()
    {
        if (Microphone.devices.Length > 0)
        {
            StartCapturingAudio();
            Debug.Log("Mic Detected");
        }
    }

    public void StartCapturingAudio()
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

    /// <summary>
    /// Stop microphone capture
    /// </summary>
    public void StopCapturingAudio()
    {
        dictationRecognizer.Stop();
        Debug.Log("Stop Capturing Audio...");
    }

    /// <summary>
    /// This handler is called every time the Dictation detects a pause in the speech. 
    /// This method will stop listening for audio, send a request to the LUIS service 
    /// and then start listening again.
    /// </summary>
    private void DictationRecognizer_DictationResult(string dictationCaptured, ConfidenceLevel confidence)
    {
        StopCapturingAudio();
        StartCoroutine(SubmitRequestToLuis(dictationCaptured, StartCapturingAudio));
        Debug.Log("Dictation: " + dictationCaptured);
        outputText.text = dictationCaptured;
    }

    private void DictationRecognizer_DictationError(string error, int hresult)
    {
        Debug.Log("Dictation exception: " + error);
    }

    [Serializable] //this class represents the LUIS response
    public class AnalysedQuery
    {
        public TopScoringIntentData topScoringIntent;
        public EntityData[] entities;
        public string query;
    }

    // This class contains the Intent LUIS determines 
    // to be the most likely
    [Serializable]
    public class TopScoringIntentData
    {
        public string intent;
        public float score;
    }

    // This class contains data for an Entity
    [Serializable]
    public class EntityData
    {
        public string entity;
        public string type;
        public int startIndex;
        public int endIndex;
        public float score;
    }

    /// <summary>
    /// Call LUIS to submit a dictation result.
    /// The done Action is called at the completion of the method.
    /// </summary>
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

                    //analyse the elements of the response 
                    AnalyseResponseElements(analysedQuery);
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

    private void AnalyseResponseElements(AnalysedQuery aQuery)
    {
        string topIntent = aQuery.topScoringIntent.intent;

        // Create a dictionary of entities associated with their type
        Dictionary<string, string> entityDic = new Dictionary<string, string>();

        foreach (EntityData ed in aQuery.entities)
        {
            entityDic.Add(ed.type, ed.entity);
        }

        // Depending on the topmost recognised intent, read the entities name
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
                UpdateOutputText(targetButton, actionToTake);
                break;
        }
    }

    public void UpdateOutputText(string targetButton, string actionToTake)
    {
        Debug.Log("Pressing button " + targetButton + " because commanded by " + actionToTake);

        switch (actionToTake)
        {
            case "launch":
                outputText.text = "launch";
                break;
            case "reset":
                outputText.text = "reset";
                break;
            case "hint":
                outputText.text = "hint";
                break;
        }
    }
}