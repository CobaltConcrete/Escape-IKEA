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

    private void Awake()
    {
        CurrentHealth = Mathf.Clamp(startingHealth, 0f, maxHealth);
    }

    private void Start()
    {
        OnHealthChanged?.Invoke();
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f || isDead)
            return;

        float previousHealth = CurrentHealth;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
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