using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LanguageManager : MonoBehaviour
{
    private static LanguageManager _instance;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // Registrar el evento que se llama cada vez que se carga una nueva escena
        SceneManager.sceneLoaded += OnSceneLoaded;
    }


    private void Start()
    {
        // Cargar el idioma almacenado en PlayerPrefs
        int localeID = PlayerPrefs.GetInt("LocaleKey", 0);  // 0 es el valor por defecto
        ChangeLocale(localeID);
    }

    private bool active = false;

    // Método para cambiar el idioma
    public void ChangeLocale(int localeID)
    {
        if (active)
            return;

        StartCoroutine(SetLocale(localeID));
    }

    // Coroutine para cambiar el idioma
    IEnumerator SetLocale(int _localeID)
    {
        active = true;
        yield return LocalizationSettings.InitializationOperation;
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[_localeID];
        PlayerPrefs.SetInt("LocaleKey", _localeID);  // Guardar el idioma en PlayerPrefs
        active = false;
    }

    // Método que se llama cada vez que se carga una nueva escena
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Comprobar si estamos en la escena 5
        if (scene.buildIndex == 5)
        {
            AssignLanguageButtons();
        }
    }

    // Método para asignar los eventos a los botones de idioma
    private void AssignLanguageButtons()
    {
        // Buscar el objeto padre que contiene los botones (supongamos que se llama "LanguageButtons")
        Transform Canvas = GameObject.Find("Canvas")?.transform;

        if (Canvas != null)
        {
            // Buscar los botones "English" y "Spanish" dentro de su objeto padre
            Button englishButton = Canvas.Find("English")?.GetComponent<Button>();
            Button spanishButton = Canvas.Find("Spanish")?.GetComponent<Button>();

            // Si los botones están presentes, asignarles los eventos
            if (englishButton != null)
            {
                englishButton.onClick.RemoveAllListeners();  // Limpiar eventos previos
                englishButton.onClick.AddListener(() => ChangeLocale(0));  // 0 para inglés
            }

            if (spanishButton != null)
            {
                spanishButton.onClick.RemoveAllListeners();  // Limpiar eventos previos
                spanishButton.onClick.AddListener(() => ChangeLocale(1));  // 1 para español
            }
        }
        else
        {
            Debug.LogError("No se encontró el objeto 'LanguageButtons' en la escena.");
        }
    }

    // Asegurarse de desregistrar el evento cuando el objeto sea destruido
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
