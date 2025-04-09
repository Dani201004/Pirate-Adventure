using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections;
using System.Collections.Generic;

public class PlayFabProgressManager : MonoBehaviour
{
    public static PlayFabProgressManager Instance;

    [Header("Progreso y Datos")]
    public int puzzleProgress = 0;  // Puzzles completados
    public int magicGems = 0;       // Número de gemas mágicas
    public int silverTrophies = 0;  // Número de trofeos de plata
    public int goldTrophies = 0;    // Número de trofeos de oro

    [Header("Transición y Escena")]
    [SerializeField] private Canvas childCanvas;
    [SerializeField] private Animator transitionAnim;

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

    /// <summary>
    /// Guarda el progreso de la partida identificada por gameName.
    /// </summary>
    public void SaveGameData(string gameName)
    {
        var data = new Dictionary<string, string>
        {
            { "PuzzleProgress_" + gameName, puzzleProgress.ToString() },
            { "MagicGems_" + gameName, magicGems.ToString() },
            { "SilverTrophies_" + gameName, silverTrophies.ToString() },
            { "GoldTrophies_" + gameName, goldTrophies.ToString() }
        };

        var request = new GetUserDataRequest();
        PlayFabClientAPI.GetUserData(request, result =>
        {
            bool isExistingGame = result.Data != null && result.Data.ContainsKey("PuzzleProgress_" + gameName);

            var updateRequest = new UpdateUserDataRequest { Data = data };
            PlayFabClientAPI.UpdateUserData(updateRequest, updateResult =>
            {
                Debug.Log("Progreso guardado para la partida " + gameName);

                if (isExistingGame)
                {
                    StartCoroutine(CaptureScreenshot(gameName));
                }
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
    /// Captura una captura de pantalla y la guarda.
    /// </summary>
    private IEnumerator CaptureScreenshot(string gameName)
    {
        yield return new WaitForEndOfFrame();

        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            Texture2D screenTexture = ScreenCapture.CaptureScreenshotAsTexture();
            byte[] imageData = screenTexture.EncodeToPNG();
            string base64 = System.Convert.ToBase64String(imageData);
            PlayerPrefs.SetString(gameName + "_screenshot_base64", base64);
            PlayerPrefs.Save();
            Debug.Log("Captura de pantalla (base64) guardada para " + gameName);
        }
        else
        {
            string filePath = string.Format("{0}/{1}_screenshot.png", Application.persistentDataPath, gameName);
            ScreenCapture.CaptureScreenshot(filePath);
            Debug.Log("Captura de pantalla guardada en: " + filePath);
        }
    }

    /// <summary>
    /// Recupera la imagen guardada para la partida. 
    /// Si la partida tiene una captura de pantalla, devuelve una Sprite.
    /// </summary>
    public Sprite GetSavedGameImage(string gameName)
    {
        string base64Image = PlayerPrefs.GetString(gameName + "_screenshot_base64", null);
        if (!string.IsNullOrEmpty(base64Image))
        {
            byte[] imageData = System.Convert.FromBase64String(base64Image);
            Texture2D texture = new Texture2D(2, 2);  // Crear una textura temporal
            texture.LoadImage(imageData);  // Cargar la imagen base64 en la textura
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            return sprite;
        }
        else
        {
            // Si no hay imagen guardada, retornar null o una imagen por defecto
            return null;
        }
    }
}