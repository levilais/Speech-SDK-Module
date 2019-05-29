using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum RecognitionMode { Speech_Recognizer, Intent_Recognizer, Tralation_Recognizer, Disabled, Offline };
public enum SimuilateOfflineMode { Enabled, Disabled };

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

    [Space(6)]
    [Header("Lunarcom Buttons")]
    public List<LunarcomButtonController> lunarcomButtons;
    public GameObject Terminal;

    public delegate void OnSelectRecognitionMode(RecognitionMode selectedMode);
    public event OnSelectRecognitionMode onSelectRecognitionMode;

    RecognitionMode speechRecognitionMode = RecognitionMode.Disabled;
    LunarcomButtonController activeButton = null;
    LunarcomWakeWordRecognizer lunarcomWakeWordRecognizer = null;
    LunarcomOfflineRecognizer lunarcomOfflineRecognizer = null;

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
        if (GetComponent<LunarcomOfflineRecognizer>())
        {
            lunarcomOfflineRecognizer = GetComponent<LunarcomOfflineRecognizer>();
            if (lunarcomOfflineRecognizer.simulateOfflineMode == SimuilateOfflineMode.Disabled)
            {
                SetupOnlineMode();
            }
            else
            {
                SetupOfflineMode();
            }
        } else
        {
            SetupOnlineMode();
        }

        if (GetComponent<LunarcomWakeWordRecognizer>())
        {
            lunarcomWakeWordRecognizer = GetComponent<LunarcomWakeWordRecognizer>();
        }
    }

    private void SetupOnlineMode()
    {
        if (lunarcomWakeWordRecognizer != null)
        {
            if (lunarcomWakeWordRecognizer.WakeWord == "")
            {
                lunarcomWakeWordRecognizer.WakeWord = "*";
                lunarcomWakeWordRecognizer.DismissWord = "*";
            }

            if (lunarcomWakeWordRecognizer.DismissWord == "")
            {
                lunarcomWakeWordRecognizer.DismissWord = "*";
            }
        }
        

        if (GetComponent<LunarcomTranslationRecognizer>())
        {
            ActivateButtonNamed("Satellite");
        }

        if (GetComponent<LunarcomIntentRecognizer>())
        {
            ActivateButtonNamed("Rocket");
        }

        ShowConnected(true);
    }

    private void SetupOfflineMode()
    {
        if (lunarcomWakeWordRecognizer != null)
        {
            lunarcomWakeWordRecognizer.WakeWord = "*";
            lunarcomWakeWordRecognizer.DismissWord = "*";
        }
        
        if (GetComponent<LunarcomWakeWordRecognizer>())
        {
            GetComponent<LunarcomWakeWordRecognizer>().enabled = false;
        }
        if (GetComponent<LunarcomSpeechRecognizer>())
        {
            GetComponent<LunarcomSpeechRecognizer>().enabled = false;
        }
        if (GetComponent<LunarcomTranslationRecognizer>())
        {
            GetComponent<LunarcomTranslationRecognizer>().enabled = false;
            ActivateButtonNamed("Satellite", false);
        }
        if (GetComponent<LunarcomIntentRecognizer>())
        {
            GetComponent<LunarcomIntentRecognizer>().enabled = false;
            ActivateButtonNamed("Rocket", false);
        }
        ShowConnected(false);
    }

    private void ActivateButtonNamed(string name, bool makeActive = true) {
        foreach (LunarcomButtonController button in lunarcomButtons)
        {
            if (button.gameObject.name == name)
            {
                button.gameObject.SetActive(makeActive);
            }
        }
    }

    public RecognitionMode CurrentRecognitionMode()
    {
        return speechRecognitionMode;
    }

    public void SetActiveButton(LunarcomButtonController buttonToSetActive)
    {
        activeButton = buttonToSetActive;
        foreach (LunarcomButtonController button in lunarcomButtons)
        {
            if (button != activeButton && button.GetIsSelected())
            {
                button.ShowNotSelected();
            }
        }
    }

    public void SelectMode(RecognitionMode speechRecognitionModeToSet)
    {
        speechRecognitionMode = speechRecognitionModeToSet;
        onSelectRecognitionMode(speechRecognitionMode);
    }

    public void ShowConnected(bool showConnected)
    {
        if (showConnected)
        {
            connectionLight.sprite = connectedLight;
        } else
        {
            connectionLight.sprite = disconnectedLight;
        }
    }

    public void ShowTerminal()
    {
        Terminal.SetActive(true);
    }

    public void HideTerminal()
    {
        if (Terminal.activeSelf)
        {
            foreach (LunarcomButtonController button in lunarcomButtons)
            {
                if (button.GetIsSelected())
                {
                    button.ShowNotSelected();
                }
            }

            outputText.text = "Select a mode to begin.";
            Terminal.SetActive(false);
            SelectMode(RecognitionMode.Disabled);
        }
    }

    public void UpdateLunarcomText(string textToUpdate)
    {
        if (lunarcomWakeWordRecognizer != null)
        {
            if (!textToUpdate.ToLower().Contains(lunarcomWakeWordRecognizer.DismissWord.ToLower()))
            {
                outputText.text = textToUpdate;
            } else
            {
                HideTerminal();
            }
        }
        else
        {
            outputText.text = textToUpdate;
        }
    }
}
