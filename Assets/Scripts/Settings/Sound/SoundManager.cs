using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using System.Collections;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Referencia al administrador de gameobjects del apartado settings")]
    [SerializeField] private SettingsUIManager settingsUIManager;

    [SerializeField] private AudioMixer mixer;
    [SerializeField] private AudioSource musicSource;

    private const string MusicVolumeKey = "musicVolume";
    private const string EffectsVolumeKey = "effectsVolume";
    private const float DefaultVolume = 1f;

    private void Awake()
    {
        // Configuración de Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

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
        // Espera un pequeño retraso para asegurarse de que los objetos estén activos
        yield return new WaitForSeconds(0.1f);

        GameObject settingsObject = GameObject.Find("SettingsUIManager");

        if (settingsObject != null)
        {
            settingsUIManager = settingsObject.GetComponent<SettingsUIManager>();
        }
        else
        {
            Debug.LogWarning("No se encontró el objeto 'SettingsUIManager' en la escena.");
        }

        yield return new WaitForSeconds(0.2f);

        settingsUIManager.MusicSlider.onValueChanged.RemoveAllListeners();
        settingsUIManager.MusicSlider.onValueChanged.AddListener(OnMusicSliderChanged);

        settingsUIManager.EffectsSlider.onValueChanged.RemoveAllListeners();
        settingsUIManager.EffectsSlider.onValueChanged.AddListener(OnEffectsSliderChanged);

        settingsUIManager.MusicToggle.onValueChanged.RemoveAllListeners();
        settingsUIManager.MusicToggle.onValueChanged.AddListener(OnMusicToggleChanged);

        // Sincronizar la UI con los valores guardados
        LoadVolume();
        if (settingsUIManager.MusicSlider != null)
            OnMusicSliderChanged(settingsUIManager.MusicSlider.value);
        if (settingsUIManager.EffectsSlider != null)
            OnEffectsSliderChanged(settingsUIManager.EffectsSlider.value);
    }
    public void OnMusicSliderChanged(float value)
    {
        // Si el toggle está apagado, se silencia la música
        if (settingsUIManager.MusicToggle != null && !settingsUIManager.MusicToggle.isOn)
        {
            mixer.SetFloat("music", -80f);
            return;
        }

        if (value <= 0.0001f)
        {
            mixer.SetFloat("music", -80f);
        }
        else
        {
            mixer.SetFloat("music", Mathf.Log10(value) * 20);
        }
        PlayerPrefs.SetFloat(MusicVolumeKey, value);
    }

    public void OnEffectsSliderChanged(float value)
    {
        if (value <= 0.0001f)
        {
            mixer.SetFloat("effects", -80f);
        }
        else
        {
            mixer.SetFloat("effects", Mathf.Log10(value) * 20);
        }
        PlayerPrefs.SetFloat(EffectsVolumeKey, value);
    }

    public void OnMusicToggleChanged(bool isOn)
    {
        if (isOn)
        {
            if (settingsUIManager.MusicSlider != null)
                OnMusicSliderChanged(settingsUIManager.MusicSlider.value);
        }
        else
        {
            mixer.SetFloat("music", -80f);
        }
    }

    private void LoadVolume()
    {
        if (!PlayerPrefs.HasKey(MusicVolumeKey))
            PlayerPrefs.SetFloat(MusicVolumeKey, DefaultVolume);

        if (!PlayerPrefs.HasKey(EffectsVolumeKey))
            PlayerPrefs.SetFloat(EffectsVolumeKey, DefaultVolume);

        if (settingsUIManager.MusicSlider != null)
            settingsUIManager.MusicSlider.value = PlayerPrefs.GetFloat(MusicVolumeKey, DefaultVolume);

        if (settingsUIManager.EffectsSlider != null)
            settingsUIManager.EffectsSlider.value = PlayerPrefs.GetFloat(EffectsVolumeKey, DefaultVolume);
    }
}
