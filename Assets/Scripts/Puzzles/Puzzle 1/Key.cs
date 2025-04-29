using UnityEngine;

public class Key : MonoBehaviour
{
    [SerializeField] public string Color;  // Color de la llave, debe coincidir con el del cofre correspondiente
    public bool isKeyUsed = false; // Verifica si la llave ya fue utilizada correctamente
    private Vector3 originalPosition; // Almacena la posición original de la llave
    [SerializeField] private float detectionRange = 1.0f; // Rango de detección para verificar la proximidad al cofre

    private void Start()
    {
        // Guardar la posición original de la llave al iniciar
        originalPosition = transform.position;

    }

    public void CheckForMatchingChest()
    {
        if (isKeyUsed) return; // Si la llave ya fue utilizada, no hacer nada

        // Obtener los cofres activos según la dificultad
        Chest[] chests = GetActiveChests();
        bool isCorrectChest = false;

        foreach (var chest in chests)
        {
            // Verificar si la llave está dentro del rango de detección del cofre
            if (Vector3.Distance(transform.position, chest.transform.position) <= detectionRange)
            {
                if (chest.Color == Color && !chest.isOpened)
                {
                    isCorrectChest = true;
                    OpenChest();
                    isKeyUsed = true;

                    SyncKeyUseWithOtherPlayers(Color, chest.transform.position);
                    Debug.Log("¡Llave correcta! El cofre se ha abierto.");
                    break;
                }
                else if (!chest.isOpened)
                {
                    // Si está cerca pero no coincide el color, reproducir animación de rechazo
                    chest.RejectKey();
                }
            }
        }

        // Si la llave no está en el cofre correcto, vuelve a su posición original
        if (!isCorrectChest)
        {
            transform.position = originalPosition;
            Debug.Log("La llave no es correcta. Vuelve a intentar.");
        }
    }

    public void OpenChest()
    {
        if (isKeyUsed) return;

        Chest[] chests = GetActiveChests();
        foreach (var chest in chests)
        {
            if (Vector3.Distance(transform.position, chest.transform.position) <= detectionRange &&
                chest.Color == Color && !chest.isOpened)
            {
                chest.Open();
                isKeyUsed = true;
                Destroy(gameObject);
                break;
            }
        }
    }
    private void SyncKeyUseWithOtherPlayers(string color, Vector3 position)
    {
        // Llamamos al PlayFabController para sincronizar los datos en PlayFab
        PlayFabController.Instance.SendKeyUseEventToSharedGroupViaCloud(color, position);
    }

    private Chest[] GetActiveChests()
    {
        // Obtener los cofres activos del ChestManager según la dificultad
        switch (DifficultyManager.Instance.CurrentDifficulty)
        {
            case DifficultyLevel.Easy:
                return ChestManager.Instance.easyChests;
            case DifficultyLevel.Medium:
                return ChestManager.Instance.mediumChests;
            case DifficultyLevel.Hard:
                return ChestManager.Instance.hardChests;
            default:
                return new Chest[0]; // Si no hay cofres, devolver vacío
        }
    }
}
