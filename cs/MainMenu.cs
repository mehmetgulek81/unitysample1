using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour, IStoreListener
{
    private Button volumeButton;
    private Sprite volumeOffSprite;
    private Sprite volumeOnSprite;
    private SoundEffectPlayer soundEffectPlayer;

    private void Awake()
    {
        this.soundEffectPlayer = FindObjectOfType<SoundEffectPlayer>();
        if (this.soundEffectPlayer == null)
        {
            this.soundEffectPlayer = this.gameObject.AddComponent<SoundEffectPlayer>();
        }

        this.volumeButton.image.overrideSprite = PlayerPrefs.GetInt(Constants.Keys.SoundOn, 1) == 1
            ? this.volumeOnSprite 
            : this.volumeOffSprite;

        AudioListener.volume = PlayerPrefs.GetInt(Constants.Keys.SoundOn, 1) == 1 ? 1 : 0;
    }

    public void Start()
    {
        _ = GameManager.instance;
        AdManager.instance.ShowHideBottomBanner();
    }

    public void PurchaseCompleted(Product product)
    {
        Debug.Log("purchase complete: " + product.ToString());
        if (product.definition.id == Constants.NoAdsProductID)
        {
            Debug.Log("should remove ads now");
            AdManager.SetNoAdsEnabled(true);
        }
        else if (product.definition.id == Constants.SuperLevelsProductID)
        {
            Debug.Log("should unlock super levels now");
            PlayerPrefs.SetInt(Constants.Keys.SuperLevelsEnabled, 1);
            PlayerPrefs.SetInt(Constants.Keys.MaxPassedLevelNumber, Constants.NumberOfNormalLevels);
            PlayerPrefs.Save();
        }
    }

    public void NoAdsTapped()
    {
        Debug.Log("No ads tapped");
        this.soundEffectPlayer.Play("click");
        Debug.Log("should remove ads now");
        AdManager.SetNoAdsEnabled(true);
    }

    public void PlayTapped()
    {
        this.soundEffectPlayer.Play("click");

        if (GameManager.instance.IsGameFinished())
        {
            Debug.Log("Game finished, restarting from level 1");
            GameManager.instance.PassedLevelNumber = 0;
            PlayerPrefs.Save(); 
        }

        var levelIndex = GameManager.instance.PassedLevelNumber;
        GameManager.instance.SetCurrentLevelNumber(levelIndex + 1);
        SceneManager.LoadScene(Constants.SceneIndex.Level);
    }

    public void LeaderboardTapped()
    {
        this.soundEffectPlayer.Play("click");
        Debug.Log("Leaderboard tapped");
        
    }

    public void LevelsTapped()
    {
        Debug.Log("Levels tapped");
        this.soundEffectPlayer.Play("click");
        LevelListMenu.showingSuperLevels = false;
        SceneManager.LoadScene(Constants.SceneIndex.LevelList);
    }

    public void SuperLevelsTapped()
    {
        Debug.Log("Super Levels tapped");
        this.soundEffectPlayer.Play("click");
        if (PlayerPrefs.GetInt(Constants.Keys.SuperLevelsEnabled, 0) == 1)
        {
            Debug.Log("Super Levels already purchased");
            LevelListMenu.showingSuperLevels = true;
            SceneManager.LoadScene(Constants.SceneIndex.LevelList);
        }
        else
        {
            Debug.Log("Initiating Super Levels purchase");
            // CodelessIAPButton'u programlý olarak tetikle
            CodelessIAPButton purchaseButton = transform.Find("SuperLevelsButton").GetComponentInChildren<CodelessIAPButton>();
            if (purchaseButton != null)
            {
                // TODO
                // purchaseButton.OnClick();
            }
        }
    }

    public void WatchAdsTapped()
    {
        Debug.Log("WatchAds tapped");
    }

    public void VolumeTapped()
    {
        Debug.Log("Volume tapped");
        var soundOn = GameManager.instance.ToggleSoundSetting();
        Debug.Log("Volume changed to: " + soundOn);
        this.volumeButton.image.overrideSprite = soundOn ? volumeOnSprite : volumeOffSprite;
        AudioListener.volume = soundOn ? 1 : 0;
    }

    public void QuitTapped()
    {
        Application.Quit();
    }

    void IStoreListener.OnInitializeFailed(InitializationFailureReason error)
    {
        // throw new System.NotImplementedException();
    }

    void IStoreListener.OnInitializeFailed(InitializationFailureReason error, string message)
    {
        // throw new System.NotImplementedException();
    }

    PurchaseProcessingResult IStoreListener.ProcessPurchase(PurchaseEventArgs purchaseEvent)
    {
        // throw new System.NotImplementedException();
        return PurchaseProcessingResult.Complete;
    }

    void IStoreListener.OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        // throw new System.NotImplementedException();
    }

    void IStoreListener.OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        // throw new System.NotImplementedException();
    }
}
