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

    // Variable para almacenar la b�squeda (ahora ser� el nombre del amigo)
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
    //       INICIALIZACI�N Y CICLO DE VIDA DE LA ESCENA
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

        // Si se vuelve al men� principal (escena 0), cerramos la lobby si existe
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
        // Verificar si estamos en la escena n�mero 8
        if (SceneManager.GetActiveScene().buildIndex == 8)
        {
            // Buscar autom�ticamente el bot�n por su nombre en la escena
            GameObject onlineButtonObject = GameObject.Find("Host Game");

            if (onlineButtonObject != null)
            {
                onlineButton = onlineButtonObject.GetComponent<Button>();

                if (onlineButton != null)
                {
                    // Activar temporalmente el bot�n para agregar el listener
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
                        Debug.LogError("No se encontr� el GameObject Pause Menu en la escena.");
                    }
                }
            }
            else
            {
                Debug.LogError("No se encontr� el GameObject Host Game en la escena.");
            }
        }

        // Verificar si estamos en la escena n�mero 2
        if (SceneManager.GetActiveScene().buildIndex == 2 || SceneManager.GetActiveScene().buildIndex == 0)
        {
            // Buscar autom�ticamente el bot�n por su nombre en la escena
            GameObject socialButtonObject = GameObject.Find("Social");

            // Verificar si el bot�n existe y asignar el evento
            if (socialButtonObject != null)
            {
                socialButton = socialButtonObject.GetComponent<Button>();  // Aseg�rate de asignar el componente correctamente
                if (socialButton != null)
                {
                    socialButton.onClick.AddListener(GetFriendsList);
                }
                else
                {
                    Debug.LogError("El componente Button no se encontr� en el objeto 'Social'.");
                }
            }
            else
            {
                Debug.LogError("El bot�n social no se encuentra en la escena.");
            }
        }
    }
    private void Start()
    {
        PlayFabSettings.TitleId = "10A1E8";
        Login();

        FindComponents();

        // Establecer el l�mite de caracteres en el campo de entrada
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
    //                M�TODOS AUXILIARES
    // ================================================
    // M�todo para buscar recursivamente dentro de los hijos
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
                // Recursi�n para buscar en los hijos de los hijos
                Transform result = FindChildRecursive(child, childName);
                if (result != null)
                    return result;
            }
        }
        return null;  // Si no se encuentra el objeto
    }

    // Configuraci�n del scroll para mostrar la lista de amigos
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
                    Debug.LogError("No se encontr� FriendsLayoutGroup en la escena");
                }
            }
        }
    }

    // ================================================
    //         M�TODOS DE AUTENTICACI�N
    // ================================================
    // M�todo para autenticaci�n an�nima
    public void Login()
    {
        var request = new LoginWithCustomIDRequest
        {
            CustomId = GetOrCreateCustomId(),  // Usamos el CustomId generado o almacenado.
            CreateAccount = true  // Si no existe, se crear� una cuenta autom�ticamente.
        };

        PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnLoginFailure);
    }

    // M�todo para obtener o crear un identificador �nico en WebGL
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

        // Verificamos si el EntityToken est� disponible
        if (result.EntityToken != null)
        {
            // Usamos MultiplayerEntityKey ahora para evitar ambig�edad
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
        isAuthenticated = false; // Si el login falla, no est� autenticado.
    }

    // ================================================
    //     M�TODOS DE INTERACCI�N CON LA UI
    // ================================================
    private void ShowNameInputUI()
    {
        Debug.Log("Mostrando UI para ingresar nombre");

        namePanel.gameObject.SetActive(true);
        principalButtons.gameObject.SetActive(false);
        socialButton.gameObject.SetActive(false);
        settingsButton.gameObject.SetActive(false);

        // Establecer el l�mite de caracteres
        nameInputField.characterLimit = 15; // Limitar el nombre a 15 caracteres
    }

    public void OnNameSubmit()
    {
        string playerName = nameInputField.text;

        // Validar que el nombre no est� vac�o y que tenga una longitud aceptable
        if (!string.IsNullOrEmpty(playerName) && playerName.Length <= 15)
        {
            // Verificar si el nombre ya est� en uso en PlayFab
            CheckIfNameExistsInPlayFab(playerName);
        }
        else
        {
            if (string.IsNullOrEmpty(playerName))
            {
                Debug.LogError("El nombre est� vac�o.");
            }
            else
            {
                Debug.LogError("El nombre es demasiado largo. El l�mite es de 15 caracteres.");
            }

            // Mostrar el mensaje de error si el nombre excede el l�mite
            if (errorNameText != null)
            {
                errorNameText.gameObject.SetActive(true); // Activamos el mensaje de error
            }
        }
    }

    // Verifica si el nombre est� en uso en el UserData de PlayFab
    private void CheckIfNameExistsInPlayFab(string playerName)
    {
        if (string.IsNullOrEmpty(PlayFabController.PlayFabId))
        {
            Debug.LogError("El jugador no est� autenticado. No se puede verificar el nombre.");
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
                    Debug.Log("El nombre de usuario ya est� en uso.");

                    if (errorNameExistText != null)
                    {
                        errorNameExistText.gameObject.SetActive(true);
                    }
                }
                else
                {
                    Debug.Log("El nombre est� disponible.");
                    namePanel.gameObject.SetActive(false);  // Desactivar el panel de nombre
                    principalButtons.gameObject.SetActive(true);
                    socialButton.gameObject.SetActive(true);
                    settingsButton.gameObject.SetActive(true);
                    SavePlayerNameToPlayFab(playerName);
                }
            }
            else
            {
                Debug.Log("El nombre est� disponible.");
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
        // Aseg�rate de obtener el PlayFabId din�micamente de la sesi�n actual del jugador
        string playFabId = PlayFabController.PlayFabId;  // Deber�as obtenerlo desde el controlador de PlayFab

        if (string.IsNullOrEmpty(playFabId))
        {
            Debug.LogError("No se encontr� el PlayFabId del jugador. No se puede guardar el nombre.");
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

                // Guardar tambi�n el nombre en PlayerPrefs para que sea accesible en otras partes de la aplicaci�n
                PlayerPrefs.SetString("PlayerName", playerName);
                PlayerPrefs.Save();  // Aseg�rate de guardar los datos de PlayerPrefs

                // Actualizar DisplayName que ser� visible para otros jugadores en el juego
                var displayNameRequest = new UpdateUserTitleDisplayNameRequest
                {
                    DisplayName = playerName
                };

                PlayFabClientAPI.UpdateUserTitleDisplayName(displayNameRequest,
                    displayNameResult =>
                    {
                        Debug.Log("DisplayName actualizado correctamente en PlayFab.");
                        // Aqu� podr�as llamar otros m�todos que quieras ejecutar despu�s de actualizar el DisplayName
                        DisplayDeviceId();
                        GetFriendsList();
                        RequestUserData();  // Aseg�rate de refrescar los datos despu�s de actualizar
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

        // Despu�s de guardar el nombre o avatar, obtener los datos del usuario y la lista de amigos
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
    //         M�TODOS DE DATOS DE USUARIO
    // ================================================
    public void RequestUserData()
    {
        if (!isAuthenticated) // Verificamos si el usuario est� autenticado
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
    //          M�TODOS DE AMIGOS Y RED SOCIAL
    // ================================================

    // M�todo para capturar el nombre del amigo ingresado
    public void InputFriendID(string friendNameInput)
    {
        friendSearch = friendNameInput;
        Debug.Log("Friend Name input: " + friendSearch);
    }

    // M�todo para enviar la solicitud de amistad a trav�s del nombre
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
                    Debug.LogError("ERROR: No se encontr� PlayFabId para el nombre: " + friendSearch);
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
            Debug.LogError("El nombre del amigo est� vac�o.");
        }
    }

    public void AddFriendByDisplayName(string displayName)
    {
        if (string.IsNullOrEmpty(displayName))
        {
            Debug.LogError("El DisplayName est� vac�o.");
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
                        OnAddFriendSuccess(addResult); // si tienes esta funci�n
                                                       // Aseg�rate de pasar los PlayFabId correctamente
                        //AddReciprocalFriend(PlayFabController.PlayFabId, playFabId); // Se pasa correctamente el PlayFabId del jugador 1 (t�) y el del jugador 2 (amigo)
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

    // M�todo para obtener el PlayFabId del nombre usando la base de datos de PlayFab
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
                Debug.LogError("No se encontr� PlayFabId para el DisplayName: " + displayName);
                onError?.Invoke("No se encontr� PlayFabId.");
            }
        }, error =>
        {
            Debug.LogError("Error obteniendo PlayFabId: " + error.GenerateErrorReport());
            onError?.Invoke(error.GenerateErrorReport());
        });
    }

    // M�todo para agregar amigos usando el PlayFabId
    public void AddFriend(string friendPlayFabId)
    {
        var request = new AddFriendRequest
        {
            FriendPlayFabId = friendPlayFabId
        };

        PlayFabClientAPI.AddFriend(request, result =>
        {
            Debug.Log("Amigo agregado correctamente.");
            // Llamamos al m�todo OnAddFriendSuccess despu�s de que el amigo sea agregado con �xito
            OnAddFriendSuccess(result);

        }, error =>
        {
            Debug.LogError("Error al agregar amigo: " + error.GenerateErrorReport());
        });
    }


    // Agregar al amigo de manera rec�proca.
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

    // M�todo que se llama cuando se agrega un amigo con �xito
    private void OnAddFriendSuccess(AddFriendResult result)
    {
        Debug.Log("Amigo agregado con �xito! Actualizando lista de amigos...");
        GetFriendsList(); // Refresca la lista inmediatamente
    }

    // M�todo que se llama cuando ocurre un error al agregar un amigo
    private void OnAddFriendFailure(PlayFabError error)
    {
        Debug.LogError("Error al agregar amigo: " + error.ErrorMessage);
        // Aqu� podr�as agregar m�s l�gica para mostrar el error al usuario en la UI, si es necesario
    }

    // M�todo para obtener la lista de amigos
    public void GetFriendsList()
    {
        var request = new GetFriendsListRequest
        {
            ProfileConstraints = new PlayerProfileViewConstraints { ShowDisplayName = true }
        };

        PlayFabClientAPI.GetFriendsList(request, OnGetFriendsListSuccess, OnGetFriendsListFailure);
    }

    // M�todo que se llama cuando se obtiene la lista de amigos con �xito
    private void OnGetFriendsListSuccess(GetFriendsListResult result)
    {
        Debug.Log("Lista de amigos obtenida con �xito!");

        // Verifica si hay amigos en la respuesta
        if (result.Friends == null || result.Friends.Count == 0)
        {
            Debug.LogWarning("No se encontraron amigos.");
            return;  // Si no hay amigos, salir temprano
        }

        // Actualiza la interfaz de usuario con la nueva lista de amigos
        DisplayFriends(result.Friends);
    }

    // M�todo que se llama cuando ocurre un error al obtener la lista de amigos
    private void OnGetFriendsListFailure(PlayFabError error)
    {
        Debug.LogError("Error al obtener la lista de amigos: " + error.ErrorMessage);
    }

    // M�todo para eliminar un amigo
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
                // Despu�s de eliminar al amigo, llamamos a OnRemoveFriendSuccess para actualizar la lista.
                OnRemoveFriendSuccess(result);  // Actualiza la lista de amigos
                //RemoveFriendReciprocal(friendPlayFabId);
            },
            error =>
            {
                Debug.LogError("Error al eliminar amigo: " + error.ErrorMessage);
                OnRemoveFriendFailure(error);  // Llama al m�todo de manejo de error
            });
    }

    // Eliminar al amigo de manera rec�proca.
    /*public void RemoveFriendReciprocal(string friendPlayFabId)
    {
        // Obtener el PlayFabId del jugador actual (jugador 1)
        var myPlayFabId = PlayFabSettings.staticPlayer.PlayFabId;

        // Llamar al Cloud Script para eliminar la amistad rec�procamente
        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "RemoveFriendFromOtherPlayer",
            FunctionParameter = new { requesterPlayFabId = myPlayFabId, targetPlayFabId = friendPlayFabId },
            GeneratePlayStreamEvent = true
        };

        PlayFabClientAPI.ExecuteCloudScript(request,
            result =>
            {
                Debug.Log("Amigos eliminados rec�procamente.");
            },
            error =>
            {
                Debug.LogError("Error al eliminar amigo rec�procamente: " + error.GenerateErrorReport());
            });
    }*/

    // M�todo que se llama cuando se elimina un amigo con �xito
    private void OnRemoveFriendSuccess(RemoveFriendResult result)
    {
        Debug.Log("Amigo eliminado con �xito!");

        // Actualiza la lista de amigos despu�s de eliminar uno
        GetFriendsList();
    }

    private void OnRemoveFriendFailure(PlayFabError error)
    {
        Debug.LogError("Failed to remove friend: " + error.ErrorMessage);
    }

    // M�todo para mostrar la lista de amigos en la interfaz de usuario
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

        // Destruimos todos los hijos (incluido el �ltimo) fuera del ciclo foreach
        foreach (Transform child in childrenToDestroy)
        {
            Debug.Log("Destruyendo objeto: " + child.name);
            Destroy(child.gameObject);  // Eliminamos el objeto
        }

        // Ahora, instanciamos los nuevos amigos
        foreach (FriendInfo friend in friendsCache)
        {
            GameObject listing = Instantiate(listingPrefab, friendScrollView);
            listing.transform.SetSiblingIndex(0);  // Asegura que los nuevos elementos est�n al principio

            ListingPrefab tempListing = listing.GetComponent<ListingPrefab>();
            string displayName = string.IsNullOrEmpty(friend.TitleDisplayName) ? "Nombre no definido" : friend.TitleDisplayName;
            tempListing.Setup(displayName, friend.FriendPlayFabId);
        }
    }

    // ================================================
    //          M�TODOS DE MULTIJUGADOR (LOBBY)
    // ================================================
    public void JoinRoom(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            Debug.LogError("Error: ConnectionString inv�lido.");
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
                    Debug.Log("Lobby creado con �xito! ConnectionString: " + connectionString);

                    // Ahora crea el grupo compartido usando el SharedGroupId generado
                    CreateSharedGroup();

                    // Guarda ConnectionString y SharedGroupId en el perfil del jugador
                    SaveConnectionString(connectionString, sharedGroupId);
                }
                else
                {
                    Debug.LogError("Error: La creaci�n del lobby no devolvi� una ConnectionString v�lida.");
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

    // M�todo para eliminar el ConnectionString de PlayFab
    public void RemoveConnectionString(bool isLobbyDeleted = false)
    {
        // Verifica si el jugador est� autenticado
        if (PlayFabClientAPI.IsClientLoggedIn())
        {
            // Obt�n el PlayFabId del jugador autenticado
            string playFabId = PlayFabClientAPI.IsClientLoggedIn() ? PlayFabSettings.staticPlayer.PlayFabId : string.Empty;

            if (string.IsNullOrEmpty(playFabId))
            {
                Debug.LogError("El jugador no est� autenticado.");
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
                                Debug.Log("Lobby tambi�n eliminada.");
                            }
                        },
                        updateError =>
                        {
                            Debug.LogError("Error al eliminar el ConnectionString: " + updateError.ErrorMessage);
                        });
                }
                else
                {
                    Debug.Log("No se encontr� el ConnectionString en el perfil del jugador.");
                }
            }, error =>
            {
                Debug.LogError("Error al obtener los datos del jugador: " + error.ErrorMessage);
            });
        }
        else
        {
            Debug.LogError("El jugador no est� autenticado. No se puede eliminar el ConnectionString.");
        }
    }

    // ================================================
    //         M�TODOS DE CICLO DE VIDA DEL APLICATIVO
    // ================================================
    // M�todo de ciclo de vida: cuando la aplicaci�n se cierra
    private void OnApplicationQuit()
    {
        // Si eres host, probablemente cierres todo el lobby
        CloseLobbyIfHost();

        // Elimina las claves del jugador para limpiar la sesi�n
        RemoveConnectionString();
        RemoveSharedGroupId();

        OnPlayerLeave();
    }

    // M�todo de ciclo de vida: cuando la aplicaci�n se pone en pausa
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            CloseLobbyIfHost();  // Solo cerrar lobby si el jugador es el host
        }
    }

    // M�todo para cerrar el lobby si el jugador es el host
    private void CloseLobbyIfHost()
    {
        if (isHost && !string.IsNullOrEmpty(CurrentLobbyId))
        {
            Debug.Log("Cerrando el lobby porque el host sali� del juego...");

            OnPlayerLeave();
            // Solicitar eliminaci�n del lobby en PlayFab
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
                    RemoveConnectionString(true); // Aqu� pasamos true para indicar que se ha cerrado el lobby
                },
                error =>
                {
                    Debug.LogError("Error al eliminar el lobby: " + error.ErrorMessage);
                });
        }
    }
    // M�todo que maneja la salida del jugador
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
            Debug.LogError("No se encontr� el LobbyId.");
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
                // Aqu� puedes realizar cualquier limpieza o actualizaciones necesarias
            },
            error =>
            {
                Debug.LogError("Error al abandonar el lobby: " + error.ErrorMessage);
            });
    }
    // ================================================
    //         M�TODOS DE SINCRONIZACI�N DE EVENTOS
    // ================================================

    private List<string> openedChests = new List<string>();

    public string sharedGroupId; // ID com�n para la partida

    public void CreateSharedGroup()
    {
        var request = new CreateSharedGroupRequest
        {
            SharedGroupId = sharedGroupId
        };

        PlayFabClientAPI.CreateSharedGroup(request, result =>
        {
            Debug.Log("Shared group creado con �xito. SharedGroupId: " + sharedGroupId);
        }, error =>
        {
            Debug.LogError("Error al crear shared group: " + error.GenerateErrorReport());
        });
    }
    // M�todo similar para borrar SharedGroupId, si lo necesitas
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
    // Funci�n para enviar el evento de uso de llave
    public void SendKeyUseEventToSharedGroup(string color, Vector3 position)
    {
        if (string.IsNullOrEmpty(sharedGroupId))
        {
            Debug.LogError("Error: sharedGroupId no est� inicializado.");
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
    //             M�TODOS DE MANEJO DE ERRORES
    // ================================================
    private void OnError(PlayFabError error)
    {
        Debug.LogError("Error: " + error.ErrorMessage);
    }
}
