using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonClickSound : MonoBehaviour
{
    private Button button;
    private UIButtonSoundBinder binder;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(PlayClickSound);
    }

    public void Initialize(UIButtonSoundBinder owner)
    {
        binder = owner;
    }

    private void PlayClickSound()
    {
        binder?.PlayButtonClick();
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(PlayClickSound);
    }
}