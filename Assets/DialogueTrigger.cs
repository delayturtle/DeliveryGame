using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    public enum TriggerType
    {
        ScoreThreshold,
        Collision,
        TriggerVolume,
        Manual
    }

    [Header("Trigger Settings")]
    public TriggerType triggerType;
    public bool triggerOnce = true;

    [Header("Score Trigger Settings")]
    public QuestManager questManager;
    public int requiredScore = 100;

    [Header("Collision / Trigger Settings")]
    public string requiredTag = "Player";

    [Header("Dialogue")]
    public DialogueLine[] dialogueLines;

    private bool hasTriggered = false;

    void Update()
    {
        if (triggerType == TriggerType.ScoreThreshold)
        {
            if (questManager == null)
                return;

            if (!hasTriggered && questManager.GetScore() >= requiredScore)
            {
                ActivateDialogue();
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (triggerType != TriggerType.Collision)
            return;

        if (collision.gameObject.CompareTag(requiredTag))
        {
            ActivateDialogue();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (triggerType != TriggerType.TriggerVolume)
            return;

        if (other.CompareTag(requiredTag))
        {
            ActivateDialogue();
        }
    }

    public void ManualTrigger()
    {
        if (triggerType != TriggerType.Manual)
            return;

        ActivateDialogue();
    }

    void ActivateDialogue()
    {
        if (triggerOnce && hasTriggered)
            return;

        if (dialogueLines == null || dialogueLines.Length == 0)
            return;

        DialogueManager.Instance.StartDialogue(dialogueLines);

        hasTriggered = true;
    }
}