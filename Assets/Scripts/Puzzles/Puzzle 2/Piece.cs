using UnityEngine;

public class Piece : MonoBehaviour
{
    private int xCoordinate;
    private int yCoordinate;

    // Establecer las coordenadas de la pieza
    public void SetCoordinates(int x, int y)
    {
        xCoordinate = x;
        yCoordinate = y;
    }

    // Obtener las coordenadas de la pieza
    public Vector2 GetCoordinates()
    {
        return new Vector2(xCoordinate, yCoordinate);
    }
}
