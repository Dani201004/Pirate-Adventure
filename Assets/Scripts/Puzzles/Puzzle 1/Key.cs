using UnityEngine;

public class Key : MonoBehaviour
{
    [SerializeField] public string Color;
    public bool isKeyUsed = false;
    private Vector3 originalPosition;
    [SerializeField] private float detectionRange = 1.0f;

    [Header("Aura Effect")]
    [SerializeField] private GameObject auraObject; // Asigna el objeto del aura desde el Inspector

    private void Start()
    {
        originalPosition = transform.position;

        if (auraObject != null)
        {
            auraObject.SetActive(false); // Asegúrate de que empieza desactivada
        }
    }

    public void Select()
    {
        if (auraObject != null)
        {
            auraObject.SetActive(true);
        }
    }

    public void Deselect()
    {
        if (auraObject != null)
        {
            auraObject.SetActive(false);
        }
    }

    public void CheckForMatchingChest()
    {
        if (isKeyUsed) return;

        Chest[] chests = GetActiveChests();
        bool isCorrectChest = false;

        foreach (var chest in chests)
        {
            if (Vector3.Distance(transform.position, chest.transform.position) <= detectionRange)
            {
                if (chest.Color == Color && !chest.isOpened)
                {
                    isCorrectChest = true;

                    SyncKeyUseWithOtherPlayers(Color, chest.transform.position);

                    OpenChest();
                    isKeyUsed = true;

                    Debug.Log("¡Llave correcta! El cofre se ha abierto.");
                    break;
                }
                else if (!chest.isOpened)
                {
                    chest.RejectKey();
                }
            }
        }

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

                // Solo destruir la llave si no es multijugador
                if (!PlayFabController.Instance.IsMultiplayerMatch)
                {
                    Destroy(gameObject);
                    Debug.Log("Se elimina la llave (modo individual)");
                }
                else
                {
                    Debug.Log("Llave no se elimina aún (modo multijugador)");
                }

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
