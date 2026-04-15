using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{
    public bool explored = false;

    private static readonly List<Room> s_AllRooms = new List<Room>();

    [Header("Room Visuals")]
    [SerializeField]
    [Tooltip("Renderers under this object (and children) are toggled. Only one room is visible at a time while the player is inside a room.")]
    private GameObject roomVisuals;

    private Renderer[] renderers;

    private void Awake()
    {
        s_AllRooms.Add(this);
        if (roomVisuals != null)
            renderers = roomVisuals.GetComponentsInChildren<Renderer>(true);
    }

    private void OnDestroy()
    {
        s_AllRooms.Remove(this);
    }

    private void Start()
    {
        if (renderers != null && !explored)
            SetRenderersVisible(false);
    }

    /// <summary>Hides every other room and shows only this one (called when the player enters this room).</summary>
    public void ApplyAsCurrentVisibleRoom()
    {
        for (int i = 0; i < s_AllRooms.Count; i++)
        {
            Room room = s_AllRooms[i];
            if (room == null || room.renderers == null)
                continue;

            bool isThis = room == this;
            room.SetRenderersVisible(isThis);
            if (isThis)
                room.explored = true;
        }
    }

    public void Explore()
    {
        if (explored)
            return;
        explored = true;
        SetRenderersVisible(true);
    }

    public void Hide()
    {
        explored = false;
        SetRenderersVisible(false);
    }

    private void SetRenderersVisible(bool visible)
    {
        if (renderers == null)
            return;
        foreach (Renderer rend in renderers)
        {
            if (rend != null)
                rend.enabled = visible;
        }
    }

    /// <summary>Re-scan renderers after <see cref="RoomPresentation"/> adds floor tiles.</summary>
    public void RefreshRendererRegistry()
    {
        if (roomVisuals == null)
            return;

        renderers = roomVisuals.GetComponentsInChildren<Renderer>(true);
        if (!explored)
            SetRenderersVisible(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            ApplyAsCurrentVisibleRoom();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            Hide();
    }
}
