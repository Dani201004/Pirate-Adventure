using UnityEngine.UI;
using UnityEngine;

public class SlidingPuzzle : MonoBehaviour
{
    public GameObject piecePrefab;  // Prefabricado de la pieza
    public GameObject hiddenObject; // Objeto que se revelará detrás de las piezas

    private GameObject[,] puzzlePieces;
    private int gridSize;
    private int emptyX, emptyY;  // Coordenadas del espacio vacío
    private float spacing = 2f;

    public void CreatePuzzle(int gridSize)
    {
        this.gridSize = gridSize;
        puzzlePieces = new GameObject[gridSize, gridSize];

        // Iniciar espacio vacío en la esquina inferior derecha
        emptyX = gridSize - 1;
        emptyY = gridSize - 1;

        // Crear las piezas del puzzle
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                if (x == emptyX && y == emptyY)
                    continue;  // No crear pieza en el espacio vacío

                Vector3 position = new Vector3(x * spacing, y * spacing, 0);
                GameObject piece = Instantiate(piecePrefab, position, Quaternion.identity);
                piece.name = $"Piece_{x}_{y}";
                puzzlePieces[x, y] = piece;
                piece.GetComponent<Piece>().SetCoordinates(x, y);
                piece.GetComponent<Button>().onClick.AddListener(() => OnPieceClick(x, y));
            }
        }
    }

    private void OnPieceClick(int x, int y)
    {
        // Verificar si la pieza está adyacente al espacio vacío
        if (IsAdjacentToEmpty(x, y))
        {
            MovePiece(x, y);
        }
    }

    private bool IsAdjacentToEmpty(int x, int y)
    {
        return (Mathf.Abs(x - emptyX) == 1 && y == emptyY) || (Mathf.Abs(y - emptyY) == 1 && x == emptyX);
    }

    public void MovePiece(int x, int y)
    {
        // Mover la pieza si está adyacente al espacio vacío
        if (IsAdjacentToEmpty(x, y))
        {
            GameObject pieceToMove = puzzlePieces[x, y];
            puzzlePieces[emptyX, emptyY] = pieceToMove;
            puzzlePieces[x, y] = null;

            pieceToMove.GetComponent<Piece>().SetCoordinates(emptyX, emptyY);
            pieceToMove.transform.position = new Vector3(emptyX * spacing, emptyY * spacing, 0);

            // Actualizar el espacio vacío
            emptyX = x;
            emptyY = y;

            // Lógica para actualizar la visibilidad del objeto oculto
            UpdateObjectVisibility();
        }
    }

    void UpdateObjectVisibility()
    {
        // Aquí puedes añadir la lógica de visibilidad progresiva
    }
}

