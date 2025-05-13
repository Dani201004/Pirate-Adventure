using UnityEngine;

public class Piece : MonoBehaviour
{
    private Vector2Int currentCoord;
    private Vector2Int correctCoord;

    // Propiedad para almacenar un identificador único de la pieza
    public int Id { get; private set; }

    // Método para asignar un ID único a la pieza
    public void SetId(int id)
    {
        Id = id;
    }
    public void SetCurrentCoordinates(int x, int y)
    {
        currentCoord = new Vector2Int(x, y);
    }

    public void UpdateCurrentCoordinates(int x, int y)
    {
        currentCoord = new Vector2Int(x, y);
    }

    public Vector2Int GetCurrentCoordinates()
    {
        return currentCoord;
    }

    public void SetCorrectCoordinates(int x, int y)
    {
        correctCoord = new Vector2Int(x, y);
    }

    public Vector2Int GetCorrectCoordinates()
    {
        return correctCoord;
    }
}
