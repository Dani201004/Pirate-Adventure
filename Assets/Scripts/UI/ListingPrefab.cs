using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ListingPrefab : MonoBehaviour
{
    public TMP_Text playerNameText;
    private string friendPlayFabId; // El ID de PlayFab del amigo
    private bool isCooldownActive = false; // Para controlar si el cooldown est� activo

    public Button removeButton;
    private float cooldownTime = 2f; // Tiempo de cooldown en segundos

    public Button joinButton; // Bot�n para unirse a la partida del amigo

    // M�todo que configura los datos del amigo
    public void Setup(string friendName, string friendId)
    {
        playerNameText.text = friendName;    // Mostrar el nombre del amigo
        friendPlayFabId = friendId;          // Almacenar el ID del amigo

        // Asignar el evento al bot�n de eliminar
        removeButton.onClick.RemoveAllListeners(); // Limpiar cualquier listener previo
        removeButton.onClick.AddListener(() => RemoveFriend());

        // Asignar el evento al bot�n de unirse a la partida
        joinButton.onClick.RemoveAllListeners();
        joinButton.onClick.AddListener(() => JoinFriendGame());
    }

    // M�todo que llama a PlayFabController para eliminar al amigo
    public void RemoveFriend()
    {
        if (isCooldownActive)
        {
            // Si el cooldown est� activo, no hacer nada
            return;
        }

        // Iniciar el cooldown
        StartCoroutine(CooldownCoroutine());

        // Llamar al m�todo RemoveFriend en el PlayFabController
        PlayFabController.Instance.RemoveFriend(friendPlayFabId);

        // Desactivar el bot�n temporalmente
        removeButton.interactable = false;
    }

    // Coroutine para manejar el cooldown
    private IEnumerator CooldownCoroutine()
    {
        isCooldownActive = true;

        // Esperamos el tiempo del cooldown
        yield return new WaitForSeconds(cooldownTime);

        // Reactivar el bot�n despu�s del cooldown
        removeButton.interactable = true;
        isCooldownActive = false;
    }

    // M�todo para unirse a la partida del amigo
    public void JoinFriendGame()
    {
        PlayFabController.Instance.GetFriendConnectionString(friendPlayFabId);
    }
}
