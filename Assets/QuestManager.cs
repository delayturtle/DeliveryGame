using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class QuestManager : MonoBehaviour
{
    [Header("References")]
    public TruckInventory inventory;
    public TextMeshProUGUI questText;
    public TextMeshProUGUI scoreText;

    [Header("Package Search")]
    public float searchRadius = 50f;

public int GetScore()
{
    return totalScore;
}

    [Header("Delivery Points")]
    public List<DeliveryPoint> allDeliveryPoints = new List<DeliveryPoint>();

    [Header("Arrow Prefabs")]
    public GameObject compassArrowPrefab;
    public GameObject worldArrowPrefab;

    private PlayerCompassArrow compassArrow;
    private FloatingArrow worldArrow;

    private List<DeliveryPoint> activeDeliveries = new List<DeliveryPoint>();
    private DeliveryPoint lastDeliveryPoint;

    private GameObject currentTargetPackage;
    private int totalScore = 0;

    // Dialogue milestone flags
    private bool triggered100 = false;
    private bool triggered300 = false;
    private bool triggered600 = false;

    void Start()
    {
        if (compassArrowPrefab != null && inventory != null)
        {
            GameObject compassObj = Instantiate(compassArrowPrefab);
            compassArrow = compassObj.GetComponent<PlayerCompassArrow>();

            if (compassArrow != null)
                compassArrow.player = inventory.transform;
        }

        if (worldArrowPrefab != null)
        {
            GameObject worldObj = Instantiate(worldArrowPrefab);
            worldArrow = worldObj.GetComponent<FloatingArrow>();
        }

        UpdateScoreUI();
    }

    void Update()
    {
        HandleQuestState();
        UpdateArrowTargets();
    }

    // =====================================================
    // QUEST STATE MANAGEMENT
    // =====================================================

    void HandleQuestState()
    {
        if (inventory == null) return;

        int packageCount = inventory.GetPackageCount();

        // SEARCH MODE
        if (packageCount == 0)
        {
            activeDeliveries.Clear();
            currentTargetPackage = FindNearestPackage();

            if (questText != null)
                questText.text = "Find a package!";

            return;
        }

        // DELIVERY MODE
        if (activeDeliveries.Count < packageCount)
        {
            int missing = packageCount - activeDeliveries.Count;
            AssignDeliveries(missing);
        }

        UpdateDeliveryQuestText();
    }

    // =====================================================
    // PACKAGE SEARCH
    // =====================================================

    GameObject FindNearestPackage()
    {
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

    // =====================================================
    // DELIVERY ASSIGNMENT
    // =====================================================

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

    // =====================================================
    // DIALOGUE MILESTONES (UPDATED)
    // =====================================================

    void CheckScoreMilestones()
    {
        if (totalScore >= 100 && !triggered100)
        {
            triggered100 = true;

            DialogueManager.Instance.StartDialogue(new DialogueLine[]
            {
                new DialogueLine { speakerName = "Dispatcher", sentence = "Nice work!" },
                new DialogueLine { speakerName = "Dispatcher", sentence = "You've completed your first deliveries." },
                new DialogueLine { speakerName = "Dispatcher", sentence = "Keep it up!" }
            });
        }

        if (totalScore >= 300 && !triggered300)
        {
            triggered300 = true;

            DialogueManager.Instance.StartDialogue(new DialogueLine[]
            {
                new DialogueLine { speakerName = "Dispatcher", sentence = "Impressive!" },
                new DialogueLine { speakerName = "Dispatcher", sentence = "You're becoming a reliable courier." }
            });
        }

        if (totalScore >= 600 && !triggered600)
        {
            triggered600 = true;

            DialogueManager.Instance.StartDialogue(new DialogueLine[]
            {
                new DialogueLine { speakerName = "Dispatcher", sentence = "Outstanding performance!" },
                new DialogueLine { speakerName = "Dispatcher", sentence = "You're one of the best couriers on the road!" }
            });
        }
    }

    void UpdateDeliveryQuestText()
    {
        if (questText == null) return;

        if (activeDeliveries.Count == 1)
        {
            questText.text = "Deliver package to:\n" + activeDeliveries[0].name;
        }
        else
        {
            string list = "Deliver packages to:\n";
            foreach (DeliveryPoint dp in activeDeliveries)
                list += dp.name + "\n";

            questText.text = list;
        }
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + totalScore;
    }

    // =====================================================
    // ARROW SYSTEM
    // =====================================================

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

    private void OnDrawGizmosSelected()
    {
        if (inventory == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(inventory.transform.position, searchRadius);
    }
}