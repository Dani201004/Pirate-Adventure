using System;
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

    [Header("Dialogue System")]
    [SerializeField] private DialogueSystem dialogueSystem;

    private int totalChests;     // Total de cofres
    private int chestsOpened;    // Cofres abiertos
    private bool puzzleCompleted = false; // Estado para saber si el puzzle ha sido completado

    private int totalGemsCollected = 0;

    private int totalMagicGemsCollected = 0;

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

        // Limpiar el valor guardado de tiempo al finalizar el puzzle
        PlayerPrefs.DeleteKey("TimeRemaining");

        InitializeChestCounters();

        // Llamar al método repetidamente para comprobar si la partida es online
        InvokeRepeating("CheckMultiplayerMatchStatus", 0f, 1f); // Cada 1 segundo
    }
    private void CheckMultiplayerMatchStatus()
    {
        // Solo ejecutar los métodos si la partida es online
        if (PlayFabController.Instance.IsMultiplayerMatch)
        {
            DifficultyLevel savedDifficulty = GetSavedDifficulty();
            SetDifficultyForAllPlayers(savedDifficulty);

            GetDifficultyFromPlayFab();

            // Detener la comprobación después de ejecutar los métodos
            CancelInvoke("CheckMultiplayerMatchStatus");
        }
    }
    private void OnDestroy()
    {
        // Detener cualquier invocación cuando el objeto sea destruido
        CancelInvoke("CheckMultiplayerMatchStatus");
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
        totalGemsCollected++;

        // Aumentar el número de gemas normales en PlayFabProgressManager
        PlayFabProgressManager.Instance.Gems++;

        Debug.Log("Gema normal recolectada. Total actuales: " + PlayFabProgressManager.Instance.Gems);

        chestsOpened++;

        // Verificar si chestsOpened es igual a totalChests
        Debug.Log("Cofres abiertos: " + chestsOpened + " de " + totalChests);

        // Comprobar si el número de gemas instanciadas es igual al número de cofres abiertos
        GameObject[] gemasInstanciadas = GameObject.FindGameObjectsWithTag("Gem");

        // Verificar si el número de gemas instanciadas es igual al número de cofres abiertos
        if (totalGemsCollected == totalChests)
        {

            Debug.Log("Número de gemas instanciadas es igual al número de cofres abiertos.");

            // Verificar si la última gema ha sido destruida
            bool ultimaGemaDestruida = gemasInstanciadas.Length == 0;

            if (ultimaGemaDestruida && !puzzleCompleted)
            {
                totalMagicGemsCollected++;

                // Aumentar el número de gemas normales en PlayFabProgressManager
                PlayFabProgressManager.Instance.magicGems++;

                puzzleCompleted = true;
                EndPuzzle(); // Terminar el puzzle
            }
        }
        else
        {
            Debug.Log("El número de gemas instanciadas no coincide con los cofres abiertos.");
        }
    }
    public void EndPuzzle()
    {

        // Detener el temporizador de ChestManager
        StopTimer();

        // Detener el temporizador de PuzzleTimer (si está presente)
        PuzzleTimer puzzleTimer = FindObjectOfType<PuzzleTimer>();
        if (puzzleTimer != null)
        {
            puzzleTimer.StopTimer();
        }

        // Limpiar el valor guardado de tiempo al finalizar el puzzle
        PlayerPrefs.DeleteKey("TimeRemaining");

        // Aquí se inicia el diálogo de éxito cuando se complete el puzzle
        Debug.Log("¡Puzzle completado!");

        // Obtener el valor de currentPuzzle desde PlayFabProgressManager
        int currentPuzzleID = PlayFabProgressManager.Instance.currentPuzzle;

        // Verificar si este puzzle ya ha sido completado
        if (!PlayFabProgressManager.Instance.completedPuzzles.Contains(currentPuzzleID))
        {
            // Aumentar el progreso del puzzle solo si es la primera vez que se pasa
            PlayFabProgressManager.Instance.puzzleProgress++;

            // Registrar este puzzle como completado
            PlayFabProgressManager.Instance.completedPuzzles.Add(currentPuzzleID);
        }

        // Obtener el componente DialogueSystem
        DialogueSystem dialogueSystem = FindObjectOfType<DialogueSystem>();

        // Llamar al método que inicia el diálogo de éxito
        dialogueSystem.StartSuccessDialogue();

        // Obtener el nombre del juego actual desde PlayFabProgressManager
        string currentGameName = PlayFabProgressManager.Instance.lastGamePlayed;

        // Si no se encuentra el nombre del juego, usar un valor predeterminado
        if (string.IsNullOrEmpty(currentGameName))
        {
            currentGameName = "DefaultGame"; // Nombre por defecto si no hay ninguno disponible
        }

        // Guardar el progreso actualizado
        PlayFabProgressManager.Instance.SaveGameData(currentGameName);
    }
    public void StartTimerAfterDialogue()
    {
        ApplyDifficultySettings();

        // Comprobar si hay un tiempo guardado y usarlo
        if (PlayerPrefs.HasKey("TimeRemaining"))
        {
            timeRemaining = PlayerPrefs.GetFloat("TimeRemaining");
        }
        else
        {
            timeRemaining = totalTime;  // Si no hay un tiempo guardado, usar el tiempo inicial
        }

        isTimerRunning = true;

        // Asignar este manager como proveedor de tiempo al PuzzleTimer
        PuzzleTimer puzzleTimer = FindObjectOfType<PuzzleTimer>();
        if (puzzleTimer != null)
        {
            puzzleTimer.SetTimeProvider(this);
        }
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
    public void SetDifficultyForAllPlayers(DifficultyLevel difficulty)
    {
        // Guardar la dificultad en Shared Group Data para que sea accesible para todos los jugadores
        PlayFabController.Instance.SetSharedGroupData("difficulty", difficulty.ToString(), // Usar "difficulty" aquí
            () =>
            {
                // Callback de éxito (sin parámetros)
                //Debug.Log("Dificultad guardada con éxito: " + difficulty);
            });
    }
    private void GetDifficultyFromPlayFab()
    {
        PlayFabController.Instance.GetSharedGroupData(sharedData =>
        {
            if (sharedData.ContainsKey("difficulty"))
            {
                string difficultyString = sharedData["difficulty"];
                DifficultyLevel difficulty = (DifficultyLevel)Enum.Parse(typeof(DifficultyLevel), difficultyString);

                // Establecer la dificultad en el juego
                DifficultyManager.Instance.SetDifficulty(difficulty);

                // Ajustar cofres y llaves según la dificultad
                AdjustChestAndKeyVisibility(difficulty);
                PlayDifficultyParticles(difficulty);
            }
        });
    }

    // Método para detener el temporizador
    public void StopTimer()
    {
        // Guardar el tiempo restante antes de detener el temporizador
        PlayerPrefs.SetFloat("TimeRemaining", timeRemaining);
        isTimerRunning = false;  // Detener el temporizador
    }
}
