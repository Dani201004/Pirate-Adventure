using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ParentalControlUIManager : MonoBehaviour
{
    [Header("Texto de tiempo de juego")]
    [SerializeField] private TMP_Text selectedTimeText;
    [SerializeField] private TMP_Text currentTimeText;

    [Header("Texto de nivel de dificultad")]
    [SerializeField] private TMP_Text difficultyText;

    [Header("Texto de puzzles completados")]
    [SerializeField] private TMP_Text completedPuzzlesText;

    [Header("Referencias de botones de tiempo de juego")]
    [SerializeField] private Button decreaseTimeButton;
    [SerializeField] private Button increaseTimeButton;
    [SerializeField] private Button startTimeButton;
    [SerializeField] private Button resetTimeButton;

    [Header("Referencias de botones de dificultad")]
    [SerializeField] private Button leftDifficultyButton;
    [SerializeField] private Button rightDifficultyButton;

    // Propiedades públicas de solo lectura
    public TMP_Text SelectedTimeText => selectedTimeText;
    public TMP_Text CurrentTimeText => currentTimeText;
    public TMP_Text DifficultyText => difficultyText;
    public TMP_Text CompletedPuzzlesText => completedPuzzlesText;

    public Button DecreaseTimeButton => decreaseTimeButton;
    public Button IncreaseTimeButton => increaseTimeButton;
    public Button StartTimeButton => startTimeButton;
    public Button ResetTimeButton => resetTimeButton;

    public Button LeftDifficultyButton => leftDifficultyButton;
    public Button RightDifficultyButton => rightDifficultyButton;
}
