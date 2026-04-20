using UnityEngine;

[DisallowMultipleComponent]
public class SportsRoomBatAutoPickup : MonoBehaviour
{
    [SerializeField] private ItemDefinition batDefinition;
    [SerializeField] private int amount = 1;
    [SerializeField] private float pickupRadius = 0.95f;

    private bool pickedUp;

    public void Configure(ItemDefinition definition, int pickupAmount)
    {
        batDefinition = definition != null ? definition : ResolveBatDefinition();
        amount = Mathf.Max(1, pickupAmount);
        EnsureTriggerCollider();
    }

    private void Awake()
    {
        if (batDefinition == null)
            batDefinition = ResolveBatDefinition();

        EnsureTriggerCollider();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryPickup(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryPickup(other);
    }

    private void Update()
    {
        if (pickedUp || batDefinition == null)
            return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, pickupRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            if (TryPickup(hits[i]))
                return;
        }
    }

    private bool TryPickup(Collider2D other)
    {
        if (pickedUp || other == null || batDefinition == null)
            return false;

        PlayerInventoryInteraction player = other.GetComponent<PlayerInventoryInteraction>();
        if (player == null)
            player = other.GetComponentInParent<PlayerInventoryInteraction>();
        if (player == null)
            return false;

        pickedUp = true;
        player.PickupWeaponFromWorld(batDefinition, amount, gameObject);
        return true;
    }

    private void EnsureTriggerCollider()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box == null)
            box = gameObject.AddComponent<BoxCollider2D>();

        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>(true);
        if (sr != null && sr.sprite != null)
        {
            Vector2 size = sr.sprite.bounds.size;
            box.size = new Vector2(
                Mathf.Max(0.8f, size.x * 2.6f),
                Mathf.Max(0.55f, size.y * 2.6f));
            box.offset = sr.transform.localPosition;
        }
        else
        {
            box.size = new Vector2(1.1f, 0.7f);
            box.offset = Vector2.zero;
        }

        box.isTrigger = true;
    }

    private ItemDefinition ResolveBatDefinition()
    {
        ItemWorldSpawner spawner = GetComponent<ItemWorldSpawner>();
        if (spawner == null)
            spawner = GetComponentInChildren<ItemWorldSpawner>(true);

        if (spawner != null && spawner.ItemDefinition != null)
            return spawner.ItemDefinition;

        MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null ||
                !string.Equals(behaviour.GetType().Name, "WeaponWorldPickup", System.StringComparison.Ordinal))
            {
                continue;
            }

            System.Reflection.PropertyInfo property = behaviour.GetType().GetProperty("WeaponDefinition");
            ItemDefinition definition = property != null ? property.GetValue(behaviour, null) as ItemDefinition : null;
            if (definition != null)
                return definition;
        }

        return Resources.Load<ItemDefinition>(BatWeapon.ItemName);
    }
}
