using System.Collections.Generic;
using UnityEngine;
using static EquipmentEnum;

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

    /// <summary>Hides every other room and shows only this one (called when the player enters this room).</summary>
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
        if (roomVisuals == null)
            return;

        // Loot / pickups spawn after Awake; re-scan so hidden rooms do not leave new renderers enabled
        // (fixes seeing adjacent rooms through doors and "invisible" items).
        renderers = roomVisuals.GetComponentsInChildren<Renderer>(true);
        if (renderers == null)
            return;

        foreach (Renderer rend in renderers)
        {
            if (rend != null)
                rend.enabled = visible;
        }

        // Spawned room pickups (including bat weapon pickup) may live outside roomVisuals hierarchy.
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

    [Header("Sports bat hint")]
    [SerializeField] private float sportsBatHintRadius = 2.35f;
    [SerializeField] private float sportsBatHintCooldownSeconds = 7f;

    private float _nextSportsBatHintUnscaledTime;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            ApplyAsCurrentVisibleRoom();
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        TryShowSportsBatProximityHint(other);
    }

    private void TryShowSportsBatProximityHint(Collider2D playerCollider)
    {
        if (!RoomLootSpawnTypeHelper.TryGetRoomType(transform, out RoomType roomType) ||
            roomType != RoomType.SportsRoom)
        {
            return;
        }

        PlayerInventoryInteraction inv = playerCollider.GetComponent<PlayerInventoryInteraction>();
        EquipmentData equipment = inv != null ? inv.EquipmentData : null;
        if (equipment == null)
        {
            return;
        }

        Item weapon = equipment.GetEquippedItem(EquipTag.Weapon);
        if (weapon != null && weapon.definition != null)
        {
            return;
        }

        Transform batPickup = FindSportsBatPickupTransformInRoom();
        if (batPickup == null)
        {
            return;
        }

        float dist = Vector2.Distance(playerCollider.transform.position, batPickup.position);
        if (dist > sportsBatHintRadius)
        {
            return;
        }

        if (Time.unscaledTime < _nextSportsBatHintUnscaledTime)
        {
            return;
        }

        _nextSportsBatHintUnscaledTime = Time.unscaledTime + sportsBatHintCooldownSeconds;
        BossRoomNoticeUI.Instance?.ShowMessage(
            "Press F to pick up the bat to defend yourself.",
            3.8f);
    }

    /// <summary>Uses <see cref="SportsRoomBatPlacer.BatInstanceName"/> so this file does not depend on <c>WeaponWorldPickup</c> (avoids asm / import issues).</summary>
    private Transform FindSportsBatPickupTransformInRoom()
    {
        Transform spawned = transform.Find("SpawnedItems");
        if (spawned == null)
            return null;

        return spawned.Find(SportsRoomBatPlacer.BatInstanceName);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            Hide();
    }
}
