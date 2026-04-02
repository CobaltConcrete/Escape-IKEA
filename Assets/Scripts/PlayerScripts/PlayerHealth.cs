using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float startingHealth = 100f;

    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }

    public event Action OnHealthChanged;

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
        if (amount <= 0f){
            return;
        }
        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        OnHealthChanged?.Invoke();
        if (CurrentHealth <= 0){
            PageManager.LoseGame();
        }
    }

    public void Heal(float amount)
    {
        if (amount <= 0f) return;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        OnHealthChanged?.Invoke();
    }

    public void SetMaxHealth(float newMax)
    {
        maxHealth = Mathf.Max(1f, newMax);
        CurrentHealth = Mathf.Min(CurrentHealth, maxHealth);
        OnHealthChanged?.Invoke();
    }

    public float getHealth(){
        return CurrentHealth;
    }
}
