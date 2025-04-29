using UnityEngine;

public class SavesUIManager : MonoBehaviour
{

    [Header("Contenedor layout de partidas guardadas")]
    [SerializeField] private Transform savedGamesLayoutGroup;          // Panel donde se mostrar�n las partidas guardadas

    [Header("prefab de partida guardada")]
    [SerializeField] private GameObject savedGamePrefab;          // Prefab para cada entrada en la lista

    // Propiedades p�blicas de solo lectura
    // === Lista de partidas guardadas ===
    public Transform SavedGamesLayoutGroup => savedGamesLayoutGroup;
    public GameObject SavedGamePrefab => savedGamePrefab;

}
