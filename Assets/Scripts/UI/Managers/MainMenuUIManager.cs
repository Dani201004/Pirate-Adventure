using TMPro;
using UnityEngine.UI;
using UnityEngine;

public class MainMenuUIManager : MonoBehaviour
{
    [Header("Referencias de botones principales")]
    [SerializeField] private GameObject principalButtons;
    [SerializeField] private Button socialButton;
    [SerializeField] private GameObject settingsButton;

    [Header("Paneles e Inputs de introducir nombre de usuario")]
    [SerializeField] private GameObject namePanel;
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private Button submitNameButton;

    [Header("Paneles de errores al asignarte un nombre de usuario")]
    [SerializeField] private TMP_Text errorNameText;
    [SerializeField] private TMP_Text errorNameExistText;

    [Header("Paneles e Inputs de crear partida")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private GameObject newGamePanel;             // Panel para crear una nueva partida
    [SerializeField] private TMP_InputField newGameNameInput;       // InputField donde el jugador ingresa el nombre de la partida
    [SerializeField] private Button submitButton;                   // Botón para confirmar la creación de la partida

    [Header("Paneles e Inputs de errores al crear partida")]
    [SerializeField] private GameObject error1Panel;                // Panel para error: nombre vacío
    [SerializeField] private GameObject error2Panel;                // Panel para error: límite de partidas alcanzado
    [SerializeField] private GameObject error3Panel;                // Panel para error: partida con nombre ya existente
    [SerializeField] private Button continueError1Button;
    [SerializeField] private Button continueError2Button;
    [SerializeField] private Button continueError3Button;

    // Propiedades públicas de solo lectura
    // === Botones principales y configuración ===
    public GameObject PrincipalButtons => principalButtons;
    public Button SocialButton => socialButton;
    public GameObject SettingsButton => settingsButton;

    // === Panel de nombre del jugador ===
    public GameObject NamePanel => namePanel;
    public TMP_InputField NameInputField => nameInputField;
    public Button SubmitNameButton => submitNameButton;

    public TMP_Text ErrorNameText => errorNameText;
    public TMP_Text ErrorNameExistText => errorNameExistText;

    // === Paneles e Inputs de nueva partida ===
    public Button NewGameButton => newGameButton;
    public GameObject NewGamePanel => newGamePanel;
    public TMP_InputField NewGameNameInput => newGameNameInput;
    public Button SubmitButton => submitButton;

    public GameObject Error1Panel => error1Panel;
    public GameObject Error2Panel => error2Panel;
    public GameObject Error3Panel => error3Panel;

    public Button ContinueError1Button => continueError1Button;
    public Button ContinueError2Button => continueError2Button;
    public Button ContinueError3Button => continueError3Button;

}
