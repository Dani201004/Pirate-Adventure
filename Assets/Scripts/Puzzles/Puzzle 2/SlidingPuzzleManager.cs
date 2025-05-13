using UnityEngine;
using System.Collections;
using System;

public class SlidingPuzzleManager : MonoBehaviour, ITimeProvider
{
    public static SlidingPuzzleManager Instance { get; private set; }

    public delegate void TimeOutHandler();
    public event TimeOutHandler OnTimeOut; // Evento para cuando se agota el tiempo

    [Header("Dialogue System")]
    [SerializeField] private DialogueSystem dialogueSystem;

    [SerializeField] private SlidingPuzzle slidingPuzzle;

    private float totalTime;
    private float timeRemaining;
    private bool isTimerRunning = false;

    private bool puzzleCompleted = false; // Estado para saber si el puzzle ha sido completado

    private int totalGemsCollected = 0;

    private int totalMagicGemsCollected = 0;

    private bool hasShownHintDialogue = false;

    bool puzzleAlreadyCompleted = false;

    private bool isSyncing = false;

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

        // Obtener la forma personalizada desde SlidingPuzzleManager
        bool[,] puzzleShape = GetPuzzleShape(savedDifficulty);  // Esto ya tiene la forma según la dificultad

        // Buscar el objeto SlidingPuzzle en la escena por nombre
        slidingPuzzle = GameObject.Find("SlidingPuzzle").GetComponent<SlidingPuzzle>();

        if (slidingPuzzle != null)
        {
            slidingPuzzle.SetImageByDifficulty(savedDifficulty);

            CreatePuzzle(puzzleShape);  // Pasar la forma al SlidingPuzzle
        }
        else
        {
            Debug.LogError("No se ha encontrado el objeto SlidingPuzzle en la escena.");
        }

        // Borrar la clave de TimeRemaining antes de iniciar el temporizador
        PlayerPrefs.DeleteKey("TimeRemaining");

        InitializePuzzleTimer();

        StartCoroutine(WaitForMultiplayerMatch());

        hasShownHintDialogue = false;

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
    private DifficultyLevel GetSavedDifficulty()
    {
        int savedValue = PlayerPrefs.GetInt("SelectedDifficulty", (int)DifficultyLevel.Easy);
        return (DifficultyLevel)savedValue;
    }
    // Devuelve una matriz que define la forma del puzzle basada en la dificultad
    public bool[,] GetPuzzleShape(DifficultyLevel difficulty)
    {
        switch ((DifficultyLevel)PlayerPrefs.GetInt("SelectedDifficulty", (int)DifficultyLevel.Easy))
        {
            case DifficultyLevel.Easy:
                return new bool[,]
                {
                { true, true, true },
                { true, true, true },
                { true, true, true }
                };
            case DifficultyLevel.Medium:
                return new bool[,]
                {
                { true, true, true, true },
                { true, true, true, true },
                { true, true, true, true },
                { true, true, true, true }
                };
            case DifficultyLevel.Hard:
                return new bool[,]
                {
                { true, true, true, true, true },
                { true, true, true, true, true },
                { true, true, true, true, true },
                { true, true, true, true, true },
                { true, true, true, true, true }
                };
            default:
                return new bool[,] { { true } };
        }
    }
    private void CreatePuzzle(bool[,] puzzleShape)
    {
        slidingPuzzle.CreatePuzzle(puzzleShape);  // Pasar la forma al SlidingPuzzle
    }

    private void InitializePuzzleTimer()
    {
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
                    double startTime = GetUnixTimestamp();
                    PlayFabController.Instance.SetSharedGroupData("StartTime", startTime.ToString(), () =>
                    {
                        timeRemaining = totalTime;
                        isTimerRunning = true;
                    });
                }
                else
                {
                    double startTime = double.Parse(sharedData["StartTime"]);
                    double timeElapsed = GetUnixTimestamp() - startTime;
                    timeRemaining = Mathf.Max(0f, totalTime - (float)timeElapsed);
                    isTimerRunning = true;
                }
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
    private IEnumerator WaitForMultiplayerMatch()
    {
        // Espera hasta que IsMultiplayerMatch sea verdadero
        while (!PlayFabController.Instance.IsMultiplayerMatch)
        {
            yield return null; // Espera hasta el siguiente frame
        }

        // Cuando sea verdadero, inicia la sincronización
        StartCoroutine(SyncPuzzleLoop());
        isSyncing = true;  // Para asegurarse de que solo se inicie una vez
    }

    private void Update()
    {

        if (isTimerRunning && timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;

            // Solo mostrar pista si totalTime es mayor que 0
            if (!hasShownHintDialogue && totalTime > 0 && timeRemaining <= totalTime / 2)
            {
                dialogueSystem.StartTemporaryDialogue(dialogueSystem.hintLines);
                hasShownHintDialogue = true;
            }
        }
        else if (timeRemaining <= 0 && isTimerRunning)
        {
            Debug.Log("El tiempo ha expirado, ejecutando el evento de timeout.");
            OnTimeOut?.Invoke();
            isTimerRunning = false;
        }
    }

    public void EndPuzzle()
    {
        MarkPuzzleAsCompleted();

        totalGemsCollected++;

        // Aumentar el número de gemas normales en PlayFabProgressManager
        PlayFabProgressManager.Instance.Gems++;

        totalMagicGemsCollected++;

        // Aumentar el número de gemas normales en PlayFabProgressManager
        PlayFabProgressManager.Instance.magicGems++;

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

    // Método para detener el temporizador
    public void StopTimer()
    {
        // Guardar el tiempo restante antes de detener el temporizador
        PlayerPrefs.SetFloat("TimeRemaining", timeRemaining);
        isTimerRunning = false;  // Detener el temporizador
    }

    public float GetTimeRemaining()
    {
        return timeRemaining;
    }
    public bool IsTimeRunning()
    {
        return isTimerRunning;
    }
    void SyncCurrentStateToPlayFab()
    {
        if (string.IsNullOrEmpty(PlayFabController.Instance.sharedGroupId))
        {
            Debug.LogError("El SharedGroupId no está configurado.");
            return;
        }

        if (slidingPuzzle.totalPieceCount == 0)  // Accede a totalPieceCount desde la instancia de slidingPuzzle
        {
            Debug.LogWarning("No se sincroniza porque el puzzle aún no está creado.");
            return;
        }

        string gridData = slidingPuzzle.SerializePuzzleState();
        PlayFabController.Instance.SetSharedGroupData("PuzzleGrid", gridData, () =>
        {
            PlayFabController.Instance.SetSharedGroupData("MoveTimestamp", GetUnixTimestamp().ToString(), null);
        });
    }
    private IEnumerator SyncPuzzleLoop()
    {
        while (true)
        {
            // Verifica si SharedGroupId está disponible antes de sincronizar
            if (string.IsNullOrEmpty(PlayFabController.Instance.sharedGroupId))
            {
                Debug.LogError("El SharedGroupId no está configurado, no se puede sincronizar.");
                yield break; // Terminar la corrutina si el SharedGroupId no está disponible
            }

            // Sincronizar estado local a PlayFab
            SyncCurrentStateToPlayFab();

            // Obtener datos remotos de PlayFab
            PlayFabController.Instance.GetSharedGroupData(data =>
            {
                if (!puzzleAlreadyCompleted && data.TryGetValue("PuzzleCompleted", out string completedStr) && completedStr == "true")
                {
                    puzzleAlreadyCompleted = true;
                    SlidingPuzzleManager.Instance.EndPuzzle(); // Llamar localmente si no lo hemos hecho
                }

                if (data.TryGetValue("PuzzleGrid", out string remoteGrid))
                {
                    // Verificar si el jugador es el host o no, para decidir cuándo cargar el puzzle
                    if (PlayFabController.Instance.IsMultiplayerMatch && !PlayFabController.Instance.IsHost)
                    {
                        slidingPuzzle.LoadPuzzleFromString(remoteGrid); // Solo cargar el puzzle si no es el host
                    }
                }
            });

            yield return new WaitForSeconds(1.5f);
        }
    }
    public void SetDifficultyForAllPlayers(DifficultyLevel difficulty)
    {
        PlayFabController.Instance.SetSharedGroupData("difficulty", difficulty.ToString(), () =>
        {
            Debug.Log("Dificultad guardada con éxito: " + difficulty);
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

                DifficultyManager.Instance.SetDifficulty(difficulty);

                // Actualizar la imagen del puzzle al recibir la dificultad
                slidingPuzzle.SetImageByDifficulty(difficulty);
            }
            else
            {
                Debug.LogWarning("No se encontró una dificultad en SharedGroupData.");
            }
        });
    }
    void MarkPuzzleAsCompleted()
    {
        PlayFabController.Instance.SetSharedGroupData("PuzzleCompleted", "true", null);
    }
}

