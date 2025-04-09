using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using PlayFab.MultiplayerModels;

// Usamos alias para desambiguar entre las dos clases EntityKey
using ClientEntityKey = PlayFab.ClientModels.EntityKey;
using MultiplayerEntityKey = PlayFab.MultiplayerModels.EntityKey;
using System.Collections;

// ================================================
//               GLOBAL VARIABLES
// ================================================
public class PlayFabController : MonoBehaviour
{
    public static PlayFabController Instance;

    public event Action<string, string, string> OnUserDataReceived;

    // Referencias a los elementos UI para ingresar el nombre
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private TMP_Text errorNameText;
    [SerializeField] private TMP_Text errorNameExistText;

    private string playerName;

    [SerializeField] private Button submitNameButton;
    [SerializeField] private GameObject namePanel;
    [SerializeField] private GameObject principalButtons;
    [SerializeField] private Button socialButton;
    [SerializeField] private GameObject settingsButton;

    // Variable para almacenar la búsqueda (ahora será el nombre del amigo)
    private string friendSearch;

    [SerializeField] private GameObject listingPrefab;
    public Transform friendScrollView;

    private TMP_Text deviceIdText;

    public static string PlayFabId { get; private set; }

    private bool isAuthenticated = false;
    public event Action OnLoginCompleted;

    [SerializeField] private Button onlineButton;
    [SerializeField] private GameObject pauseMenu;

    public string CurrentLobbyId { get; private set; }

    private bool isHost = false;

    public static MultiplayerEntityKey MyEntityKey;

    // ================================================
    //       INICIALIZACIÓN Y CICLO DE VIDA DE LA ESCENA
    // ================================================
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded; // Suscribimos al evento de carga de escena
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex == 6)
        {
            SetUpFriendScrollView();
            GetFriendsList();
        }

        // Si se vuelve al menú principal (escena 0), cerramos la lobby si existe
        if (scene.buildIndex == 0 && !string.IsNullOrEmpty(CurrentLobbyId))
        {
            DeleteLobby(CurrentLobbyId);
            RemoveConnectionString();
            RemoveSharedGroupId();
        }
        FindComponents();


    }
    private void FindComponents()
    {
        // Verificar si estamos en la escena número 8
        if (SceneManager.GetActiveScene().buildIndex == 8)
        {
            // Buscar automáticamente el botón por su nombre en la escena
            GameObject onlineButtonObject = GameObject.Find("Host Game");

            if (onlineButtonObject != null)
            {
                onlineButton = onlineButtonObject.GetComponent<Button>();

                if (onlineButton != null)
                {
                    // Activar temporalmente el botón para agregar el listener
                    onlineButtonObject.SetActive(true);

                    // Limpiar listeners previos y asignar el nuevo listener
                    onlineButton.onClick.RemoveAllListeners();
                    onlineButton.onClick.AddListener(MakeGameOnline);

                    // Buscar y desactivar el Pause Menu
                    GameObject pauseMenuObject = GameObject.Find("Pause Menu");
                    if (pauseMenuObject != null)
                    {
                        pauseMenuObject.SetActive(false); // Desactivar el Pause Menu
                    }
                    else
                    {
                        Debug.LogError("No se encontró el GameObject Pause Menu en la escena.");
                    }
                }
            }
            else
            {
                Debug.LogError("No se encontró el GameObject Host Game en la escena.");
            }
        }

        // Verificar si estamos en la escena número 2
        if (SceneManager.GetActiveScene().buildIndex == 2 || SceneManager.GetActiveScene().buildIndex == 0)
        {
            // Buscar automáticamente el botón por su nombre en la escena
            GameObject socialButtonObject = GameObject.Find("Social");

            // Verificar si el botón existe y asignar el evento
            if (socialButtonObject != null)
            {
                socialButton = socialButtonObject.GetComponent<Button>();  // Asegúrate de asignar el componente correctamente
                if (socialButton != null)
                {
                    socialButton.onClick.AddListener(GetFriendsList);
                }
                else
                {
                    Debug.LogError("El componente Button no se encontró en el objeto 'Social'.");
                }
            }
            else
            {
                Debug.LogError("El botón social no se encuentra en la escena.");
            }
        }
    }
    private void Start()
    {
        PlayFabSettings.TitleId = "10A1E8";
        Login();

        FindComponents();

        // Establecer el límite de caracteres en el campo de entrada
        nameInputField.characterLimit = 15;
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().buildIndex == 6 && deviceIdText == null)
        {
            deviceIdText = GameObject.FindGameObjectWithTag("IDText")?.GetComponent<TMP_Text>();

            if (deviceIdText != null)
            {
                Debug.Log("Found deviceIdText in scene 6");
                DisplayDeviceId(); // Mostramos el ID en la UI
            }
        }
    }

    // ================================================
    //                MÉTODOS AUXILIARES
    // ================================================
    // Método para buscar recursivamente dentro de los hijos
    Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
            {
                return child;  // Devuelve el hijo si tiene el nombre que buscamos
            }
            else
            {
                // Recursión para buscar en los hijos de los hijos
                Transform result = FindChildRecursive(child, childName);
                if (result != null)
                    return result;
            }
        }
        return null;  // Si no se encuentra el objeto
    }

    // Configuración del scroll para mostrar la lista de amigos
    void SetUpFriendScrollView()
    {
        if (SceneManager.GetActiveScene().buildIndex == 6)
        {
            if (friendScrollView == null)
            {
                GameObject scrollViewObj = GameObject.Find("FriendsLayoutGroup"); // Busca en toda la escena

                if (scrollViewObj != null)
                {
                    friendScrollView = scrollViewObj.transform;
                    Debug.Log("FriendsLayoutGroup asignado correctamente");
                }
                else
                {
                    Debug.LogError("No se encontró FriendsLayoutGroup en la escena");
                }
            }
        }
    }

    // ================================================
    //         MÉTODOS DE AUTENTICACIÓN
    // ================================================
    // Método para autenticación anónima
    public void Login()
    {
        var request = new LoginWithCustomIDRequest
        {
            CustomId = GetOrCreateCustomId(),  // Usamos el CustomId generado o almacenado.
            CreateAccount = true  // Si no existe, se creará una cuenta automáticamente.
        };

        PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnLoginFailure);
    }

    // Método para obtener o crear un identificador único en WebGL
    private string GetOrCreateCustomId()
    {
        string customId = PlayerPrefs.GetString("CustomId", string.Empty);
        if (string.IsNullOrEmpty(customId))
        {
            // Generar un GUID y tomar solo los primeros 8 caracteres
            customId = System.Guid.NewGuid().ToString().Substring(0, 8); // Limitar a los primeros 8 caracteres
            PlayerPrefs.SetString("CustomId", customId);
            PlayerPrefs.Save();
        }
        return customId;
    }

    private void OnLoginSuccess(LoginResult result)
    {
        Debug.Log("Login successful! PlayFabId: " + result.PlayFabId);
        PlayFabId = result.PlayFabId;
        isAuthenticated = true;

        // Verificamos si el EntityToken está disponible
        if (result.EntityToken != null)
        {
            // Usamos MultiplayerEntityKey ahora para evitar ambigüedad
            MyEntityKey = new MultiplayerEntityKey
            {
                Id = result.EntityToken.Entity.Id,
                Type = result.EntityToken.Entity.Type
            };

            Debug.Log($"EntityKey asignado: {MyEntityKey.Id} ({MyEntityKey.Type})");
        }
        else
        {
            Debug.LogError("EntityToken no disponible en el login result.");
            return;
        }

        OnLoginCompleted?.Invoke();

        if (!PlayerPrefs.HasKey("PlayerName"))
        {
            Debug.Log("No PlayerName found, showing input UI");
            ShowNameInputUI();
        }
        else
        {
            string playerName = PlayerPrefs.GetString("PlayerName");

            Debug.Log("PlayerName exists, displaying device ID and friends list");

            namePanel.gameObject.SetActive(false);
            principalButtons.gameObject.SetActive(true);
            socialButton.gameObject.SetActive(true);
            settingsButton.gameObject.SetActive(true);

            DisplayDeviceId();
            GetFriendsList();
            RequestUserData();

            RemoveConnectionString();
            RemoveSharedGroupId();
        }
    }

    private void OnLoginFailure(PlayFabError error)
    {
        Debug.LogError("Login failed: " + error.ErrorMessage);
        isAuthenticated = false; // Si el login falla, no está autenticado.
    }

    // ================================================
    //     MÉTODOS DE INTERACCIÓN CON LA UI
    // ================================================
    private void ShowNameInputUI()
    {
        Debug.Log("Mostrando UI para ingresar nombre");

        namePanel.gameObject.SetActive(true);
        principalButtons.gameObject.SetActive(false);
        socialButton.gameObject.SetActive(false);
        settingsButton.gameObject.SetActive(false);

        // Establecer el límite de caracteres
        nameInputField.characterLimit = 15; // Limitar el nombre a 15 caracteres
    }

    public void OnNameSubmit()
    {
        string playerName = nameInputField.text;

        // Validar que el nombre no esté vacío y que tenga una longitud aceptable
        if (!string.IsNullOrEmpty(playerName) && playerName.Length <= 15)
        {
            // Verificar si el nombre ya está en uso en PlayFab
            CheckIfNameExistsInPlayFab(playerName);
        }
        else
        {
            if (string.IsNullOrEmpty(playerName))
            {
                Debug.LogError("El nombre está vacío.");
            }
            else
            {
                Debug.LogError("El nombre es demasiado largo. El límite es de 15 caracteres.");
            }

            // Mostrar el mensaje de error si el nombre excede el límite
            if (errorNameText != null)
            {
                errorNameText.gameObject.SetActive(true); // Activamos el mensaje de error
            }
        }
    }

    // Verifica si el nombre está en uso en el UserData de PlayFab
    private void CheckIfNameExistsInPlayFab(string playerName)
    {
        if (string.IsNullOrEmpty(PlayFabController.PlayFabId))
        {
            Debug.LogError("El jugador no está autenticado. No se puede verificar el nombre.");
            return;
        }

        var request = new GetUserDataRequest
        {
            PlayFabId = PlayFabController.PlayFabId
        };

        PlayFabClientAPI.GetUserData(request, result =>
        {
            if (result.Data != null && result.Data.ContainsKey("PlayerName"))
            {
                if (result.Data["PlayerName"].Value == playerName)
                {
                    Debug.Log("El nombre de usuario ya está en uso.");

                    if (errorNameExistText != null)
                    {
                        errorNameExistText.gameObject.SetActive(true);
                    }
                }
                else
                {
                    Debug.Log("El nombre está disponible.");
                    namePanel.gameObject.SetActive(false);  // Desactivar el panel de nombre
                    principalButtons.gameObject.SetActive(true);
                    socialButton.gameObject.SetActive(true);
                    settingsButton.gameObject.SetActive(true);
                    SavePlayerNameToPlayFab(playerName);
                }
            }
            else
            {
                Debug.Log("El nombre está disponible.");
                namePanel.gameObject.SetActive(false);  // Desactivar el panel de nombre
                principalButtons.gameObject.SetActive(true);
                socialButton.gameObject.SetActive(true);
                settingsButton.gameObject.SetActive(true);
                SavePlayerNameToPlayFab(playerName);
            }
        },
        error =>
        {
            Debug.LogError("Error al verificar UserData: " + error.GenerateErrorReport());
        });
    }

    // Manejo de errores
    private void OnCheckNameFailure(PlayFabError error)
    {
        Debug.LogError("Error al verificar el nombre: " + error.GenerateErrorReport());
    }

    private void SavePlayerNameToPlayFab(string playerName)
    {
        // Asegúrate de obtener el PlayFabId dinámicamente de la sesión actual del jugador
        string playFabId = PlayFabController.PlayFabId;  // Deberías obtenerlo desde el controlador de PlayFab

        if (string.IsNullOrEmpty(playFabId))
        {
            Debug.LogError("No se encontró el PlayFabId del jugador. No se puede guardar el nombre.");
            return;
        }

        // Crear la solicitud para actualizar los datos del jugador en PlayFab
        var userDataRequest = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
        {
            { "PlayerName", playerName }  // Guardamos el nombre del jugador en los datos del usuario
        }
        };

        PlayFabClientAPI.UpdateUserData(userDataRequest,
            result =>
            {
                Debug.Log("User data saved successfully in PlayFab!");

                // Guardar también el nombre en PlayerPrefs para que sea accesible en otras partes de la aplicación
                PlayerPrefs.SetString("PlayerName", playerName);
                PlayerPrefs.Save();  // Asegúrate de guardar los datos de PlayerPrefs

                // Actualizar DisplayName que será visible para otros jugadores en el juego
                var displayNameRequest = new UpdateUserTitleDisplayNameRequest
                {
                    DisplayName = playerName
                };

                PlayFabClientAPI.UpdateUserTitleDisplayName(displayNameRequest,
                    displayNameResult =>
                    {
                        Debug.Log("DisplayName actualizado correctamente en PlayFab.");
                        // Aquí podrías llamar otros métodos que quieras ejecutar después de actualizar el DisplayName
                        DisplayDeviceId();
                        GetFriendsList();
                        RequestUserData();  // Asegúrate de refrescar los datos después de actualizar
                    },
                    displayNameError =>
                    {
                        Debug.LogError("Error al actualizar DisplayName: " + displayNameError.GenerateErrorReport());
                    });
            },
            error =>
            {
                Debug.LogError("Error al guardar UserData: " + error.GenerateErrorReport());
            });
    }

    private void OnDataSend(UpdateUserDataResult result)
    {
        Debug.Log("User data saved successfully in PlayFab!");

        // Después de guardar el nombre o avatar, obtener los datos del usuario y la lista de amigos
        DisplayDeviceId();
        GetFriendsList();
        RequestUserData();
    }

    private void DisplayDeviceId()
    {
        // Aseguramos que solo se muestre en la escena 6
        if (SceneManager.GetActiveScene().buildIndex != 6)
        {
            return;
        }

        if (deviceIdText != null)
        {
            // Ahora obtenemos el nombre del jugador desde PlayerPrefs
            string playerName = PlayerPrefs.GetString("PlayerName", "Jugador Desconocido");

            // Mostrar el nombre en vez del ID
            deviceIdText.text = "Nombre: " + playerName;
            Debug.Log("Nombre del jugador mostrado en UI: " + playerName);
        }
        else
        {
            Debug.LogError("deviceIdText sigue siendo null, verifica que el objeto tenga el tag 'IDText'.");
        }
    }

    // ================================================
    //         MÉTODOS DE DATOS DE USUARIO
    // ================================================
    public void RequestUserData()
    {
        if (!isAuthenticated) // Verificamos si el usuario está autenticado
        {
            Debug.LogError("You must be logged in to request user data.");
            return;
        }

        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), OnDataReceived, OnError);
    }

    private void OnDataReceived(GetUserDataResult result)
    {
        Debug.Log("Received user data!");
        if (result.Data != null && result.Data.ContainsKey("Hat") && result.Data.ContainsKey("Skin") && result.Data.ContainsKey("Clothes"))
        {
            string hat = result.Data["Hat"].Value;
            string skin = result.Data["Skin"].Value;
            string clothes = result.Data["Clothes"].Value;

            OnUserDataReceived?.Invoke(hat, skin, clothes);
        }
        else
        {
            Debug.Log("Player data not complete!");
        }
    }

    public void SaveAppearanceToPlayFab(string hat, string skin, string clothes)
    {
        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { "Hat", hat },
                { "Skin", skin },
                { "Clothes", clothes }
            }
        };

        PlayFabClientAPI.UpdateUserData(request, OnDataSend, OnError);
    }

    // ================================================
    //          MÉTODOS DE AMIGOS Y RED SOCIAL
    // ================================================

    // Método para capturar el nombre del amigo ingresado
    public void InputFriendID(string friendNameInput)
    {
        friendSearch = friendNameInput;
        Debug.Log("Friend Name input: " + friendSearch);
    }

    // Método para enviar la solicitud de amistad a través del nombre
    public void SubmitFriendRequest()
    {
        if (!string.IsNullOrEmpty(friendSearch))
        {
            SetUpFriendScrollView();

            if (friendScrollView == null)
            {
                Debug.LogError("friendScrollView no fue asignado correctamente.");
                return;
            }

            // Usar el nombre para agregar al amigo
            GetPlayFabIdFromDisplayName(friendSearch, (friendPlayFabId) =>
            {
                if (string.IsNullOrEmpty(friendPlayFabId))
                {
                    Debug.LogError("ERROR: No se encontró PlayFabId para el nombre: " + friendSearch);
                    return;
                }

                AddFriend(friendPlayFabId); // Usar el PlayFabId para agregarlo como amigo
            }, (errorMessage) =>
            {
                Debug.LogError("ERROR: " + errorMessage);
            });
        }
        else
        {
            Debug.LogError("El nombre del amigo está vacío.");
        }
    }

    public void AddFriendByDisplayName(string displayName)
    {
        if (string.IsNullOrEmpty(displayName))
        {
            Debug.LogError("El DisplayName está vacío.");
            return;
        }

        var request = new GetAccountInfoRequest
        {
            TitleDisplayName = displayName
        };

        PlayFabClientAPI.GetAccountInfo(request, result =>
        {
            string playFabId = result.AccountInfo?.PlayFabId;

            if (!string.IsNullOrEmpty(playFabId))
            {
                Debug.Log("PlayFabId obtenido correctamente: " + playFabId);

                var addFriendRequest = new AddFriendRequest
                {
                    FriendPlayFabId = playFabId
                };

                PlayFabClientAPI.AddFriend(addFriendRequest,
                    addResult =>
                    {
                        Debug.Log("Amigo agregado correctamente.");
                        OnAddFriendSuccess(addResult); // si tienes esta función
                                                       // Asegúrate de pasar los PlayFabId correctamente
                        //AddReciprocalFriend(PlayFabController.PlayFabId, playFabId); // Se pasa correctamente el PlayFabId del jugador 1 (tú) y el del jugador 2 (amigo)
                    },
                    addError =>
                    {
                        Debug.LogError("Error al agregar amigo: " + addError.GenerateErrorReport());
                    });
            }
            else
            {
                Debug.LogError("PlayFabId no encontrado con ese DisplayName.");
            }
        },
        error =>
        {
            Debug.LogError("Error al obtener PlayFabId desde DisplayName: " + error.GenerateErrorReport());
        });
    }

    // Método para obtener el PlayFabId del nombre usando la base de datos de PlayFab
    public void GetPlayFabIdFromDisplayName(string displayName, Action<string> onSuccess, Action<string> onError)
    {
        var request = new GetAccountInfoRequest
        {
            TitleDisplayName = displayName
        };

        PlayFabClientAPI.GetAccountInfo(request, result =>
        {
            string playFabId = result.AccountInfo?.PlayFabId;

            if (!string.IsNullOrEmpty(playFabId))
            {
                Debug.Log("PlayFabId encontrado: " + playFabId);
                onSuccess?.Invoke(playFabId);
            }
            else
            {
                Debug.LogError("No se encontró PlayFabId para el DisplayName: " + displayName);
                onError?.Invoke("No se encontró PlayFabId.");
            }
        }, error =>
        {
            Debug.LogError("Error obteniendo PlayFabId: " + error.GenerateErrorReport());
            onError?.Invoke(error.GenerateErrorReport());
        });
    }

    // Método para agregar amigos usando el PlayFabId
    public void AddFriend(string friendPlayFabId)
    {
        var request = new AddFriendRequest
        {
            FriendPlayFabId = friendPlayFabId
        };

        PlayFabClientAPI.AddFriend(request, result =>
        {
            Debug.Log("Amigo agregado correctamente.");
            // Llamamos al método OnAddFriendSuccess después de que el amigo sea agregado con éxito
            OnAddFriendSuccess(result);

        }, error =>
        {
            Debug.LogError("Error al agregar amigo: " + error.GenerateErrorReport());
        });
    }


    // Agregar al amigo de manera recíproca.
    /*public void AddReciprocalFriend(string requesterPlayFabId, string targetPlayFabId)
    {
        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "AddReciprocalFriend",
            FunctionParameter = new { requesterPlayFabId = requesterPlayFabId, targetPlayFabId = targetPlayFabId },
            GeneratePlayStreamEvent = true
        };

        PlayFabClientAPI.ExecuteCloudScript(request,
            result =>
            {
                Debug.Log("Reciprocidad agregada: " + result.FunctionResult);

                // Convertir el resultado a un diccionario para leer el flag
                var resultDict = result.FunctionResult as Dictionary<string, object>;
                if (resultDict != null && resultDict.ContainsKey("updateFriendList"))
                {
                    bool updateFlag = Convert.ToBoolean(resultDict["updateFriendList"]);
                    if (updateFlag)
                    {
                        Debug.Log("Actualizando la lista de amigos en tiempo real.");
                        // Se actualiza la lista de amigos del cliente
                        GetFriendsList();
                    }
                }
            },
            error =>
            {
                Debug.LogError("Error al agregar reciprocidad: " + error.GenerateErrorReport());
            });
    }*/

    // Método que se llama cuando se agrega un amigo con éxito
    private void OnAddFriendSuccess(AddFriendResult result)
    {
        Debug.Log("Amigo agregado con éxito! Actualizando lista de amigos...");
        GetFriendsList(); // Refresca la lista inmediatamente
    }

    // Método que se llama cuando ocurre un error al agregar un amigo
    private void OnAddFriendFailure(PlayFabError error)
    {
        Debug.LogError("Error al agregar amigo: " + error.ErrorMessage);
        // Aquí podrías agregar más lógica para mostrar el error al usuario en la UI, si es necesario
    }

    // Método para obtener la lista de amigos
    public void GetFriendsList()
    {
        var request = new GetFriendsListRequest
        {
            ProfileConstraints = new PlayerProfileViewConstraints { ShowDisplayName = true }
        };

        PlayFabClientAPI.GetFriendsList(request, OnGetFriendsListSuccess, OnGetFriendsListFailure);
    }

    // Método que se llama cuando se obtiene la lista de amigos con éxito
    private void OnGetFriendsListSuccess(GetFriendsListResult result)
    {
        Debug.Log("Lista de amigos obtenida con éxito!");

        // Verifica si hay amigos en la respuesta
        if (result.Friends == null || result.Friends.Count == 0)
        {
            Debug.LogWarning("No se encontraron amigos.");
            return;  // Si no hay amigos, salir temprano
        }

        // Actualiza la interfaz de usuario con la nueva lista de amigos
        DisplayFriends(result.Friends);
    }

    // Método que se llama cuando ocurre un error al obtener la lista de amigos
    private void OnGetFriendsListFailure(PlayFabError error)
    {
        Debug.LogError("Error al obtener la lista de amigos: " + error.ErrorMessage);
    }

    // Método para eliminar un amigo
    public void RemoveFriend(string friendPlayFabId)
    {
        var request = new RemoveFriendRequest
        {
            FriendPlayFabId = friendPlayFabId  // El PlayFab ID del amigo a eliminar.
        };

        PlayFabClientAPI.RemoveFriend(request,
            result =>
            {
                Debug.Log("Amigo eliminado correctamente!");
                // Después de eliminar al amigo, llamamos a OnRemoveFriendSuccess para actualizar la lista.
                OnRemoveFriendSuccess(result);  // Actualiza la lista de amigos
                //RemoveFriendReciprocal(friendPlayFabId);
            },
            error =>
            {
                Debug.LogError("Error al eliminar amigo: " + error.ErrorMessage);
                OnRemoveFriendFailure(error);  // Llama al método de manejo de error
            });
    }

    // Eliminar al amigo de manera recíproca.
    /*public void RemoveFriendReciprocal(string friendPlayFabId)
    {
        // Obtener el PlayFabId del jugador actual (jugador 1)
        var myPlayFabId = PlayFabSettings.staticPlayer.PlayFabId;

        // Llamar al Cloud Script para eliminar la amistad recíprocamente
        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "RemoveFriendFromOtherPlayer",
            FunctionParameter = new { requesterPlayFabId = myPlayFabId, targetPlayFabId = friendPlayFabId },
            GeneratePlayStreamEvent = true
        };

        PlayFabClientAPI.ExecuteCloudScript(request,
            result =>
            {
                Debug.Log("Amigos eliminados recíprocamente.");
            },
            error =>
            {
                Debug.LogError("Error al eliminar amigo recíprocamente: " + error.GenerateErrorReport());
            });
    }*/

    // Método que se llama cuando se elimina un amigo con éxito
    private void OnRemoveFriendSuccess(RemoveFriendResult result)
    {
        Debug.Log("Amigo eliminado con éxito!");

        // Actualiza la lista de amigos después de eliminar uno
        GetFriendsList();
    }

    private void OnRemoveFriendFailure(PlayFabError error)
    {
        Debug.LogError("Failed to remove friend: " + error.ErrorMessage);
    }

    // Método para mostrar la lista de amigos en la interfaz de usuario
    void DisplayFriends(List<FriendInfo> friendsCache)
    {
        if (SceneManager.GetActiveScene().buildIndex != 6)
        {
            return;
        }

        SetUpFriendScrollView();

        if (friendScrollView == null)
        {
            Debug.LogError("friendScrollView sigue siendo null, no se puede mostrar la lista de amigos.");
            return;
        }

        // Guardamos los hijos actuales en una lista
        List<Transform> childrenToDestroy = new List<Transform>();
        foreach (Transform child in friendScrollView)
        {
            childrenToDestroy.Add(child);  // Guardamos la referencia al hijo
        }

        // Destruimos todos los hijos (incluido el último) fuera del ciclo foreach
        foreach (Transform child in childrenToDestroy)
        {
            Debug.Log("Destruyendo objeto: " + child.name);
            Destroy(child.gameObject);  // Eliminamos el objeto
        }

        // Ahora, instanciamos los nuevos amigos
        foreach (FriendInfo friend in friendsCache)
        {
            GameObject listing = Instantiate(listingPrefab, friendScrollView);
            listing.transform.SetSiblingIndex(0);  // Asegura que los nuevos elementos estén al principio

            ListingPrefab tempListing = listing.GetComponent<ListingPrefab>();
            string displayName = string.IsNullOrEmpty(friend.TitleDisplayName) ? "Nombre no definido" : friend.TitleDisplayName;
            tempListing.Setup(displayName, friend.FriendPlayFabId);
        }
    }

    // ================================================
    //          MÉTODOS DE MULTIJUGADOR (LOBBY)
    // ================================================
    public void JoinRoom(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            Debug.LogError("Error: ConnectionString inválido.");
            return;
        }

        // Crear el EntityKey para el jugador actual
        var playerEntityKey = new PlayFab.MultiplayerModels.EntityKey
        {
            Id = MyEntityKey.Id,  // ID del jugador
            Type = MyEntityKey.Type  // Tipo de la entidad (por ejemplo, "title_player_account")
        };

        // Crear la solicitud para unirse al lobby
        var request = new PlayFab.MultiplayerModels.JoinLobbyRequest
        {
            ConnectionString = connectionString,  // ConnectionString para unirse a la sala
            MemberEntity = playerEntityKey  // El jugador actual como MemberEntity
        };

        PlayFabMultiplayerAPI.JoinLobby(request,
            result =>
            {
                isHost = false;  // Este jugador no es el host, es un jugador secundario
                Debug.Log("Unido a la partida correctamente!");

                UpdateUIForJoinedLobby();
            },
            OnError);  // Usando el manejador de errores OnError
    }

    void UpdateUIForJoinedLobby()
    {
        // Cambia de escena o activa los objetos que representan la partida
        SceneTransition.Instance.LoadLevelJoinRoom();
    }

    public void MakeGameOnline()
    {
        // Definir la clave de entidad del jugador para el cliente
        var clientEntityKey = new ClientEntityKey
        {
            Id = MyEntityKey.Id,
            Type = MyEntityKey.Type
        };

        // Crear un nuevo EntityKey para Multiplayer basado en el ClientEntityKey
        var multiplayerEntityKey = new MultiplayerEntityKey
        {
            Id = clientEntityKey.Id,
            Type = clientEntityKey.Type
        };

        // Crear el objeto que representa al propietario del lobby
        var ownerEntityKey = multiplayerEntityKey;

        // Generar el SharedGroupId antes de crear el lobby
        sharedGroupId = System.Guid.NewGuid().ToString();
        Debug.Log("SharedGroupId generado: " + sharedGroupId);

        // Crear la solicitud para el lobby
        var request = new CreateLobbyRequest
        {
            Owner = ownerEntityKey,
            MaxPlayers = 3,
            AccessPolicy = AccessPolicy.Public,
            UseConnections = true,
            Members = new List<Member> { new Member { MemberEntity = ownerEntityKey } }
        };

        Debug.Log("Creando lobby con la siguiente solicitud: " + JsonUtility.ToJson(request));

        // Crear el lobby usando la API de PlayFab
        PlayFabMultiplayerAPI.CreateLobby(request,
            result =>
            {
                if (result != null && !string.IsNullOrEmpty(result.ConnectionString))
                {
                    string connectionString = result.ConnectionString;
                    CurrentLobbyId = result.LobbyId;
                    Debug.Log("Lobby creado con éxito! ConnectionString: " + connectionString);

                    // Ahora crea el grupo compartido usando el SharedGroupId generado
                    CreateSharedGroup();

                    // Guarda ConnectionString y SharedGroupId en el perfil del jugador
                    SaveConnectionString(connectionString, sharedGroupId);
                }
                else
                {
                    Debug.LogError("Error: La creación del lobby no devolvió una ConnectionString válida.");
                }
            },
            error =>
            {
                Debug.LogError("Error al crear la partida online: " + error.ErrorMessage);
            });
    }

    public void SaveConnectionString(string connectionString, string sharedGroupId)
    {
        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
        {
            { "ConnectionString", connectionString },
            { "SharedGroupId", sharedGroupId }
        },
            Permission = UserDataPermission.Public
        };

        PlayFabClientAPI.UpdateUserData(request,
            result =>
            {
                Debug.Log("Datos guardados correctamente, incluyendo SharedGroupId.");
            },
            OnError);
    }

    public void GetFriendConnectionString(string friendPlayFabId)
    {
        Debug.Log("Buscando ConnectionString y SharedGroupId del amigo con PlayFabId: " + friendPlayFabId);

        var request = new GetUserDataRequest
        {
            PlayFabId = friendPlayFabId
        };

        PlayFabClientAPI.GetUserData(request, result =>
        {
            if (result.Data != null && result.Data.ContainsKey("ConnectionString"))
            {
                string friendConnectionString = result.Data["ConnectionString"].Value;
                Debug.Log("ConnectionString encontrado: " + friendConnectionString);

                if (result.Data.ContainsKey("SharedGroupId"))
                {
                    sharedGroupId = result.Data["SharedGroupId"].Value;
                    Debug.Log("SharedGroupId sincronizado: " + sharedGroupId);
                }

                StartCoroutine(WaitAndJoinLobby(friendConnectionString));
            }
            else
            {
                Debug.LogError("El amigo no tiene una sala activa.");
            }
        }, OnError);
    }

    IEnumerator WaitAndJoinLobby(string connectionString)
    {
        yield return new WaitForSeconds(2); // Esperar 2 segundos antes de intentar unirse
        JoinRoom(connectionString);
    }


    public void DeleteLobby(string lobbyId)
    {
        var request = new DeleteLobbyRequest
        {
            LobbyId = lobbyId
        };

        PlayFabMultiplayerAPI.DeleteLobby(request,
            result =>
            {
                Debug.Log("Lobby eliminada correctamente.");
                CurrentLobbyId = null;

                var updateRequest = new UpdateUserDataRequest
                {
                    KeysToRemove = new List<string> { "ConnectionString" }
                };

                PlayFabClientAPI.UpdateUserData(updateRequest,
                    updateResult => {
                        Debug.Log("ConnectionString eliminado del perfil.");
                    },
                    updateError => {
                        Debug.LogError("Error al eliminar el ConnectionString: " + updateError.ErrorMessage);
                    });
            },
            error => {
                Debug.LogError("Error al eliminar la lobby: " + error.ErrorMessage);
            });
    }

    // Método para eliminar el ConnectionString de PlayFab
    public void RemoveConnectionString(bool isLobbyDeleted = false)
    {
        // Verifica si el jugador está autenticado
        if (PlayFabClientAPI.IsClientLoggedIn())
        {
            // Obtén el PlayFabId del jugador autenticado
            string playFabId = PlayFabClientAPI.IsClientLoggedIn() ? PlayFabSettings.staticPlayer.PlayFabId : string.Empty;

            if (string.IsNullOrEmpty(playFabId))
            {
                Debug.LogError("El jugador no está autenticado.");
                return;
            }

            // Crear la solicitud para obtener los datos del jugador
            var request = new GetUserDataRequest
            {
                PlayFabId = playFabId
            };

            PlayFabClientAPI.GetUserData(request, result =>
            {
                if (result.Data != null && result.Data.ContainsKey("ConnectionString"))
                {
                    // El ConnectionString existe, entonces lo eliminamos
                    var updateRequest = new UpdateUserDataRequest
                    {
                        KeysToRemove = new List<string> { "ConnectionString" }
                    };

                    PlayFabClientAPI.UpdateUserData(updateRequest,
                        updateResult =>
                        {
                            Debug.Log("ConnectionString eliminado del perfil.");

                            if (isLobbyDeleted)
                            {
                                Debug.Log("Lobby también eliminada.");
                            }
                        },
                        updateError =>
                        {
                            Debug.LogError("Error al eliminar el ConnectionString: " + updateError.ErrorMessage);
                        });
                }
                else
                {
                    Debug.Log("No se encontró el ConnectionString en el perfil del jugador.");
                }
            }, error =>
            {
                Debug.LogError("Error al obtener los datos del jugador: " + error.ErrorMessage);
            });
        }
        else
        {
            Debug.LogError("El jugador no está autenticado. No se puede eliminar el ConnectionString.");
        }
    }

    // ================================================
    //         MÉTODOS DE CICLO DE VIDA DEL APLICATIVO
    // ================================================
    // Método de ciclo de vida: cuando la aplicación se cierra
    private void OnApplicationQuit()
    {
        // Si eres host, probablemente cierres todo el lobby
        CloseLobbyIfHost();

        // Elimina las claves del jugador para limpiar la sesión
        RemoveConnectionString();
        RemoveSharedGroupId();

        OnPlayerLeave();
    }

    // Método de ciclo de vida: cuando la aplicación se pone en pausa
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            CloseLobbyIfHost();  // Solo cerrar lobby si el jugador es el host
        }
    }

    // Método para cerrar el lobby si el jugador es el host
    private void CloseLobbyIfHost()
    {
        if (isHost && !string.IsNullOrEmpty(CurrentLobbyId))
        {
            Debug.Log("Cerrando el lobby porque el host salió del juego...");

            OnPlayerLeave();
            // Solicitar eliminación del lobby en PlayFab
            var request = new DeleteLobbyRequest
            {
                LobbyId = CurrentLobbyId
            };

            PlayFabMultiplayerAPI.DeleteLobby(request,
                result =>
                {
                    Debug.Log("Lobby cerrada correctamente.");
                    CurrentLobbyId = null;

                    // Eliminar ConnectionString de los datos del jugador
                    RemoveConnectionString(true); // Aquí pasamos true para indicar que se ha cerrado el lobby
                },
                error =>
                {
                    Debug.LogError("Error al eliminar el lobby: " + error.ErrorMessage);
                });
        }
    }
    // Método que maneja la salida del jugador
    public void OnPlayerLeave()
    {
        if (isHost)
        {
            Debug.Log("El jugador host ha salido. Cerrando el lobby.");

            // Cerrar el lobby si es el host
            CloseLobbyIfHost();
        }
        else
        {
            Debug.Log("El jugador ha abandonado la partida.");

            // Dejar el lobby si el jugador no es el host
            if (!string.IsNullOrEmpty(CurrentLobbyId))
            {
                LeaveLobby();  // Deja el lobby
            }

            // Eliminar el ConnectionString del jugador
            RemoveConnectionString();
        }
    }
    public void LeaveLobby()
    {
        if (string.IsNullOrEmpty(CurrentLobbyId))
        {
            Debug.LogError("No se encontró el LobbyId.");
            return;
        }

        // Crear la solicitud para abandonar el lobby
        var request = new PlayFab.MultiplayerModels.LeaveLobbyRequest
        {
            LobbyId = CurrentLobbyId,
            MemberEntity = new PlayFab.MultiplayerModels.EntityKey
            {
                Id = MyEntityKey.Id,
                Type = MyEntityKey.Type
            }
        };

        PlayFabMultiplayerAPI.LeaveLobby(request,
            result =>
            {
                Debug.Log("Jugador ha dejado el lobby exitosamente.");
                // Aquí puedes realizar cualquier limpieza o actualizaciones necesarias
            },
            error =>
            {
                Debug.LogError("Error al abandonar el lobby: " + error.ErrorMessage);
            });
    }
    // ================================================
    //         MÉTODOS DE SINCRONIZACIÓN DE EVENTOS
    // ================================================

    private List<string> openedChests = new List<string>();

    public string sharedGroupId; // ID común para la partida

    public void CreateSharedGroup()
    {
        var request = new CreateSharedGroupRequest
        {
            SharedGroupId = sharedGroupId
        };

        PlayFabClientAPI.CreateSharedGroup(request, result =>
        {
            Debug.Log("Shared group creado con éxito. SharedGroupId: " + sharedGroupId);
        }, error =>
        {
            Debug.LogError("Error al crear shared group: " + error.GenerateErrorReport());
        });
    }
    // Método similar para borrar SharedGroupId, si lo necesitas
    public void RemoveSharedGroupId()
    {
        var updateRequest = new UpdateUserDataRequest
        {
            KeysToRemove = new List<string> { "SharedGroupId" }
        };

        PlayFabClientAPI.UpdateUserData(updateRequest,
            updateResult =>
            {
                Debug.Log("SharedGroupId eliminado del perfil.");
            },
            updateError =>
            {
                Debug.LogError("Error al eliminar SharedGroupId: " + updateError.ErrorMessage);
            });
    }
    // Función para enviar el evento de uso de llave
    public void SendKeyUseEventToSharedGroup(string color, Vector3 position)
    {
        if (string.IsNullOrEmpty(sharedGroupId))
        {
            Debug.LogError("Error: sharedGroupId no está inicializado.");
            return;
        }

        var request = new UpdateSharedGroupDataRequest
        {
            SharedGroupId = sharedGroupId,
            Data = new Dictionary<string, string>
        {
            { "KeyUsedColor", color },
            { "ChestPosition", position.ToString() }
        },
            Permission = UserDataPermission.Public
        };

        PlayFabClientAPI.UpdateSharedGroupData(request,
            result =>
            {
                Debug.Log("Evento de uso de llave sincronizado correctamente.");
            },
            error =>
            {
                Debug.LogError("Error al sincronizar SharedGroup: " + error.GenerateErrorReport());
            });
    }

    // ================================================
    //             MÉTODOS DE MANEJO DE ERRORES
    // ================================================
    private void OnError(PlayFabError error)
    {
        Debug.LogError("Error: " + error.ErrorMessage);
    }
}
