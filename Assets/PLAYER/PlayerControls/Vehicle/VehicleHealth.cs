using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class VehicleHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth = 100;

    [Header("Damage from collisions")]
    [Tooltip("Which physics layers should cause damage (default = Default). Use the Layer dropdown in the inspector.")]
    public LayerMask damageLayer = 1 << 0; // Default layer

    [Tooltip("Impact speed (m/s) below this will not cause damage")]
    public float minDamageSpeed = 2f;

    [Tooltip("How much damage per (m/s) above minDamageSpeed. Increase to make impacts more punishing.")]
    public float damageScale = 10f;

    [Tooltip("Maximum damage a single collision can do")]
    public int maxDamagePerHit = 100;

    [Tooltip("Seconds of invulnerability after taking a collision hit (prevents multiple contacts in one impact)")]
    public float invulnerabilityAfterHit = 0.25f;

    private float lastDamageTime = -10f;
    private bool isDead = false;

    [Header("Damage Audio")]
    public AudioSource damageAudioSource;
    public AudioClip damageSound1;
    public AudioClip damageSound2;
    public AudioClip damageSound3;

    [Header("Death behaviour (assign in Inspector)")]
    [Tooltip("GameObject to disable when health reaches zero (e.g. the intact car model)")]
    public GameObject objectToDisableOnDeath;

    [Tooltip("GameObject to enable when health reaches zero (e.g. wrecked model / ragdoll)")]
    public GameObject objectToEnableOnDeath;

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

        // check layer mask
        if ((damageLayer.value & (1 << collision.gameObject.layer)) == 0)
            return;

        // guard invulnerability window
        if (Time.time < lastDamageTime + invulnerabilityAfterHit)
            return;

        // use relativeVelocity to measure impact speed (accounts for both objects)
        float impactSpeed = collision.relativeVelocity.magnitude;

        // ignore small bumps
        if (impactSpeed < minDamageSpeed)
            return;

        // compute damage: linear with speed above minDamageSpeed
        float rawDamage = (impactSpeed - minDamageSpeed) * damageScale;
        int damageAmount = Mathf.Clamp(Mathf.FloorToInt(rawDamage), 1, maxDamagePerHit);

        // apply damage
        TakeDamage(damageAmount);

        lastDamageTime = Time.time;
    }

    // public API to apply damage from other sources too
    public void TakeDamage(int amount)
    {
        if (isDead) return;
        if (amount <= 0) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log($"Took {amount} damage. Health: {currentHealth}/{maxHealth}");

        // Play random damage sound
        PlayRandomDamageSound();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void PlayRandomDamageSound()
    {
        if (damageAudioSource == null)
        {
            Debug.LogWarning("[VehicleHealth] No AudioSource assigned for damage sounds!");
            return;
        }

        // Collect available damage sounds
        AudioClip[] damageSounds = new AudioClip[] { damageSound1, damageSound2, damageSound3 };
        
        // Filter out null clips
        int availableSounds = 0;
        for (int i = 0; i < damageSounds.Length; i++)
        {
            if (damageSounds[i] != null)
                availableSounds++;
        }

        if (availableSounds == 0)
        {
            Debug.LogWarning("[VehicleHealth] No damage sound clips assigned!");
            return;
        }

        // Pick a random non-null sound
        AudioClip selectedClip = null;
        while (selectedClip == null)
        {
            int randomIndex = Random.Range(0, damageSounds.Length);
            selectedClip = damageSounds[randomIndex];
        }

        // Play the selected sound
        damageAudioSource.PlayOneShot(selectedClip);
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("YOU'RE DIED!!!");

        // Disable the assigned object (if any)
        if (objectToDisableOnDeath != null)
        {
            objectToDisableOnDeath.SetActive(false);
        }

        // Enable the assigned object (if any)
        if (objectToEnableOnDeath != null)
        {
            objectToEnableOnDeath.SetActive(true);
        }

        // Optional: disable this GameObject's collider/controls/rigidbody to prevent further movement:
        // Collider col = GetComponent<Collider>();
        // if (col) col.enabled = false;
        // Rigidbody rb = GetComponent<Rigidbody>();
        // if (rb) rb.isKinematic = true;

        // TODO: add more death handling (respawn, UI, sounds) as needed
    }
}