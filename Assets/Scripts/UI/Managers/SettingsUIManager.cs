using UnityEngine;
using UnityEngine.UI;


public class SettingsUIManager : MonoBehaviour
{
    [Header("Botones para cambiar de idioma")]
    [SerializeField] private Button englishButton;
    [SerializeField] private Button spanishButton;

    [Header("Inputs para el sonido del juego")]
    [SerializeField] private Slider effectsSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Toggle musicToggle;


    // Propiedades públicas de solo lectura
    public Button EnglishButton => englishButton;
    public Button SpanishButton => spanishButton;

    public Slider EffectsSlider => effectsSlider;
    public Slider MusicSlider => musicSlider;
    public Toggle MusicToggle => musicToggle;

}
