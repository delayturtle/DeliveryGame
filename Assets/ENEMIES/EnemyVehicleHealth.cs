using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyVehicleHealth : MonoBehaviour
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

    [Header("Progressive Damage Effects")]
    [Tooltip("Light smoke effect - enabled at 70 health")]
    public GameObject lightSmokeEffect;
    
    [Tooltip("Heavy smoke effect - enabled at 40 health")]
    public GameObject heavySmokeEffect;
    
    [Tooltip("Fire effect - enabled at 20 health")]
    public GameObject fireEffect;

    private bool lightSmokeEnabled = false;
    private bool heavySmokeEnabled = false;
    private bool fireEnabled = false;

    [Header("Death behaviour (assign in Inspector)")]
    [Tooltip("GameObject to disable when health reaches zero (e.g. the intact car model)")]
    public GameObject objectToDisableOnDeath;

    [Tooltip("GameObject to enable when health reaches zero (e.g. wrecked model / ragdoll)")]
    public GameObject objectToEnableOnDeath;

    [Header("Enemy AI Integration")]
    [Tooltip("Optional: Reference to EnemyAI script to disable AI on death")]
    public EnemyAI enemyAI;

    void Reset()
    {
        currentHealth = maxHealth;
    }

    void Awake()
    {
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        // Try to auto-find EnemyAI if not assigned
        if (enemyAI == null)
            enemyAI = GetComponent<EnemyAI>();
        
        // Ensure all damage effects start disabled
        if (lightSmokeEffect != null)
            lightSmokeEffect.SetActive(false);
        if (heavySmokeEffect != null)
            heavySmokeEffect.SetActive(false);
        if (fireEffect != null)
            fireEffect.SetActive(false);
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

        Debug.Log($"[EnemyVehicle] Took {amount} damage. Health: {currentHealth}/{maxHealth}");

        // Play random damage sound
        PlayRandomDamageSound();

        // Update damage effects based on health thresholds
        UpdateDamageEffects();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void UpdateDamageEffects()
    {
        // Fire effect at 20 health or below
        if (currentHealth <= 20 && !fireEnabled)
        {
            if (fireEffect != null)
                fireEffect.SetActive(true);
            if (heavySmokeEffect != null)
                heavySmokeEffect.SetActive(false);
            if (lightSmokeEffect != null)
                lightSmokeEffect.SetActive(false);
            
            fireEnabled = true;
            heavySmokeEnabled = false;
            lightSmokeEnabled = false;
        }
        // Heavy smoke at 40 health or below (but above 20)
        else if (currentHealth <= 40 && !heavySmokeEnabled)
        {
            if (heavySmokeEffect != null)
                heavySmokeEffect.SetActive(true);
            if (lightSmokeEffect != null)
                lightSmokeEffect.SetActive(false);
            if (fireEffect != null)
                fireEffect.SetActive(false);
            
            heavySmokeEnabled = true;
            lightSmokeEnabled = false;
            fireEnabled = false;
        }
        // Light smoke at 70 health or below (but above 40)
        else if (currentHealth <= 70 && !lightSmokeEnabled)
        {
            if (lightSmokeEffect != null)
                lightSmokeEffect.SetActive(true);
            if (heavySmokeEffect != null)
                heavySmokeEffect.SetActive(false);
            if (fireEffect != null)
                fireEffect.SetActive(false);
            
            lightSmokeEnabled = true;
            heavySmokeEnabled = false;
            fireEnabled = false;
        }
    }

    void PlayRandomDamageSound()
    {
        if (damageAudioSource == null)
            return;

        // Collect available damage sounds
        AudioClip[] damageSounds = new AudioClip[] { damageSound1, damageSound2, damageSound3 };
        
        // Filter out null clips and count available sounds
        int availableSounds = 0;
        for (int i = 0; i < damageSounds.Length; i++)
        {
            if (damageSounds[i] != null)
                availableSounds++;
        }

        if (availableSounds == 0)
            return;

        // Select a random non-null sound
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

        Debug.Log("[EnemyVehicle] Enemy vehicle destroyed!");

        // Disable the EnemyAI script if present
        if (enemyAI != null)
        {
            enemyAI.enabled = false;
        }

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

        // Make kinematic to freeze physics
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }
    }

    public bool IsDead()
    {
        return isDead;
    }

    public float GetHealthPercentage()
    {
        return (float)currentHealth / maxHealth;
    }
}
