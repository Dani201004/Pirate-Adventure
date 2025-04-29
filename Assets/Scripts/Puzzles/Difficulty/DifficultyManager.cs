using UnityEngine;

public enum DifficultyLevel
{
    Easy,
    Medium,
    Hard
}

public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance { get; private set; }

    public DifficultyLevel CurrentDifficulty { get; private set; } = DifficultyLevel.Medium;

    private const string DifficultyKey = "SelectedDifficulty";

    public event System.Action<DifficultyLevel> OnDifficultyChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadDifficulty();
    }

    public void SetDifficulty(DifficultyLevel newDifficulty)
    {
        CurrentDifficulty = newDifficulty;
        PlayerPrefs.SetInt(DifficultyKey, (int)newDifficulty);
        OnDifficultyChanged?.Invoke(newDifficulty); // Notificar a otros scripts
        Debug.Log("Dificultad establecida a: " + newDifficulty);
    }

    public void LoadDifficulty()
    {
        if (PlayerPrefs.HasKey(DifficultyKey))
        {
            int savedIndex = PlayerPrefs.GetInt(DifficultyKey);
            if (savedIndex >= 0 && savedIndex < System.Enum.GetValues(typeof(DifficultyLevel)).Length)
            {
                CurrentDifficulty = (DifficultyLevel)savedIndex;
                Debug.Log("Dificultad cargada: " + CurrentDifficulty);
            }
        }
    }
}
