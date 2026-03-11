using UnityEngine;

public class Room : MonoBehaviour
{
    public bool explored = false;

    [Header("Room Visuals")]
    [SerializeField]
    [Tooltip("Visible state of the room.")]
    private GameObject roomVisuals;

    private Renderer[] renderers;

    private void Awake()
    {
        // Get all renderers in roomVisuals and its children
        if (roomVisuals != null)
            renderers = roomVisuals.GetComponentsInChildren<Renderer>(true);
    }

    private void Start()
    {
        // Hide room visibility inititally if unexplored.
        if (renderers != null && !explored)
            SetRenderers(false);
    }

    // Make room visible.
    public void Explore()
    {
        if (explored) return;
        explored = true;
        SetRenderers(true);
    }

    // Make room invisible.
    public void Hide()
    {
        explored = false;
        SetRenderers(false);
    }

    // Render True or False
    private void SetRenderers(bool visible)
    {
        if (renderers == null) return;
        foreach (Renderer rend in renderers)
        {
            rend.enabled = visible;
        }
    }

    // Make room visible when player enters it.
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Explore();
        }
    }

    // Make room invisible when player leaves it.
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Hide();
        }
    }
}