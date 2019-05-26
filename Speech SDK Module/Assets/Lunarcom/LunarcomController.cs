using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LunarcomController : MonoBehaviour
{
    public static LunarcomController lunarcomController = null;
    // https://westus.api.cognitive.microsoft.com/sts/v1.0
    // Key 1: 37b71e1e68fd497aa06367bb75bd2351
    // Key 2: febaa5534609486b852704fcffbf1d2a

    [Header("Connection Light References")]
    public Sprite connectedLight;
    public Sprite disconnectedLight;
    public Image connectionLight;

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
        ShowConnected();
    }

    public void ShowConnected()
    {
        connectionLight.sprite = connectedLight;
    }
}
