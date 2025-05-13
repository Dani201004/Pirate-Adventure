using System.Collections;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using System.Globalization;
using System;

public class LanguageManager : MonoBehaviour
{
    public static LanguageManager _instance;

    // Crear un evento para notificar a los dem�s scripts cuando el idioma cambia
    public static event Action OnLanguageChanged;

    public static int CurrentLanguage { get; private set; } // Propiedad est�tica para acceder al idioma actual

    private bool active = false;

    [Header("Referencia al administrador de gameobjects del apartado settings")]
    [SerializeField] private SettingsUIManager settingsUIManager;

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
        // Comprobar si ya existe una preferencia de idioma guardada en PlayerPrefs
        int storedLocaleID = PlayerPrefs.GetInt("LocaleKey", -1); // Valor -1 si no existe preferencia

        if (storedLocaleID == -1)
        {
            // Si no hay preferencia guardada, usa el idioma del dispositivo
            string deviceLanguage = GetDeviceLanguage();
            int localeID = GetLocaleIDFromLanguage(deviceLanguage);
            ChangeLocale(localeID); // Cambiar al idioma del dispositivo
        }
        else
        {
            // Si existe preferencia guardada, usarla
            ChangeLocale(storedLocaleID);
        }
    }

    // M�todo para cambiar el idioma
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

        // Llamar al evento para que los dem�s scripts reciban la notificaci�n
        OnLanguageChanged?.Invoke();
    }

    // M�todo que se llama cada vez que se carga una nueva escena
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Comprobar si estamos en la escena 5
        if (scene.buildIndex == 5)
        {
            StartCoroutine(FindSettingsUIManagerAfterDelay());
        }
    }

    private IEnumerator FindSettingsUIManagerAfterDelay()
    {
        // Espera un peque�o retraso para asegurarse de que los objetos est�n activos
        yield return new WaitForSeconds(0.1f);

        GameObject settingsObject = GameObject.Find("SettingsUIManager");

        if (settingsObject != null)
        {
            settingsUIManager = settingsObject.GetComponent<SettingsUIManager>();
        }
        else
        {
            Debug.LogWarning("No se encontr� el objeto 'SettingsUIManager' en la escena.");
        }

        yield return new WaitForSeconds(0.2f);

        settingsUIManager.EnglishButton.onClick.RemoveAllListeners();  // Limpiar eventos previos
        settingsUIManager.EnglishButton.onClick.AddListener(() => ChangeLocale(0));  // 0 para ingl�s

        settingsUIManager.SpanishButton.onClick.RemoveAllListeners();  // Limpiar eventos previos
        settingsUIManager.SpanishButton.onClick.AddListener(() => ChangeLocale(1));  // 1 para espa�ol

    }

    // Asegurarse de desregistrar el evento cuando el objeto sea destruido
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // M�todo para obtener el idioma del dispositivo
    private string GetDeviceLanguage()
    {
        return CultureInfo.CurrentCulture.TwoLetterISOLanguageName; // Obtiene el c�digo de idioma en formato de dos letras (es, en, etc.)
    }

    // M�todo para mapear el idioma a un ID de locale (0 para ingl�s, 1 para espa�ol)
    private int GetLocaleIDFromLanguage(string language)
    {
        switch (language)
        {
            case "en":
                return 0; // Ingl�s
            case "es":
                return 1; // Espa�ol
            default:
                return 0; // Ingl�s por defecto si el idioma no est� soportado
        }
    }
}
