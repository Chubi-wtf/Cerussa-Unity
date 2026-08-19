using System;
using UnityEngine;


public sealed class HealthScript_ : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;
    [SerializeField] private Movement_ movement;
    [SerializeField] private Gunslinger gunslinger;
    [SerializeField] private CharacterController characterController;

    public bool IsDead { get; private set; }
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    public event Action<int, int> OnHealthChanged; 
    public event Action OnDied;

    private void Awake()
    {
        currentHealth = maxHealth;

        if (movement == null) movement = GetComponent<Movement_>();
        if (gunslinger == null) gunslinger = GetComponent<Gunslinger>();
        if (characterController == null) characterController = GetComponent<CharacterController>();
    }

    public void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
            Die();
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        IsDead = true;

        if (movement != null) movement.enabled = false;
        if (gunslinger != null) gunslinger.enabled = false;
        if (characterController != null) characterController.enabled = false;

        OnDied?.Invoke(); 
    }

    public void Revive()
    {
        IsDead = false;
        currentHealth = maxHealth;

        if (movement != null) movement.enabled = true;
        if (gunslinger != null) gunslinger.enabled = true;
        if (characterController != null) characterController.enabled = true;

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}