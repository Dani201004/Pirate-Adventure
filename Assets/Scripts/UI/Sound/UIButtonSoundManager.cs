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
        Button[] allButtons = Resources.FindObjectsOfTypeAll<Button>();

        int count = 0;
        foreach (Button btn in allButtons)
        {
            if (btn.gameObject.hideFlags == HideFlags.NotEditable || btn.gameObject.hideFlags == HideFlags.HideAndDontSave)
                continue;

            // Evitar botones que no estén en la escena activa
            if (!btn.gameObject.scene.IsValid() || !btn.gameObject.scene.isLoaded)
                continue;

            btn.onClick.RemoveListener(PlayClickSound);
            btn.onClick.AddListener(PlayClickSound);
            count++;
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
