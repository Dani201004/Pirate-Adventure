using TMPro;
using UnityEngine.UI;
using UnityEngine;

public class SocialUIManager : MonoBehaviour
{
    [Header("Prefab de amigo")]
    [SerializeField] private GameObject listingPrefab;

    [Header("Contenedor layout de amigos")]
    [SerializeField] public Transform friendsLayoutGroup;

    [Header("Input Field y Boton enviar solicitud")]
    [SerializeField] private TMP_InputField friendNameInputField;
    [SerializeField] private Button submitButton;

    [Header("Paneles e Inputs de usuario no encontrado")]
    [SerializeField] private GameObject notFoundPanel;

    [Header("Paneles e Inputs de solicitud enviada correctamente")]
    [SerializeField] private GameObject confirmationPanel;

    [Header("Boton y texto de cantidad de solicitudes entrantes")]
    [SerializeField] private Button requestsButton;
    [SerializeField] private TMP_Text quantityRequestsText;

    [Header("Paneles e Inputs de solicitud de amistad entrante")]
    [SerializeField] private GameObject requestPanel;
    [SerializeField] private TMP_Text adviceText;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button declineButton;

    [Header("Texto donde muestra tu nombre de usuario")]
    [SerializeField] private TMP_Text deviceIdText;


    // Propiedades públicas de solo lectura
    public GameObject ListingPrefab => listingPrefab;
    public Transform FriendsLayoutGroup => friendsLayoutGroup;

    public TMP_InputField FriendNameInputField => friendNameInputField;
    public Button SubmitButton => submitButton;

    public GameObject NotFoundPanel => notFoundPanel;
    public GameObject ConfirmationPanel => confirmationPanel;

    public Button RequestsButton => requestsButton;
    public TMP_Text QuantityRequestsText => quantityRequestsText;

    public GameObject RequestPanel => requestPanel;
    public TMP_Text AdviceText => adviceText;
    public Button AcceptButton => acceptButton;
    public Button DeclineButton => declineButton;

    public TMP_Text DeviceIdText => deviceIdText;
}
