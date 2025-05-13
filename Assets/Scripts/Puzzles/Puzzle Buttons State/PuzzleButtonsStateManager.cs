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
        SetGameName(name);
    }

    public void SetGameName(string name)
    {
        // Usamos el nombre guardado en PlayFabProgressManager si está disponible
        gameName = PlayFabProgressManager.Instance.lastGamePlayed;
        Debug.Log("Setting Game Name: " + gameName);  // Aseguramos que se asigna correctamente

        LoadPuzzleState();  // Llamamos para cargar el estado de los puzzles
    }

    private void LoadPuzzleState()
    {
        PlayFabProgressManager.Instance.LoadGameData(gameName);
        StartCoroutine(ApplyPuzzleStatesAfterDelay());
    }

    private IEnumerator<WaitForSeconds> ApplyPuzzleStatesAfterDelay()
    {
        yield return new WaitForSeconds(0.2f);

        HashSet<int> completedPuzzles = PlayFabProgressManager.Instance.completedPuzzles;

        foreach (var entry in puzzleButtons)
        {
            // Cambiar el sprite según si el puzzle está completado o no
            if (entry.statusImage != null)
            {
                bool isCompleted = completedPuzzles.Contains(entry.puzzleID);
                entry.statusImage.sprite = isCompleted ? completedSprite : incompleteSprite;
            }

            // Preparar el botón
            entry.button.onClick.RemoveAllListeners();

            string sceneToLoad = entry.sceneName;
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
