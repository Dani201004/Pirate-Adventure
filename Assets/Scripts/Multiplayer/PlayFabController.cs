using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;
using PlayFab.MultiplayerModels;
using System.Collections;
using System.Globalization;
using System.Linq;

// Usamos alias para desambiguar entre las dos clases EntityKey
using ClientEntityKey = PlayFab.ClientModels.EntityKey;
using MultiplayerEntityKey = PlayFab.MultiplayerModels.EntityKey;


// ================================================
//               GLOBAL VARIABLES
// ================================================
public class PlayFabController : MonoBehaviour
{
    [Header("Instancia playfabcontroller")]
    public static PlayFabController Instance;

    public event Action<string, string, string> OnUserDataReceived;

    [Header("Referencia al administrador de gameobjects del apartado social")]
    [SerializeField] private SocialUIManager socialUIManager;

    [Header("Referencia al administrador de gameobjects del menu principal")]
    [SerializeField] private MainMenuUIManager mainMenuUIManager;

    [Header("Referencia al administrador de gameobjects del apartado puzzles")]
    [SerializeField] private PuzzlesUIManager puzzlesUIManager;

    [Header("Referencia al administrador de gameobjects del apartado game")]
    [SerializeField] private GameUIManager gameUIManager;

    [Header("Contenedor que almacena el nombre de usuario del amigo que buscas")]
    private string friendSearch;

    [Header("listado de solicitudes entrantes pendientes de decision")]
    private List<string> pendingRequests = new List<string>();
    private int currentRequestIndex = 0;

    [Header("Comprobacion de si estamos en la escena 6")]
    private bool isInScene6 = false;  // Variable para saber si estamos en la escena 6
    private float checkInterval = 1f; // Intervalo de tiempo para verificar (en segundos)

    [Header("Tu ID")]
    public static string PlayFabId { get; private set; }

    [Header("Comprobaciones para saber si has iniciado sesion")]
    private bool isAuthenticated = false;
    public event Action OnLoginCompleted;

    [Header("ID de la lobby en la que te encuentras")]
    public string CurrentLobbyId { get; private set; }

    [Header("ID del sharedgroup por el que se comparte la info de los cambios en la lobby")]
    public string sharedGroupId; // ID común para la partida

    [Header("Comprobacion para saber si eres el creador de la lobby o no")]
    private bool isHost = false;

    [Header("ID como usuario multijugador")]
    public static MultiplayerEntityKey MyEntityKey;

    [Header("Cooldown para botones")]
    private bool isCooldownActive = false;
    private float cooldownTime = 3f; // Tiempo en segundos para el cooldown

    [Header("ID del puzzle actual para partidas online")]
    private string currentPuzzleId;

    [Header("Lista de jugadores en la partida")]
    private List<string> CurrentLobbyMemberIds = new List<string>();

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
            // Llamar a la corutina después de un pequeño retraso
            StartCoroutine(FindSocialUIManagerAfterDelay());

            isInScene6 = true;  // Desactivar la comprobación periódica

            StartCoroutine(CheckRequestsPeriodically());
        }
        // Verificar si la escena ha cambiado a algo que no sea la escena 6
        if (SceneManager.GetActiveScene().buildIndex != 6)
        {
            isInScene6 = false;  // Desactivar la comprobación periódica

            StopCoroutine(CheckRequestsPeriodically()); // Detener la Coroutine
        }

        // Buscar y asignar 'MainMenuUIManager' si estamos en la escena con índice 0
        if (scene.buildIndex == 0)
        {
            StartCoroutine(FindMainMenuUIManagerAfterDelay());
        }

        // Buscar y asignar 'PuzzlesUIManager' si estamos en una escena que comienza con "Puzzle"
        if (scene.name.StartsWith("Puzzle"))
        {
            StartCoroutine(FindPuzzlesUIManagerAfterDelay());
        }

        // Buscar y asignar 'GameUIManager' si estamos en la escena con índice 8
        if (scene.buildIndex == 2)
        {
            StartCoroutine(FindGameUIManagerAfterDelay());
        }

        // Si se vuelve al menú principal (escena 0), cerramos la lobby si existe
        if (SceneManager.GetActiveScene().buildIndex == 0 && !string.IsNullOrEmpty(CurrentLobbyId))
        {
            DeleteLobby(CurrentLobbyId);
            RemoveConnectionString();
            RemoveSharedGroupId();
        }
    }
    private IEnumerator FindMainMenuUIManagerAfterDelay()
    {
        // Espera un pequeño retraso para asegurarse de que los objetos estén activos
        yield return new WaitForSeconds(0.1f);

        GameObject mainMenuObject = GameObject.Find("MainMenuUIManager");

        if (mainMenuObject != null)
        {
            mainMenuUIManager = mainMenuObject.GetComponent<MainMenuUIManager>();
        }
        else
        {
            Debug.LogWarning("No se encontró el objeto 'MainMenuUIManager' en la escena.");
        }

        yield return new WaitForSeconds(0.2f);

        mainMenuUIManager.SocialButton.onClick.AddListener(GetFriendsList);
    }

    private IEnumerator FindSocialUIManagerAfterDelay()
    {
        // Espera un pequeño retraso para asegurarse de que los objetos estén activos
        yield return new WaitForSeconds(0.1f);  // Espera un pequeño retraso para asegurar que los objetos estén activos

        GameObject socialObject = GameObject.Find("SocialUIManager");

        if (socialObject != null)
        {
            socialUIManager = socialObject.GetComponent<SocialUIManager>();
        }
        else
        {
            Debug.LogWarning("No se encontró el objeto 'SocialUIManager' en la escena.");
        }

        yield return new WaitForSeconds(0.2f);

        DisplayDeviceId();

        socialUIManager.RequestsButton.onClick.AddListener(ShowCurrentRequest);

        socialUIManager.AcceptButton.onClick.AddListener(AcceptCurrentRequest);

        socialUIManager.DeclineButton.onClick.AddListener(DeclineCurrentRequest);

        socialUIManager.SubmitButton.onClick.AddListener(SubmitFriendRequest);

        socialUIManager.FriendNameInputField.onValueChanged.AddListener(InputFriendID);
    }
    private IEnumerator FindPuzzlesUIManagerAfterDelay()
    {
        // Espera un pequeño retraso para asegurarse de que los objetos estén activos
        yield return new WaitForSeconds(0.1f);

        GameObject puzzlesObject = GameObject.Find("PuzzlesUIManager");

        if (puzzlesObject != null)
        {
            puzzlesUIManager = puzzlesObject.GetComponent<PuzzlesUIManager>();
        }
        else
        {
            Debug.LogWarning("No se encontró el objeto 'PuzzlesUIManager' en la escena.");
        }

        yield return new WaitForSeconds(0.2f);

        // Activar temporalmente el botón para agregar el listener
        puzzlesUIManager.OnlineButton.gameObject.SetActive(true);
        puzzlesUIManager.OnlineButton.interactable = true;

        // Limpiar listeners previos y asignar el nuevo listener
        puzzlesUIManager.OnlineButton.onClick.RemoveAllListeners();
        puzzlesUIManager.OnlineButton.onClick.AddListener(MakeGameOnline);

        puzzlesUIManager.PauseMenu.gameObject.SetActive(false); // Desactivar el Pause Menu

        // Limpiar listeners previos y asignar el nuevo listener
        puzzlesUIManager.RetryButton.onClick.RemoveAllListeners();
        puzzlesUIManager.RetryButton.onClick.AddListener(VoteRetry);
    }
    private IEnumerator FindGameUIManagerAfterDelay()
    {
        // Espera un pequeño retraso para asegurarse de que los objetos estén activos
        yield return new WaitForSeconds(0.1f);

        GameObject gamesObject = GameObject.Find("GameUIManager");

        if (gamesObject != null)
        {
            gameUIManager = gamesObject.GetComponent<GameUIManager>();
        }
        else
        {
            Debug.LogWarning("No se encontró el objeto 'GameUIManager' en la escena.");
        }

        yield return new WaitForSeconds(0.2f);

        gameUIManager.SocialButton.onClick.AddListener(GetFriendsList);
    }
    private void Start()
    {
        PlayFabSettings.TitleId = "10A1E8";

        Login();

        CloseLobbyIfHost();

        GameObject mainMenuObject = GameObject.Find("MainMenuUIManager");

        if (mainMenuObject != null)
        {
            mainMenuUIManager = mainMenuObject.GetComponent<MainMenuUIManager>();
        }
        else
        {
            Debug.LogWarning("No se encontró el objeto 'MainMenuUIManager' en la escena.");
        }

        // Establecer el límite de caracteres en el campo de entrada
        mainMenuUIManager.NameInputField.characterLimit = 15;
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

            mainMenuUIManager.NamePanel.SetActive(false);
            mainMenuUIManager.PrincipalButtons.SetActive(true);
            mainMenuUIManager.SocialButton.gameObject.SetActive(true);
            mainMenuUIManager.SettingsButton.SetActive(true);

            RequestUserData();
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

        mainMenuUIManager.NamePanel.SetActive(true);
        mainMenuUIManager.PrincipalButtons.SetActive(false);
        mainMenuUIManager.SocialButton.gameObject.SetActive(false);
        mainMenuUIManager.SettingsButton.SetActive(false);

        // Establecer el límite de caracteres
        mainMenuUIManager.NameInputField.characterLimit = 15; // Limitar el nombre a 15 caracteres
    }

    public void OnNameSubmit()
    {
        string playerName = mainMenuUIManager.NameInputField.text;

        // Validar que el nombre no esté vacío y que tenga una longitud aceptable
        if (!string.IsNullOrEmpty(playerName) && playerName.Length <= 12)
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

            mainMenuUIManager.ErrorNameText.gameObject.SetActive(true); // Activamos el mensaje de error
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

                    mainMenuUIManager.ErrorNameExistText.gameObject.SetActive(true);
                }
                else
                {
                    Debug.Log("El nombre está disponible.");
                    mainMenuUIManager.NamePanel.SetActive(false);  // Desactivar el panel de nombre
                    mainMenuUIManager.PrincipalButtons.SetActive(true);
                    mainMenuUIManager.SocialButton.gameObject.SetActive(true);
                    mainMenuUIManager.SettingsButton.SetActive(true);
                    SavePlayerNameToPlayFab(playerName);
                }
            }
            else
            {
                Debug.Log("El nombre está disponible.");
                mainMenuUIManager.NamePanel.SetActive(false);  // Desactivar el panel de nombre
                mainMenuUIManager.PrincipalButtons.SetActive(true);
                mainMenuUIManager.SocialButton.gameObject.SetActive(true);
                mainMenuUIManager.SettingsButton.SetActive(true);
                SavePlayerNameToPlayFab(playerName);
            }
        },
        error =>
        {
            Debug.LogError("Error al verificar UserData: " + error.GenerateErrorReport());
        });
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

        // Después de guardar el nombre o avatar, obtener los datos del usuario
        RequestUserData();
    }

    private void DisplayDeviceId()
    {
        // Aseguramos que solo se muestre en la escena 6
        if (SceneManager.GetActiveScene().buildIndex != 6)
        {
            return;
        }

        // Ahora obtenemos el nombre del jugador desde PlayerPrefs
        string playerName = PlayerPrefs.GetString("PlayerName", "Jugador Desconocido");

        // Mostrar el nombre en vez del ID
        socialUIManager.DeviceIdText.text = "ID: " + playerName;
        Debug.Log("Nombre del jugador mostrado en UI: " + playerName);
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
        //Debug.Log("Received user data!");
        if (result.Data != null && result.Data.ContainsKey("Hat") && result.Data.ContainsKey("Skin") && result.Data.ContainsKey("Clothes"))
        {
            string hat = result.Data["Hat"].Value;
            string skin = result.Data["Skin"].Value;
            string clothes = result.Data["Clothes"].Value;

            OnUserDataReceived?.Invoke(hat, skin, clothes);
        }
        else
        {
            //Debug.Log("Player data not complete!");
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

            // Usar el nombre para agregar al amigo
            GetPlayFabIdFromDisplayName(friendSearch, (friendPlayFabId) =>
            {
                //Debug.Log("Callback de éxito recibido con ID: " + friendPlayFabId);

                if (string.IsNullOrEmpty(friendPlayFabId))
                {
                    Debug.LogError("ERROR: No se encontró PlayFabId para el nombre: " + friendSearch);
                    return;
                }

                SendFriendRequest(friendPlayFabId);

            }, (errorMessage) =>
            {
                Debug.LogError("ERROR: " + errorMessage);
                if (socialUIManager.NotFoundPanel != null)
                {
                    socialUIManager.NotFoundPanel.SetActive(true);
                }
            });
        }
        else
        {
            Debug.LogError("El nombre del amigo está vacío.");
        }
    }
    // Enviar solicitud de amistad, almacenándola en la data del jugador receptor
    public void SendFriendRequest(string targetPlayFabId)
    {
        string senderPlayFabId = PlayFabController.PlayFabId;

        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "AddFriendRequest",
            FunctionParameter = new
            {
                requesterPlayFabId = senderPlayFabId,
                targetPlayFabId = targetPlayFabId
            },
            GeneratePlayStreamEvent = true
        };

        PlayFabClientAPI.ExecuteCloudScript(request, updateResult =>
        {
            Debug.Log("Solicitud de amistad enviada con éxito (CloudScript).");
            socialUIManager.ConfirmationPanel.SetActive(true);
        },
        error =>
        {
            Debug.LogError("Error al enviar la solicitud (CloudScript): " + error.GenerateErrorReport());
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
            if (result.AccountInfo != null &&
                !string.IsNullOrEmpty(result.AccountInfo.PlayFabId) &&
                string.Equals(result.AccountInfo.TitleInfo?.DisplayName, displayName, StringComparison.OrdinalIgnoreCase)) // Verifica coincidencia real
            {
                string playFabId = result.AccountInfo.PlayFabId;
                Debug.Log("PlayFabId encontrado: " + playFabId);
                onSuccess?.Invoke(playFabId);
            }
            else
            {
                Debug.LogError("No se encontró cuenta para el DisplayName: " + displayName);
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
    public void AddReciprocalFriend(string requesterPlayFabId, string targetPlayFabId)
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

                // Actualiza la lista de amigos de ambos jugadores
                GetFriendsList(); // Para refrescar la lista de amigos en el cliente
            },
            error =>
            {
                Debug.LogError("Error al agregar reciprocidad: " + error.GenerateErrorReport());
            });
    }

    // Método que se llama cuando se agrega un amigo con éxito
    private void OnAddFriendSuccess(AddFriendResult result)
    {
        Debug.Log("Amigo agregado con éxito! Actualizando lista de amigos...");
        GetFriendsList(); // Refresca la lista inmediatamente
    }

    IEnumerator CheckRequestsPeriodically()
    {
        while (isInScene6) // Ejecuta mientras estemos en la escena 6
        {
            CheckFriendRequests(); // Comprobar las solicitudes de amistad
            yield return new WaitForSeconds(checkInterval); // Esperar el intervalo antes de comprobar de nuevo
        }
    }
    public void CheckFriendRequests()
    {
        // Asegúrate de obtener el PlayFabId del usuario actual
        string currentPlayFabId = PlayFabController.PlayFabId;

        // Solicita la data del usuario actual usando su PlayFabId
        PlayFabClientAPI.GetUserData(new GetUserDataRequest
        {
            PlayFabId = currentPlayFabId
        },
        result =>
        {
            if (result.Data != null && result.Data.ContainsKey("FriendRequests"))
            {
                string requests = result.Data["FriendRequests"].Value;
                pendingRequests = requests.Split(',')
                                          .Where(id => !string.IsNullOrWhiteSpace(id))
                                          .ToList();

                // Si existen solicitudes, inicializa el índice actual
                if (pendingRequests.Count > 0)
                {
                    currentRequestIndex = 0;
                }

                StartCoroutine(AssignQuantityRequests());
            }
            else
            {
                StartCoroutine(AssignQuantityRequestsZero());
            }
        },
        error =>
        {
            Debug.LogError("Error al revisar solicitudes: " + error.GenerateErrorReport());
        });
    }
    private IEnumerator AssignQuantityRequests()
    {
        yield return new WaitForSeconds(0.5f);

        // Mostrar la cantidad de solicitudes en el texto
        socialUIManager.QuantityRequestsText.text = $"{pendingRequests.Count}";
    }
    private IEnumerator AssignQuantityRequestsZero()
    {
        yield return new WaitForSeconds(0.5f);

        // En caso de no existir la key, se muestra "0"
        socialUIManager.QuantityRequestsText.text = "0";
    }
    private void ShowCurrentRequest()
    {
        if (pendingRequests.Count == 0 || currentRequestIndex >= pendingRequests.Count)
        {
            socialUIManager.RequestPanel.SetActive(false);
            return;
        }

        string currentRequesterId = pendingRequests[currentRequestIndex];

        socialUIManager.RequestPanel.SetActive(true);
        socialUIManager.NotFoundPanel.SetActive(false);
        socialUIManager.ConfirmationPanel.SetActive(false);

        var request = new GetAccountInfoRequest
        {
            PlayFabId = currentRequesterId
        };

        PlayFabClientAPI.GetAccountInfo(request, result =>
        {
            string displayName = result.AccountInfo?.TitleInfo?.DisplayName
                                 ?? result.AccountInfo?.Username
                                 ?? currentRequesterId;

            if (socialUIManager.AdviceText != null)
            {
                if (LanguageManager.CurrentLanguage == 0) // Inglés
                {
                    socialUIManager.AdviceText.text = $"{displayName} has sent you a friend request. Do you want to accept it?";
                }
                else if (LanguageManager.CurrentLanguage == 1) // Español
                {
                    socialUIManager.AdviceText.text = $"{displayName} te ha enviado una solicitud de amistad. ¿La quieres aceptar?";
                }
            }
        },
        error =>
        {
            Debug.LogError("No se pudo obtener el DisplayName: " + error.GenerateErrorReport());

            if (socialUIManager.AdviceText != null)
            {
                if (LanguageManager.CurrentLanguage == 0) // Inglés
                {
                    socialUIManager.AdviceText.text = $"{currentRequesterId} has sent you a friend request. Do you want to accept it?";
                }
                else if (LanguageManager.CurrentLanguage == 1) // Español
                {
                    socialUIManager.AdviceText.text = $"{currentRequesterId} te ha enviado una solicitud de amistad. ¿La quieres aceptar?";
                }
            }
        });
    }
    private void AcceptCurrentRequest()
    {
        if (currentRequestIndex < pendingRequests.Count)
        {
            string requesterId = pendingRequests[currentRequestIndex];
            AcceptFriendRequest(requesterId);
        }
    }
    private void DeclineCurrentRequest()
    {
        if (currentRequestIndex < pendingRequests.Count)
        {
            string requesterId = pendingRequests[currentRequestIndex];

            PlayFabClientAPI.GetUserData(new GetUserDataRequest(),
            result =>
            {
                if (result.Data != null && result.Data.ContainsKey("FriendRequests"))
                {
                    var currentRequests = result.Data["FriendRequests"].Value.Split(',').ToList();
                    currentRequests.Remove(requesterId);

                    PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest
                    {
                        Data = new Dictionary<string, string>
                        {
                        { "FriendRequests", string.Join(",", currentRequests) }
                        }
                    },
                    updateResult =>
                    {
                        Debug.Log("Solicitud rechazada y eliminada.");

                        pendingRequests.RemoveAt(currentRequestIndex);

                        socialUIManager.RequestPanel.SetActive(false);

                        ShowCurrentRequest();
                    },
                    error =>
                    {
                        Debug.LogError("Error al eliminar la solicitud: " + error.GenerateErrorReport());
                    });
                }
            },
            error =>
            {
                Debug.LogError("Error al cargar solicitudes: " + error.GenerateErrorReport());
            });
        }
    }
    public void AcceptFriendRequest(string requesterPlayFabId)
    {
        AddFriend(requesterPlayFabId);
        AddReciprocalFriend(PlayFabController.PlayFabId, requesterPlayFabId);

        // Luego elimina al requester de la lista
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(),
        result =>
        {
            if (result.Data != null && result.Data.ContainsKey("FriendRequests"))
            {
                var currentRequests = result.Data["FriendRequests"].Value.Split(',').ToList();
                currentRequests.Remove(requesterPlayFabId);

                PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest
                {
                    Data = new Dictionary<string, string>
                    {
                    { "FriendRequests", string.Join(",", currentRequests) }
                    }
                },
                updateResult =>
                {
                    Debug.Log("Solicitud aceptada y eliminada.");

                    // También actualizamos la lista local
                    pendingRequests.RemoveAt(currentRequestIndex);

                    socialUIManager.RequestPanel.SetActive(false);

                    ShowCurrentRequest();
                },
                error =>
                {
                    Debug.LogError("Error al eliminar la solicitud: " + error.GenerateErrorReport());
                });
            }
        },
        error =>
        {
            Debug.LogError("Error al cargar solicitudes: " + error.GenerateErrorReport());
        });
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

        StartCoroutine(DisplayFriendsTime(result.Friends));
    }

    private IEnumerator DisplayFriendsTime(List<FriendInfo> friends)
    {
        yield return new WaitForSeconds(0.5f);

        // Actualiza la interfaz de usuario con la nueva lista de amigos
        DisplayFriends(friends);
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

                // Llamamos a RemoveFriendReciprocal para hacer la eliminación recíproca
                RemoveFriendReciprocal(friendPlayFabId); // Elimina al amigo de manera recíproca
            },
            error =>
            {
                Debug.LogError("Error al eliminar amigo: " + error.ErrorMessage);
                OnRemoveFriendFailure(error);  // Llama al método de manejo de error
            });
    }

    // Eliminar al amigo de manera recíproca.
    public void RemoveFriendReciprocal(string friendPlayFabId)
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
    }

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

        if (socialUIManager.friendsLayoutGroup == null)
        {
            Debug.LogError("friendScrollView sigue siendo null, no se puede mostrar la lista de amigos.");
            return;
        }

        // Guardamos los hijos actuales en una lista
        List<Transform> childrenToDestroy = new List<Transform>();
        foreach (Transform child in socialUIManager.friendsLayoutGroup)
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
            GameObject listing = Instantiate(socialUIManager.ListingPrefab, socialUIManager.friendsLayoutGroup);
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
            string currentLobbyId = result.LobbyId;

            // Ahora result contiene el LobbyId, pero debemos obtener más información si es necesario
            // Aquí hay que hacer otra llamada para obtener detalles completos del lobby si es necesario
            var lobbyRequest = new PlayFab.MultiplayerModels.GetLobbyRequest
            {
                LobbyId = currentLobbyId
            };

            PlayFabMultiplayerAPI.GetLobby(lobbyRequest,
                lobbyResult =>
                {
                    Lobby currentLobby = lobbyResult.Lobby;

                    // Acceder a los miembros del lobby
                    if (currentLobby.Members != null)
                    {
                        var memberIds = currentLobby.Members
                            .Select(m => m.MemberEntity.Id)  // Accede al Id de cada miembro a través de MemberEntity
                            .ToList();

                        Debug.Log("Miembros del lobby: " + string.Join(", ", memberIds));

                        // Guardar esa lista para futuras comprobaciones
                        SetLobbyMembers(memberIds);
                    }
                    else
                    {
                        Debug.LogWarning("No se encontraron miembros en el lobby.");
                    }

                    // Cargar la escena del puzzle
                    LoadPuzzleSceneFromSharedGroup();
                },
                OnError);
        },
        OnError);
    }

    public void MakeGameOnline()
    {
        // Deshabilitar el botón mientras se crea el lobby
        puzzlesUIManager.OnlineButton.interactable = false;

        puzzlesUIManager.NotOnlineImage.gameObject.SetActive(false);

        puzzlesUIManager.OnlineImage.gameObject.SetActive(true);

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
            MaxPlayers = 4,
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

                    // Actualizar la lista de miembros del lobby (aquí solo es el host por ahora)
                    SetLobbyMembers(new List<string> { MyEntityKey.Id });

                    // Ahora crea el grupo compartido usando el SharedGroupId generado
                    CreateSharedGroup(() =>
                    {
                        // Una vez que el SharedGroup ha sido creado correctamente, puedes guardar los datos del puzzle
                        SaveCurrentPuzzleToSharedGroup(currentPuzzleId);  // Usar el puzzle ID actual

                        // Guarda ConnectionString y SharedGroupId en el perfil del jugador
                        SaveConnectionString(connectionString, sharedGroupId);

                        SetCurrentPuzzleId(currentPuzzleId); // Aquí actualizas el puzzleId
                    });

                }
                else
                {
                    Debug.LogError("Error: La creación del lobby no devolvió una ConnectionString válida.");
                }
            },
            error =>
            {
                Debug.LogError("Error al crear la partida online: " + error.ErrorMessage);

                // Volver a habilitar el botón si ocurre un error
                puzzlesUIManager.OnlineButton.interactable = true;
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
        if (isCooldownActive)
        {
            Debug.Log("Cooldown activo. Por favor, espera.");
            return; // Si el cooldown está activo, no hace nada
        }

        // Activamos el cooldown
        isCooldownActive = true;
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

            // Desactivamos el cooldown después de realizar la operación
            StartCoroutine(ResetCooldown());
        }, OnError);
    }
    // Coroutine para resetear el cooldown
    private IEnumerator ResetCooldown()
    {
        yield return new WaitForSeconds(cooldownTime); // Esperamos el tiempo del cooldown
        isCooldownActive = false; // Desactivamos el cooldown
    }

    IEnumerator WaitAndJoinLobby(string connectionString)
    {
        yield return new WaitForSeconds(1); // Esperar 1 segundos antes de intentar unirse
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
        RemoveSharedGroupId();

        OnPlayerLeave();

        CloseLobbyIfHost();
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
                    RemoveSharedGroupId();
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

            // Actualizar la lista de miembros después de que el jugador haya salido
            SetLobbyMembers(CurrentLobbyMemberIds.Where(id => id != MyEntityKey.Id).ToList());

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

    // Método para crear el Shared Group
    public void CreateSharedGroup(Action onGroupCreated)
    {
        var request = new CreateSharedGroupRequest
        {
            SharedGroupId = sharedGroupId
        };

        // Realizamos la solicitud para crear el Shared Group
        PlayFabClientAPI.CreateSharedGroup(request, result =>
        {
            // Se ejecuta si el Shared Group se crea correctamente
            Debug.Log("Shared group creado con éxito. SharedGroupId: " + sharedGroupId);

            // Llamamos al callback para indicar que el grupo se ha creado correctamente
            onGroupCreated?.Invoke();
        }, error =>
        {
            // Si ocurre un error en la creación del Shared Group
            Debug.LogError("Error al crear shared group: " + error.GenerateErrorReport());
        });
    }
    // Puedes llenar esta lista con los IDs de los jugadores que están en la lobby
    public void SetLobbyMembers(List<string> memberIds)
    {
        CurrentLobbyMemberIds = new List<string>(memberIds);
    }
    // Función para guardar datos en el grupo compartido
    public void SetSharedGroupData(string key, string value, Action onSuccess)
    {
        var req = new UpdateSharedGroupDataRequest
        {
            SharedGroupId = sharedGroupId,
            Data = new Dictionary<string, string> { { key, value } }
        };
        PlayFabClientAPI.UpdateSharedGroupData(req, res =>
        {
            Debug.Log($"SharedGroupData updated: {key} = {value}");
            onSuccess?.Invoke();
        },
        err => Debug.LogError("UpdateSharedGroupData: " + err.GenerateErrorReport()));
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
    public void SetCurrentPuzzleId(string puzzleId)
    {
        currentPuzzleId = puzzleId;
        Debug.Log("Puzzle seleccionado: " + currentPuzzleId);
    }

    public void SaveCurrentPuzzleToSharedGroup(string puzzleId)
    {
        var data = new Dictionary<string, string> {
        { "CurrentPuzzleId", puzzleId }
    };

        PlayFabClientAPI.UpdateSharedGroupData(new UpdateSharedGroupDataRequest
        {
            SharedGroupId = sharedGroupId,
            Data = data
        },
        result => {
            Debug.Log("Puzzle actual guardado en SharedGroup: " + puzzleId);
        },
        error => {
            Debug.LogError("Error al guardar puzzle actual en SharedGroup: " + error.GenerateErrorReport());
        });
    }
    public void LoadPuzzleSceneFromSharedGroup()
    {
        PlayFabClientAPI.GetSharedGroupData(new GetSharedGroupDataRequest
        {
            SharedGroupId = sharedGroupId
        },
        result =>
        {
            if (result.Data.TryGetValue("CurrentPuzzleId", out var puzzleEntry))
            {
                string puzzleId = puzzleEntry.Value;
                int sceneIndex = GetSceneIndexFromPuzzleId(puzzleId);

                Debug.Log("Cargando escena del puzzle: " + puzzleId + " (índice: " + sceneIndex + ")");
                SceneTransition.Instance.LoadLevelJoinRoom(sceneIndex);  // Pasamos el índice de la escena
            }
            else
            {
                Debug.LogWarning("No se encontró CurrentPuzzleId en el SharedGroup. Cargando escena por defecto.");
                SceneTransition.Instance.LoadLevelJoinRoom(8); // Escena por defecto si no se encuentra el puzzle
            }
        },
        error =>
        {
            Debug.LogError("Error al obtener CurrentPuzzleId desde SharedGroup: " + error.GenerateErrorReport());
        });
    }
    // Método para obtener el índice de la escena basado en el puzzleId
    private int GetSceneIndexFromPuzzleId(string puzzleId)
    {
        switch (puzzleId)
        {
            case "Puzzle1":
                return 8;
            case "Puzzle2":
                return 10;
            case "Puzzle3":
                return 11;
            case "Puzzle4":
                return 12;
            case "Puzzle5":
                return 13;
            default:
                return 8;  // Escena por defecto si no se encuentra el puzzle
        }
    }
    // Función para obtener datos del grupo compartido
    public void GetSharedGroupData(Action<Dictionary<string, string>> callback)
    {
        var req = new GetSharedGroupDataRequest { SharedGroupId = sharedGroupId };
        PlayFabClientAPI.GetSharedGroupData(req, res =>
        {
            var data = new Dictionary<string, string>();
            foreach (var kv in res.Data)
                data[kv.Key] = kv.Value.Value;
            callback?.Invoke(data);
        },
        err => Debug.LogError("GetSharedGroupData: " + err.GenerateErrorReport()));
    }
    public void VoteRetry()
    {
        string playerId = PlayFabId;

        if (!string.IsNullOrEmpty(sharedGroupId))
        {
            SetSharedGroupData($"retry_vote_{playerId}", "1", CheckAllPlayersVotedRetry);
        }
        else
        {
            Debug.Log("Partida individual: reiniciando sin votar.");
            RetryGame(); // Reinicia directamente si es partida individual
        }
    }

    // Comprueba si todos han votado
    public void CheckAllPlayersVotedRetry()
    {
        GetSharedGroupData(data =>
        {
            int votes = CurrentLobbyMemberIds.Count(pid => data.ContainsKey($"retry_vote_{pid}"));
            int total = CurrentLobbyMemberIds.Count;

            Debug.Log($"Votes: {votes}/{total}");

            // Mostrar el recuento en la UI
            if (puzzlesUIManager.RetryVoteText != null)
            {
                puzzlesUIManager.RetryVoteText.text = $"Votos para reintentar: {votes}/{total}";
            }

            if (votes >= Mathf.CeilToInt(total / 2f))  // Mitad o más (maneja impares también)
            {
                ResetVotesAndRetry();
            }
        });
    }

    // Método para reiniciar la partida y reintentar
    public void ResetVotesAndRetry()
    {
        foreach (var pid in CurrentLobbyMemberIds)
            SetSharedGroupData($"retry_vote_{pid}", string.Empty, null);  // Limpia los votos

        RetryGame();  // Llama a un método que reintenta el juego
    }

    private void RetryGame()
    {
        Debug.Log("Reintentando la partida...");

        // Aquí puedes agregar la lógica para reiniciar el juego. Por ejemplo:
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneTransition.Instance.LoadLevelJoinRoom(currentSceneIndex);  // Cambiar al nivel que desees
    }

    // ================================================
    //    MÉTODOS DE SINCRONIZACIÓN DE EVENTO LLAVE
    // ================================================
    private string SerializeVector(Vector3 v)
    {
        // Usamos la cultura actual para que el formato coincida con el que se guarda en PlayFab (por ejemplo, "1,80,2,45,1,55")
        return $"{v.x.ToString("F2", CultureInfo.CurrentCulture)},{v.y.ToString("F2", CultureInfo.CurrentCulture)},{v.z.ToString("F2", CultureInfo.CurrentCulture)}";
    }

    // Función para enviar el evento de uso de llave
    public void SendKeyUseEventToSharedGroupViaCloud(string color, Vector3 position)
    {
        if (string.IsNullOrEmpty(sharedGroupId))
        {
            Debug.LogError("Error: sharedGroupId no está inicializado.");
            return;
        }

        // Asegúrate de que el color y la posición no sean nulos o vacíos
        if (string.IsNullOrEmpty(color))
        {
            Debug.LogError("Error: color no está definido.");
            return;
        }

        if (position == null)
        {
            Debug.LogError("Error: position no está definido.");
            return;
        }

        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "updateSharedGroupData",
            FunctionParameter = new
            {
                sharedGroupId = sharedGroupId,
                color = color,
                position = SerializeVector(position)  // Asegúrate de que la conversión de posición sea correcta
            },
            GeneratePlayStreamEvent = false
        };

        PlayFabClientAPI.ExecuteCloudScript(request,
            result =>
            {
                // Imprime toda la respuesta para ver qué datos contiene
                Debug.Log("Respuesta del servidor: " + result.FunctionResult);

                if (result.FunctionResult is Dictionary<string, object> response)
                {
                    // Verifica si contiene los datos esperados
                    if (response.ContainsKey("updatedData"))
                    {
                        Debug.Log($"Evento enviado al servidor correctamente: {response["updatedData"]}");
                    }
                    else
                    {
                        Debug.LogWarning("La respuesta no contiene datos útiles.");
                    }
                }
                else
                {
                    Debug.LogWarning("La respuesta no tiene el formato esperado.");
                }
            },
            error =>
            {
                Debug.LogError("Error al ejecutar CloudScript: " + error.GenerateErrorReport());
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
