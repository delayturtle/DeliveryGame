using UnityEngine;

public class EmissionFade : MonoBehaviour
{
    [Header("Emission Settings")]
    [Tooltip("Array of emission colors to cycle through")]
    public Color[] emissionColors = new Color[]
    {
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow
    };

    [Tooltip("Time in seconds to fade between colors")]
    public float fadeTime = 2f;

    [Tooltip("Emission intensity multiplier")]
    public float emissionIntensity = 1f;

    [Header("Material Settings")]
    [Tooltip("Renderer component (auto-finds if null)")]
    public Renderer targetRenderer;

    [Tooltip("Material index to modify (if multiple materials)")]
    public int materialIndex = 0;

    private Material targetMaterial;
    private int currentColorIndex = 0;
    private int nextColorIndex = 1;
    private float fadeProgress = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Auto-find renderer if not assigned
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        if (targetRenderer == null)
        {
            Debug.LogError("[EmissionFade] No Renderer found on GameObject!");
            enabled = false;
            return;
        }

        // Get the material (creates instance automatically)
        if (targetRenderer.materials.Length > materialIndex)
        {
            targetMaterial = targetRenderer.materials[materialIndex];
        }
        else
        {
            Debug.LogError($"[EmissionFade] Material index {materialIndex} out of range!");
            enabled = false;
            return;
        }

        // Enable emission keyword
        targetMaterial.EnableKeyword("_EMISSION");

        // Validate colors array
        if (emissionColors == null || emissionColors.Length == 0)
        {
            Debug.LogWarning("[EmissionFade] No emission colors assigned! Using default red.");
            emissionColors = new Color[] { Color.red };
        }

        // Set initial emission color
        SetEmissionColor(emissionColors[currentColorIndex]);
    }

    // Update is called once per frame
    void Update()
    {
        if (targetMaterial == null || emissionColors.Length == 0)
            return;

        // Only cycle if we have more than one color
        if (emissionColors.Length == 1)
        {
            SetEmissionColor(emissionColors[0]);
            return;
        }

        // Increment fade progress
        fadeProgress += Time.deltaTime / fadeTime;

        // Lerp between current and next color
        Color currentColor = emissionColors[currentColorIndex];
        Color nextColor = emissionColors[nextColorIndex];
        Color lerpedColor = Color.Lerp(currentColor, nextColor, fadeProgress);

        SetEmissionColor(lerpedColor);

        // Check if fade is complete
        if (fadeProgress >= 1f)
        {
            fadeProgress = 0f;
            currentColorIndex = nextColorIndex;
            nextColorIndex = (nextColorIndex + 1) % emissionColors.Length;
        }
    }

    void SetEmissionColor(Color color)
    {
        // Apply emission intensity
        Color finalColor = color * emissionIntensity;
        targetMaterial.SetColor("_EmissionColor", finalColor);
    }

    void OnDestroy()
    {
        // Clean up material instance
        if (targetMaterial != null && Application.isPlaying)
        {
            Destroy(targetMaterial);
        }
    }

    void OnValidate()
    {
        // Clamp values in inspector
        if (fadeTime < 0.1f)
            fadeTime = 0.1f;

        if (emissionIntensity < 0f)
            emissionIntensity = 0f;
    }
}
