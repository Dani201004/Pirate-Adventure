using UnityEngine;

public class GemRise : MonoBehaviour
{
    public float riseDistance = 1.0f;      // Distancia que sube
    public float riseDuration = 1.0f;      // Tiempo de subida
    public float delayBeforeDestroy = 2f;  // Tiempo antes de destruir (opcional)
    public float rotationSpeed = 180f;     // Velocidad de giro en grados por segundo

    private Vector3 startPosition;
    private Vector3 endPosition;
    private float elapsedTime = 0f;

    private void Start()
    {
        startPosition = transform.position;
        endPosition = startPosition + Vector3.up * riseDistance;

        if (delayBeforeDestroy > 0f)
        {
            Destroy(gameObject, delayBeforeDestroy);
        }
    }

    private void Update()
    {
        // Movimiento ascendente
        if (elapsedTime < riseDuration)
        {
            transform.position = Vector3.Lerp(startPosition, endPosition, elapsedTime / riseDuration);
            elapsedTime += Time.deltaTime;
        }
        else
        {
            transform.position = endPosition;
        }

        // Rotación constante
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);
    }
}
