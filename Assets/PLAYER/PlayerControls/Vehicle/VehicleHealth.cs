using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
public class VehicleHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth = 100;

    [Header("Respawn Event")]
    public UnityEvent onDeathImmediate;

    [Header("Damage from collisions")]
    [Tooltip("Which physics layers should cause damage (default = Default). Use the Layer dropdown in the inspector.")]
    public LayerMask damageLayer = 1 << 0;

    [Tooltip("Impact speed (m/s) below this will not cause damage")]
    public float minDamageSpeed = 2f;

    [Tooltip("How much damage per (m/s) above minDamageSpeed.")]
    public float damageScale = 10f;

    [Tooltip("Maximum damage a single collision can do")]
    public int maxDamagePerHit = 100;

    [Tooltip("Seconds of invulnerability after taking a collision hit")]
    public float invulnerabilityAfterHit = 0.25f;

    private float lastDamageTime = -10f;
    private bool isDead = false;
    private float deathTime = 0f;

    [Header("Damage Audio")]
    public AudioSource damageAudioSource;
    public AudioClip damageSound1;
    public AudioClip damageSound2;
    public AudioClip damageSound3;

    [Header("Progressive Damage Effects")]
    public GameObject lightSmokeEffect;
    public GameObject heavySmokeEffect;
    public GameObject fireEffect;

    private bool lightSmokeEnabled = false;
    private bool heavySmokeEnabled = false;
    private bool fireEnabled = false;

    [Header("Death behaviour")]
    public GameObject objectToDisableOnDeath;
    public GameObject objectToEnableOnDeath;

    [System.Serializable]
    public class DeathEvent
    {
        public float delay;
        public UnityEvent onTrigger;
        [HideInInspector] public bool hasTriggered = false;
    }

    [Header("Timed Death Events")]
    public DeathEvent[] deathEvents;

    void Reset()
    {
        currentHealth = maxHealth;
    }

    void Awake()
    {
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (lightSmokeEffect != null)
            lightSmokeEffect.SetActive(false);
        if (heavySmokeEffect != null)
            heavySmokeEffect.SetActive(false);
        if (fireEffect != null)
            fireEffect.SetActive(false);

        if (deathEvents != null)
        {
            for (int i = 0; i < deathEvents.Length; i++)
            {
                deathEvents[i].hasTriggered = false;
            }
        }
    }

void Start()
{
    if (RespawnManager.Instance != null)
    {
        RespawnManager.Instance.RegisterPlayer(gameObject);
    }
}

    void Update()
    {
        if (isDead)
        {
            float timeSinceDeath = Time.time - deathTime;

            if (deathEvents != null)
            {
                for (int i = 0; i < deathEvents.Length; i++)
                {
                    if (!deathEvents[i].hasTriggered && timeSinceDeath >= deathEvents[i].delay)
                    {
                        deathEvents[i].hasTriggered = true;
                        deathEvents[i].onTrigger?.Invoke();
                    }
                }
            }
        }
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

        PlayRandomDamageSound();
        UpdateDamageEffects();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void UpdateDamageEffects()
    {
        if (currentHealth <= 20 && !fireEnabled)
        {
            if (fireEffect != null) fireEffect.SetActive(true);
            if (heavySmokeEffect != null) heavySmokeEffect.SetActive(false);
            if (lightSmokeEffect != null) lightSmokeEffect.SetActive(false);

            fireEnabled = true;
            heavySmokeEnabled = false;
            lightSmokeEnabled = false;
        }
        else if (currentHealth <= 40 && !heavySmokeEnabled)
        {
            if (heavySmokeEffect != null) heavySmokeEffect.SetActive(true);
            if (lightSmokeEffect != null) lightSmokeEffect.SetActive(false);
            if (fireEffect != null) fireEffect.SetActive(false);

            heavySmokeEnabled = true;
            lightSmokeEnabled = false;
            fireEnabled = false;
        }
        else if (currentHealth <= 70 && !lightSmokeEnabled)
        {
            if (lightSmokeEffect != null) lightSmokeEffect.SetActive(true);
            if (heavySmokeEffect != null) heavySmokeEffect.SetActive(false);
            if (fireEffect != null) fireEffect.SetActive(false);

            lightSmokeEnabled = true;
            heavySmokeEnabled = false;
            fireEnabled = false;
        }
    }

    void PlayRandomDamageSound()
    {
        if (damageAudioSource == null)
            return;

        AudioClip[] damageSounds = new AudioClip[] { damageSound1, damageSound2, damageSound3 };

        AudioClip selectedClip = null;
        int safety = 0;

        while (selectedClip == null && safety < 10)
        {
            int randomIndex = Random.Range(0, damageSounds.Length);
            selectedClip = damageSounds[randomIndex];
            safety++;
        }

        if (selectedClip != null)
        {
            damageAudioSource.PlayOneShot(selectedClip);
        }
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;
        deathTime = Time.time;

        if (objectToDisableOnDeath != null)
            objectToDisableOnDeath.SetActive(false);

        if (objectToEnableOnDeath != null)
            objectToEnableOnDeath.SetActive(true);

        // 🔹 Trigger Respawn Manager
        onDeathImmediate?.Invoke();

// Tell RespawnManager to respawn
if (RespawnManager.Instance != null)
{
    RespawnManager.Instance.Respawn();
}
    }
}