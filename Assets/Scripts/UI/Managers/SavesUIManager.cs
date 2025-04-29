using UnityEngine;

public class SavesUIManager : MonoBehaviour
{

    [Header("Contenedor layout de partidas guardadas")]
    [SerializeField] private Transform savedGamesLayoutGroup;          // Panel donde se mostrarán las partidas guardadas

    [Header("prefab de partida guardada")]
    [SerializeField] private GameObject savedGamePrefab;          // Prefab para cada entrada en la lista

    // Propiedades públicas de solo lectura
    // === Lista de partidas guardadas ===
    public Transform SavedGamesLayoutGroup => savedGamesLayoutGroup;
    public GameObject SavedGamePrefab => savedGamePrefab;

}
