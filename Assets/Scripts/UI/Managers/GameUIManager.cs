using UnityEngine;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    [Header("Boton para ir al apartado social")]
    [SerializeField] private Button socialButton;

    [Header("Boton para volver al men� principal")]
    [SerializeField] private Button exitButton;

    // Propiedades p�blicas de solo lectura
    // === Botones principales y configuraci�n ===
    public Button SocialButton => socialButton;

    public Button ExitButton => exitButton;
}
