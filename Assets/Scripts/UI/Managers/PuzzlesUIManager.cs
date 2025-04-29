using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PuzzlesUIManager : MonoBehaviour
{
    [Header("Texto para mostrar el tiempo restante para resolver el puzzle")]
    [SerializeField] private TMP_Text timerText;

    [Header("Texto para mostrar que se ha acabado el tiempo del puzzle")]
    [SerializeField] private TMP_Text timeOutText;

    [Header("Boton de ayuda")]
    [SerializeField] private Button hintButton;

    [Header("Panel de pausa")]
    [SerializeField] private GameObject pauseMenu;

    [Header("Boton de pausa")]
    [SerializeField] private Button pauseButton;

    [Header("Boton para crear lobby")]
    [SerializeField] private Button onlineButton;

    [Header("Simbolo que muestra que la partida no está online")]
    [SerializeField] private Image notOnlineImage;

    [Header("Simbolo que muestra que la partida ya está online")]
    [SerializeField] private Image onlineImage;

    [Header("Panel de reintentar")]
    [SerializeField] private GameObject retryPanel;

    [Header("Boton para votar reintentar")]
    [SerializeField] private Button retryButton;

    [Header("Texto para mostrar el recuento de votos de reintentar")]
    [SerializeField] private TMP_Text retryVoteText;


    // Propiedades públicas de solo lectura
    public TMP_Text TimerText => timerText;
    public TMP_Text TimeOutText => timeOutText;

    public Button HintButton => hintButton;

    public GameObject PauseMenu => pauseMenu;
    public Button PauseButton => pauseButton;

    public Button OnlineButton => onlineButton;

    public Image NotOnlineImage => notOnlineImage;
    public Image OnlineImage => onlineImage;

    public GameObject RetryPanel => retryPanel;
    public Button RetryButton => retryButton;
    public TMP_Text RetryVoteText => retryVoteText;

    private void Start()
    {
        OnlineImage.gameObject.SetActive(false);
    }


}
