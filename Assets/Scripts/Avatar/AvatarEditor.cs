using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AvatarEditor : MonoBehaviour
{
    public SkinnedMeshRenderer skinRenderer;
    public GameObject[] hatStyles;
    public Material[] skinColors;
    public GameObject[] clothes;

    // Listas de nombres configurables desde el Inspector
    public string[] hatNames;
    public Image[] skinImage;
    public string[] clothesNames;

    public TMP_Text hatText;
    public Image skinText;
    public TMP_Text clothesText;

    public Button hatLeftButton, hatRightButton;
    public Button skinLeftButton, skinRightButton;
    public Button clothesLeftButton, clothesRightButton;

    private int currentSkinIndex = 0;
    private int currentHatIndex = 0;
    private int currentClothesIndex = 0;

    public string Hat => currentHatIndex.ToString();
    public string Skin => currentSkinIndex.ToString();
    public string Clothes => currentClothesIndex.ToString();

    private PlayFabController playFabController;

    private void Start()
    {
        playFabController = FindObjectOfType<PlayFabController>();
        playFabController.OnUserDataReceived += OnDataReceived;
        playFabController.RequestUserData();

        // Asignar eventos a los botones
        hatLeftButton.onClick.AddListener(PreviousHat);
        hatRightButton.onClick.AddListener(NextHat);

        skinLeftButton.onClick.AddListener(PreviousSkin);
        skinRightButton.onClick.AddListener(NextSkin);

        clothesLeftButton.onClick.AddListener(PreviousOutfit);
        clothesRightButton.onClick.AddListener(NextClothes);

        UpdateTextUI();
    }

    // === MÉTODOS PARA CAMBIAR LA APARIENCIA ===

    public void NextSkin()
    {
        currentSkinIndex = (currentSkinIndex + 1) % skinColors.Length;
        ApplySkin();
    }

    public void PreviousSkin()
    {
        currentSkinIndex = (currentSkinIndex - 1 + skinColors.Length) % skinColors.Length;
        ApplySkin();
    }

    private void ApplySkin()
    {
        skinRenderer.material = skinColors[currentSkinIndex];
        SaveAppearance();
        UpdateSkinImage();
    }

    public void NextHat()
    {
        currentHatIndex = (currentHatIndex + 1) % hatStyles.Length;
        ApplyHat();
    }

    public void PreviousHat()
    {
        currentHatIndex = (currentHatIndex - 1 + hatStyles.Length) % hatStyles.Length;
        ApplyHat();
    }

    private void ApplyHat()
    {
        foreach (var hat in hatStyles) hat.SetActive(false);
        hatStyles[currentHatIndex].SetActive(true);
        SaveAppearance();
        UpdateTextUI();
    }

    public void NextClothes()
    {
        currentClothesIndex = (currentClothesIndex + 1) % clothes.Length;
        ApplyClothes();
    }

    public void PreviousOutfit()
    {
        currentClothesIndex = (currentClothesIndex - 1 + clothes.Length) % clothes.Length;
        ApplyClothes();
    }

    private void ApplyClothes()
    {
        foreach (var outfit in clothes) outfit.SetActive(false);
        clothes[currentClothesIndex].SetActive(true);
        SaveAppearance();
        UpdateTextUI();
    }

    // === GUARDAR APARIENCIA EN PLAYFAB ===
    private void SaveAppearance()
    {
        if (playFabController != null)
        {
            playFabController.SaveAppearanceToPlayFab(Hat, Skin, Clothes);
        }
    }

    // === CARGAR APARIENCIA DESDE PLAYFAB ===
    public void OnDataReceived(string hat, string skin, string clothes)
    {
        currentHatIndex = int.Parse(hat);
        currentSkinIndex = int.Parse(skin);
        currentClothesIndex = int.Parse(clothes);

        ApplySkin();
        ApplyHat();
        ApplyClothes();
    }

    // === ACTUALIZAR TEXTOS E IMÁGENES EN LA UI ===
    private void UpdateTextUI()
    {
        hatText.text = $"{GetItemName(hatNames, currentHatIndex)}";
        clothesText.text = $"{GetItemName(clothesNames, currentClothesIndex)}";
    }

    // Método para actualizar la imagen de la piel en la UI
    private void UpdateSkinImage()
    {
        if (skinImage.Length > 0)
        {
            // Cambiar la imagen para reflejar el color de piel actual
            for (int i = 0; i < skinImage.Length; i++)
            {
                skinImage[i].gameObject.SetActive(i == currentSkinIndex);
            }
        }
    }

    private string GetItemName(string[] names, int index)
    {
        if (names != null && index >= 0 && index < names.Length)
        {
            return names[index];
        }
        return "Desconocido"; // Nombre por defecto si no se asigna
    }
}
