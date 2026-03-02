using UnityEngine;

public class MatterBabyCargoTrigger : MonoBehaviour
{
    [Header("References")]
    public QuestManager questManager;

    [Header("Reward Settings")]
    public int scoreReward = 9000;

    [TextArea]
    public string overrideObjectiveText = "DELIVER THE MATTERBABY TO THE FINAL POINT";

    [Header("Target Settings")]
    [Tooltip("Tag used to find the final delivery point")]
    public string finalPointTag = "FINALPOINT";

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered)
            return;

        if (other.CompareTag("MATTERBABY"))
        {
            triggered = true;

            if (questManager == null)
            {
                Debug.LogWarning("MatterBabyCargoTrigger: No QuestManager assigned.");
                return;
            }

            // Give score
            questManager.AddScore(scoreReward);

            // Find final point automatically by tag
            GameObject finalPoint = GameObject.FindGameObjectWithTag(finalPointTag);

            if (finalPoint != null)
            {
                questManager.OverrideObjective(
                    overrideObjectiveText,
                    finalPoint.transform
                );

                Debug.Log("MATTERBABY collected! Objective set to FINALPOINT.");
            }
            else
            {
                Debug.LogWarning("MatterBabyCargoTrigger: No object found with tag " + finalPointTag);
            }
        }
    }
}