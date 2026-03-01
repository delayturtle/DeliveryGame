using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using System;

[System.Serializable]
public class DialogueLine
{
    public string speakerName;

    [TextArea(2,5)]
    public string sentence;
}



public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    // ✅ NEW EVENT
    public event Action OnDialogueFinished;

    [Header("UI References")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;

public void ForceEndDialogue()
{
    StopAllCoroutines();
    OnDialogueFinished?.Invoke();
}

    [Header("Typing Settings")]
    [Range(0.001f, 0.1f)]
    public float textSpeed = 0.03f;

    [Header("Auto Advance")]
    public bool autoAdvance = false;
    public float autoAdvanceDelay = 2f;

    [Header("Time Control")]
    public bool freezeTimeDuringDialogue = false;
    [Range(0f,1f)]
    public float frozenTimeScale = 0f;

    [Header("Voice Bleeps (Optional)")]
    public AudioSource audioSource;
    public AudioClip typingSound;
    public float soundFrequency = 0.05f;

    [Header("Input (New Input System)")]
    public PlayerInput playerInput;

    private InputAction nextDialogueAction;

    private float originalTimeScale = 1f;

    private Queue<DialogueLine> lines = new Queue<DialogueLine>();
    private Coroutine typingCoroutine;

    private bool isTyping = false;
    private bool waitingForAutoAdvance = false;
    private string currentLine;

    private float soundTimer;

    // =====================================================
    // INITIALIZATION
    // =====================================================

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        dialoguePanel.SetActive(false);
    }

    void OnEnable()
    {
        if (playerInput != null)
        {
            nextDialogueAction = playerInput.actions["NextDialogue"];

            if (nextDialogueAction != null)
                nextDialogueAction.performed += OnNextDialogue;
        }
    }

    void OnDisable()
    {
        if (nextDialogueAction != null)
            nextDialogueAction.performed -= OnNextDialogue;
    }

    // =====================================================
    // START DIALOGUE
    // =====================================================

    public void StartDialogue(DialogueLine[] dialogueLines)
    {
        if (freezeTimeDuringDialogue)
        {
            originalTimeScale = Time.timeScale;
            Time.timeScale = frozenTimeScale;
        }

        dialoguePanel.SetActive(true);

        lines.Clear();

        foreach (DialogueLine line in dialogueLines)
            lines.Enqueue(line);

        DisplayNextLine();
    }

    // =====================================================
    // INPUT HANDLER
    // =====================================================

    void OnNextDialogue(InputAction.CallbackContext context)
    {
        if (dialoguePanel.activeSelf)
            DisplayNextLine();
    }

    // =====================================================
    // DISPLAY NEXT LINE
    // =====================================================

    public void DisplayNextLine()
    {
        if (isTyping)
        {
            StopCoroutine(typingCoroutine);
            dialogueText.text = currentLine;
            isTyping = false;
            return;
        }

        if (waitingForAutoAdvance)
        {
            waitingForAutoAdvance = false;
            StopAllCoroutines();
            DisplayNextLine();
            return;
        }

        if (lines.Count == 0)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = lines.Dequeue();

        speakerNameText.text = line.speakerName;
        currentLine = line.sentence;

        typingCoroutine = StartCoroutine(TypeLine(currentLine));
    }

    // =====================================================
    // TYPEWRITER EFFECT
    // =====================================================

    IEnumerator TypeLine(string line)
    {
        isTyping = true;
        dialogueText.text = "";
        soundTimer = 0f;

        foreach (char c in line)
        {
            dialogueText.text += c;

            if (typingSound != null && audioSource != null)
            {
                soundTimer += textSpeed;

                if (soundTimer >= soundFrequency)
                {
                    audioSource.PlayOneShot(typingSound);
                    soundTimer = 0f;
                }
            }

            yield return new WaitForSecondsRealtime(textSpeed);
        }

        isTyping = false;

        if (autoAdvance)
            StartCoroutine(AutoAdvance());
    }

    IEnumerator AutoAdvance()
    {
        waitingForAutoAdvance = true;

        yield return new WaitForSecondsRealtime(autoAdvanceDelay);

        waitingForAutoAdvance = false;
        DisplayNextLine();
    }

    // =====================================================
    // END DIALOGUE
    // =====================================================

    void EndDialogue()
    {
        dialoguePanel.SetActive(false);

        StopAllCoroutines();
        isTyping = false;
        waitingForAutoAdvance = false;

        if (freezeTimeDuringDialogue)
            Time.timeScale = originalTimeScale;

        // ✅ Invoke event AFTER everything is cleaned up
        OnDialogueFinished?.Invoke();
    }
}