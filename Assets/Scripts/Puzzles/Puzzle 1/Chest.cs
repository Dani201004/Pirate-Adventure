using UnityEngine;
using UnityEngine.Localization;

public class Chest : MonoBehaviour
{
    public string Color;
    [SerializeField] private Animator chestAnimator;
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip rejectSound;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private ParticleSystem particles;
    [SerializeField] private ParticleSystem lockParticles;

    private ChestShake shakeScript;
    private Quaternion originalRotation;

    public bool isOpened = false;

    [SerializeField] private GameObject gemPrefab;
    [SerializeField] private Transform gemSpawnPoint; // Lugar exacto donde aparecerá

    [SerializeField] private DialogueSystem dialogueSystem;  // Instancia de DialogueSystem

    private int puzzleID = 1;  // PuzzleID para identificar el cofre

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        shakeScript = GetComponent<ChestShake>();
        originalRotation = transform.localRotation;
    }
    void Update()
    {
        // Si NO está abierto y NO se está sacudiendo, aplica balanceo suave
        if (!isOpened && (shakeScript == null || !shakeScript.IsShaking()))
        {
            float angle = Mathf.Sin(Time.time * 2f) * 5f; // Ajusta velocidad y amplitud
            transform.localRotation = originalRotation * Quaternion.Euler(0f, 0f, angle);
        }
        else
        {
            // Mantiene la rotación fija (evita que siga balanceando)
            transform.localRotation = originalRotation;
        }
    }

    public void Open()
    {
        if (isOpened) return;
        isOpened = true;

        chestAnimator.SetTrigger("Open");

        if (lockParticles != null && lockParticles.isPlaying)
            lockParticles.Stop();

        Instantiate(gemPrefab, gemSpawnPoint.position, gemSpawnPoint.rotation);

        PlaySound(openSound);
        TriggerParticles();

        // Verifica si es la primera vez que se abre un cofre con éxito
        if (!DialogueFlags.Instance.HasShownFirstSuccessDialogue(puzzleID))
        {
            DialogueFlags.Instance.SetFirstSuccessDialogueShown(puzzleID);
            // Llama al diálogo de éxito
            dialogueSystem?.StartTemporaryDialogue(dialogueSystem.successFirstTimeLines); // Usamos la instancia de DialogueSystem
        }

        ChestManager.Instance.OnChestOpened();
    }
    public void RejectKey()
    {
        if (isOpened) return;

        StartShake();
        Debug.Log("Llave incorrecta usada en cofre de color: " + Color);
        PlaySound(rejectSound);

        // Verifica si es la primera vez que fallas con la llave
        if (!DialogueFlags.Instance.HasShownFirstFailureDialogue(puzzleID))
        {
            DialogueFlags.Instance.SetFirstFailureDialogueShown(puzzleID);
            // Llama al diálogo de fracaso
            dialogueSystem?.StartTemporaryDialogue(dialogueSystem.failureFirstTimeLines); // Usamos la instancia de DialogueSystem
        }
    }
    public void StartShake()
    {
        if (shakeScript != null)
        {
            shakeScript.StartShake();
        }
        else
        {
            Debug.LogWarning("El script ChestShake no está asignado.");
        }
    }
    private void PlaySound(AudioClip sound)
    {
        audioSource.PlayOneShot(sound); // Reproducir el sonido pasado como parámetro
    }

    private void TriggerParticles()
    {
        particles.Play(); // Reproducir partículas
    }

    public void SyncOpenEffects()
    {
        Open(); // Reutilizamos Open(), que ya evita duplicados
    }
}