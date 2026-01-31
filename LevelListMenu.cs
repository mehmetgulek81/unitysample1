using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LevelListMenu : MonoBehaviour
{
    [SerializeField] private Transform levelButtonContainer;
    [SerializeField] private Button levelButtonPrefab;
    [SerializeField] private ScrollRect scrollRect;

    public static bool showingSuperLevels = false;
    private SoundEffectPlayer soundEffectPlayer;

    public void Start()
    {
        Debug.Log("LevelListMenu.Start()");
        
        // Debug amaçlý test deðerleri - en baþta set edilmelidir
        // PlayerPrefs.SetInt(Constants.Keys.MaxPassedLevelNumber, 25);
        // AdManager.SetNoAdsEnabled(false);
        // PlayerPrefs.SetInt(Constants.Keys.SuperLevelsEnabled, 1);
        // PlayerPrefs.SetInt(Constants.Keys.MaxPassedLevelNumber, Constants.NumberOfNormalLevels);
        // PlayerPrefs.Save();
        
        this.soundEffectPlayer = FindObjectOfType<SoundEffectPlayer>();
        if (this.soundEffectPlayer == null)
        {
            this.soundEffectPlayer = gameObject.AddComponent<SoundEffectPlayer>();
        }

        _ = GameManager.instance;
        AdManager.instance.ShowHideBottomBanner();

        if (this.scrollRect == null)
        {
            this.scrollRect = FindObjectOfType<ScrollRect>();
            if (this.scrollRect != null)
            {
                Debug.Log($"ScrollRect bulundu: {this.scrollRect.gameObject.name}");
            }
            else
            {
                Debug.LogError("ScrollRect bulunamadý!");
                return;
            }
        }

        if (this.levelButtonContainer == null)
        {
            this.levelButtonContainer = this.transform.Find("LevelButtonContainer2");
            
            if (this.levelButtonContainer == null)
            {
                this.levelButtonContainer = GameObject.Find("LevelButtonContainer2")?.transform;
            }

            if (this.levelButtonContainer != null)
            {
                Debug.Log($"LevelButtonContainer2 bulundu: {this.levelButtonContainer.gameObject.name}");
            }
            else
            {
                Debug.LogError("LevelButtonContainer2 bulunamadý!");
                return;
            }
        }

        if (this.levelButtonPrefab == null)
        {
            this.levelButtonPrefab = Resources.Load<Button>("Prefabs/LevelButton");
            if (this.levelButtonPrefab == null)
            {
                Debug.LogError("LevelButton prefab'ý bulunamadý!");
                return;
            }
        }

        this.SetupGridLayout();
        this.CreateLevelButtons();
    }

    private void SetupGridLayout()
    {
        // GridLayoutGroup ekle/güncelle
        GridLayoutGroup glg = levelButtonContainer.GetComponent<GridLayoutGroup>();
        if (glg == null)
        {
            glg = levelButtonContainer.gameObject.AddComponent<GridLayoutGroup>();
        }

        glg.cellSize = new Vector2(200, 125);
        glg.spacing = new Vector2(50, 50);
        glg.startCorner = GridLayoutGroup.Corner.UpperLeft;
        glg.startAxis = GridLayoutGroup.Axis.Horizontal;
        glg.constraintCount = 4;
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;

        // ContentSizeFitter ekle/güncelle - Scroll'un çalýþmasý için ZORUNLU
        ContentSizeFitter csf = levelButtonContainer.GetComponent<ContentSizeFitter>();
        if (csf == null)
        {
            csf = levelButtonContainer.gameObject.AddComponent<ContentSizeFitter>();
        }
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ScrollRect'in Content ayarýný kontrol et
        if (scrollRect.content != levelButtonContainer as RectTransform)
        {
            scrollRect.content = levelButtonContainer as RectTransform;
        }

        // ScrollRect ayarlarý
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        Debug.Log("GridLayoutGroup ve ContentSizeFitter kuruldu!");
    }

    private void CreateLevelButtons()
    {
        int numberOfLevels = Constants.NumberOfLevels;
        int maxPassedLevel = GameManager.instance.MaxPassedLevelNumber;
        if (maxPassedLevel == 0) {
            maxPassedLevel = -1; // Hiç level geçilmemiþse -1 yap
        }

        Debug.Log($"Oluþturulacak Level Sayýsý: {numberOfLevels}");
        Debug.Log($"Max Passed Level: {maxPassedLevel}");

        for (int i = (showingSuperLevels ? Constants.NumberOfNormalLevels : 0);
            i < (showingSuperLevels ? numberOfLevels : Constants.NumberOfNormalLevels);
            ++i)
        {
            Button newButton = Instantiate(levelButtonPrefab, levelButtonContainer);

            TextMeshProUGUI buttonText = newButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = $"{i + 1}";
            }

            // Level numarasý max passed level'den büyükse butonu inaktif yap
            bool isLevelUnlocked = i <= maxPassedLevel;
            newButton.interactable = isLevelUnlocked;

            // Metin rengini ayarla - inaktif butonlarýn metni koyu gri/kahverengi olmalý
            if (buttonText != null)
            {
                if (isLevelUnlocked)
                {
                    buttonText.color = Color.white;
                }
                else
                {
                    // Koyu gri/kahverengi renk
                    buttonText.color = new Color(0.4f, 0.25f, 0.15f, 1f);
                }
            }

            // Görünümü ayarla - inaktif butonlarý koyu kahverengi yap
            Image buttonImage = newButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                if (isLevelUnlocked)
                {
                    buttonImage.color = Color.white;
                }
                else
                {
                    // Arka plan rengi açýk tutulacak, sadece metin rengi deðiþtirildi
                    buttonImage.color = Color.white;
                }
            }

            int levelIndex = i;
            newButton.onClick.AddListener(() => OnLevelButtonClicked(levelIndex));
        }

        // Layout'u yeniden hesapla - Sýrasý önemli!
        LayoutRebuilder.ForceRebuildLayoutImmediate(levelButtonContainer as RectTransform);
        Canvas.ForceUpdateCanvases();
        
        RectTransform containerRect = levelButtonContainer as RectTransform;
        RectTransform viewportRect = scrollRect.viewport as RectTransform;
        
        Debug.Log($"? Container Height: {containerRect.rect.height}");
        Debug.Log($"? Container Preferred Height: {LayoutUtility.GetPreferredHeight(containerRect)}");
        Debug.Log($"? Viewport Height: {viewportRect.rect.height}");
        Debug.Log($"? Scrollable: {containerRect.rect.height > viewportRect.rect.height}");
        
        // ScrollRect'in scroll position'ýný sýfýrla
        scrollRect.verticalNormalizedPosition = 1f;
    }

    private void OnLevelButtonClicked(int levelIndex)
    {
        Debug.Log($"Level {levelIndex + 1} seçildi");
        soundEffectPlayer.Play("click");
        GameManager.instance.CurrentLevelNumber = levelIndex + 1;
        SceneManager.LoadScene(Constants.SceneIndex.Level);
    }

    public void BackTapped()
    {
        soundEffectPlayer.Play("click");
        SceneManager.LoadScene(Constants.SceneIndex.MainMenu);
    }
}
