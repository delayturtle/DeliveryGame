using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BattleChatter : MonoBehaviour
{
    [Header("Chatter Audio Clips")]
    [Tooltip("Array of audio clips to play randomly")]
    public AudioClip[] chatterClips;

    [Header("Timing Settings")]
    [Tooltip("Minimum time (seconds) between chatter")]
    public float minInterval = 6f;
    
    [Tooltip("Maximum time (seconds) between chatter")]
    public float maxInterval = 30f;

    private AudioSource audioSource;
    private float nextChatterTime;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        // Set the first chatter time
        ResetTimer();
    }

    void Update()
    {
        // Check if it's time to play chatter
        if (Time.time >= nextChatterTime)
        {
            PlayRandomChatter();
            ResetTimer();
        }
    }

    void PlayRandomChatter()
    {
        // Ensure we have clips and an audio source
        if (chatterClips == null || chatterClips.Length == 0)
        {
            Debug.LogWarning("[BattleChatter] No chatter clips assigned!");
            return;
        }

        if (audioSource == null)
        {
            Debug.LogWarning("[BattleChatter] No AudioSource found!");
            return;
        }

        // Select a random non-null clip
        AudioClip selectedClip = null;
        int attempts = 0;
        int maxAttempts = chatterClips.Length * 2;

        while (selectedClip == null && attempts < maxAttempts)
        {
            int randomIndex = Random.Range(0, chatterClips.Length);
            selectedClip = chatterClips[randomIndex];
            attempts++;
        }

        if (selectedClip != null)
        {
            audioSource.PlayOneShot(selectedClip);
        }
        else
        {
            Debug.LogWarning("[BattleChatter] All clips in array are null!");
        }
    }

    void ResetTimer()
    {
        // Set next chatter time to a random interval from now
        float randomInterval = Random.Range(minInterval, maxInterval);
        nextChatterTime = Time.time + randomInterval;
    }
}
