using UnityEngine;

public class ChestShake : MonoBehaviour
{
    public float shakeAmount = 0.1f;  // Cantidad de la sacudida
    public float shakeDuration = 1f;  // Duración de la sacudida
    private Vector3 originalPosition;  // Posición original del objeto
    private float shakeTime = 0f;  // Tiempo restante de la sacudida

    void Start()
    {
        // Guarda la posición original del objeto
        originalPosition = transform.position;
    }

    void Update()
    {
        // Si la sacudida está activa
        if (shakeTime > 0)
        {
            // Calcula el desplazamiento aleatorio en los ejes X, Y y Z
            Vector3 shakeOffset = new Vector3(
                Random.Range(-shakeAmount, shakeAmount),
                Random.Range(-shakeAmount, shakeAmount),
                Random.Range(-shakeAmount, shakeAmount)
            );

            // Aplica el desplazamiento al objeto
            transform.position = originalPosition + shakeOffset;

            // Reduce el tiempo restante de la sacudida
            shakeTime -= Time.deltaTime;
        }
        else
        {
            // Si ya no está en sacudida, vuelve a la posición original
            transform.position = originalPosition;
        }
    }

    // Función para iniciar la sacudida
    public void StartShake()
    {
        shakeTime = shakeDuration;
    }
}
