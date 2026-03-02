using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
public class EnemyVehicleBossHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth = 100;

    [Header("Damage from collisions")]
    public LayerMask damageLayer = 1 << 0;
    public float minDamageSpeed = 2f;
    public float damageScale = 10f;
    public int maxDamagePerHit = 100;
    public float invulnerabilityAfterHit = 0.25f;

    private float lastDamageTime = -10f;
    private bool isDead = false;

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

    [Header("Death Behaviour")]
    public GameObject objectToDisableOnDeath;
    public GameObject objectToEnableOnDeath;
    public float destroyDelay = 6f;

    [Header("Item Drop (Optional)")]
    public GameObject dropItemPrefab;
    public Transform dropSpawnPoint;

    [Header("Objective Integration (Optional Boss Settings)")]
    public bool clearsObjectiveOnDeath = false;
    public QuestManager questManager;

    [Header("Enemy AI Integration")]
    public EnemyAI enemyAI;
    public BattleChatter battleChatter;

    void Reset()
    {
        currentHealth = maxHealth;
    }

    void Awake()
    {
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (enemyAI == null)
            enemyAI = GetComponent<EnemyAI>();

        if (battleChatter == null)
            battleChatter = GetComponent<BattleChatter>();

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
        if (isDead || amount <= 0) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0);

        PlayRandomDamageSound();
        UpdateDamageEffects();

        if (currentHealth <= 0)
            Die();
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

        AudioClip[] damageSounds = { damageSound1, damageSound2, damageSound3 };

        AudioClip selectedClip = null;
        int safety = 0;

        while (selectedClip == null && safety < 10)
        {
            selectedClip = damageSounds[Random.Range(0, damageSounds.Length)];
            safety++;
        }

        if (selectedClip != null)
            damageAudioSource.PlayOneShot(selectedClip);
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        if (enemyAI != null)
            enemyAI.enabled = false;

        if (battleChatter != null)
            battleChatter.enabled = false;

        if (dropItemPrefab != null)
        {
            Transform spawnPoint = dropSpawnPoint != null ? dropSpawnPoint : transform;
            Instantiate(dropItemPrefab, spawnPoint.position, spawnPoint.rotation);
        }

        if (clearsObjectiveOnDeath && questManager != null)
            questManager.ClearObjectiveOverride();

        if (objectToDisableOnDeath != null)
            objectToDisableOnDeath.SetActive(false);

        if (objectToEnableOnDeath != null)
            objectToEnableOnDeath.SetActive(true);

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = true;

        Destroy(gameObject, destroyDelay);
    }

    public bool IsDead() => isDead;
    public float GetHealthPercentage() => (float)currentHealth / maxHealth;
}