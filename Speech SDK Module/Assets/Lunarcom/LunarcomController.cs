using UnityEngine;
using UnityEngine.UI;

public enum RecognitionMode { Speech_Recognizer, Intent_Recognizer, Tralation_Recognizer, Disabled };
public enum EnableOfflineRecognition { Enabled, Disabled };

public class LunarcomController : MonoBehaviour
{
    public static LunarcomController lunarcomController = null;
    // https://westus.api.cognitive.microsoft.com/sts/v1.0
    // Key 1: 37b71e1e68fd497aa06367bb75bd2351
    // Key 2: febaa5534609486b852704fcffbf1d2a

    [Header("Object References")]
    public Text outputText;
    public Sprite connectedLight;
    public Sprite disconnectedLight;
    public Image connectionLight;
    public GameObject micButton;
    public GameObject satelliteButton;
    public GameObject rocketButton;

    [Space(6)]
    [Header("Runtime Variables")]
    public RecognitionMode speechRecognitionMode = RecognitionMode.Disabled;

    public delegate void OnSelectRecognitionMode(RecognitionMode selectedMode);
    public event OnSelectRecognitionMode onSelectRecognitionMode;

    [Space(6)]
    [Header("Lunarcom Settings")]
    public EnableOfflineRecognition EnableOfflineRecognition = EnableOfflineRecognition.Disabled;
    public string WakeWord = "Activate Lunarcom";
    public string DismissWord = "Hide Lunarcom";

    private LunarcomButtonController activeButton = null;

    private void Awake()
    {
        if (lunarcomController == null)
            lunarcomController = this;
        else if (lunarcomController != this)
            Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (GetComponent<LunarcomTranslationRecognizer>())
        {
            satelliteButton.SetActive(true);
        }

        if (GetComponent<LunarcomIntentRecognizer>())
        {
            rocketButton.SetActive(true);
        }

        SelectMode(RecognitionMode.Disabled);

        ShowConnected();
    }

    public void SetActiveButton(LunarcomButtonController buttonToSetActive)
    {
        activeButton = buttonToSetActive;
    }

    public void SelectMode(RecognitionMode speechRecognitionModeToSet)
    {
        speechRecognitionMode = speechRecognitionModeToSet;
        onSelectRecognitionMode(speechRecognitionMode);
    }

    public void ShowConnected()
    {
        connectionLight.sprite = connectedLight;
    }

    public void UpdateLunarcomText(string textToUpdate)
    {
        if (!textToUpdate.Contains(DismissWord.ToLower()))
        {
            outputText.text = textToUpdate;
        } else
        {
            SelectMode(RecognitionMode.Disabled);
        }
    }
}
