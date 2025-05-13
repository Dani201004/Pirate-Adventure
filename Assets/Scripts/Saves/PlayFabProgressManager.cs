using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;
using System.Linq;

public class PlayFabProgressManager : MonoBehaviour
{
    public static PlayFabProgressManager Instance;

    [Header("Referencia al administrador de gameobjects del apartado game")]
    [SerializeField] private GameUIManager gameUIManager;

    // Lista de puzzles completados
    public HashSet<int> completedPuzzles = new HashSet<int>();

    [Header("Progreso y Datos")]
    [SerializeField] public int puzzleProgress = 0;  // Puzzles completados
    [SerializeField] public int Gems = 0;       // Número de gemas mágicas
    [SerializeField] public int magicGems = 0;       // Número de gemas mágicas
    [SerializeField] public int silverTrophies = 0;  // Número de trofeos de plata
    [SerializeField] public int goldTrophies = 0;    // Número de trofeos de oro
    [SerializeField] public string lastGamePlayed = ""; // Última partida jugada

    public int currentPuzzle { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    private void OnEnable()
    {
        // Nos suscribimos al evento sceneLoaded
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // Nos desuscribimos del evento cuando el objeto es desactivado
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Método que se llama cuando se carga una nueva escena
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Verifica si la escena cargada tiene el índice 2
        if (scene.buildIndex == 2)
        {
            // Buscar el GameUIManager solo cuando estamos en la escena 2
            gameUIManager = FindObjectOfType<GameUIManager>();

            if (gameUIManager != null)
            {
                // Llama al método para actualizar los textos
                StartCoroutine(UpdateUITextCoroutine());
            }
            else
            {
                Debug.LogWarning("No se encontró GameUIManager en la escena 2.");
            }
        }
    }

    /// <summary>
    /// Guarda el progreso de la partida identificada por gameName.
    /// </summary>
    public void SaveGameData(string gameName)
    {
        //Debug.Log("Guardando progreso para el juego: " + gameName);  // Verifica que el valor de gameName es correcto
        lastGamePlayed = gameName;  // Guardamos el nombre de la última partida jugada

        // Convertir el conjunto de puzzles completados a una lista serializada
        List<string> completedPuzzlesList = new List<string>(completedPuzzles.Select(id => id.ToString()));

        var data = new Dictionary<string, string>
        {
            { "PuzzleProgress_" + gameName, puzzleProgress.ToString() },
            { "Gems_" + gameName, Gems.ToString() },
            { "MagicGems_" + gameName, magicGems.ToString() },
            { "SilverTrophies_" + gameName, silverTrophies.ToString() },
            { "GoldTrophies_" + gameName, goldTrophies.ToString() },
            { "LastGamePlayed", gameName },  // Guardamos también la última partida jugada
            { "CompletedPuzzles_" + gameName, string.Join(",", completedPuzzlesList) }  // Guardamos los puzzles completados
        };

        var request = new GetUserDataRequest();
        PlayFabClientAPI.GetUserData(request, result =>
        {
            bool isExistingGame = result.Data != null && result.Data.ContainsKey("PuzzleProgress_" + gameName);

            var updateRequest = new UpdateUserDataRequest { Data = data };
            PlayFabClientAPI.UpdateUserData(updateRequest, updateResult =>
            {
                //Debug.Log("Progreso guardado para la partida " + gameName);

                StartCoroutine(CaptureScreenshot(gameName));
            }, error =>
            {
                Debug.LogError("Error al guardar el progreso: " + error.GenerateErrorReport());
            });
        }, error =>
        {
            Debug.LogError("Error al verificar si la partida existe: " + error.GenerateErrorReport());
        });
    }

    /// <summary>
    /// Carga el progreso de la partida identificada por gameName.
    /// </summary>
    public void LoadGameData(string gameName)
    {
        var request = new GetUserDataRequest();
        PlayFabClientAPI.GetUserData(request, result =>
        {
            if (result.Data != null)
            {
                if (result.Data.ContainsKey("PuzzleProgress_" + gameName))
                    puzzleProgress = int.Parse(result.Data["PuzzleProgress_" + gameName].Value);
                else
                    Debug.LogWarning("No se encontró el progreso de puzzles para " + gameName);

                if (result.Data.ContainsKey("Gems_" + gameName))
                    Gems = int.Parse(result.Data["Gems_" + gameName].Value);
                else
                    Debug.LogWarning("No se encontraron gemas para " + gameName);

                if (result.Data.ContainsKey("MagicGems_" + gameName))
                    magicGems = int.Parse(result.Data["MagicGems_" + gameName].Value);
                else
                    Debug.LogWarning("No se encontraron gemas mágicas para " + gameName);

                if (result.Data.ContainsKey("SilverTrophies_" + gameName))
                    silverTrophies = int.Parse(result.Data["SilverTrophies_" + gameName].Value);
                else
                    Debug.LogWarning("No se encontraron trofeos de plata para " + gameName);

                if (result.Data.ContainsKey("GoldTrophies_" + gameName))
                    goldTrophies = int.Parse(result.Data["GoldTrophies_" + gameName].Value);
                else
                    Debug.LogWarning("No se encontraron trofeos de oro para " + gameName);

                if (result.Data.ContainsKey("LastGamePlayed"))
                {
                    //Debug.Log("LastGamePlayed desde PlayFab: " + result.Data["LastGamePlayed"].Value);
                    lastGamePlayed = result.Data["LastGamePlayed"].Value;
                }

                // Cargar puzzles completados
                if (result.Data.ContainsKey("CompletedPuzzles_" + gameName) && !string.IsNullOrEmpty(result.Data["CompletedPuzzles_" + gameName].Value))
                {
                    string completedPuzzlesData = result.Data["CompletedPuzzles_" + gameName].Value;
                    Debug.Log("Puzzles completados cargados: " + completedPuzzlesData);
                    completedPuzzles = new HashSet<int>(completedPuzzlesData.Split(',').Select(int.Parse));
                }
                else
                {
                    // Si no hay puzzles completados, inicializar como conjunto vacío
                    //Debug.Log("No hay puzzles completados aún.");
                    completedPuzzles = new HashSet<int>();
                }

                Debug.Log("Datos cargados para la partida " + gameName);
            }
            else
            {
                Debug.LogWarning("No hay datos guardados para " + gameName);
            }
        }, error =>
        {
            Debug.LogError("Error al cargar el progreso: " + error.GenerateErrorReport());
        });
    }

    /// <summary>
    /// Captura una imagen del juego
    /// </summary>
    private IEnumerator CaptureScreenshot(string gameName)
    {
        yield return new WaitForEndOfFrame();

        // Captura la pantalla
        Texture2D screenTexture = ScreenCapture.CaptureScreenshotAsTexture();

        if (screenTexture == null)
        {
            Debug.LogError("La textura de la captura de pantalla es nula.");
            yield break;
        }

        // Convierte la textura a un array de bytes en formato PNG
        byte[] imageData = screenTexture.EncodeToPNG();

        if (imageData == null || imageData.Length == 0)
        {
            Debug.LogError("No se pudo convertir la captura de pantalla en imagen.");
            yield break;
        }

        // Android: Guardar en almacenamiento local
#if UNITY_ANDROID
        string androidFilePath = Path.Combine(Application.persistentDataPath, gameName + "_screenshot.png");
        File.WriteAllBytes(androidFilePath, imageData);
        Debug.Log("Captura de pantalla guardada en Android: " + androidFilePath);
#endif

        // WebGL: Guardar como base64 en memoria (sin enlace de descarga)
#if UNITY_WEBGL
    string base64 = System.Convert.ToBase64String(imageData);
    // Puedes almacenar este base64 en PlayerPrefs, enviar a un servidor, o lo que necesites
    PlayerPrefs.SetString(gameName + "_screenshot_base64", base64);
    PlayerPrefs.Save();
    Debug.Log("Captura de pantalla guardada como base64 para WebGL: " + gameName);
#endif

        // Windows: Guardar en el sistema de archivos
#if UNITY_STANDALONE_WIN
    string windowsFilePath = Path.Combine(Application.persistentDataPath, gameName + "_screenshot.png");
    File.WriteAllBytes(windowsFilePath, imageData);
    Debug.Log("Captura de pantalla guardada en Windows: " + windowsFilePath);
#endif

        Destroy(screenTexture); // Libera la memoria
    }

    /// <summary>
    /// Devuelve un Sprite, o null si no hay imagen.
    /// </summary>
    public Sprite GetSavedGameImage(string gameName)
    {
        string filePath = Path.Combine(Application.persistentDataPath, gameName + "_screenshot.png");

        if (File.Exists(filePath))
        {
            byte[] imageData = File.ReadAllBytes(filePath);
            Texture2D texture = new Texture2D(2, 2);
            if (texture.LoadImage(imageData))
            {
                return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            }
        }
        else
        {
            Debug.LogWarning("No se encontró el archivo de imagen en: " + filePath);
        }

        return null;
    }
    /// <summary>
    /// Actualiza los textos en la UI con los datos cargados de PlayFab, después de un pequeño retraso.
    /// </summary>
    private IEnumerator UpdateUITextCoroutine()
    {
        yield return new WaitForSeconds(0.2f);

        // Verifica si la escena actual es la número 2
        if (SceneManager.GetActiveScene().buildIndex == 2 && gameUIManager != null)
        {
            if (gameUIManager.GemsAmountText != null)
                gameUIManager.GemsAmountText.text = Gems.ToString();

            if (gameUIManager.MagicGemsAmountText != null)
                gameUIManager.MagicGemsAmountText.text = magicGems.ToString();

            if (gameUIManager.SilverTrophiesAmountText != null)
                gameUIManager.SilverTrophiesAmountText.text = silverTrophies.ToString();

            if (gameUIManager.GoldenTrophiesAmountText != null)
                gameUIManager.GoldenTrophiesAmountText.text = goldTrophies.ToString();
        }
    }
    public void SetCurrentPuzzle(int puzzleID)
    {
        currentPuzzle = puzzleID;
    }
    public void SetCurrentGame(string gameName)
    {
        lastGamePlayed = gameName;
        Debug.Log("SetCurrentGame llamado con: " + gameName);
    }

}