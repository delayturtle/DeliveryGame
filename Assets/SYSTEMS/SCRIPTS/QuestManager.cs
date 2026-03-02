using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Events;

[System.Serializable]
public class ScoreMilestone
{
    public string name;
    public int requiredScore;
    public UnityEvent onReached;

    [HideInInspector] public bool triggered;
}

public class QuestManager : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI questText;
    public TextMeshProUGUI scoreText;

    [Header("Respawn Settings")]
    public int respawnScorePenalty = 100;

    [Header("Package Search")]
    public float searchRadius = 50f;

    [Header("Delivery Points")]
    public List<DeliveryPoint> allDeliveryPoints = new List<DeliveryPoint>();

    [Header("Arrow Prefabs")]
    public GameObject compassArrowPrefab;
    public GameObject worldArrowPrefab;

    [Header("Score Settings")]
    public int maxScore = 45000;

    [Header("Score Milestones")]
    public List<ScoreMilestone> scoreMilestones = new List<ScoreMilestone>();

    // =====================================================
    // MANUAL OBJECTIVE OVERRIDE (INSPECTOR FRIENDLY)
    // =====================================================

    [Header("Manual Override Settings")]
    [TextArea]
    public string manualOverrideText;
    public Transform manualOverrideTarget;

    private bool objectiveOverrideActive = false;
    private string overrideObjectiveText;
    private Transform overrideTarget;

    // =====================================================

    private TruckInventory inventory;
    private PlayerCompassArrow compassArrow;
    private FloatingArrow worldArrow;

    private Transform currentPlayerTransform;

    private List<DeliveryPoint> activeDeliveries = new List<DeliveryPoint>();
    private DeliveryPoint lastDeliveryPoint;

    private GameObject currentTargetPackage;
    private int totalScore = 0;

    public int GetScore()
    {
        return totalScore;
    }

    void OnEnable()
    {
        RespawnManager.OnPlayerRespawned += ApplyRespawnPenalty;
    }

    void OnDisable()
    {
        RespawnManager.OnPlayerRespawned -= ApplyRespawnPenalty;
    }

    void Start()
    {
        if (compassArrowPrefab != null)
        {
            GameObject compassObj = Instantiate(compassArrowPrefab);
            compassArrow = compassObj.GetComponent<PlayerCompassArrow>();
        }

        if (worldArrowPrefab != null)
        {
            GameObject worldObj = Instantiate(worldArrowPrefab);
            worldArrow = worldObj.GetComponent<FloatingArrow>();
        }

        UpdateScoreUI();
    }

// PUBLIC SCORE ADDER (for special events)
public void AddScore(int amount)
{
    totalScore += amount;
    totalScore = Mathf.Clamp(totalScore, 0, maxScore);

    UpdateScoreUI();
    CheckScoreMilestones();
}

    void Update()
    {
        UpdatePlayerReference();

        if (objectiveOverrideActive)
        {
            UpdateOverrideUI();
            return;
        }

        HandleQuestState();
        UpdateArrowTargets();
    }

    // =====================================================
    // PUBLIC OVERRIDE METHODS
    // =====================================================

    // Full override (can be called via code)
    public void OverrideObjective(string newObjective, Transform newTarget)
    {
        objectiveOverrideActive = true;
        overrideObjectiveText = newObjective;
        overrideTarget = newTarget;

        UpdateOverrideUI();

        Debug.Log("Objective overridden: " + newObjective);
    }

    // Inspector-friendly version (for UnityEvents)
    public void TriggerManualOverride()
    {
        OverrideObjective(manualOverrideText, manualOverrideTarget);
    }

    public void ClearObjectiveOverride()
    {
        objectiveOverrideActive = false;
        overrideObjectiveText = "";
        overrideTarget = null;

        Debug.Log("Objective override cleared.");
    }

    void UpdateOverrideUI()
    {
        if (!objectiveOverrideActive)
            return;

        // Auto-clear if target destroyed
        if (overrideTarget == null)
        {
            ClearObjectiveOverride();
            return;
        }

        if (questText != null)
            questText.text = overrideObjectiveText;

        compassArrow?.SetTarget(overrideTarget);
        worldArrow?.SetTarget(overrideTarget);
    }

    // =====================================================
    // RESPAWN PENALTY
    // =====================================================

    void ApplyRespawnPenalty()
    {
        totalScore -= respawnScorePenalty;
        totalScore = Mathf.Max(0, totalScore);

        UpdateScoreUI();

        foreach (var milestone in scoreMilestones)
        {
            if (totalScore < milestone.requiredScore)
                milestone.triggered = false;
        }

        Debug.Log("Respawn penalty applied.");
    }

    // =====================================================
    // PLAYER DETECTION
    // =====================================================

    void UpdatePlayerReference()
    {
        if (RespawnManager.Instance == null)
            return;

        Transform activePlayer = RespawnManager.Instance.GetActivePlayerTransform();

        if (activePlayer == currentPlayerTransform)
            return;

        currentPlayerTransform = activePlayer;

        if (currentPlayerTransform == null)
        {
            inventory = null;
            compassArrow?.gameObject.SetActive(false);
            worldArrow?.gameObject.SetActive(false);
            return;
        }

        inventory = currentPlayerTransform.GetComponentInChildren<TruckInventory>();

        if (inventory == null)
        {
            Debug.LogWarning("QuestManager: No TruckInventory found on player.");
            return;
        }

        if (compassArrow != null)
        {
            compassArrow.player = currentPlayerTransform;
            compassArrow.gameObject.SetActive(true);
        }

        if (worldArrow != null)
            worldArrow.gameObject.SetActive(true);
    }

    // =====================================================
    // QUEST STATE
    // =====================================================

    void HandleQuestState()
    {
        if (inventory == null) return;

        int packageCount = inventory.GetPackageCount();

        if (packageCount == 0)
        {
            activeDeliveries.Clear();
            currentTargetPackage = FindNearestPackage();

            if (questText != null)
                questText.text = "LOCATE APME CARGO";

            return;
        }

        if (activeDeliveries.Count < packageCount)
        {
            int missing = packageCount - activeDeliveries.Count;
            AssignDeliveries(missing);
        }

        UpdateDeliveryQuestText();
    }

    GameObject FindNearestPackage()
    {
        if (inventory == null) return null;

        Collider[] hits = Physics.OverlapSphere(
            inventory.transform.position,
            searchRadius,
            LayerMask.GetMask("Package")
        );

        float closestDistance = Mathf.Infinity;
        GameObject closestPackage = null;

        foreach (Collider hit in hits)
        {
            float distance = Vector3.Distance(
                inventory.transform.position,
                hit.transform.position
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPackage = hit.gameObject;
            }
        }

        return closestPackage;
    }

    void AssignDeliveries(int amountToAssign)
    {
        List<DeliveryPoint> availablePoints = new List<DeliveryPoint>();

        foreach (DeliveryPoint dp in allDeliveryPoints)
        {
            if (!dp.IsActive())
                continue;

            if (!activeDeliveries.Contains(dp) && dp != lastDeliveryPoint)
                availablePoints.Add(dp);
        }

        if (availablePoints.Count == 0) return;

        int assignCount = Mathf.Min(amountToAssign, availablePoints.Count);

        for (int i = 0; i < assignCount; i++)
        {
            int randomIndex = Random.Range(0, availablePoints.Count);
            DeliveryPoint selected = availablePoints[randomIndex];

            activeDeliveries.Add(selected);
            selected.SetQuestManager(this);

            availablePoints.RemoveAt(randomIndex);
        }
    }

    public void CompleteDelivery(DeliveryPoint deliveryPoint)
    {
        if (!activeDeliveries.Contains(deliveryPoint))
            return;

        totalScore += deliveryPoint.rewardPoints;
        lastDeliveryPoint = deliveryPoint;

        activeDeliveries.Remove(deliveryPoint);

        UpdateScoreUI();
        CheckScoreMilestones();
        HandleQuestState();
    }

    void CheckScoreMilestones()
    {
        foreach (var milestone in scoreMilestones)
        {
            if (!milestone.triggered && totalScore >= milestone.requiredScore)
            {
                milestone.triggered = true;
                milestone.onReached?.Invoke();
                Debug.Log("Milestone reached: " + milestone.name);
            }
        }
    }

    void UpdateDeliveryQuestText()
    {
        if (questText == null) return;

        if (activeDeliveries.Count == 1)
            questText.text = "DROP OFF:\n" + activeDeliveries[0].name;
        else
        {
            string list = "DROP OFFS:\n";
            foreach (DeliveryPoint dp in activeDeliveries)
                list += dp.name + "\n";

            questText.text = list;
        }
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = "Score " + totalScore + " / " + maxScore;
    }

    void UpdateArrowTargets()
    {
        if (inventory == null) return;

        int packageCount = inventory.GetPackageCount();

        if (packageCount == 0)
        {
            if (currentTargetPackage != null)
            {
                compassArrow?.SetTarget(currentTargetPackage.transform);
                worldArrow?.SetTarget(currentTargetPackage.transform);
            }
            return;
        }

        DeliveryPoint closest = GetClosestDelivery();

        if (closest != null)
        {
            compassArrow?.SetTarget(closest.transform);
            worldArrow?.SetTarget(closest.transform);
        }
    }

    DeliveryPoint GetClosestDelivery()
    {
        if (inventory == null) return null;

        float closestDistance = Mathf.Infinity;
        DeliveryPoint closest = null;

        foreach (DeliveryPoint dp in activeDeliveries)
        {
            float distance = Vector3.Distance(
                inventory.transform.position,
                dp.transform.position
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = dp;
            }
        }

        return closest;
    }
}