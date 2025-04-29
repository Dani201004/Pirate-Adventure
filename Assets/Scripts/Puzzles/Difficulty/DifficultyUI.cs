using UnityEngine;
using UnityEngine.SceneManagement;

public class DifficultyUI : MonoBehaviour
{
    [Header("Referencia al administrador de gameobjects del apartado control parental")]
    [SerializeField] private ParentalControlUIManager parentalControlUIManager;

    private DifficultyLevel[] difficulties = (DifficultyLevel[])System.Enum.GetValues(typeof(DifficultyLevel));
    private int currentIndex;
    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex == 9)
        {
            
            FindParentalControlUIManagerAfterDelay();
        }
    }

    private void FindParentalControlUIManagerAfterDelay()
    {

        GameObject parentalControlObject = GameObject.Find("ParentalControlUIManager");


        parentalControlUIManager = parentalControlObject.GetComponent<ParentalControlUIManager>();

        parentalControlUIManager.LeftDifficultyButton.onClick.AddListener(OnLeftArrow);
        parentalControlUIManager.RightDifficultyButton.onClick.AddListener(OnRightArrow);

        currentIndex = (int)DifficultyManager.Instance.CurrentDifficulty;

        UpdateUI();
    }

    private void OnLeftArrow()
    {
        currentIndex = (currentIndex - 1 + difficulties.Length) % difficulties.Length;
        UpdateUI();
    }

    private void OnRightArrow()
    {
        currentIndex = (currentIndex + 1) % difficulties.Length;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (parentalControlUIManager != null && parentalControlUIManager.DifficultyText != null)
        {
            DifficultyLevel selectedDifficulty = difficulties[currentIndex];

            string translated = GetLocalizedDifficultyName(selectedDifficulty, LanguageManager.CurrentLanguage);
            parentalControlUIManager.DifficultyText.text = translated;

            // Asegúrate de que DifficultyManager.Instance no sea nulo
            if (DifficultyManager.Instance != null)
            {
                DifficultyManager.Instance.SetDifficulty(selectedDifficulty);
                Debug.Log("Dificultad actual: " + translated);
            }
            else
            {
                Debug.LogError("DifficultyManager no está instanciado.");
            }
        }
    }

    private string GetLocalizedDifficultyName(DifficultyLevel level, int languageID)
    {
        switch (languageID)
        {
            case 0: // Inglés
                return level.ToString();
            case 1: // Español
                switch (level)
                {
                    case DifficultyLevel.Easy:
                        return "Fácil";
                    case DifficultyLevel.Medium:
                        return "Media";
                    case DifficultyLevel.Hard:
                        return "Difícil";
                }
                break;
        }
        return level.ToString(); // Fallback
    }
}
