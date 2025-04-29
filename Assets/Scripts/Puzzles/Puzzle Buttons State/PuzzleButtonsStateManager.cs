using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PuzzleButtonsStateManager : MonoBehaviour
{
    [System.Serializable]
    public class PuzzleButtonData
    {
        public Button button;              // Botón del puzzle
        public int puzzleID;              // Índice del puzzle
        public string sceneName;          // Nombre de la escena del puzzle
        public Image statusImage;         // Imagen para mostrar estado (puedes arrastrar un hijo del botón aquí)
    }

    public List<PuzzleButtonData> puzzleButtons;
    public Sprite completedSprite;
    public Sprite incompleteSprite;

    [SerializeField] private string gameName;

    public static PuzzleButtonsStateManager Instance;

    private void Awake()
    {
        // Si ya existe una instancia, destruye esta.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Establece la instancia a esta
        Instance = this;
    }
    private void Start()
    {
        StartCoroutine(WaitForProgressManager());
    }

    private IEnumerator<WaitUntil> WaitForProgressManager()
    {
        yield return new WaitUntil(() => PlayFabProgressManager.Instance != null);
        LoadPuzzleState();
    }

    public void SetGameName(string name)
    {
        gameName = name;
        Debug.Log("Setting Game Name: " + gameName); // Añadido para depurar si el valor se asigna
        LoadPuzzleState();
    }

    private void LoadPuzzleState()
    {
        PlayFabProgressManager.Instance.LoadGameData(gameName);
        StartCoroutine(ApplyPuzzleStatesAfterDelay(1f));
    }

    private IEnumerator<WaitForSeconds> ApplyPuzzleStatesAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        int progress = PlayFabProgressManager.Instance.puzzleProgress;

        foreach (var entry in puzzleButtons)
        {
            // Cambiar el sprite según el progreso
            if (entry.statusImage != null)
            {
                bool isCompleted = entry.puzzleID < progress;
                entry.statusImage.sprite = isCompleted ? completedSprite : incompleteSprite;
            }

            // Preparar el botón
            entry.button.onClick.RemoveAllListeners();

            string sceneToLoad = entry.sceneName; // capturar por cierre
            int puzzleID = entry.puzzleID;

            entry.button.onClick.AddListener(() =>
            {
                PlayFabProgressManager.Instance.SetCurrentGame(gameName);
                PlayFabProgressManager.Instance.SetCurrentPuzzle(puzzleID);

                SceneTransition.Instance.LoadLevelCorrespondingPuzzle(sceneToLoad);
            });
        }
    }
}
