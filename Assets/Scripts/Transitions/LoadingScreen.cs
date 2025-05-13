using TMPro;
using UnityEngine;
using System.Collections;

public class LoadingScreen : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI adviceText;
    [SerializeField] private TextMeshProUGUI loadingText;

    [SerializeField] private float changeInterval = 3f; // Tiempo en segundos entre cambios
    [SerializeField] private float fadeDuration = 1f; // Duración del desvanecimiento

    // Array de consejos en inglés
    private string[] consejosIngles = new string[]
    {
        "We're preparing something fun for you!",
        "Take a deep breath... We're almost there!",
        "Loading fun... hang on, champion.",
        "A magical moment is about to begin...",
        "You're amazing. Keep going!",
        "Each puzzle is a new challenge. You can do it!",
        "Preparing new surprises for you.",
        "Ready for a new adventure?",
    };

    // Array de consejos en español
    private string[] consejosEspañol = new string[]
    {
        "¡Estamos preparando algo divertido para ti!",
        "Respira hondo... ¡Ya casi estamos!",
        "Cargando diversión... paciencia, campeón.",
        "Un momento mágico está por empezar...",
        "Eres increíble. ¡Sigue adelante!",
        "Cada puzzle es un nuevo desafío. ¡Tú puedes!",
        "Preparando nuevas sorpresas para ti.",
        "¿Listo para una nueva aventura?",
    };

    private void Start()
    {
        if (adviceText == null || loadingText == null)
        {
            Debug.LogError("No se ha asignado el Text UI en el Inspector.");
            return;
        }

        // Suscribirse al evento de cambio de idioma
        LanguageManager.OnLanguageChanged += UpdateAdviceText;

        // Inicializar los consejos en el idioma actual
        UpdateAdviceText(); // This will call CambiarConsejo with the correct advice array

        StartCoroutine(FadeTextCoroutine(loadingText)); // Inicia el desvanecimiento del texto de carga
    }
    private void UpdateAdviceText()
    {
        // Obtener el idioma del jugador (0 = Inglés, 1 = Español)
        int idioma = PlayerPrefs.GetInt("LocaleKey", 0);  // Valor por defecto es 0 (Inglés)

        // Elige el array de consejos según el idioma
        string[] consejos = idioma == 0 ? consejosIngles : consejosEspañol;

        // Detener la coroutine anterior si está corriendo
        StopCoroutine(CambiarConsejo(consejos));

        // Iniciar la coroutine para cambiar los consejos en el idioma correcto
        StartCoroutine(CambiarConsejo(consejos));
    }
    private IEnumerator CambiarConsejo(string[] consejos)
    {
        while (true)
        {
            // Selecciona un consejo aleatorio del array según el idioma
            adviceText.text = consejos[Random.Range(0, consejos.Length)];

            // Aparece el texto suavemente (opacidad de 0 a 1)
            yield return StartCoroutine(FadeTextCoroutineAdvice(adviceText, 1f)); // Aparece el texto

            // Espera el tiempo definido por 'changeInterval' antes de empezar a desvanecer
            yield return new WaitForSeconds(changeInterval);

            // Desvanece el texto suavemente (opacidad de 1 a 0)
            yield return StartCoroutine(FadeTextCoroutineAdvice(adviceText, 0f)); // Desvanece el texto
        }
    }

    private IEnumerator FadeTextCoroutine(TextMeshProUGUI text)
    {
        float alphaValue = 1f; // Comienza visible
        bool fadingOut = true; // Controla si el texto debe empezar a desvanecerse

        Color startColor = text.color;

        while (true)
        {
            // Si está desvaneciendo, disminuye la opacidad
            if (fadingOut)
            {
                alphaValue -= Time.deltaTime / fadeDuration; // Disminuye la opacidad
                if (alphaValue <= 0f) // Si llega a ser completamente invisible
                {
                    alphaValue = 0f;
                    fadingOut = false; // Cambia a aparecer
                }
            }
            else // Si está apareciendo, aumenta la opacidad
            {
                alphaValue += Time.deltaTime / fadeDuration; // Aumenta la opacidad
                if (alphaValue >= 1f) // Si llega a ser completamente visible
                {
                    alphaValue = 1f;
                    fadingOut = true; // Cambia a desvanecer
                }
            }

            text.color = new Color(startColor.r, startColor.g, startColor.b, alphaValue); // Aplica el valor de opacidad
            yield return null; // Espera un cuadro antes de continuar el bucle
        }
    }

    private IEnumerator FadeTextCoroutineAdvice(TextMeshProUGUI text, float targetAlpha)
    {
        float alphaValue = text.color.a; // Obtiene el valor actual de opacidad
        float timeElapsed = 0f; // Para medir el tiempo de transición

        Color startColor = text.color; // El color inicial del texto

        while (timeElapsed < fadeDuration)
        {
            // Interpolación de opacidad entre el valor actual y el valor objetivo
            alphaValue = Mathf.Lerp(startColor.a, targetAlpha, timeElapsed / fadeDuration);
            text.color = new Color(startColor.r, startColor.g, startColor.b, alphaValue); // Actualiza el color con la nueva opacidad

            timeElapsed += Time.deltaTime; // Acumula el tiempo
            yield return null; // Espera un cuadro antes de continuar
        }

        // Asegura que la opacidad final se ajuste exactamente al valor objetivo
        text.color = new Color(startColor.r, startColor.g, startColor.b, targetAlpha);
    }
}