using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    [Header("Boton para ir al apartado social")]
    [SerializeField] private Button socialButton;

    [Header("Boton para volver al menú principal")]
    [SerializeField] private Button exitButton;

    [Header("Texto con la cantidad de gemas")]
    [SerializeField] private TMP_Text gemsAmountText;

    [Header("Texto con la cantidad de gemas mágicas")]
    [SerializeField] private TMP_Text magicGemsAmountText;

    [Header("Texto con la cantidad de trofeos plateados")]
    [SerializeField] private TMP_Text silverTrophiesAmountText;

    [Header("Texto con la cantidad de trofeos dorados")]
    [SerializeField] private TMP_Text goldenTrophiesAmountText;

    // Propiedades públicas de solo lectura
    // === Botones principales y configuración ===
    public Button SocialButton => socialButton;

    public Button ExitButton => exitButton;

    // === Textos para la cantidad de gemas ===
    public TMP_Text GemsAmountText => gemsAmountText;

    public TMP_Text MagicGemsAmountText => magicGemsAmountText;

    // === Textos para la cantidad de trofeos ===
    public TMP_Text SilverTrophiesAmountText => silverTrophiesAmountText;

    public TMP_Text GoldenTrophiesAmountText => goldenTrophiesAmountText;
}
