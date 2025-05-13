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
    [SerializeField] private LocalizedString[] presentationDialogueLines;
    [SerializeField] private LocalizedString[] dialogueLines;
    [SerializeField] private LocalizedString[] successDialogueLines;
    [SerializeField] public LocalizedString[] successFirstTimeLines;
    [SerializeField] public LocalizedString[] failureFirstTimeLines;
    [SerializeField] public LocalizedString[] hintLines;

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
        // Obtener el �ndice de la escena actual
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

        // Inicia el di�logo solo si no estamos en la escena 2
        if (currentSceneIndex != 2)
        {
            StartDialogue(dialogueLines); // Iniciar el di�logo en cualquier escena excepto la escena 2
        }

        // Llamar al di�logo de presentaci�n solo en la escena 2
        if (currentSceneIndex == 2)
        {
            StartPresentationDialogue(); // Solo en la escena 2
        }

        // Suscribirse al evento de cambio de idioma
        LanguageManager.OnLanguageChanged += UpdateDialogue;
    }

    private void Update()
    {
        if (!isDialogueActive)
            return;

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
        dialogueLines = dialogueToDisplay;

        if (dialogueLines.Length == 0 || dialogueLines[0] == null)
        {
            Debug.LogWarning("La primera l�nea del di�logo es nula.");
            return;
        }

        dialoguePanel.SetActive(true);
        dialogueText.gameObject.SetActive(true);
        isDialogueActive = true;

        StartTyping(dialogueLines[currentLineIndex]);
    }
    public void StartPresentationDialogue()
    {
        int puzzleID = 1;  // Aqu� asignas el valor de puzzleID que necesites, por ejemplo, 0 o uno espec�fico

        // Verifica si ya se mostr� el di�logo de presentaci�n
        if (DialogueFlags.Instance.HasShownFirstPresentationDialogue(puzzleID))
        {
            dialoguePanel.SetActive(false);
            dialogueText.gameObject.SetActive(false);
            isDialogueActive = false;

            // Si ya se ha mostrado, no lo mostramos de nuevo
            return;
        }

        isLastDialogue = false; // Se puede cambiar seg�n el tipo de di�logo
        StartDialogue(presentationDialogueLines);  // Usa las l�neas de presentaci�n definidas en el Inspector

        // Marca que el di�logo de presentaci�n ya ha sido mostrado
        DialogueFlags.Instance.SetFirstPresentationDialogueShown(puzzleID);  // Usa el m�todo para marcarlo
    }
    public void StartTemporaryDialogue(LocalizedString[] lines)
    {

        if (lines == null || lines.Length == 0)
        {
            Debug.LogWarning("No hay l�neas de di�logo disponibles.");
            return;
        }

        // Obtener el n�mero de la escena actual
        int sceneIndex = SceneManager.GetActiveScene().buildIndex;

        // Pausar el temporizador seg�n la escena
        if (sceneIndex == 8)
        {
            // Si estamos en la escena 8, usamos ChestManager
            if (ChestManager.Instance != null)
            {
                ChestManager.Instance.StopTimer();
            }
            else
            {
                Debug.LogError("ChestManager no est� disponible en la escena 8.");
            }
        }
        else if (sceneIndex == 10)
        {
            // Si estamos en la escena 10, usamos SlidingPuzzleManager
            SlidingPuzzleManager slidingPuzzleManager = FindObjectOfType<SlidingPuzzleManager>();
            if (slidingPuzzleManager != null)
            {
                SlidingPuzzleManager.Instance.StopTimer();
            }
            else
            {
                Debug.LogError("SlidingPuzzleManager no est� disponible en la escena 10.");
            }
        }
        else
        {
            Debug.LogWarning("No se maneja esta escena para pausas de temporizador.");
        }


        PuzzleTimer puzzleTimer = FindObjectOfType<PuzzleTimer>();
        if (puzzleTimer != null)
        {
            puzzleTimer.PauseTimer();
        }

        // Inicia el di�logo temporal
        StartDialogue(lines);
    }
    public void EndTemporaryDialogue()
    {
        // Llama a EndSuccessDialogue para reactivar el temporizador
        EndSuccessDialogue();
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

        string fullLine = dialogueLines[currentLineIndex].GetLocalizedString();

        if (dialogueText.text == fullLine)
        {
            // Forzamos avanzar aunque sea la �ltima l�nea
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
        else
        {
            dialogueText.text = fullLine;
            isTyping = false;
        }
    }

    private void EndDialogue()
    {
        dialoguePanel.SetActive(false);
        dialogueText.gameObject.SetActive(false);
        isDialogueActive = false;

        // Reiniciar el �ndice de di�logo despu�s de terminar
        currentLineIndex = 0;

        // Si es el primer di�logo, ejecutamos la acci�n correspondiente
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

        // Si es el �ltimo di�logo, llamamos a EndSuccessDialogue() y a EndTemporaryDialogue
        if (isLastDialogue)
        {
            SceneTransition.Instance.LoadLevelGame();  // Cambiamos de escena

            // Obtener el n�mero de la escena actual
            int sceneIndex = SceneManager.GetActiveScene().buildIndex;

            // Pausar el temporizador seg�n la escena
            if (sceneIndex == 8)
            {
                // Si estamos en la escena 8, usamos ChestManager
                if (ChestManager.Instance != null)
                {
                    ChestManager.Instance.StopTimer();
                }
                else
                {
                    Debug.LogError("ChestManager no est� disponible en la escena 8.");
                }
            }
            else if (sceneIndex == 10)
            {
                // Si estamos en la escena 10, usamos SlidingPuzzleManager
                SlidingPuzzleManager slidingPuzzleManager = FindObjectOfType<SlidingPuzzleManager>();
                if (slidingPuzzleManager != null)
                {
                    SlidingPuzzleManager.Instance.StopTimer();
                }
                else
                {
                    Debug.LogError("SlidingPuzzleManager no est� disponible en la escena 10.");
                }
            }
            else
            {
                Debug.LogWarning("No se maneja esta escena para pausas de temporizador.");
            }

            PuzzleTimer puzzleTimer = FindObjectOfType<PuzzleTimer>();
            if (puzzleTimer != null)
            {
                puzzleTimer.PauseTimer();
            }
        }

        // Si es un di�logo temporal, se llama a EndTemporaryDialogue
        if (dialogueLines == successDialogueLines || dialogueLines == failureFirstTimeLines || dialogueLines == successFirstTimeLines || dialogueLines == hintLines)
        {
            EndTemporaryDialogue();  // Llamamos a EndTemporaryDialogue para reactivar el temporizador
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
        int sceneIndex = SceneManager.GetActiveScene().buildIndex;

        // Pausar ambos relojes (ChestManager o SlidingPuzzleManager dependiendo de la escena)
        if (sceneIndex == 8)
        {
            // Usar ChestManager en la escena 8
            ChestManager.Instance.StopTimer();
        }
        else if (sceneIndex == 10)
        {
            // Usar SlidingPuzzleManager en la escena 10
            SlidingPuzzleManager.Instance.StopTimer();
        }

        // Pausar el PuzzleTimer
        PuzzleTimer puzzleTimer = FindObjectOfType<PuzzleTimer>();
        if (puzzleTimer != null)
        {
            puzzleTimer.PauseTimer();
        }

        isLastDialogue = true;
        StartDialogue(successDialogueLines);  // Usa las l�neas de �xito definidas en el Inspector
    }
    public void EndSuccessDialogue()
    {
        int sceneIndex = SceneManager.GetActiveScene().buildIndex;

        // Reanudar el temporizador correspondiente dependiendo de la escena
        if (sceneIndex == 8)
        {
            // Usar ChestManager en la escena 8
            ChestManager.Instance.StartTimerAfterDialogue();
        }
        else if (sceneIndex == 10)
        {
            // Usar SlidingPuzzleManager en la escena 10
            SlidingPuzzleManager.Instance.StartTimerAfterDialogue();
        }

        // Reanudar el PuzzleTimer
        PuzzleTimer puzzleTimer = FindObjectOfType<PuzzleTimer>();
        if (puzzleTimer != null)
        {
            puzzleTimer.StartTimer();
        }
    }
    private void OnDestroy()
    {
        // Desuscribirse del evento cuando se destruya el objeto
        LanguageManager.OnLanguageChanged -= UpdateDialogue;
    }
}
