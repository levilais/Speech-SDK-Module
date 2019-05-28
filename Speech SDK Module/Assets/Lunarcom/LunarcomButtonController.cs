using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LunarcomButtonController : MonoBehaviour
{
    [Header("Reference Objects")]
    public RecognitionMode speechRecognitionMode = RecognitionMode.Disabled;

    [Space(6)]
    [Header("Button States")]
    public Sprite Default;
    public Sprite Activated;

    private Button button;
    public bool isSelected = false;

    private void Start()
    {
        button = GetComponent<Button>();
    }

    public void ToggleSelected()
    {
        if (isSelected)
        {
            DeselectButton();
        }
        else
        {
            button.image.sprite = Activated;
            isSelected = true;
            LunarcomController.lunarcomController.SetActiveButton(GetComponent<LunarcomButtonController>());
            LunarcomController.lunarcomController.SelectMode(speechRecognitionMode);
        }
    }

    public void ShowNotSelected()
    {
        button.image.sprite = Default;
        isSelected = false;
    }

    public void DeselectButton()
    {
        ShowNotSelected();
        LunarcomController.lunarcomController.SelectMode(RecognitionMode.Disabled);
    }
}
