using TMPro;
using UnityEngine;
using System.Collections;
using UnityEngine.Localization;
using UnityEngine.SceneManagement;

public class DialogueSystem : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;

    [Header("Dialogue")]
    [SerializeField] private LocalizedString[] dialogueLines;
    [SerializeField] private LocalizedString[] successDialogueLines;
    [SerializeField] private float typingSpeed = 0.03f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip typingSound;

    private int currentLineIndex = 0;
    private bool isDialogueActive = false;
    private bool isTyping = false;
    private Coroutine typingCoroutine;

    private bool isFirstDialogue = true; // Controla si es el primer di�logo

    private bool isLastDialogue = false; // Controla si es el �ltimo di�logo
    private void Start()
    {
        // Inicia el di�logo cuando comience el juego
        StartDialogue(dialogueLines);

        // Suscribirse al evento de cambio de idioma
        LanguageManager.OnLanguageChanged += UpdateDialogue;
    }

    private void Update()
    {
        if (!isDialogueActive)
            return;

        // Avanzar si se toca/clica en pantalla
        if (Input.GetMouseButtonDown(0) || Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            if (isTyping)
            {
                CompleteCurrentLine();
            }
            else
            {
                NextLine();
            }
        }
    }

    // M�todo para iniciar el di�logo (puedes pasar diferentes l�neas seg�n el contexto)
    public void StartDialogue(LocalizedString[] dialogueToDisplay)
    {
        currentLineIndex = 0;
        dialogueLines = dialogueToDisplay; // Asigna las l�neas del di�logo
        dialoguePanel.SetActive(true);
        isDialogueActive = true;
        StartTyping(dialogueLines[currentLineIndex]);
    }
    private void NextLine()
    {
        currentLineIndex++;
        if (currentLineIndex < dialogueLines.Length)
        {
            StartTyping(dialogueLines[currentLineIndex]);
        }
        else
        {
            EndDialogue();
        }
    }

    private void StartTyping(LocalizedString line)
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeLine(line));
    }

    private IEnumerator TypeLine(LocalizedString line)
    {
        isTyping = true;
        dialogueText.text = "";

        // Obtener la cadena traducida usando GetLocalizedString()
        string localizedText = line.GetLocalizedString();

        foreach (char letter in localizedText.ToCharArray())
        {
            dialogueText.text += letter;

            if (!char.IsWhiteSpace(letter) && typingSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(typingSound);
            }

            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }

    private void CompleteCurrentLine()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        // Completar la l�nea actual con la traducci�n
        dialogueText.text = dialogueLines[currentLineIndex].GetLocalizedString();
        isTyping = false;
    }

    private void EndDialogue()
    {
        dialoguePanel.SetActive(false);
        isDialogueActive = false;

        if (isFirstDialogue)
        {
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

            switch (currentSceneIndex)
            {
                case 8: // Escena 8 es ChestPuzzle
                    if (ChestManager.Instance != null)
                        ChestManager.Instance.StartTimerAfterDialogue();
                    break;

                case 10: // Escena 10 es SlidingPuzzle
                    SlidingPuzzleManager slidingPuzzleManager = FindObjectOfType<SlidingPuzzleManager>();
                    if (slidingPuzzleManager != null)
                        slidingPuzzleManager.StartTimerAfterDialogue();
                    break;

                
                default:
                    Debug.LogWarning("No se ha definido una acci�n para esta escena.");
                    break;
            }

            isFirstDialogue = false;
        }

        if (isLastDialogue)
        {
            SceneTransition.Instance.LoadLevelGame();
        }
    }
    // Actualizar el di�logo cuando cambie el idioma
    private void UpdateDialogue()
    {
        if (isDialogueActive)
        {
            // No reiniciar el di�logo si ya est� activo
            StopAllCoroutines();
            StartDialogue(dialogueLines);
        }
    }
    // M�todo que puede ser llamado por otros scripts para iniciar el di�logo de �xito
    public void StartSuccessDialogue()
    {
        isLastDialogue = true;
        StartDialogue(successDialogueLines);  // Usa las l�neas de �xito definidas en el Inspector
    }

    private void OnDestroy()
    {
        // Desuscribirse del evento cuando se destruya el objeto
        LanguageManager.OnLanguageChanged -= UpdateDialogue;
    }
}
