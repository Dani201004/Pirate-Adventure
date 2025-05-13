using UnityEngine.UI;
using UnityEngine;
using System;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;

public class SlidingPuzzle : MonoBehaviour
{
    [Header("Principal Components")]
    [SerializeField] private GameObject piecePrefab;
    [SerializeField] private GameObject hiddenObject;

    [Header("Hidden Object Position")]
    [SerializeField] private GameObject hiddenObjectPosition;

    [Header("Sound Settings")]
    [SerializeField] private AudioClip[] easySounds;
    [SerializeField] private AudioClip[] mediumSounds;
    [SerializeField] private AudioClip[] hardSounds;
    [SerializeField] private AudioSource audioSource;

    private AudioClip[] currentMoveSounds;

    [Header("Puzzle Settings")]
    [SerializeField] private Vector2 cellSize = new Vector2(120, 120);
    [SerializeField] private float spacing = 4f;
    [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.5f);

    [Header("Sprite")]
    [SerializeField] private Texture2D sourceImage;

    [Header("Sprite por dificultad")]
    [SerializeField] private Texture2D easyImage;
    [SerializeField] private Texture2D mediumImage;
    [SerializeField] private Texture2D hardImage;

    private GameObject[,] puzzlePieces;

    private Vector2Int? selectedPiece = null;

    private bool hasMovedFirstPiece = false;

    private bool puzzleEnded = false;

    [Header("Dialogue system")]
    [SerializeField] private DialogueSystem dialogueSystem;  // Instancia de DialogueSystem

    private int puzzleID = 2;

    private GameObject puzzleContainer;

    public int totalPieceCount;  // Variable para almacenar el recuento de piezas

    private int rows;
    private int cols;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }
    public void SetImageByDifficulty(DifficultyLevel level)
    {
        switch (level)
        {
            case DifficultyLevel.Easy:
                sourceImage = easyImage;
                currentMoveSounds = easySounds;
                break;
            case DifficultyLevel.Medium:
                sourceImage = mediumImage;
                currentMoveSounds = mediumSounds;
                break;
            case DifficultyLevel.Hard:
                sourceImage = hardImage;
                currentMoveSounds = hardSounds;
                break;
        }
    }
    private Sprite[,] SliceImage(Texture2D image, int rows, int cols)
    {
        Sprite[,] pieces = new Sprite[rows, cols];

        int pieceWidth = image.width / cols;
        int pieceHeight = image.height / rows;

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                Rect rect = new Rect(x * pieceWidth, (rows - 1 - y) * pieceHeight, pieceWidth, pieceHeight);
                Sprite piece = Sprite.Create(image, rect, new Vector2(0.5f, 0.5f));
                pieces[y, x] = piece;
            }
        }

        return pieces;
    }
    public void CreatePuzzle(bool[,] customShape)
    {
        int rows = customShape.GetLength(0);
        int cols = customShape.GetLength(1);

        this.rows = rows;  // Asignar las filas
        this.cols = cols;  // Asignar las columnas

        puzzlePieces = new GameObject[rows, cols];

        Sprite[,] slicedSprites = SliceImage(sourceImage, rows, cols);

        GameObject canvas = GameObject.Find("Level 2 Grid");
        if (canvas == null)
        {
            Debug.LogError("Level 2 Grid no encontrado.");
            return;
        }

        puzzleContainer = new GameObject("PuzzleContainer");
        puzzleContainer.transform.SetParent(canvas.transform, false);

        RectTransform rectTransform = puzzleContainer.AddComponent<RectTransform>();
        rectTransform.anchorMin = rectTransform.anchorMax = rectTransform.pivot = new Vector2(0.5f, 0.5f);

        float totalWidth = cols * (cellSize.x + spacing);
        float totalHeight = rows * (cellSize.y + spacing);
        rectTransform.sizeDelta = new Vector2(totalWidth, totalHeight);
        rectTransform.anchoredPosition = Vector2.zero;

        Image bgImage = puzzleContainer.AddComponent<Image>();
        bgImage.color = backgroundColor;

        // Crear lista ordenada de piezas
        List<PuzzlePieceData> pieceDataList = new List<PuzzlePieceData>();
        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < cols; y++)
            {
                if (!customShape[x, y]) continue;
                pieceDataList.Add(new PuzzlePieceData(slicedSprites[x, y], new Vector2Int(x, y)));
            }
        }

        // Mezclar la lista
        System.Random rng = new System.Random();
        int n = pieceDataList.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            var temp = pieceDataList[k];
            pieceDataList[k] = pieceDataList[n];
            pieceDataList[n] = temp;
        }

        int dataIndex = 0;
        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < cols; y++)
            {
                if (!customShape[x, y]) continue;

                GameObject piece = Instantiate(piecePrefab, puzzleContainer.transform);
                piece.name = $"Piece_{x}_{y}";
                puzzlePieces[x, y] = piece;

                RectTransform pieceRect = piece.GetComponent<RectTransform>();
                pieceRect.sizeDelta = cellSize;
                pieceRect.anchorMin = pieceRect.anchorMax = pieceRect.pivot = new Vector2(0, 1);
                pieceRect.anchoredPosition = new Vector2(y * (cellSize.x + spacing), -x * (cellSize.y + spacing));

                var data = pieceDataList[dataIndex++];
                Image image = piece.GetComponent<Image>();
                if (image != null)
                {
                    image.sprite = data.sprite;
                }

                Piece pieceScript = piece.GetComponent<Piece>();
                pieceScript.SetCurrentCoordinates(x, y);
                pieceScript.SetCorrectCoordinates(data.correctPosition.x, data.correctPosition.y);

                // Asignar un ID único a cada pieza
                pieceScript.SetId(x * cols + y);

                piece.GetComponent<Button>()
                    .onClick
                    .AddListener(() => OnPieceClick(pieceScript));
            }
        }

        // Guardar el recuento total de piezas
        totalPieceCount = TotalPieceCount();  // Actualiza el recuento de piezas
        Debug.Log("Total de piezas: " + totalPieceCount);  // Mostrar en consola
    }

    private void OnPieceClick(Piece clicked)
    {
        if (puzzleEnded) return;

        Vector2Int clickedCoordinates = clicked.GetCurrentCoordinates();

        // If the same piece is clicked, deselect it
        if (selectedPiece.HasValue && selectedPiece.Value == clickedCoordinates)
        {
            selectedPiece = null;
            return;
        }

        if (selectedPiece.HasValue)
        {
            // Swap the pieces
            Piece selectedPieceObject = puzzlePieces[selectedPiece.Value.x, selectedPiece.Value.y].GetComponent<Piece>();
            SwapPieces(selectedPieceObject, clicked);
            selectedPiece = null;
        }
        else
        {
            // Select the piece
            selectedPiece = clickedCoordinates;
        }
    }
    private void SwapPieces(Piece a, Piece b)
    {
        var from = a.GetCurrentCoordinates();
        var to = b.GetCurrentCoordinates();

        // swap in matrix
        puzzlePieces[to.x, to.y] = a.gameObject;
        puzzlePieces[from.x, from.y] = b.gameObject;

        // update their internal coords
        a.UpdateCurrentCoordinates(to.x, to.y);
        b.UpdateCurrentCoordinates(from.x, from.y);

        // move visually
        var aRT = a.GetComponent<RectTransform>();
        var bRT = b.GetComponent<RectTransform>();
        aRT.anchoredPosition = new Vector2(to.y * (cellSize.x + spacing), -to.x * (cellSize.y + spacing));
        bRT.anchoredPosition = new Vector2(from.y * (cellSize.x + spacing), -from.x * (cellSize.y + spacing));

        if (!hasMovedFirstPiece)
        {
            hasMovedFirstPiece = true;
            if (!DialogueFlags.Instance.HasShownFirstSuccessDialogue(puzzleID))
            {
                DialogueFlags.Instance.SetFirstSuccessDialogueShown(puzzleID);
                dialogueSystem?.StartTemporaryDialogue(dialogueSystem.successFirstTimeLines);
            }
        }

        if (currentMoveSounds != null && currentMoveSounds.Length > 0)
        {
            audioSource.PlayOneShot(currentMoveSounds[UnityEngine.Random.Range(0, currentMoveSounds.Length)]);
        }

        CheckIfPuzzleComplete();

        // Sincronizar el estado después de un movimiento
        SyncPuzzleState();
    }

    private void CheckIfPuzzleComplete()
    {
        for (int x = 0; x < puzzlePieces.GetLength(0); x++)
        {
            for (int y = 0; y < puzzlePieces.GetLength(1); y++)
            {
                var go = puzzlePieces[x, y];
                if (go == null) continue;
                var pc = go.GetComponent<Piece>();
                if (pc.GetCurrentCoordinates() != pc.GetCorrectCoordinates())
                    return;
            }
        }

        puzzleEnded = true;

        Instantiate(hiddenObject, hiddenObjectPosition.transform.position, Quaternion.identity);

        SlidingPuzzleManager.Instance.EndPuzzle();
    }
    // Método para sincronizar el estado del puzzle (posiciones de las piezas) entre los jugadores
    private void SyncPuzzleState()
    {
        string puzzleState = SerializePuzzleState();

        // Usar PlayFab o sistema de red para enviar el estado actualizado
        PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { "puzzleState", puzzleState }
            }
        }, result => { }, error => { Debug.LogError(error.GenerateErrorReport()); });
    }

    public string SerializePuzzleState()
    {
        List<int> pieceIds = new List<int>();

        if (puzzleContainer == null)
        {
            Debug.LogError("PuzzleContainer no está asignado.");
            return "";
        }

        foreach (Transform child in puzzleContainer.transform)
        {
            Piece piece = child.GetComponent<Piece>();
            if (piece != null)
            {
                pieceIds.Add(piece.Id);
            }
        }

        return string.Join("|", pieceIds);
    }
    public void LoadPuzzleFromString(string data)
    {
        string[] ids = data.Split('|');
        List<Piece> allPieces = new List<Piece>(GetComponentsInChildren<Piece>());

        if (ids.Length != allPieces.Count)
        {
            Debug.LogError($"Error al cargar el puzzle. El número de piezas no coincide. Piezas: {allPieces.Count}, IDs: {ids.Length}");
            return;
        }

        Dictionary<int, Piece> idToPieceMap = new Dictionary<int, Piece>();
        foreach (Piece piece in allPieces)
        {
            idToPieceMap[piece.Id] = piece;
        }

        int rows = puzzlePieces.GetLength(0);
        int cols = puzzlePieces.GetLength(1);
        int index = 0;

        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < cols; y++)
            {
                if (puzzlePieces[x, y] == null) continue;

                int id = int.Parse(ids[index++]);
                if (idToPieceMap.TryGetValue(id, out Piece piece))
                {
                    piece.UpdateCurrentCoordinates(x, y);
                    RectTransform rect = piece.GetComponent<RectTransform>();
                    rect.anchoredPosition = new Vector2(y * (cellSize.x + spacing), -x * (cellSize.y + spacing));

                    puzzlePieces[x, y] = piece.gameObject;
                }
            }
        }
    }
    public int TotalPieceCount()
    {
        int total = 0;
        for (int x = 0; x < rows; x++)  // rows es ahora accesible aquí
        {
            for (int y = 0; y < cols; y++)  // cols es ahora accesible aquí
            {
                if (puzzlePieces[x, y] != null) // Verificar que la pieza no sea nula
                {
                    total++;
                }
            }
        }
        return total;
    }

}

[Serializable]
public class PuzzlePieceData
{
    public Sprite sprite;
    public Vector2Int correctPosition;

    public PuzzlePieceData(Sprite sprite, Vector2Int correctPosition)
    {
        this.sprite = sprite;
        this.correctPosition = correctPosition;
    }
}
