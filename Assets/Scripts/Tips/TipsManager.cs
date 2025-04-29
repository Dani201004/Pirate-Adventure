using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TipsManager : MonoBehaviour
{
    [Header("Titulos consejos")]
    [SerializeField] private TextMeshProUGUI tipTitle1Text;
    [SerializeField] private TextMeshProUGUI tipTitle2Text;
    [SerializeField] private TextMeshProUGUI tipTitle3Text;

    [Header("Textos de contenido de los consejos")]
    [SerializeField] private TextMeshProUGUI tip1Text;
    [SerializeField] private TextMeshProUGUI tip2Text;
    [SerializeField] private TextMeshProUGUI tip3Text;


    [Header("Botones de navegacion")]
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;

    private List<string> tipsES = new List<string>
    {
        "Acompaña a tu hijo mientras juega al principio. Al participar activamente en sus primeras sesiones, puedes orientarlo sin resolverle todo, fomentar su autonomía y observar cómo se enfrenta a los desafíos.",
        "Fomenta el pensamiento crítico con preguntas abiertas. En vez de dar respuestas, haz preguntas como: ¿Por qué crees que eso no funcionó? o ¿Qué podrías intentar ahora? Esto estimula la reflexión y la toma de decisiones.",
        "Celebra el esfuerzo más que el resultado. Reconocer el intento, la perseverancia o la creatividad ayuda a construir una mentalidad de crecimiento y les enseña que equivocarse es parte del proceso.",
        "Aprovecha los errores como oportunidades de aprendizaje. Si algo no funciona, anímalo a probar otra estrategia. Evita corregirlo de inmediato y permítele experimentar para encontrar soluciones por sí mismo.",
        "Establece momentos específicos para jugar. El juego puede ser parte de una rutina equilibrada. Evita que se convierta en una actividad sin control marcando horarios y combinándolo con otras actividades físicas y sociales.",
        "Participa activamente en sus progresos. Interésate por lo que tu hijo ha aprendido o descubierto. Puedes pedirle que te explique lo que hizo o lo que más le gustó. Esto fortalece su comunicación y autoestima."
    };

    private List<string> tipsEN = new List<string>
    {
        "Play alongside your child in the beginning. Being present in early gameplay allows you to guide them without giving away solutions, support their independence, and see how they approach challenges.",
        "Encourage critical thinking with open-ended questions. Instead of giving answers, ask things like: Why do you think that didn’t work? or What else could you try? This promotes reflection and decision-making.",
        "Celebrate effort over results. Acknowledge perseverance, creativity, and problem-solving. This helps build a growth mindset and teaches that making mistakes is part of learning.",
        "Use mistakes as opportunities for learning. When something goes wrong, encourage your child to try a new approach. Avoid stepping in too quickly—let them explore and discover alternatives.",
        "Set specific times for gameplay. Gameplay can be part of a balanced routine. Prevent it from becoming a limitless activity by setting clear timeframes and combining it with physical and social activities.",
        "Engage with their progress actively. Show interest in what your child has learned or discovered. Ask them to explain what they did or what they enjoyed most. This boosts communication and self-esteem."
    };

    private List<string> tipTitlesES = new List<string>
    {
        "Consejo 1", "Consejo 2", "Consejo 3", "Consejo 4", "Consejo 5", "Consejo 6"
    };

    private List<string> tipTitlesEN = new List<string>
    {
        "Tip 1", "Tip 2", "Tip 3", "Tip 4", "Tip 5", "Tip 6"
    };

    private List<string> currentTips;
    private List<string> currentTitles;
    private int currentIndex = 0;

    private void Start()
    {
        string language = GetLanguageFromLocaleKey();

        currentTips = (language == "en") ? tipsEN : tipsES;
        currentTitles = (language == "en") ? tipTitlesEN : tipTitlesES;

        UpdateTips();

        leftButton.onClick.AddListener(ShowPrevious);
        rightButton.onClick.AddListener(ShowNext);
    }

    private string GetLanguageFromLocaleKey()
    {
        int localeID = PlayerPrefs.GetInt("LocaleKey", 0); // 0: inglés, 1: español

        switch (localeID)
        {
            case 1:
                return "es";
            default:
                return "en";
        }
    }

    private void UpdateTips()
    {
        tipTitle1Text.text = GetTitle(currentIndex);
        tip1Text.text = GetTip(currentIndex);

        tipTitle2Text.text = GetTitle(currentIndex + 1);
        tip2Text.text = GetTip(currentIndex + 1);

        tipTitle3Text.text = GetTitle(currentIndex + 2);
        tip3Text.text = GetTip(currentIndex + 2);

        leftButton.gameObject.SetActive(currentIndex > 0);
        rightButton.gameObject.SetActive(currentIndex + 3 < currentTips.Count);
    }

    private string GetTip(int index)
    {
        return (index >= 0 && index < currentTips.Count) ? currentTips[index] : "";
    }

    private string GetTitle(int index)
    {
        return (index >= 0 && index < currentTitles.Count) ? currentTitles[index] : "";
    }

    private void ShowPrevious()
    {
        if (currentIndex >= 3)
        {
            currentIndex -= 3;
            UpdateTips();
        }
    }

    private void ShowNext()
    {
        if (currentIndex + 3 < currentTips.Count)
        {
            currentIndex += 3;
            UpdateTips();
        }
    }
}
