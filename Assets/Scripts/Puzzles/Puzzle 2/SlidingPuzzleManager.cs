using UnityEngine;

public class SlidingPuzzleManager : MonoBehaviour, ITimeProvider
{
    public GameObject piecePrefab;
    public GameObject hiddenObject;

    public float easyTimeLimit = 60f;
    public float mediumTimeLimit = 120f;
    public float hardTimeLimit = 180f;

    private float totalTime;
    private float timeRemaining;
    private bool isTimerRunning = false;
    private bool puzzleCompleted = false;

    private int gridSize;
    private SlidingPuzzle slidingPuzzle;

    private void Start()
    {
        DifficultyLevel currentDifficulty = DifficultyManager.Instance.CurrentDifficulty;

        SetGridSize(currentDifficulty);
        slidingPuzzle = GetComponent<SlidingPuzzle>();
        CreatePuzzle();

        InitializePuzzleTimer();
    }

    private void SetGridSize(DifficultyLevel difficulty)
    {
        switch (difficulty)
        {
            case DifficultyLevel.Easy:
                gridSize = 3;
                break;
            case DifficultyLevel.Medium:
                gridSize = 4;
                break;
            case DifficultyLevel.Hard:
                gridSize = 5;
                break;
        }
    }

    private void CreatePuzzle()
    {
        slidingPuzzle.CreatePuzzle(gridSize);
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
                totalTime = easyTimeLimit;
                break;
            case DifficultyLevel.Medium:
                totalTime = mediumTimeLimit;
                break;
            case DifficultyLevel.Hard:
                totalTime = hardTimeLimit;
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
        isTimerRunning = true;

        // Asegurar que este manager esté asignado como proveedor de tiempo
        PuzzleTimer puzzleTimer = FindObjectOfType<PuzzleTimer>();
        if (puzzleTimer != null)
        {
            puzzleTimer.SetTimeProvider(this);
        }
    }

    private void Update()
    {
        if (!isTimerRunning || puzzleCompleted) return;

        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            isTimerRunning = false;
        }
    }

    public void OnPuzzleCompleted()
    {
        if (puzzleCompleted) return;

        puzzleCompleted = true;
        StopTimer();

        Debug.Log("¡Puzzle completado!");

        // Aumentar progreso
        PlayFabProgressManager.Instance.puzzleProgress++;

        // Guardar progreso
        string currentGameName = PlayerPrefs.GetString("CurrentGameName", "DefaultGame");
        PlayFabProgressManager.Instance.SaveGameData(currentGameName);

        // Diálogo de éxito
        DialogueSystem dialogueSystem = FindObjectOfType<DialogueSystem>();
        if (dialogueSystem != null)
        {
            dialogueSystem.StartSuccessDialogue();
        }
    }

    public void StopTimer()
    {
        isTimerRunning = false;
    }

    // ITimeProvider
    public float GetTimeRemaining() => timeRemaining;
    public bool IsTimeRunning() => isTimerRunning;
}

