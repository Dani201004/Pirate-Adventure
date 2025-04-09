using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System.Linq;

public class KeyEventReceiver : MonoBehaviour
{
    private string hostPlayFabId;

    private void Start()
    {
        if (string.IsNullOrEmpty(PlayFabController.PlayFabId))
        {
            Debug.Log("El usuario no está autenticado.");
            return;
        }

        hostPlayFabId = PlayFabController.PlayFabId;

        // Llama a GetLobbyData cada 3 segundos
        InvokeRepeating(nameof(GetLobbyData), 2f, 3f);
    }

    private void GetLobbyData()
    {
        if (string.IsNullOrEmpty(hostPlayFabId))
        {
            Debug.LogError("PlayFabId del host no está definido.");
            return;
        }

        // Usamos GetUserData para obtener la información sincronizada que el host ha almacenado
        var request = new GetUserDataRequest
        {
            PlayFabId = hostPlayFabId
        };

        PlayFabClientAPI.GetUserData(request, OnLobbyDataReceived, OnPlayFabError);
    }

    private void OnLobbyDataReceived(GetUserDataResult result)
    {
        if (result.Data != null && result.Data.ContainsKey("OpenedChests"))
        {
            string chestDataString = result.Data["OpenedChests"].Value;
            string[] chests = chestDataString.Split(';');

            foreach (string entry in chests)
            {
                if (string.IsNullOrEmpty(entry)) continue;

                var parts = entry.Split('|');
                if (parts.Length != 2) continue;

                string keyColor = parts[0];
                Vector3 chestPos = StringToVector3(parts[1]);

                // Abrir cofre si no está ya abierto (puedes agregar lógica extra para evitar duplicados)
                ActivateChestEffects(keyColor, chestPos);
            }
        }
    }

    private void ActivateChestEffects(string keyColor, Vector3 chestPosition)
    {
        Chest chest = FindChestAtPosition(chestPosition);
        if (chest != null && !chest.isOpened)
        {
            chest.SyncOpenEffects();
        }
    }

    private Chest FindChestAtPosition(Vector3 position)
    {
        // Añadir una tolerancia para comparar posiciones (ejemplo: 0.1f)
        const float tolerance = 0.1f;
        return GameObject.FindObjectsOfType<Chest>().FirstOrDefault(chest =>
            Vector3.Distance(chest.transform.position, position) < tolerance);
    }

    private Vector3 StringToVector3(string str)
    {
        // Convierte la cadena en un Vector3
        string trimmed = str.Trim('(', ')');
        var values = trimmed.Split(',');
        if (values.Length != 3)
        {
            Debug.LogError("Formato de Vector3 inválido: " + str);
            return Vector3.zero;
        }
        return new Vector3(float.Parse(values[0]), float.Parse(values[1]), float.Parse(values[2]));
    }
    public void GetOpenedChestsFromSharedGroup()
    {
        var request = new GetSharedGroupDataRequest
        {
            SharedGroupId = PlayFabController.Instance.sharedGroupId,
            GetMembers = false
        };

        PlayFabClientAPI.GetSharedGroupData(request, result =>
        {
            foreach (var entry in result.Data)
            {
                string key = entry.Key; // ejemplo: "Rojo|(1.0,0.0,3.0)"
                string[] parts = key.Split('|');

                if (parts.Length == 2)
                {
                    string keyColor = parts[0];
                    Vector3 position = StringToVector3(parts[1]);
                    ActivateChestEffects(keyColor, position);
                }
            }
        }, error =>
        {
            Debug.LogError("Error al leer SharedGroupData: " + error.GenerateErrorReport());
        });
    }

    private void OnPlayFabError(PlayFabError error)
    {
        // Log con más detalles sobre el error
        Debug.LogError($"Error al obtener datos del host: {error.GenerateErrorReport()}");

        if (error.Error == PlayFabErrorCode.AccountNotFound)
        {
            Debug.LogError("No se pudo encontrar el jugador con el PlayFabId proporcionado.");
        }
        else if (error.Error == PlayFabErrorCode.InvalidParams)
        {
            Debug.LogError("Parámetros inválidos proporcionados para la solicitud.");
        }
        else
        {
            Debug.LogError($"Error desconocido: {error.ErrorMessage}");
        }
    }
}