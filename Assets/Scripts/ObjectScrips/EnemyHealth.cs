using UnityEngine;
using UnityEngine.Events;

public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 3;
    public bool destroyOnDeath = true;   // turn off once the enemy has a death animation

    public UnityEvent onHurt;   // e.g. trigger the Hurt animation
    public UnityEvent onDeath;  // e.g. trigger the Death animation

    [SerializeField] int currentHealth;   // visible in the Inspector so you can watch it drop

    public int Health => currentHealth;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount = 1)
    {
        if (currentHealth <= 0)
            return;

        currentHealth -= amount;
        Debug.Log(name + " was hit, health left: " + currentHealth);

        if (currentHealth <= 0)
        {
            onDeath?.Invoke();

            if (destroyOnDeath)
                Destroy(gameObject);
        }
        else
        {
            onHurt?.Invoke();
        }
    }
}
