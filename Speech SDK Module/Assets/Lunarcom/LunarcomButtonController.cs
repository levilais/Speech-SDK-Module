using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LunarcomButtonController : MonoBehaviour
{
    [Header("Reference Objects")]
    public SpeechRecognitionMode speechRecognitionMode = SpeechRecognitionMode.Disabled;
    public LunarcomSpeechRecognizer lunarcomSpeechRecognizer;

    [Space(6)]
    [Header("Button States")]
    public Sprite Default;
    public Sprite Activated;

    private Button button;
    private bool isSelected = false;

    private void Start()
    {
        button = GetComponent<Button>();
    }

    public void ToggleSelected()
    {
        if (isSelected)
        {
            foreach (Transform button in transform.parent)
            {
                button.GetComponent<LunarcomButtonController>().DeselectButton();
            }

            lunarcomSpeechRecognizer.SelectMode(SpeechRecognitionMode.Disabled);
        } else
        {
            foreach (Transform button in transform.parent)
            {
                button.GetComponent<LunarcomButtonController>().DeselectButton();
            }

            button.image.sprite = Activated;
            isSelected = true;
            lunarcomSpeechRecognizer.SetActiveButton(GetComponent<LunarcomButtonController>());
            lunarcomSpeechRecognizer.SelectMode(speechRecognitionMode);
        }
    }

    public void DeselectButton()
    {
        button.image.sprite = Default;
        isSelected = false;
        lunarcomSpeechRecognizer.SelectMode(SpeechRecognitionMode.Disabled);
    }
}
