using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIButtonGlobalPointerSound : MonoBehaviour
{
    private static UIButtonGlobalPointerSound instance;

    [SerializeField] private AudioClip buttonClickClip;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;

    private AudioSource source;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        source = GetComponent<AudioSource>();
        if (source == null)
            source = gameObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.ignoreListenerPause = true;
        if (buttonClickClip != null)
        {
            buttonClickClip.LoadAudioData();
        }
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        if (EventSystem.current == null)
            return;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (RaycastResult result in results)
        {
            Debug.Log("Hit UI: " + result.gameObject.name);

            Button button = result.gameObject.GetComponentInParent<Button>();

            if (button != null)
            {
                Debug.Log("Found Button: " + button.gameObject.name);

                if (button.interactable)
                {
                    PlayClick();
                    return;
                }
            }
        }
    }

    private void PlayClick()
    {
        if (buttonClickClip == null || source == null)
            return;

        if (buttonClickClip.loadState == AudioDataLoadState.Unloaded)
            buttonClickClip.LoadAudioData();

        source.PlayOneShot(buttonClickClip, volume);
    }
}