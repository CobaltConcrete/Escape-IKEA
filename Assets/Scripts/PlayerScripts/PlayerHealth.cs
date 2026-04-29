using System.Collections;
using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float startingHealth = 100f;
    [SerializeField] private float deathSceneDelay = 0.6f;

    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }

    public event Action OnHealthChanged;

    private bool isDead = false;
    private PlayerInventoryInteraction playerInventoryInteraction;

    private void Awake()
    {
        CurrentHealth = Mathf.Clamp(startingHealth, 0f, maxHealth);
        playerInventoryInteraction = GetComponent<PlayerInventoryInteraction>();
    }

    private void Start()
    {
        OnHealthChanged?.Invoke();
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f || isDead)
            return;

        float finalDamage = amount;

        HandleArmorBeforeHealthDamage(amount, ref finalDamage);

        float previousHealth = CurrentHealth;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - finalDamage);
        OnHealthChanged?.Invoke();

        if (CurrentHealth < previousHealth)
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayPlayerHurt();
            }
        }

        if (CurrentHealth <= 0f && !isDead)
        {
            isDead = true;
            StartCoroutine(HandleDeath());
        }
    }

    private void HandleArmorBeforeHealthDamage(float incomingDamage, ref float finalDamage)
    {
        if (playerInventoryInteraction == null)
            return;

        Item equippedArmor = playerInventoryInteraction.GetEquippedArmorItem();
        if (equippedArmor == null || !equippedArmor.IsArmor())
            return;
        if (equippedArmor.GetArmorSpecialAbility() == ArmorSpecialAbility.Dash)
        {
            // DashBelt£º²»²ÎÓë¼õÉË¡¢²»µôÄÍ¾Ã
            return;
        }

        equippedArmor.InitializeRuntimeDataIfNeeded();

        // ¼õÉË
        float damageReduction = equippedArmor.GetArmorDamageReduction();
        finalDamage = Mathf.Max(0f, incomingDamage - damageReduction);

        // µôÄÍ¾Ã
        float durabilityLoss = incomingDamage;

        if (equippedArmor.definition != null)
        {
            durabilityLoss *= Mathf.Max(0f, equippedArmor.definition.armorDurabilityLossMultiplier);
        }

        bool broken = equippedArmor.DamageArmor(durabilityLoss);
        playerInventoryInteraction.RefreshEquipmentAndInventoryUI();

        if (broken)
        {
            playerInventoryInteraction.BreakEquippedArmor();
        }
    }

    private IEnumerator HandleDeath()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopAmbient();

            RoomLightBuzz[] allBuzz = FindObjectsOfType<RoomLightBuzz>();
            foreach (var buzz in allBuzz)
            {
                buzz.StopBuzz();
            }

            SoundManager.Instance.PlayPlayerDeath();
        }

        yield return new WaitForSeconds(deathSceneDelay);

        PageManager.LoseGame();
    }

    public void Heal(float amount)
    {
        if (amount <= 0f || isDead)
            return;

        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        OnHealthChanged?.Invoke();
    }

    public void SetMaxHealth(float newMax)
    {
        maxHealth = Mathf.Max(1f, newMax);
        CurrentHealth = Mathf.Min(CurrentHealth, maxHealth);
        OnHealthChanged?.Invoke();
    }

    public float getHealth()
    {
        return CurrentHealth;
    }
}