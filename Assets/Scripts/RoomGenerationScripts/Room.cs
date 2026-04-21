using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{
    public bool explored = false;

    private static readonly List<Room> s_AllRooms = new List<Room>();

    public static void ResetOneShotHintsForNewMap()
    {
    }

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

    public void ApplyAsCurrentVisibleRoom()
    {
        for (int i = 0; i < s_AllRooms.Count; i++)
        {
            Room room = s_AllRooms[i];
            if (room == null)
                continue;

            bool isThis = room == this;
            room.SetRenderersVisible(isThis);
            if (isThis)
                room.explored = true;
        }
    }

    public static void ApplyVisibleRoomAtPosition(Vector3 worldPosition)
    {
        Room bestRoom = null;
        float bestArea = 0f;

        for (int i = 0; i < s_AllRooms.Count; i++)
        {
            Room room = s_AllRooms[i];
            if (room == null)
                continue;

            Collider2D[] colliders = room.GetComponentsInChildren<Collider2D>(true);
            for (int c = 0; c < colliders.Length; c++)
            {
                Collider2D col = colliders[c];
                if (col == null || !col.isTrigger || !col.OverlapPoint(worldPosition))
                    continue;

                float area = col.bounds.size.x * col.bounds.size.y;
                if (area > bestArea)
                {
                    bestArea = area;
                    bestRoom = room;
                }
            }
        }

        if (bestRoom != null)
            bestRoom.ApplyAsCurrentVisibleRoom();
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
        if (roomVisuals != null)
        {

            renderers = roomVisuals.GetComponentsInChildren<Renderer>(true);
            if (renderers != null)
            {
                foreach (Renderer rend in renderers)
                {
                    if (rend != null)
                        rend.enabled = visible;
                }
            }
        }

        Transform spawnedItems = transform.Find("SpawnedItems");
        if (spawnedItems != null)
        {
            Renderer[] spawnedRenderers = spawnedItems.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < spawnedRenderers.Length; i++)
            {
                if (spawnedRenderers[i] != null)
                    spawnedRenderers[i].enabled = visible;
            }

            Collider2D[] spawnedColliders = spawnedItems.GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < spawnedColliders.Length; i++)
            {
                if (spawnedColliders[i] != null)
                    spawnedColliders[i].enabled = visible;
            }
        }

        Transform decorations = transform.Find("Decorations");
        if (decorations != null)
        {
            Renderer[] decorRenderers = decorations.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < decorRenderers.Length; i++)
            {
                if (decorRenderers[i] != null)
                    decorRenderers[i].enabled = visible;
            }

            Collider2D[] decorColliders = decorations.GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < decorColliders.Length; i++)
            {
                if (decorColliders[i] != null)
                    decorColliders[i].enabled = visible;
            }
        }

        Transform floorClipRoot = transform.Find("FloorClipRoot");
        if (floorClipRoot != null)
        {
            Renderer[] floorRenderers = floorClipRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < floorRenderers.Length; i++)
            {
                if (floorRenderers[i] != null)
                    floorRenderers[i].enabled = visible;
            }
        }
    }

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
        {
            ApplyAsCurrentVisibleRoom();
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            ApplyAsCurrentVisibleRoom();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            Hide();
    }
}
