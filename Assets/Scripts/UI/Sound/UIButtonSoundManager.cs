using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class UIButtonSoundManager : MonoBehaviour
{
    public static UIButtonSoundManager Instance;

    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioSource audioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AssignSoundToAllButtons();
    }

    private void AssignSoundToAllButtons()
    {
        List<Button> allButtons = new List<Button>();
        Canvas[] allCanvases = Resources.FindObjectsOfTypeAll<Canvas>();

        foreach (Canvas canvas in allCanvases)
        {
            if (canvas.gameObject.hideFlags == HideFlags.NotEditable || canvas.gameObject.hideFlags == HideFlags.HideAndDontSave)
                continue;

            Button[] buttons = canvas.GetComponentsInChildren<Button>(true); // true incluye inactivos
            foreach (Button btn in buttons)
            {
                btn.onClick.RemoveListener(PlayClickSound); // Evita duplicados
                btn.onClick.AddListener(PlayClickSound);
                allButtons.Add(btn);
            }
        }

        //Debug.Log($"Botones actualizados: {allButtons.Count}");
    }

    private void PlayClickSound()
    {
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
}
