using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Globalization;

public class KeyEventReceiver : MonoBehaviour
{
    private string hostPlayFabId;
    private List<string> openedChests = new List<string>();

    private void Start()
    {
        if (string.IsNullOrEmpty(PlayFabController.PlayFabId))
        {
            return;
        }

        hostPlayFabId = PlayFabController.PlayFabId;

        // Llama a GetLobbyData cada 3 segundos
        InvokeRepeating(nameof(GetLobbyData), 2f, 3f);

        // Se puede iniciar con un peque�o retraso si es que las llaves a�n no est�n en escena.
        StartCoroutine(CheckForKeyUseEvent());
    }

    private void GetLobbyData()
    {
        if (string.IsNullOrEmpty(hostPlayFabId))
        {
            //Debug.LogError("PlayFabId del host no est� definido.");
            return;
        }

        // Obtener datos del host a trav�s de GetUserData
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

                // Abre el cofre si a�n no est� abierto (puedes agregar l�gica extra para evitar duplicados)
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
        // A�ade una tolerancia para comparar posiciones (ejemplo: 0.1f)
        const float tolerance = 0.1f;
        return GameObject.FindObjectsOfType<Chest>().FirstOrDefault(chest =>
            Vector3.Distance(chest.transform.position, position) < tolerance);
    }

    private Vector3 StringToVector3(string str)
    {
        string[] parts = str.Split(',');
        if (parts.Length == 3)
        {
            float x = float.Parse(parts[0], CultureInfo.InvariantCulture);
            float y = float.Parse(parts[1], CultureInfo.InvariantCulture);
            float z = float.Parse(parts[2], CultureInfo.InvariantCulture);
            return new Vector3(x, y, z);
        }

        Debug.LogError($"Formato de Vector3 inv�lido: '{str}'. Se esperaban 3 valores separados por coma.");
        return Vector3.zero;
    }

    private string SerializeVector(Vector3 v)
    {
        // Usa "." como separador decimal sin importar la cultura del sistema
        return $"{v.x.ToString("F2", CultureInfo.InvariantCulture)}," +
               $"{v.y.ToString("F2", CultureInfo.InvariantCulture)}," +
               $"{v.z.ToString("F2", CultureInfo.InvariantCulture)}";
    }

    IEnumerator CheckForKeyUseEvent()
    {
        while (true)
        {
            // Actualiza los datos en cada ciclo
            GetSharedGroupData();
            yield return new WaitForSeconds(1f);
        }
    }

    public void GetSharedGroupData()
    {
        string sharedGroupId = PlayFabController.Instance.sharedGroupId;
        if (string.IsNullOrEmpty(sharedGroupId))
        {
            //Debug.LogError("Error: sharedGroupId no est� inicializado.");
            return;
        }

        //Debug.Log($"[SYNC] sharedGroupId: {sharedGroupId}"); // Log para el sharedGroupId

        var request = new GetSharedGroupDataRequest
        {
            SharedGroupId = sharedGroupId,
            GetMembers = false
        };

        PlayFabClientAPI.GetSharedGroupData(request,
            result =>
            {
                //Debug.Log("[SYNC] Respuesta recibida de PlayFab"); // Log para indicar que se recibi� la respuesta

                if (result.Data != null && result.Data.Count > 0)
                {
                    //Debug.Log("[SYNC] Datos recibidos del Shared Group."); // Log cuando hay datos disponibles

                    foreach (var entry in result.Data)
                    {
                        //Debug.Log($"[SYNC] Entrada recibida: {entry.Key} - {entry.Value}"); // Log de cada entrada

                        string key = entry.Key; // Ejemplo: "Red|(-1.82, 2.45, 1.55)"
                        string chestStatus = entry.Value?.Value ?? string.Empty;

                        // Agrega un log para inspeccionar el valor de chestStatus
                        //Debug.Log($"[SYNC] Valor de chestStatus: '{chestStatus}'");

                        //Debug.Log($"[SYNC] key: {key}, chestStatus: {chestStatus}"); // Log para ver las variables key y chestStatus

                        if (chestStatus.Trim().ToLower() == "cofre abierto")
                        {
                            //Debug.Log("[SYNC] Cofre encontrado con estado 'cofre abierto'"); // Log cuando se detecta el cofre abierto

                            string[] keyParts = key.Split('|');
                            if (keyParts.Length == 2)
                            {
                                //Debug.Log($"[SYNC] keyParts: {keyParts[0]}, {keyParts[1]}"); // Log para ver las partes separadas de la key

                                string color = keyParts[0];
                                Vector3 position = StringToVector3(keyParts[1]);

                                // Usar SerializeVector para garantizar el formato
                                string chestKey = color + "|" + SerializeVector(position);
                                //Debug.Log($"[SYNC] Generada chestKey: {chestKey}"); // Log de la clave generada

                                if (!openedChests.Contains(chestKey))
                                {
                                    //Debug.Log($"[SYNC] Cofre no abierto previamente. Abriendo cofre: color {color}, posici�n {SerializeVector(position)}");
                                    openedChests.Add(chestKey);

                                    TriggerKeyEvent(chestKey);
                                }
                                else
                                {
                                    Debug.Log("[SYNC] El cofre ya estaba abierto, no se realizar� acci�n."); // Log si el cofre ya estaba abierto
                                }
                            }
                            else
                            {
                                Debug.LogError("[SYNC] Error: La clave no tiene el formato esperado (debe tener dos partes separadas por '|').");
                            }
                        }
                        else
                        {
                            //Debug.Log("[SYNC] No es un 'Cofre abierto', no se procesar� esta entrada.");
                        }
                    }
                }
                else
                {
                    Debug.Log("[SYNC] No se encontraron datos en el SharedGroup."); // Log si no hay datos
                }
            },
            error =>
            {
                Debug.LogError("Error al obtener SharedGroupData: " + error.GenerateErrorReport()); // Log de error
            });
    }

    public void TriggerKeyEvent(string chestKey)
    {
        // Divide la key para obtener el color y la posici�n
        string[] keyParts = chestKey.Split('|');
        if (keyParts.Length == 2)
        {
            string color = keyParts[0];
            Debug.Log($"[DEBUG] Color de la llave: {color}"); // Log para el color de la llave

            // Busca el objeto Key correspondiente en la escena solo por color
            Key[] keys = FindObjectsOfType<Key>();

            Debug.Log($"[DEBUG] Se encontraron {keys.Length} llaves en la escena."); // Log para cu�ntas llaves se encontraron

            foreach (var key in keys)
            {
                // Comparar solo por color
                if (key.Color == color)
                {
                    Debug.Log($"[DEBUG] Se encontr� una llave que coincide con el color: {key.Color}"); // Log si la llave coincide

                    // Busca el cofre correspondiente en la escena por color
                    Chest[] chests = FindObjectsOfType<Chest>();

                    foreach (var chest in chests)
                    {
                        // Si el color del cofre coincide con el color de la llave, abrirlo
                        if (chest.Color == color && !chest.isOpened)
                        {
                            chest.Open();  // Abre el cofre
                            key.isKeyUsed = true;  // Marca la llave como utilizada

                            // Despu�s de abrir el cofre y marcar la llave, destr�yela
                            Destroy(key.gameObject);  // Destruye la llave
                            Debug.Log("Se elimina la llave");

                            return; // Sale despu�s de llamar al m�todo
                        }
                    }

                    //Debug.LogError("[ERROR] No se encontr� un cofre que coincida con el color de la llave."); // Log de error si no se encuentra el cofre
                    return;
                }
            }

            Debug.LogError("[ERROR] No se encontr� una llave que coincida con el color del cofre."); // Log de error si no se encuentra la llave
        }
        else
        {
            Debug.LogError("[ERROR] El formato de chestKey no es v�lido."); // Log de error si el formato no es v�lido
        }
    }
    private void OnPlayFabError(PlayFabError error)
    {
        Debug.LogError($"Error al obtener datos del host: {error.GenerateErrorReport()}");

        if (error.Error == PlayFabErrorCode.AccountNotFound)
        {
            Debug.LogError("No se pudo encontrar el jugador con el PlayFabId proporcionado.");
        }
        else if (error.Error == PlayFabErrorCode.InvalidParams)
        {
            Debug.LogError("Par�metros inv�lidos proporcionados para la solicitud.");
        }
        else
        {
            Debug.LogError($"Error desconocido: {error.ErrorMessage}");
        }
    }
}