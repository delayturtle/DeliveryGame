using UnityEngine;

public class AntigravityGrenade : MonoBehaviour
{
    [Header("Detonation")]
    public GameObject antigravityFieldPrefab;
    public float detonationDelay = 1.5f;

    private bool fuseArmed = false;
    private bool detonated = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (fuseArmed || detonated)
            return;

        fuseArmed = true;
        Invoke(nameof(Detonate), detonationDelay);
    }

    private void Detonate()
    {
        if (detonated) return;
        detonated = true;

        Instantiate(
            antigravityFieldPrefab,
            transform.position,
            Quaternion.identity
        );

        Destroy(gameObject);
    }
}
