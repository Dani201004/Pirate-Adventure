using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;
using System.Linq;

public class PlayFabGameListManager : MonoBehaviour
{
    private static PlayFabGameListManager instance;

    [Header("Paneles e Inputs")]
    [SerializeField] private GameObject newGamePanel;             // Panel para crear una nueva partida
    [SerializeField] private TMP_InputField newGameNameInput;       // InputField donde el jugador ingresa el nombre de la partida
    [SerializeField] private Button submitButton;                   // Bot�n para confirmar la creaci�n de la partida
    [SerializeField] private GameObject error1Panel;                // Panel para error: nombre vac�o
    [SerializeField] private GameObject error2Panel;                // Panel para error: l�mite de partidas alcanzado
    [SerializeField] private Button continueError1Button;
    [SerializeField] private Button continueError2Button;

    [SerializeField] private GameObject principalButtons;
    [SerializeField] private Button socialButton;
    [SerializeField] private Button settingsButton;

    [Header("Lista de Partidas Guardadas")]
    [SerializeField] private Transform savedGamesPanel;           // Panel donde se mostrar�n las partidas guardadas
    [SerializeField] private GameObject savedGamePrefab;          // Prefab para cada entrada en la lista

    private const int maxSavedGames = 10;
    private List<string> savedGames = new List<string>();

    private float cooldownTime = 1.5f;  // Tiempo de espera entre clics (en segundos)
    private float lastClickedTime = -Mathf.Infinity;  // Marca de tiempo del �ltimo clic

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Buscar los paneles por nombre al inicio
        FindComponents();

        AssignButtonListeners();
    }

    private void OnDestroy()
    {

        // Desregistrar el evento cuando el objeto se destruye
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    private void RegisterNewGameButton()
    {
        Button newGameButton = GameObject.Find("New Game")?.GetComponent<Button>();  // Aseg�rate de que el bot�n tenga este nombre en la escena.
        if (newGameButton != null)
        {
            newGameButton.onClick.RemoveAllListeners();  // Limpiar cualquier evento previo
            newGameButton.onClick.AddListener(ShowNewGamePanel);  // Asignar el evento
        }
    }
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RegisterNewGameButton();  // Aseg�rate de que el bot�n "New Game" se registre al cargar una nueva escena

        // Si la escena es la 0, busca los paneles
        if (scene.buildIndex == 0)
        {
            FindComponents();
            AssignButtonListeners();
        }

        if (scene.buildIndex == 1)
        {
            Debug.Log("Ejecutando DisplaySavedGames() desde OnSceneLoaded.");
            LoadSavedGames();
        }
    }

    private void FindComponents()
    {
            // Buscar los componentes de los paneles
            newGamePanel = FindPanelInHierarchy("SaveNamePanel");
            error1Panel = FindPanelInHierarchy("Error");
            error2Panel = FindPanelInHierarchy("Error 2");

            continueError1Button = FindButtonInHierarchy("Continue Error 1");
            continueError2Button = FindButtonInHierarchy("Continue Error 2");

            // Buscar los componentes de los botones e InputField
            submitButton = FindButtonInHierarchy("Submit Name");
            newGameNameInput = FindInputFieldInHierarchy("Enter Save Name");

            // Busca todos los VerticalLayoutGroup en la escena y filtra por nombre del GameObject
            principalButtons = FindObjectsOfType<VerticalLayoutGroup>(true)
                .FirstOrDefault(v => v.gameObject.name == "Principal Buttons")?.gameObject;
            socialButton = FindButtonInHierarchy("Social");
            settingsButton = FindButtonInHierarchy("Settings");

            // Verificar si se encontraron los componentes
            if (newGamePanel == null)
                Debug.LogError("No se encontr� 'SaveNamePanel' en la escena.");
            if (error1Panel == null)
                Debug.LogError("No se encontr� 'Error' en la escena.");
            if (error2Panel == null)
                Debug.LogError("No se encontr� 'Error 2' en la escena.");
            if (continueError1Button == null)
                Debug.LogError("No se encontr� 'Continue Error 1' en la escena.");
            if (continueError2Button == null)
                Debug.LogError("No se encontr� 'Continue Error 2' en la escena.");
            if (submitButton == null)
                Debug.LogError("No se encontr� 'Submit Name' en la escena.");
            if (newGameNameInput == null)
                Debug.LogError("No se encontr� 'Enter Save Name' en la escena.");
            if (principalButtons == null)
                Debug.LogError("No se encontr� 'Principal Buttons' en la escena.");
            if (socialButton == null)
                Debug.LogError("No se encontr� 'Social' en la escena.");
            if (settingsButton == null)
                Debug.LogError("No se encontr� 'Settings' en la escena.");
    }
    private void AssignButtonListeners()
    {
        // Asignar el evento de HideError1Panel al bot�n Continue Error
        if (continueError1Button != null)
        {
            continueError1Button.onClick.RemoveAllListeners(); // Limpiar eventos previos
            continueError1Button.onClick.AddListener(HideError1Panel);
        }

        // Asignar el evento de HideError2Panel al bot�n Continue Error 2
        if (continueError2Button != null)
        {
            continueError2Button.onClick.RemoveAllListeners(); // Limpiar eventos previos
            continueError2Button.onClick.AddListener(HideError2Panel);
        }
        // Asignar el evento de OnSubmitNewGame al bot�n Submit Name
        if (submitButton != null)
        {
            submitButton.onClick.RemoveAllListeners(); // Limpiar eventos previos
            submitButton.onClick.AddListener(OnSubmitNewGame);
        }

    }

    // Funci�n recursiva para buscar un bot�n en la jerarqu�a de objetos, incluso desactivados
    private Button FindButtonInHierarchy(string buttonName)
    {
        // Obtener todos los botones (usando RectTransform para incluir todos los objetos, incluso desactivados)
        RectTransform[] rectTransforms = GameObject.Find("Canvas").GetComponentsInChildren<RectTransform>(true);

        foreach (RectTransform rt in rectTransforms)
        {
            if (rt.gameObject.name == buttonName)
            {
                return rt.GetComponent<Button>();
            }
        }

        return null;
    }

    // Funci�n recursiva para buscar un InputField en la jerarqu�a de objetos, incluso desactivados
    private TMP_InputField FindInputFieldInHierarchy(string inputFieldName)
    {
        // Obtener todos los InputFields (usando RectTransform para incluir todos los objetos, incluso desactivados)
        RectTransform[] rectTransforms = GameObject.Find("Canvas").GetComponentsInChildren<RectTransform>(true);

        foreach (RectTransform rt in rectTransforms)
        {
            if (rt.gameObject.name == inputFieldName)
            {
                return rt.GetComponent<TMP_InputField>();
            }
        }

        return null;
    }

    // Funci�n recursiva para buscar un objeto por nombre en todos los hijos, incluso desactivados
    private GameObject FindPanelInHierarchy(string panelName)
    {
        // Obtener todos los RectTransforms en la jerarqu�a del Canvas (o el objeto ra�z adecuado)
        RectTransform[] rectTransforms = GameObject.Find("Canvas").GetComponentsInChildren<RectTransform>(true);

        foreach (RectTransform rt in rectTransforms)
        {
            if (rt.gameObject.name == panelName)
            {
                return rt.gameObject;
            }
        }

        return null;
    }

    public void ShowNewGamePanel()
    {
        newGamePanel.SetActive(true);
        principalButtons.SetActive(false);
        socialButton.gameObject.SetActive(false);
        settingsButton.gameObject.SetActive(false);
    } 
    private void HideNewGamePanel()
    {
        newGamePanel.SetActive(false);
        principalButtons.SetActive(true);
        socialButton.gameObject.SetActive(true);
        settingsButton.gameObject.SetActive(true);
    }

    // Se recoge el nombre ingresado y se crea una nueva partida
    public void OnSubmitNewGame()
    {
        // Verificar si el cooldown ha pasado
        if (Time.time - lastClickedTime < cooldownTime)
        {
            //Debug.Log("Esperando para el siguiente clic...");
            return;  // Salir del m�todo si no ha pasado el tiempo de cooldown
        }

        // Registrar el tiempo de este clic
        lastClickedTime = Time.time;

        string newGameName = newGameNameInput.text;
        if (string.IsNullOrEmpty(newGameName))
        {
            error1Panel.SetActive(true);
            return;
        }
        if (savedGames.Count >= maxSavedGames)
        {
            error2Panel.SetActive(true);
            return;
        }
        SaveNewGame(newGameName);
    }

    // Guarda el nombre de la nueva partida en PlayFab
    private void SaveNewGame(string gameName)
    {
        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
        {
            { "GameName_" + gameName, gameName }
        }
        };
        PlayFabClientAPI.UpdateUserData(request, result =>
        {
            savedGames.Add(gameName);
      

            // Solo despu�s de guardar los datos, cambiamos de escena
            OnNewGame();
        }, error =>
        {
            Debug.LogError("Error al guardar la nueva partida: " + error.GenerateErrorReport());
        });

        HideNewGamePanel();
    }

    // Carga las partidas guardadas de PlayFab
    private void LoadSavedGames()
    {
        var request = new GetUserDataRequest();
        PlayFabClientAPI.GetUserData(request, result =>
        {
            savedGames.Clear();
            if (result.Data != null)
            {
                foreach (var data in result.Data)
                {
                    if (data.Key.StartsWith("GameName_"))
                    {
                        savedGames.Add(data.Value.Value);
                        Debug.Log("Partida cargada: " + data.Value.Value);  // Verifica las partidas cargadas
                    }
                }
            }
            DisplaySavedGames();  // Mostrar las partidas despu�s de cargar
        }, error =>
        {
            Debug.LogError("Error al cargar partidas guardadas: " + error.GenerateErrorReport());
        });
    }

    // Genera la lista de partidas en la interfaz
    private void DisplaySavedGames()
    {
        Debug.Log("N�mero de partidas guardadas: " + savedGames.Count);

        if (savedGamesPanel == null)
        {
            savedGamesPanel = GameObject.Find("SavedGamesLayoutGroup")?.transform;
            if (savedGamesPanel == null)
            {
                Debug.LogError("No se encontr� 'SavedGamesLayoutGroup' en la escena.");
                return;
            }
        }

        // Limpiar los elementos anteriores
        foreach (Transform child in savedGamesPanel)
        {
            Destroy(child.gameObject);
        }

        // Crear las nuevas entradas
        foreach (string gameName in savedGames)
        {
            GameObject entry = Instantiate(savedGamePrefab, savedGamesPanel);

            // Buscar y asignar el texto del nombre de la partida
            TextMeshProUGUI nameText = entry.transform.Find("SavedGameNametext").GetComponent<TextMeshProUGUI>();
            if (nameText != null)
            {
                // Obtener el idioma actual desde LanguageManager
                int localeID = PlayerPrefs.GetInt("LocaleKey", 0);  // 0 es el valor por defecto
                string text = "";

                // Comprobar el idioma y asignar el texto correspondiente
                if (localeID == 0) // Suponiendo que 0 es para ingles
                {
                    text = "Game Name: " + gameName;
                }
                else if (localeID == 1) // Suponiendo que 1 es para espa�ol
                {
                    text = "Nombre partida: " + gameName;
                }

                nameText.text = text;
            }

            // Buscar y asignar el texto del progreso de puzzles
            TextMeshProUGUI puzzleText = entry.transform.Find("Puzzlestext")?.GetComponent<TextMeshProUGUI>();
            if (puzzleText != null)
            {
                GetPuzzleProgress(gameName, puzzleProgress =>
                {
                    puzzleText.text = puzzleProgress.ToString();
                });
            }

            // Buscar y asignar el texto de las gemas m�gicas
            TextMeshProUGUI magicGemsText = entry.transform.Find("MagicGemsQuantitytext")?.GetComponent<TextMeshProUGUI>();
            if (magicGemsText != null)
            {
                GetMagicGems(gameName, magicGems =>
                {
                    // Obtener el idioma actual desde LanguageManager
                    int localeID = PlayerPrefs.GetInt("LocaleKey", 0);  // 0 es el valor por defecto
                    string text = "";

                    // Comprobar el idioma y asignar el texto correspondiente
                    if (localeID == 0) // Suponiendo que 0 es para ingles
                    {
                        text = "Magic Gems: " + magicGems.ToString();
                    }
                    else if (localeID == 1) // Suponiendo que 1 es para espa�ol
                    {
                        text = "Gemas M�gicas: " + magicGems.ToString();
                    }

                    magicGemsText.text = text;
                });
            }

            // Asignar el bot�n de selecci�n de partida
            Button entryButton = entry.GetComponent<Button>();
            if (entryButton != null)
            {
                entryButton.onClick.AddListener(() => OnGameSelected(gameName));
            }

            // Bot�n para eliminar la partida
            Button deleteButton = entry.transform.Find("Remove")?.GetComponent<Button>();
            if (deleteButton != null)
            {
                deleteButton.onClick.AddListener(() => OnDeleteGame(gameName));
            }

            // Obtener y reemplazar la imagen del juego guardado con la imagen desde PlayFabProgressManager
            Image gameImage = entry.transform.Find("SavedGameImage")?.GetComponent<Image>(); // Suponiendo que tienes un GameObject llamado "SavedGameImage"
            if (gameImage != null)
            {
                Sprite savedGameImage = PlayFabProgressManager.Instance.GetSavedGameImage(gameName); // M�todo para obtener la imagen del progreso guardado

                if (savedGameImage != null)
                {
                    gameImage.sprite = savedGameImage;
                }
                else
                {
                    Debug.LogWarning("No se encontr� la imagen para el juego: " + gameName);
                    // Aqu� puedes asignar una imagen por defecto si lo deseas
                }
            }
        }

    }
    private void GetPuzzleProgress(string gameName, System.Action<int> callback)
    {
        var request = new GetUserDataRequest();
        PlayFabClientAPI.GetUserData(request, result =>
        {
            if (result.Data != null && result.Data.ContainsKey("PuzzleProgress_" + gameName))
            {
                int progress = int.Parse(result.Data["PuzzleProgress_" + gameName].Value);
                callback(progress);
            }
            else
            {
                callback(0); // Si no hay datos, se asume progreso 0
            }
        }, error =>
        {
            Debug.LogError("Error al cargar el progreso de puzzles para " + gameName + ": " + error.GenerateErrorReport());
            callback(0); // En caso de error, se asume progreso 0
        });
    }

    private void GetMagicGems(string gameName, System.Action<int> callback)
    {
        var request = new GetUserDataRequest();
        PlayFabClientAPI.GetUserData(request, result =>
        {
            if (result.Data != null && result.Data.ContainsKey("MagicGems_" + gameName))
            {
                int gems = int.Parse(result.Data["MagicGems_" + gameName].Value);
                callback(gems);
            }
            else
            {
                callback(0); // Si no hay datos, se asume 0 gemas
            }
        }, error =>
        {
            Debug.LogError("Error al cargar las gemas m�gicas para " + gameName + ": " + error.GenerateErrorReport());
            callback(0); // En caso de error, se asume 0 gemas
        });
    }

    // Al seleccionar una partida, se llama a PlayFabProgressManager para cargar los datos asociados
    private void OnGameSelected(string gameName)
    {
        SceneTransition.Instance.LoadLevelSave(gameName);
    }
    // M�todo para borrar una partida
    private void OnDeleteGame(string gameName)
    {
        string keyToRemove = "GameName_" + gameName;
        var request = new UpdateUserDataRequest
        {
            KeysToRemove = new List<string> { keyToRemove }  // Ahora se especifica correctamente la clave a eliminar
        };

        PlayFabClientAPI.UpdateUserData(request, result =>
        {
            Debug.Log("Partida eliminada correctamente: " + gameName);

            // Eliminar la partida de la lista local
            savedGames.Remove(gameName);
            DisplaySavedGames();  // Actualizar la lista en la UI
        }, error =>
        {
            Debug.LogError("Error al eliminar la partida: " + error.GenerateErrorReport());
        });
    }

    private void OnNewGame()
    {
        SceneTransition.Instance.LoadLevelNewGame();
    }

    public void HideError1Panel()
    {
        error1Panel.SetActive(false);
    }
    public void HideError2Panel()
    {
        error2Panel.SetActive(false);
        newGamePanel.SetActive(false);
    }
}