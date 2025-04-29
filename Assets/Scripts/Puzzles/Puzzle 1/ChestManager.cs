using UnityEngine;

public class ChestManager : MonoBehaviour, ITimeProvider
{
    public static ChestManager Instance { get; private set; }

    [Header("Chests")]
    public Chest[] easyChests;   // Cofres para dificultad fácil
    public Chest[] mediumChests; // Cofres para dificultad media
    public Chest[] hardChests;   // Cofres para dificultad difícil

    [Header("Keys")]
    public Key[] easyKeys;      // Llaves para dificultad fácil
    public Key[] mediumKeys;    // Llaves para dificultad media
    public Key[] hardKeys;      // Llaves para dificultad difícil

    [Header("Particle Effects")]
    [SerializeField] private ParticleSystem[] easyParticles; // Sistemas de particulas para dificultad fácil
    [SerializeField] private ParticleSystem[] mediumParticles; // Sistemas de particulas para dificultad media
    [SerializeField] private ParticleSystem[] hardParticles; // Sistemas de particulas para dificultad difícil

    private float totalTime; // Tiempo total para completar el nivel
    private float timeRemaining; // Tiempo restante para completar el nivel
    private bool isTimerRunning = false; // Flag para saber si el temporizador debe contar

    public delegate void TimeOutHandler();
    public event TimeOutHandler OnTimeOut; // Evento para cuando se agota el tiempo

    [Header("Dialog System")]
    [SerializeField] private DialogueSystem dialogueSystem;

    private int totalChests;     // Total de cofres
    private int chestsOpened;    // Cofres abiertos
    private bool puzzleCompleted = false; // Estado para saber si el puzzle ha sido completado

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    private void Start()
    {
        DifficultyLevel savedDifficulty = GetSavedDifficulty();
        DifficultyManager.Instance.SetDifficulty(savedDifficulty);

        AdjustChestAndKeyVisibility(savedDifficulty);
        PlayDifficultyParticles(savedDifficulty);

        InitializeChestCounters();
    }
    private void InitializeChestCounters()
    {
        // Obtener la dificultad activa desde DifficultyManager
        DifficultyLevel currentDifficulty = DifficultyManager.Instance.CurrentDifficulty;

        // Contar solo los cofres de la dificultad activa
        switch (currentDifficulty)
        {
            case DifficultyLevel.Easy:
                totalChests = easyChests.Length;
                break;
            case DifficultyLevel.Medium:
                totalChests = mediumChests.Length;
                break;
            case DifficultyLevel.Hard:
                totalChests = hardChests.Length;
                break;
            default:
                totalChests = 0;
                break;
        }

        chestsOpened = 0; // Inicializar el contador de cofres abiertos
        Debug.Log("Total de cofres para la dificultad " + currentDifficulty + ": " + totalChests);
    }
    // Llamar este método cuando un cofre sea abierto
    public void OnChestOpened()
    {
        chestsOpened++;

        // Verificar si chestsOpened es igual a totalChests
        Debug.Log("Cofres abiertos: " + chestsOpened + " de " + totalChests);

        // Comprobar si todos los cofres han sido abiertos
        if (chestsOpened == totalChests && !puzzleCompleted)
        {
            puzzleCompleted = true;
            Debug.Log("Puzzle completado!");
            EndPuzzle(); // Terminar el puzzle
        }
    }
    private void EndPuzzle()
    {
        // Detener el temporizador
        StopTimer();

        // Aquí se inicia el diálogo de éxito cuando se complete el puzzle
        Debug.Log("¡Puzzle completado!");

        // Aumentar el progreso del puzzle
        PlayFabProgressManager.Instance.puzzleProgress++;

        // Obtener el nombre del juego actual
        string currentGameName = PlayerPrefs.GetString("CurrentGameName", "DefaultGame");

        // Llamar al método SaveGameData para guardar el progreso actualizado
        PlayFabProgressManager.Instance.SaveGameData(currentGameName);

        // Obtener el componente DialogueSystem
        DialogueSystem dialogueSystem = FindObjectOfType<DialogueSystem>();

        // Llamar al método que inicia el diálogo de éxito
        dialogueSystem.StartSuccessDialogue();
    }
    private void OnEnable()
    {
        // Suscribir al evento para cuando cambie la dificultad
        DifficultyManager.Instance.OnDifficultyChanged += AdjustChestAndKeyVisibility;
    }

    private void OnDisable()
    {
        // Desuscribir cuando el script se desactive
        DifficultyManager.Instance.OnDifficultyChanged -= AdjustChestAndKeyVisibility;
    }

    private void ApplyDifficultySettings()
    {
        DifficultyLevel currentDifficulty = DifficultyManager.Instance.CurrentDifficulty;

        switch (currentDifficulty)
        {
            case DifficultyLevel.Easy:
                totalTime = 120f;
                break;
            case DifficultyLevel.Medium:
                totalTime = 90f;
                break;
            case DifficultyLevel.Hard:
                totalTime = 60f;
                break;
        }

        if (!string.IsNullOrEmpty(PlayFabController.Instance.sharedGroupId))
        {
            PlayFabController.Instance.GetSharedGroupData(sharedData =>
            {
                if (!sharedData.ContainsKey("StartTime"))
                {
                    // Guardar el tiempo de inicio en segundos desde epoch
                    double startTime = GetUnixTimestamp();
                    PlayFabController.Instance.SetSharedGroupData("StartTime", startTime.ToString(), () => {
                        // Aquí puedes manejar lo que sucede cuando se actualiza el SharedGroupData
                        timeRemaining = totalTime; // Asignar el tiempo restante
                    });
                }
                else
                {
                    // Calcular cuánto tiempo ha pasado desde el inicio
                    double startTime = double.Parse(sharedData["StartTime"]);
                    double timeElapsed = GetUnixTimestamp() - startTime;
                    timeRemaining = Mathf.Max(0f, totalTime - (float)timeElapsed);
                }

                isTimerRunning = true;
            });
        }
        else
        {
            timeRemaining = totalTime;
            isTimerRunning = true;
        }
    }
    private double GetUnixTimestamp()
    {
        return (System.DateTime.UtcNow - new System.DateTime(1970, 1, 1)).TotalSeconds;
    }
    public void StartTimerAfterDialogue()
    {
        ApplyDifficultySettings();
        isTimerRunning = true;

        // Asignar este manager como proveedor de tiempo al PuzzleTimer
        PuzzleTimer puzzleTimer = FindObjectOfType<PuzzleTimer>();
        if (puzzleTimer != null)
        {
            puzzleTimer.SetTimeProvider(this);
        }
    }
    private void Update()
    {
        if (isTimerRunning && timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime; // Reducir el tiempo restante solo si el temporizador está activo
        }
        else if (timeRemaining <= 0)
        {
            // Cuando el tiempo se agota, notificar a los suscriptores (si hay algún evento)
            OnTimeOut?.Invoke();
            isTimerRunning = false; // Detener el temporizador una vez se agote el tiempo
        }
    }

    public float GetTimeRemaining()
    {
        return timeRemaining;
    }
    public bool IsTimeRunning()
    {
        return isTimerRunning;
    }

    public void AdjustChestAndKeyVisibility(DifficultyLevel difficultyLevel)
    {
        // Primero desactivar todos los cofres y llaves
        foreach (var chest in easyChests)
            chest.gameObject.SetActive(false);
        foreach (var chest in mediumChests)
            chest.gameObject.SetActive(false);
        foreach (var chest in hardChests)
            chest.gameObject.SetActive(false);

        foreach (var key in easyKeys)
            key.gameObject.SetActive(false);
        foreach (var key in mediumKeys)
            key.gameObject.SetActive(false);
        foreach (var key in hardKeys)
            key.gameObject.SetActive(false);

        // Activar los cofres y llaves según la dificultad
        switch (difficultyLevel)
        {
            case DifficultyLevel.Easy:
                ActivateChestsAndKeys(easyChests, easyKeys);
                break;
            case DifficultyLevel.Medium:
                ActivateChestsAndKeys(mediumChests, mediumKeys);
                break;
            case DifficultyLevel.Hard:
                ActivateChestsAndKeys(hardChests, hardKeys);
                break;
        }
    }

    private void ActivateChestsAndKeys(Chest[] chests, Key[] keys)
    {
        foreach (var chest in chests)
        {
            chest.gameObject.SetActive(true);
        }

        foreach (var key in keys)
        {
            key.gameObject.SetActive(true);
        }
    }
    private void PlayDifficultyParticles(DifficultyLevel difficulty)
    {
        // Detener todos los sistemas primero
        StopAllParticles(easyParticles);
        StopAllParticles(mediumParticles);
        StopAllParticles(hardParticles);

        // Activar solo los de la dificultad actual
        switch (difficulty)
        {
            case DifficultyLevel.Easy:
                PlayAllParticles(easyParticles);
                break;
            case DifficultyLevel.Medium:
                PlayAllParticles(mediumParticles);
                break;
            case DifficultyLevel.Hard:
                PlayAllParticles(hardParticles);
                break;
        }
    }
    private void StopAllParticles(ParticleSystem[] systems)
    {
        foreach (var ps in systems)
        {
            if (ps != null)
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
    private void PlayAllParticles(ParticleSystem[] systems)
    {
        foreach (var ps in systems)
        {
            if (ps != null)
                ps.Play();
        }
    }
    private DifficultyLevel GetSavedDifficulty()
    {
        int savedValue = PlayerPrefs.GetInt("SelectedDifficulty", (int)DifficultyLevel.Easy);
        return (DifficultyLevel)savedValue;
    }
    // Método para detener el temporizador
    public void StopTimer()
    {
        isTimerRunning = false;  // Detener el temporizador
    }
}
