using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    [Header("Boton para ir al apartado social")]
    [SerializeField] private Button socialButton;

    [Header("Boton para volver al men� principal")]
    [SerializeField] private Button exitButton;

    [Header("Texto con la cantidad de gemas")]
    [SerializeField] private TMP_Text gemsAmountText;

    [Header("Texto con la cantidad de gemas m�gicas")]
    [SerializeField] private TMP_Text magicGemsAmountText;

    [Header("Texto con la cantidad de trofeos plateados")]
    [SerializeField] private TMP_Text silverTrophiesAmountText;

    [Header("Texto con la cantidad de trofeos dorados")]
    [SerializeField] private TMP_Text goldenTrophiesAmountText;

    // Propiedades p�blicas de solo lectura
    // === Botones principales y configuraci�n ===
    public Button SocialButton => socialButton;

    public Button ExitButton => exitButton;

    // === Textos para la cantidad de gemas ===
    public TMP_Text GemsAmountText => gemsAmountText;

    public TMP_Text MagicGemsAmountText => magicGemsAmountText;

    // === Textos para la cantidad de trofeos ===
    public TMP_Text SilverTrophiesAmountText => silverTrophiesAmountText;

    public TMP_Text GoldenTrophiesAmountText => goldenTrophiesAmountText;
}
