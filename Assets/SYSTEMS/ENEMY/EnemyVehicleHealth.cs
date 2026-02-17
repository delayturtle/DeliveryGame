using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyVehicleHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth = 100;

    [Header("Collision Damage")]
    public LayerMask damageLayer = 1 << 0;
    public float minDamageSpeed = 2f;
    public float damageScale = 10f;
    public int maxDamagePerHit = 100;
    public float invulnerabilityAfterHit = 0.25f;

    float lastDamageTime = -10f;
    bool isDead = false;

    [Header("Damage Audio")]
    public AudioSource damageAudioSource;
    public AudioClip damageSound1;
    public AudioClip damageSound2;
    public AudioClip damageSound3;

    [Header("Death Behaviour")]
    public GameObject objectToDisableOnDeath;
    public GameObject objectToEnableOnDeath;
    public float destroyDelay = 3f;

    void Reset()
    {
        currentHealth = maxHealth;
    }

    void Awake()
    {
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isDead) return;

        if ((damageLayer.value & (1 << collision.gameObject.layer)) == 0)
            return;

        if (Time.time < lastDamageTime + invulnerabilityAfterHit)
            return;

        float impactSpeed = collision.relativeVelocity.magnitude;

        if (impactSpeed < minDamageSpeed)
            return;

        float rawDamage = (impactSpeed - minDamageSpeed) * damageScale;
        int damageAmount = Mathf.Clamp(Mathf.FloorToInt(rawDamage), 1, maxDamagePerHit);

        TakeDamage(damageAmount);

        lastDamageTime = Time.time;
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;
        if (amount <= 0) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log($"Enemy took {amount} damage. Health: {currentHealth}/{maxHealth}");

        PlayRandomDamageSound();

        if (currentHealth <= 0)
            Die();
    }

    void PlayRandomDamageSound()
    {
        if (damageAudioSource == null)
            return;

        AudioClip[] sounds = { damageSound1, damageSound2, damageSound3 };

        int available = 0;
        foreach (var s in sounds)
            if (s != null) available++;

        if (available == 0)
            return;

        AudioClip chosen = null;
        while (chosen == null)
            chosen = sounds[Random.Range(0, sounds.Length)];

        damageAudioSource.PlayOneShot(chosen);
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log($"{gameObject.name} destroyed.");

        if (objectToDisableOnDeath != null)
            objectToDisableOnDeath.SetActive(false);

        if (objectToEnableOnDeath != null)
            objectToEnableOnDeath.SetActive(true);

        // stop physics motion
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = true;

        Destroy(gameObject, destroyDelay);
    }
}
