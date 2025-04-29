using PlayFab;
using UnityEngine;

public class DisplayPuzzleProgress : MonoBehaviour
{

    [Header("Referencia al administrador de gameobjects del apartado control parental")]
    [SerializeField] private ParentalControlUIManager parentalControlUIManager;

    void Start()
    {
        // Asegurarse de que los datos se cargan cuando comienza el juego
        PlayFabProgressManager.Instance.LoadGameData(PlayFabProgressManager.Instance.lastGamePlayed);

        // Actualizar la UI con la información cargada
        UpdateUI();
    }

    void UpdateUI()
    {
        // Verificar que la referencia al `PlayFabProgressManager` esté asignada
        if (PlayFabProgressManager.Instance != null)
        {
            // Verificar si puzzleProgress tiene un valor válido
            int puzzlesCompletados = PlayFabProgressManager.Instance.puzzleProgress;

            // Si no hay puzzles completados, mostrar 0
            if (puzzlesCompletados <= 0)
            {
                parentalControlUIManager.CompletedPuzzlesText.text = "0";
            }
            else
            {
                parentalControlUIManager.CompletedPuzzlesText.text = puzzlesCompletados.ToString();
            }
        }
    }
}
