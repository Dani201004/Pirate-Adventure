using UnityEngine;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    [Header("Boton para ir al apartado social")]
    [SerializeField] private Button socialButton;

    [Header("Boton para volver al menú principal")]
    [SerializeField] private Button exitButton;

    // Propiedades públicas de solo lectura
    // === Botones principales y configuración ===
    public Button SocialButton => socialButton;

    public Button ExitButton => exitButton;
}
