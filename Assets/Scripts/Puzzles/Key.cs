using System.Drawing;
using UnityEngine;
using UnityEngine.UIElements;

public class Key : MonoBehaviour
{
    [SerializeField] private string Color;  // Color de la llave, debe coincidir con el del cofre correspondiente
    private bool isKeyUsed = false; // Verifica si la llave ya fue utilizada correctamente
    private Vector3 originalPosition; // Almacena la posici�n original de la llave
    [SerializeField] private float detectionRange = 1.0f; // Rango de detecci�n para verificar la proximidad al cofre

    private void Start()
    {
        // Guardar la posici�n original de la llave al iniciar
        originalPosition = transform.position;
    }

    public void CheckForMatchingChest()
    {
        if (isKeyUsed) return; // Si la llave ya fue utilizada, no hacer nada

        // Buscar todos los cofres en la escena
        Chest[] chests = FindObjectsOfType<Chest>();
        bool isCorrectChest = false;

        foreach (var chest in chests)
        {
            // Verificar si la llave est� dentro del rango de detecci�n del cofre
            if (Vector3.Distance(transform.position, chest.transform.position) <= detectionRange)
            {
                // Si la llave y el cofre tienen el mismo color, abre el cofre
                if (chest.Color == Color && !chest.isOpened)
                {
                    isCorrectChest = true;
                    OpenChest(chest);
                    isKeyUsed = true;

                    SyncKeyUseWithOtherPlayers(Color, chest.transform.position);
                    Debug.Log("�Llave correcta! El cofre se ha abierto.");
                    break;
                }
            }
        }

        // Si la llave no est� en el cofre correcto, vuelve a su posici�n original
        if (!isCorrectChest)
        {
            transform.position = originalPosition;
            // Mensaje en consola cuando la llave no es correcta
            Debug.Log("La llave no es correcta. Vuelve a intentar.");
        }
    }

    private void OpenChest(Chest chest)
    {
        chest.Open(); // Activar la animaci�n, sonido y part�culas del cofre
        Destroy(gameObject); // Eliminar la llave despu�s de que se haya usado correctamente
    }

    private void SyncKeyUseWithOtherPlayers(string keyColor, Vector3 chestPosition)
    {
        // Llamamos al PlayFabController para sincronizar los datos en PlayFab
        PlayFabController.Instance.SendKeyUseEventToSharedGroup(keyColor, chestPosition);
    }
}
