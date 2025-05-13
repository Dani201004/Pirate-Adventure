using UnityEngine;

public class DialogueFlags : MonoBehaviour
{
    public static DialogueFlags Instance { get; private set; }

    private const string newGameFlagKey = "isNewGame";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // No destruir el objeto al cargar nuevas escenas
        }
        else
        {
            Destroy(gameObject); // Eliminar si ya existe una instancia
        }

        // Verifica si es un nuevo juego
        bool isNewGame = PlayerPrefs.GetInt(newGameFlagKey, 1) == 1;
        if (isNewGame)
        {
            ResetFlags(); // Restablecer las banderas al iniciar un nuevo juego
            PlayerPrefs.SetInt(newGameFlagKey, 0); // Marca que ya no es un nuevo juego
            PlayerPrefs.Save();
        }
    }

    // Verifica si es la primera vez que se muestra el di�logo de �xito para un puzzle espec�fico
    public bool HasShownFirstSuccessDialogue(int puzzleID)
    {
        return PlayerPrefs.GetInt($"puzzle_{puzzleID}_successDialogue", 0) == 1;
    }

    // Marca que se ha mostrado el di�logo de �xito para un puzzle espec�fico
    public void SetFirstSuccessDialogueShown(int puzzleID)
    {
        PlayerPrefs.SetInt($"puzzle_{puzzleID}_successDialogue", 1);
        PlayerPrefs.Save();
    }

    // Verifica si es la primera vez que se muestra el di�logo de fracaso para un puzzle espec�fico
    public bool HasShownFirstFailureDialogue(int puzzleID)
    {
        return PlayerPrefs.GetInt($"puzzle_{puzzleID}_failureDialogue", 0) == 1;
    }

    // Marca que se ha mostrado el di�logo de fracaso para un puzzle espec�fico
    public void SetFirstFailureDialogueShown(int puzzleID)
    {
        PlayerPrefs.SetInt($"puzzle_{puzzleID}_failureDialogue", 1);
        PlayerPrefs.Save();
    }

    // Verifica si es la primera vez que se muestra el di�logo de presentaci�n para un puzzle espec�fico
    public bool HasShownFirstPresentationDialogue(int puzzleID)
    {
        return PlayerPrefs.GetInt($"puzzle_{puzzleID}_presentationDialogue", 0) == 1;
    }

    // Marca que se ha mostrado el di�logo de presentaci�n para un puzzle espec�fico
    public void SetFirstPresentationDialogueShown(int puzzleID)
    {
        PlayerPrefs.SetInt($"puzzle_{puzzleID}_presentationDialogue", 1);
        PlayerPrefs.Save();
    }

    // Restablecer las banderas para todos los puzzles
    private void ResetFlags()
    {
        // Este m�todo ahora no reinicia las banderas, ya que las banderas est�n guardadas individualmente por puzzle.
        PlayerPrefs.Save();
    }
}