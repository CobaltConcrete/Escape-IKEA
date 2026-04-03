using TMPro;
using UnityEngine;

public class UI_WinText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI winText;

    private void Start()
    {
        if (winText == null)
        {
            winText = GetComponent<TextMeshProUGUI>();
        }

        int value = PlayerPrefs.GetInt("LastRunValue", 0);
        winText.text = $"+{value} IKEA aura gained";
    }
}