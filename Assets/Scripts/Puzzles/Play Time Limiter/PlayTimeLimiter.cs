using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayTimeLimiter : MonoBehaviour
{

    [Header("Referencia al administrador de gameobjects del apartado control parental")]
    [SerializeField] private ParentalControlUIManager parentalControlUIManager;

    private static PlayTimeLimiter Instance;

    [Header("Ajustes")]
    private int minTimeMinutes = 5;
    private int maxTimeMinutes = 120;
    private int defaultTimeMinutes = 10;
    private int stepMinutes = 5;

    private int selectedTimeMinutes;
    private float timeLeftSeconds;
    private bool timerStarted = false;

    private void Awake()
    {
        // Persistir entre escenas
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded; // Suscribimos al evento de carga de escena
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {

        if (scene.buildIndex == 9)
        {
            // Llamar a la corutina después de un pequeño retraso
            StartCoroutine(FindParentalControlUIManagerAfterDelay());
        }
    }

    private IEnumerator FindParentalControlUIManagerAfterDelay()
    {
        // Espera un pequeño retraso para asegurarse de que los objetos estén activos
        yield return new WaitForSeconds(0.1f);

        GameObject parentalControlObject = GameObject.Find("ParentalControlUIManager");

        if (parentalControlObject != null)
        {
            parentalControlUIManager = parentalControlObject.GetComponent<ParentalControlUIManager>();
        }
        else
        {
            Debug.LogWarning("No se encontró el objeto 'ParentalControlUIManager' en la escena.");
        }

        yield return new WaitForSeconds(0.1f);

        parentalControlUIManager.DecreaseTimeButton.onClick.AddListener(DecreaseTime);
        parentalControlUIManager.IncreaseTimeButton.onClick.AddListener(IncreaseTime);

        parentalControlUIManager.StartTimeButton.onClick.AddListener(StartTimer);

        parentalControlUIManager.ResetTimeButton.onClick.AddListener(CancelTimer);

        if (timerStarted)
        {
            parentalControlUIManager.SelectedTimeText.gameObject.SetActive(false);
            parentalControlUIManager.CurrentTimeText.gameObject.SetActive(true); // Mostrar el tiempo en cuenta regresiva
        }
        else
        {
            parentalControlUIManager.SelectedTimeText.gameObject.SetActive(true);
            parentalControlUIManager.CurrentTimeText.gameObject.SetActive(false); // Ocultar la cuenta regresiva al inicio
            UpdateSelectedTimeText();
        }

        UpdateTimeDisplay(); // Siempre actualiza la UI con el tiempo actual
    }

    private void Start()
    {
        selectedTimeMinutes = defaultTimeMinutes;
    }

    private void Update()
    {
        if (timerStarted)
        {
            timeLeftSeconds -= Time.deltaTime;

            if (timeLeftSeconds <= 0f)
            {
                EndGame();
            }

            UpdateTimeDisplay();
        }
    }

    private void StartTimer()
    {
        timeLeftSeconds = selectedTimeMinutes * 60f;
        timerStarted = true;

        // Desactivar botones para evitar cambiar tiempo después de iniciar
        parentalControlUIManager.IncreaseTimeButton.interactable = false;
        parentalControlUIManager.DecreaseTimeButton.interactable = false;

        // Ocultar el texto de tiempo seleccionado
        parentalControlUIManager.SelectedTimeText.gameObject.SetActive(false);

        parentalControlUIManager.CurrentTimeText.gameObject.SetActive(true);
    }
    private void CancelTimer()
    {
        timerStarted = false;

        parentalControlUIManager.CurrentTimeText.gameObject.SetActive(false);

        // Restaurar texto de tiempo seleccionado
        UpdateSelectedTimeText();
        parentalControlUIManager.SelectedTimeText.gameObject.SetActive(true);

        // Reactivar botones para ajustar el tiempo
        parentalControlUIManager.IncreaseTimeButton.interactable = true;
        parentalControlUIManager.DecreaseTimeButton.interactable = true;

        // También puedes resetear el tiempo si quieres:
        timeLeftSeconds = selectedTimeMinutes * 60f;

        UpdateTimeDisplay();
    }

    private void IncreaseTime()
    {
        if (selectedTimeMinutes + stepMinutes <= maxTimeMinutes)
        {
            selectedTimeMinutes += stepMinutes;
            UpdateSelectedTimeText();
        }
    }

    private void DecreaseTime()
    {
        if (selectedTimeMinutes - stepMinutes >= minTimeMinutes)
        {
            selectedTimeMinutes -= stepMinutes;
            UpdateSelectedTimeText();
        }
    }

    private void UpdateTimeDisplay()
    {
        int minutes = Mathf.FloorToInt(timeLeftSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeLeftSeconds % 60f);

        if (timerStarted)
        {
            parentalControlUIManager.CurrentTimeText.text = $"{minutes:D2}:{seconds:D2}";
        }
        else
        {
            parentalControlUIManager.CurrentTimeText.text = $"{selectedTimeMinutes} min";
        }
    }

    private void UpdateSelectedTimeText()
    {
        parentalControlUIManager.SelectedTimeText.text = $"{selectedTimeMinutes} min";
    }

    private void EndGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
