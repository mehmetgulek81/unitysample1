using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

#pragma warning disable IDE0051 // Remove unused private members

public class LevelBehaviour : MonoBehaviour
{
    private Camera cam;
    private ShooterGameObject shooter;
    private readonly List<TargetGameObject> Targets = new();
    private bool targetsInitialized = false;

    private Level level
    {
        get
        {
            return GameManager.instance.GetCurrentLevel();
        }
    }

    public Canvas levelFinishedMenu;
    private BulletGameObject templateBullet;
    private SuccessTargetGameObject templateSuccessTarget;
    private FailTargetGameObject templateFailTarget;
    public SoundEffectPlayer soundEffectPlayer;
    
    private void Awake()
    {
        this.cam = Camera.main;
        this.shooter = FindObjectOfType<ShooterGameObject>();
        this.templateBullet = FindObjectOfType<BulletGameObject>();
        this.templateSuccessTarget = FindObjectOfType<SuccessTargetGameObject>();
        this.templateFailTarget = FindObjectOfType<FailTargetGameObject>();
        this.soundEffectPlayer = FindObjectOfType<SoundEffectPlayer>();
        if (this.soundEffectPlayer == null)
        {
            this.soundEffectPlayer = this.gameObject.AddComponent<SoundEffectPlayer>();
        }
        AdManager.instance.ShowHideBottomBanner();
        if (GameManager.instance.CurrentLevelNumber < 0)
        {
            var levelIndex = GameManager.instance.PassedLevelNumber;
            GameManager.instance.SetCurrentLevelNumber(levelIndex + 1);
        }
    }

    private void GenerateLevelsAndPersist()
    {
        var levels = new List<Level>();
        for (int n= 0; n< Constants.NumberOfLevels; ++n)
        {
            var level = GameManager.instance.GenerateLevelRandomlyWithCircle(n);
            levels.Add(level);
        }
        var levelsString = JsonConvert.SerializeObject(levels);
        File.WriteAllText("Assets\\Levels\\levels.json", levelsString);
    }

    void Start()
    {
        // GenerateLevelsAndPersist(); // already done! if something changes in the generating algorithm, it should be called again! // TODO comment out after setting levels
        Debug.Log("Level start: " + level.Number);

        this.level.RemainingNOB = level.NumberOfBullets;

        this.templateSuccessTarget.currentLevel = level;
        this.templateFailTarget.currentLevel = level;
        this.templateBullet.currentLevel = level;
        this.shooter.currentLevel = level;

        this.PositionShooter();
        this.InitializeLevelObjects();

        this.shooter.SetWeaponConfiguration(level.WeaponConfiguration);
    }

    // Update is called once per frame
    void Update()
    {
        if (this.IsLevelFinished())
        {
            this.LevelSucceeded();
            return;
        }
    }

    private bool IsLevelFinished()
    {
        return targetsInitialized && Targets.FindLastIndex((TargetGameObject obj) => obj is SuccessTargetGameObject) == -1;
    }

    private void InitializeLevelObjects()
    {
        this.InitializeTargets();

        //  prevent screen dimming
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    private void PositionShooter()
    {
        Sprite successTarget = IMG2Sprite.LoadNewSprite(Application.dataPath + "/Resources/Sprites/shooter.png");
        float targetHalfWidth = successTarget.rect.width * shooter.transform.localScale.x / 2.0f;

        float adHeight = Mathf.Max(AdManager.instance.BannerHeight, 80) + 50;
        float horizontalMargin = targetHalfWidth + 30.0f;
        var totalWidth = Screen.safeArea.xMax - Screen.safeArea.xMin;
        var totalHeight = Screen.safeArea.yMax - Screen.safeArea.yMin;
        var canvasWidth = totalWidth - 2 * horizontalMargin;
        var canvasHeight = (float)Constants.GameAreaRatio * totalWidth;
        var scaleFactorX = canvasWidth / 2.0f;
        var scaleFactorY = canvasHeight / 2.0f;
        var xOffset = canvasWidth / 2.0f + horizontalMargin;
        var yOffset = (totalHeight - canvasHeight) / 2.0f + canvasHeight / 2.0f + adHeight;

        Vector3 screenPoint = new(xOffset + (float)level.WeaponConfiguration.Position.x * scaleFactorX,
                yOffset + (float)level.WeaponConfiguration.Position.y * scaleFactorY,
                cam.nearClipPlane);
        Vector3 localPoint = cam.ScreenToWorldPoint(screenPoint);
        localPoint.z = Constants.CollisionZLevel;
        this.shooter.transform.localPosition = localPoint;
        this.shooter.transform.parent = transform;

        Debug.Log("Shooter position: " + shooter.transform.position);
    }

    private void InitializeTargets()
    {
        if (this.level.Targets.Count == 0)
        {
            //  avoid division by zero exception
            return;
        }

        Sprite successTarget = IMG2Sprite.LoadNewSprite(Application.dataPath + "/Resources/Sprites/target_coin.jpeg");
        
        float targetHalfWidth = successTarget.rect.width / 2.0f;

        float adHeight = Mathf.Max(AdManager.instance.BannerHeight, 80) + 50;
        float horizontalMargin = targetHalfWidth + 10.0f;
        var totalWidth = Screen.safeArea.xMax - Screen.safeArea.xMin;
        var totalHeight = Screen.safeArea.yMax - Screen.safeArea.yMin;
        var canvasWidth = totalWidth - 2 * horizontalMargin;
        var canvasHeight = (float)Constants.GameAreaRatio * canvasWidth;
        var scaleFactorX = canvasWidth / 2.0f;
        var scaleFactorY = canvasHeight / 2.0f;
        var xOffset = canvasWidth / 2.0f + horizontalMargin;
        var yOffset = (totalHeight - canvasHeight) / 2.0f + canvasHeight / 2.0f + adHeight;
        var indexInList = -1;
        double firstTargetAngle = 0.0;

        foreach (Target targetItem in level.Targets)
        {
            ++indexInList;
            Vector3 screenPoint = new(xOffset + (float)targetItem.Position.x * scaleFactorX,
                yOffset + (float)targetItem.Position.y * scaleFactorY,
                cam.nearClipPlane);
            Vector3 localPoint = cam.ScreenToWorldPoint(screenPoint);
            localPoint.z = Constants.CollisionZLevel;

            var shooterPosition = shooter.transform.position;

            TargetGameObject theTarget = (targetItem.Type == TargetType.Success)
                ? Instantiate(templateSuccessTarget, localPoint, Quaternion.identity)
                : Instantiate(templateFailTarget, localPoint, Quaternion.identity);
            theTarget.currentLevel = templateFailTarget.currentLevel;

            theTarget.transform.parent = transform;
            TargetGameObject.rotationOrigin = shooterPosition;
            theTarget.OriginalPosition = new Position(targetItem.Position.x, targetItem.Position.y);
            Debug.Log((targetItem.Type == TargetType.Success ? "Success" : "Fail") + " target original position: " +  theTarget.OriginalPosition.x + ", " + theTarget.OriginalPosition.y);
            this.Targets.Add(theTarget);

            if (this.level.TargetDistanceSqr < 0.0)
            {
                this.level.TargetDistanceSqr = (shooterPosition.x - localPoint.x) * (shooterPosition.x - localPoint.x) +
                    (shooterPosition.y - localPoint.y) * (shooterPosition.y - localPoint.y);
            }
        }

        this.CalculateApproachSpeeds(this.Targets);

        this.targetsInitialized = true;
    }

    public void CalculateApproachSpeeds(List<TargetGameObject> targets)
    {
        Debug.Log("calculating app speeds. target count: " + targets.Count);
        int index = -1;
        double firstTargetAngle = 0.0;
        foreach (var target in targets)
        {
            ++index;
            double approachSpeed = 0.0;

            if (target.currentLevel.TargetsConfiguration.ApproachEffectDuration > 0.0)
            {
                if (index == 0)
                {
                    firstTargetAngle = Mathf.Atan2((float)target.OriginalPosition.y, (float)target.OriginalPosition.x) * 180.0 / Mathf.PI;
                }
                else
                {
                    double targetAngle = Mathf.Atan2((float)target.OriginalPosition.y, (float)target.OriginalPosition.x) * 180.0 / Mathf.PI;
                    double combinedAngle = firstTargetAngle - Constants.unitAngle * index;
                    double diffAngle = -targetAngle + combinedAngle;
                    if (diffAngle < 0.0) diffAngle += 360.0;
                    approachSpeed = diffAngle / target.currentLevel.TargetsConfiguration.ApproachEffectDuration;
                }
            }

            target.approachSpeed = approachSpeed;
        }
    }

    private void LevelSucceeded()
    {
        Debug.Log("Level succeeded: target count: " + Targets.Count);

        if (GameManager.instance.IsGameFinished())
        {
            Debug.Log("Game finished!");
            
            AdManager.instance.ShowRewardedVideo(() =>
            {
                GameManager.instance.ReturnBackToMenuScene();
            });                          
        }

        // Seviye numarası çiftse reklam göster
        if (this.level.Number % 2 == 0)
        {
            Debug.Log($"Seviye {this.level.Number} çift, reklam gösteriliyor...");
            
            AdManager.instance.ShowInterstitialAd();                        
        }

        int levelPointsNew = this.level.Number * 10 + this.level.RemainingNOB;

        GameManager.instance.OnLevelSucceeded(this.level, levelPointsNew);

        this.soundEffectPlayer.Play("bonus");

        SceneManager.LoadScene(Constants.SceneIndex.Level);
    }

    private void LevelFailed()
    {
        Debug.Log("Level failed");
        GameManager.instance.OnLevelFailed(level);

        this.soundEffectPlayer.Play("game_over2");

        SceneManager.LoadScene(Constants.SceneIndex.Level);
    }

    private IEnumerator WaitToCheckOutOfBullet()
    {
        // TODO check edge cases like; total # of bullets is less than or equal to 2!
        if (BulletGameObject.OutOfSceneMilliseconds < double.MaxValue / 2)
        {
            yield return new WaitForSeconds((float)(0.001 * 2.0 * BulletGameObject.OutOfSceneMilliseconds));

            var successTargetCount = FindObjectsByType<SuccessTargetGameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Count()
                -1; // -1 is necessary since one success target is template and always exists in the scene
            Debug.Log("success target count: " + successTargetCount + ", remaining bullets: " + level.RemainingNOB);
            if (this.level.RemainingNOB < successTargetCount && !IsLevelFinished())
            {
                Debug.Log("Level failed since bullets are not enough");
                LevelFailed();
            }
        }
    }

    public void ShootOneBullet()
    {
        Debug.Log("Shoot one bullet. Number of bullets: " + level.RemainingNOB);

        StartCoroutine(WaitToCheckOutOfBullet());

        if (this.level.RemainingNOB > 0)
        {
            --this.level.RemainingNOB;
            
            float halfWidth = Screen.width / 2;
            float halfHeight = Screen.height / 2;

            var localPoint = shooter.transform.position; // + shooter.transform.up / 2; // shooter nesnesinin bitişine getirmek için
            localPoint.z = Constants.CollisionZLevel;
            
            BulletGameObject bullet = Instantiate(templateBullet, localPoint, Quaternion.Euler(0, 0, shooter.transform.localEulerAngles.z));
            bullet.timeFired = DateTime.Now;
            bullet.soundEffectPlayer.Play("pew");
            bullet.speed = 30;
            bullet.initialPosition = localPoint;
            bullet.currentLevel = templateBullet.currentLevel;

            bullet.transform.parent = transform;
        }
    }

   private IEnumerator ShooterRotationCoroutine()
    {
        while (true)
        {
            // speed is degrees per second
            float speed = 360;
            float realtime = 0.01f;
            this.shooter.transform.Rotate(Vector3.back, speed * realtime);
            yield return new WaitForSecondsRealtime(realtime);
        }
    }

    public void TargetHitByBullet(BulletTargetPair pair)
    {
        //  find target and remove
        TargetGameObject theTarget = Targets.Find(element => element.gameObject == pair.target);
        if (theTarget != null)
        {
            if (theTarget is FailTargetGameObject)
            {
                theTarget.Freeze();
                pair.bullet.Freeze();
                LevelFailed();
            }
            else if (theTarget is SuccessTargetGameObject)
            {
                theTarget.gameObject.SetActive(false);
                Targets.Remove(theTarget);
                pair.bullet.gameObject.SetActive(false);                
            }

            CalculateApproachSpeeds(Targets);
        }

        //  find bullet and remove
        pair.bullet.OnHit();
    }

    public void GoToMenuScene()
    {
        this.soundEffectPlayer.Play("click");

        GameManager.instance.ReturnBackToMenuScene(); // Erişim hatası nedeniyle yoruma alındı
    }
}

#pragma warning restore IDE0051 // Remove unused private members