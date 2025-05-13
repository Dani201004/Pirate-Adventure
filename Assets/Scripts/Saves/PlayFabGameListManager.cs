using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayFabGameListManager : MonoBehaviour
{
    private static PlayFabGameListManager instance;

    [SerializeField] private MainMenuUIManager mainMenuUIManager;

    [SerializeField] private SavesUIManager savesUIManager;

    private const int maxSavedGames = 10;
    private List<string> savedGames = new List<string>();

    private float cooldownTime = 1.5f;  // Tiempo de espera entre clics (en segundos)
    private float lastClickedTime = -Mathf.Infinity;  // Marca de tiempo del último clic

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

    private void OnDestroy()
    {
        // Desregistrar el evento cuando el objeto se destruye
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    private IEnumerator FindMainMenuUIManagerAfterDelay()
    {
        // Espera un pequeño retraso para asegurarse de que los objetos estén activos
        yield return new WaitForSeconds(0.1f);

        GameObject mainMenuObject = GameObject.Find("MainMenuUIManager");

        if (mainMenuObject != null)
        {
            mainMenuUIManager = mainMenuObject.GetComponent<MainMenuUIManager>();
        }
        else
        {
            Debug.LogWarning("No se encontró el objeto 'MainMenuUIManager' en la escena.");
        }

        yield return new WaitForSeconds(0.2f);

        mainMenuUIManager.NewGameButton.onClick.RemoveAllListeners();  // Limpiar cualquier evento previo
        mainMenuUIManager.NewGameButton.onClick.AddListener(ShowNewGamePanel);  // Asignar el evento
        mainMenuUIManager.NewGameButton.onClick.AddListener(LoadSavedGames);  // Asignar el evento

        mainMenuUIManager.SubmitButton.onClick.RemoveAllListeners(); // Limpiar eventos previos
        mainMenuUIManager.SubmitButton.onClick.AddListener(OnSubmitNewGame);

        mainMenuUIManager.ContinueError1Button.onClick.RemoveAllListeners(); // Limpiar eventos previos
        mainMenuUIManager.ContinueError1Button.onClick.AddListener(HideError1Panel);

        mainMenuUIManager.ContinueError2Button.onClick.RemoveAllListeners(); // Limpiar eventos previos
        mainMenuUIManager.ContinueError2Button.onClick.AddListener(HideError2Panel);

        mainMenuUIManager.ContinueError3Button.onClick.RemoveAllListeners(); // Limpiar eventos previos
        mainMenuUIManager.ContinueError3Button.onClick.AddListener(HideError3Panel);
    }
    private IEnumerator FindSavesUIManagerAfterDelay()
    {
        // Espera un pequeño retraso para asegurarse de que los objetos estén activos
        yield return new WaitForSeconds(0.1f);

        GameObject savesObject = GameObject.Find("SavesUIManager");

        if (savesObject != null)
        {
            savesUIManager = savesObject.GetComponent<SavesUIManager>();
        }
        else
        {
            Debug.LogWarning("No se encontró el objeto 'SavesUIManager' en la escena.");
        }

        LoadSavedGames();

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
        // Si la escena es la 0, busca los paneles
        if (SceneManager.GetActiveScene().buildIndex == 0)
        {
            StartCoroutine(FindMainMenuUIManagerAfterDelay());
        }

        if (SceneManager.GetActiveScene().buildIndex == 1)
        {
            StartCoroutine(FindSavesUIManagerAfterDelay());
        }
    }

    public void ShowNewGamePanel()
    {
        mainMenuUIManager.NewGamePanel.SetActive(true);
        mainMenuUIManager.PrincipalButtons.SetActive(false);
        mainMenuUIManager.SocialButton.gameObject.SetActive(false);
        mainMenuUIManager.SettingsButton.SetActive(false);
    } 
    private void HideNewGamePanel()
    {
        mainMenuUIManager.NewGamePanel.SetActive(false);
        mainMenuUIManager.PrincipalButtons.SetActive(true);
        mainMenuUIManager.SocialButton.gameObject.SetActive(true);
        mainMenuUIManager.SettingsButton.SetActive(true);
    }

    // Se recoge el nombre ingresado y se crea una nueva partida
    public void OnSubmitNewGame()
    {
        // Verificar si el cooldown ha pasado
        if (Time.time - lastClickedTime < cooldownTime)
        {
            return;  // Salir del método si no ha pasado el tiempo de cooldown
        }

        // Registrar el tiempo de este clic
        lastClickedTime = Time.time;

        string newGameName = mainMenuUIManager.NewGameNameInput.text.Trim();

        if (string.IsNullOrEmpty(newGameName))
        {
            mainMenuUIManager.Error1Panel.SetActive(true);
            return;
        }

        // Comparación sin distinguir mayúsculas y minúsculas
        bool exists = savedGames.Exists(name => name.Trim().ToLowerInvariant() == newGameName.ToLowerInvariant());
        if (exists)
        {
            mainMenuUIManager.Error3Panel.SetActive(true);
            Debug.LogWarning("Ya existe una partida con ese nombre.");
            return;
        }

        if (savedGames.Count >= maxSavedGames)
        {
            mainMenuUIManager.Error2Panel.SetActive(true);
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

            PlayFabProgressManager.Instance.SetCurrentGame(gameName);
            PlayFabProgressManager.Instance.SaveGameData(gameName);

            // Solo después de guardar los datos, cambiamos de escena
            OnNewGame();
        }, error =>
        {
            Debug.LogError("Error al guardar la nueva partida: " + error.GenerateErrorReport());
        });

        HideNewGamePanel();
    }

    // Carga las partidas guardadas de PlayFab
    public void LoadSavedGames()
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
                        //Debug.Log("Partida cargada: " + data.Value.Value);  // Verifica las partidas cargadas
                    }
                }
            }
            DisplaySavedGames();  // Mostrar las partidas después de cargar
        }, error =>
        {
            Debug.LogError("Error al cargar partidas guardadas: " + error.GenerateErrorReport());
        });
    }

    // Genera la lista de partidas en la interfaz
    private void DisplaySavedGames()
    {
        if (SceneManager.GetActiveScene().buildIndex != 1)
        {
            Debug.Log("No se encuentra en la escena correcta para mostrar partidas.");
            return;
        }

        Debug.Log("Número de partidas guardadas: " + savedGames.Count);

        // Limpiar los elementos anteriores
        foreach (Transform child in savesUIManager.SavedGamesLayoutGroup)
        {
            Destroy(child.gameObject);
        }

        // Crear las nuevas entradas
        foreach (string gameName in savedGames)
        {
            GameObject entry = Instantiate(savesUIManager.SavedGamePrefab, savesUIManager.SavedGamesLayoutGroup);

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
                else if (localeID == 1) // Suponiendo que 1 es para español
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

            // Buscar y asignar el texto de las gemas mágicas
            TextMeshProUGUI GemsText = entry.transform.Find("GemsQuantitytext")?.GetComponent<TextMeshProUGUI>();
            if (GemsText != null)
            {
                GetGems(gameName, Gems =>
                {
                    // Obtener el idioma actual desde LanguageManager
                    int localeID = PlayerPrefs.GetInt("LocaleKey", 0);  // 0 es el valor por defecto
                    string text = "";

                    // Comprobar el idioma y asignar el texto correspondiente
                    if (localeID == 0) // Suponiendo que 0 es para ingles
                    {
                        text = "Gems: " + Gems.ToString();
                    }
                    else if (localeID == 1) // Suponiendo que 1 es para español
                    {
                        text = "Gemas: " + Gems.ToString();
                    }

                    GemsText.text = text;
                });
            }
            // Buscar y asignar el texto de las gemas mágicas
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
                    else if (localeID == 1) // Suponiendo que 1 es para español
                    {
                        text = "Gemas Mágicas: " + magicGems.ToString();
                    }

                    magicGemsText.text = text;
                });
            }
            // Asignar el botón de selección de partida
            Button entryButton = entry.GetComponent<Button>();
            if (entryButton != null)
            {
                entryButton.onClick.AddListener(() => OnGameSelected(gameName));
            }

            // Botón para eliminar la partida
            Button deleteButton = entry.transform.Find("Remove")?.GetComponent<Button>();
            if (deleteButton != null)
            {
                deleteButton.onClick.AddListener(() => OnDeleteGame(gameName));
            }

            // Obtener y reemplazar la imagen del juego guardado con la imagen desde PlayFabProgressManager
            Image gameImage = entry.transform.Find("SavedGameImage")?.GetComponent<Image>(); // Suponiendo que tienes un GameObject llamado "SavedGameImage"
            if (gameImage != null)
            {
                Sprite savedGameImage = PlayFabProgressManager.Instance.GetSavedGameImage(gameName); // Método para obtener la imagen del progreso guardado

                if (savedGameImage != null)
                {
                    gameImage.sprite = savedGameImage;
                }
                else
                {
                    Debug.LogError("No se encontró la imagen para el juego: " + gameName);
                    // Aquí puedes asignar una imagen por defecto si lo deseas
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
    private void GetGems(string gameName, System.Action<int> callback)
    {
        var request = new GetUserDataRequest();
        PlayFabClientAPI.GetUserData(request, result =>
        {
            string key = "Gems_" + gameName;

            if (result.Data != null && result.Data.ContainsKey(key))
            {
                int gems = int.Parse(result.Data[key].Value);
                callback(gems);
            }
            else
            {
                callback(0); // Si no hay datos, se asume 0 gemas
            }
        }, error =>
        {
            Debug.LogError("Error al cargar las gemas para " + gameName + ": " + error.GenerateErrorReport());
            callback(0);
        });
    }
    private void GetMagicGems(string gameName, System.Action<int> callback)
    {
        var request = new GetUserDataRequest();
        PlayFabClientAPI.GetUserData(request, result =>
        {
            string key = "MagicGems_" + gameName;

            if (result.Data != null && result.Data.ContainsKey(key))
            {
                int gems = int.Parse(result.Data[key].Value);
                callback(gems);
            }
            else
            {
                callback(0); // Si no hay datos, se asume 0 gemas mágicas
            }
        }, error =>
        {
            Debug.LogError("Error al cargar las gemas mágicas para " + gameName + ": " + error.GenerateErrorReport());
            callback(0);
        });
    }

    // Al seleccionar una partida, se llama a PlayFabProgressManager para cargar los datos asociados
    private void OnGameSelected(string gameName)
    {
        // Guardamos el nombre de la partida en el manager local
        PlayFabProgressManager.Instance.SetCurrentGame(gameName);

        // Subimos el valor a los User Data de PlayFab
        var request = new PlayFab.ClientModels.UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
        {
            { "LastGamePlayed", gameName }
        }
        };

        PlayFab.PlayFabClientAPI.UpdateUserData(request, result =>
        {
            Debug.Log("LastGamePlayed actualizado en PlayFab: " + gameName);

            // Ahora que ya está actualizado, pasamos a la escena
            SceneTransition.Instance.LoadLevelSave(gameName);
        },
        error =>
        {
            Debug.LogError("Error al actualizar LastGamePlayed en PlayFab: " + error.GenerateErrorReport());
        });
    }

    // Método para borrar una partida
    private void OnDeleteGame(string gameName)
    {
    // Eliminar todas las claves asociadas con la partida
    var keysToRemove = new List<string>
    {
        "CompletedPuzzles_" + gameName,
        "GameName_" + gameName,
        "PuzzleProgress_" + gameName,
        "Gems_" + gameName,
        "MagicGems_" + gameName,
        "SilverTrophies_" + gameName,
        "GoldTrophies_" + gameName,
    };

    var request = new UpdateUserDataRequest
    {
        KeysToRemove = keysToRemove  // Especifica las claves a eliminar
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
        mainMenuUIManager.Error1Panel.SetActive(false);
    }
    public void HideError2Panel()
    {
        mainMenuUIManager.Error2Panel.SetActive(false);
        mainMenuUIManager.NewGamePanel.SetActive(false);
        mainMenuUIManager.PrincipalButtons.SetActive(true);
        mainMenuUIManager.SocialButton.gameObject.SetActive(true);
        mainMenuUIManager.SettingsButton.SetActive(true);
    }
    public void HideError3Panel()
    {
        mainMenuUIManager.Error3Panel.SetActive(false);
    }
}