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
}