using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpriteStateSwapper : MonoBehaviour
{
    public Button button;
    public Sprite Mic_Default;
    public Sprite Mic_Activated;
    bool isSelected = false;

    public void ToggleSelected()
    {
        if (isSelected)
        {
            button.image.sprite = Mic_Default;
            isSelected = false;
        } else
        {
            button.image.sprite = Mic_Activated;
            isSelected = true;
        }
    }
}
