using UnityEngine;

public class ChestShake : MonoBehaviour
{
    public float shakeAmount = 0.1f;  // Cantidad de la sacudida
    public float shakeDuration = 1f;  // Duraci�n de la sacudida
    private Vector3 originalPosition;  // Posici�n original del objeto
    private float shakeTime = 0f;  // Tiempo restante de la sacudida

    void Start()
    {
        // Guarda la posici�n original del objeto
        originalPosition = transform.position;
    }

    void Update()
    {
        // Si la sacudida est� activa
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
            // Si ya no est� en sacudida, vuelve a la posici�n original
            transform.position = originalPosition;
        }
    }

    // Funci�n para iniciar la sacudida
    public void StartShake()
    {
        shakeTime = shakeDuration;
    }
}
