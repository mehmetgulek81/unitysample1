using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class GameManager
{
	private static GameManager _instance;
    public int CurrentLevelNumber { get; set; } = -1;

    public int MaxPassedLevelNumber
    {
        get
        {
            return PlayerPrefs.GetInt(Constants.Keys.MaxPassedLevelNumber, 0);
        }
        private set
        {
            PlayerPrefs.SetInt(Constants.Keys.MaxPassedLevelNumber, value);
            PlayerPrefs.Save();
        }
    }

    public int PassedLevelNumber
    {
        get
        {
            return PlayerPrefs.GetInt(Constants.Keys.PassedLevelNumber, 0);
        }
        set
        {
            PlayerPrefs.SetInt(Constants.Keys.PassedLevelNumber, value);
            PlayerPrefs.Save();
        }
    }

    public static GameManager instance
	{
		get
		{
			if (_instance == null)
				_instance = new GameManager();
			return _instance;
		}
	}

    public List<Level> AllLevels { get; private set; }

    public int TotalPoints { get; private set; }
        = PlayerPrefs.GetInt(Constants.Keys.TotalPointsKey, 0);

    private GameManager ()
    {
        this.AllLevels = JsonConvert.DeserializeObject<List<Level>>(File.ReadAllText("Assets" + Path.DirectorySeparatorChar + "Levels" + Path.DirectorySeparatorChar + "levels.json"));
    }

    public bool SuperLevelsEnabled
    {
        get
        {
            int superLevelsEnabled = PlayerPrefs.GetInt(Constants.Keys.SuperLevelsEnabled, 0);
            return superLevelsEnabled == 1;
        }
        set
        {
            PlayerPrefs.SetInt(Constants.Keys.SuperLevelsEnabled, value ? 1 : 0);
            PlayerPrefs.Save();
        }        
    }

    public bool IsGameFinished()
    {
        int passedLevel = this.PassedLevelNumber;
        if (this.SuperLevelsEnabled)
        {
            return passedLevel == Constants.NumberOfLevels;
        }
        else
        {
            return passedLevel == Constants.NumberOfNormalLevels;
        }
    }

    public void SetCurrentLevelNumber(int newLevelNumber)
    {
        this.CurrentLevelNumber = newLevelNumber;
    }

    public Level GetCurrentLevel()
    {
        return this.CurrentLevelNumber < 0 
            ? null
            : AllLevels[this.CurrentLevelNumber - 1];
    }

    public void OnLevelSucceeded (Level level, int levelPointsNew)
    {
        this.PassedLevelNumber = level.Number;
        if (level.Number <= Constants.NumberOfNormalLevels)
        {
            this.CurrentLevelNumber = level.Number + 1;
            if (this.CurrentLevelNumber > Constants.NumberOfNormalLevels)
            {
                if (!this.SuperLevelsEnabled)
                {
                    this.CurrentLevelNumber = 1;
                }
            }
        }
        else
        {
            this.CurrentLevelNumber = level.Number + 1;
            if (this.CurrentLevelNumber > Constants.NumberOfLevels)
            {
                this.CurrentLevelNumber = 1 + Constants.NumberOfNormalLevels;
            }
        }
        PlayerPrefs.Save();

        var maxpassedlevelnumber = MaxPassedLevelNumber;

        if (level.Number > maxpassedlevelnumber)
        {
            MaxPassedLevelNumber = level.Number;
        }
        Debug.Log("GameManager::OnLevelSucceeded::PassedLevelNumber" + level.Number);

        // if user has made more points from the past for this level, increase total points.
        var levelPointsString = PlayerPrefs.GetString(Constants.Keys.LevelPointsKey, "");
        var levelPoints = levelPointsString.Split("|");
        if (levelPointsString == "")
        {
            levelPoints = new string[Constants.NumberOfLevels];
            for (int i = 0; i < Constants.NumberOfLevels; ++i)
            {
                levelPoints[i] = "0";
            }
        }
        if (levelPointsNew > int.Parse(levelPoints[level.Number - 1]))
        {
            levelPoints[level.Number - 1] = levelPointsNew.ToString();
            levelPointsString = String.Join('|', levelPoints);
            PlayerPrefs.SetString(Constants.Keys.LevelPointsKey, levelPointsString);
            TotalPoints = levelPoints.Sum(lp => int.Parse(lp));
            PlayerPrefs.SetInt(Constants.Keys.TotalPointsKey, TotalPoints);
            PlayerPrefs.Save();
        }
    }

    

    public void OnLevelFailed (Level level)
    {
        this.PassedLevelNumber = level.Number - 1;
        PlayerPrefs.Save();
    }

    public bool ToggleSoundSetting()
    {
        int soundOn = PlayerPrefs.GetInt(Constants.Keys.SoundOn, 1);
        int newValue = soundOn == 1 ? 0 : 1;
        PlayerPrefs.SetInt(Constants.Keys.SoundOn, newValue);
        PlayerPrefs.Save();
        return newValue == 1;
    }

    public void ReturnBackToMenuScene ()
    {
        SceneManager.LoadScene(Constants.SceneIndex.MainMenu);
    }

    private bool TargetPositionIsInsideScreen(Position pos)
    {
        return pos.x > -1.0 + Constants.targetRadius && pos.x < 1.0 - Constants.targetRadius
            && pos.y > -1.0 + Constants.targetRadius && pos.y < 1.0 + Constants.targetRadius;
    }

    private bool TargetsCoincidePoint(IEnumerable<Target> targets, Position pos, double density = 1.0f)
    {
        foreach (Target target in targets)
        {
            if (Mathf.Pow((float)(target.Position.x) - (float)pos.x, 2.0f) + Mathf.Pow((float)(target.Position.y) - (float)pos.y, 2.0f)
                <= Mathf.Pow((float)Constants.targetRadius * 2.0f / (float)density, 2.0f)) return true;
        }
        return false;
    }

    public Level GenerateLevelRandomlyWithCircle(int passedLevel)
    {
        var level = new Level();

        // level numbers increment by 1
        level.Number = passedLevel + 1;

        // generate targets
        int sublevel = level.Number % 10;
        if (sublevel == 0) sublevel = 10;

        // number of targets; a number between 1 and 10
        int numberOfTargets = sublevel;

        // initialize Targets
        level.Targets = new List<Target>();

        string shapeJson = File.ReadAllText("Assets/Levels/Shapes/O.json");
        Shape shape = JsonConvert.DeserializeObject<Shape>(shapeJson);
        Fragment circle = shape.fragments[0];
        bool [] isFree = new bool[circle.points.Count];
        for (int i = 0; i < circle.points.Count; ++i) isFree[i] = true;
        int [] selected = new int[numberOfTargets];
        int numberOfFreePoints = circle.points.Count;

        // calculate number of adjacent positions that cannot be filled when a position is filled (collision)
        // this number can also be calculated first and written to Constants.cs
        int cannotFillAdjacent = 0;
        for (int i= 1; i< circle.points.Count; ++i)
        {
            if (!GeometryUtil.DistanceIsLessThan(circle.points[0], circle.points[i], (float)Constants.targetRadius * 2.0f))
            {
                cannotFillAdjacent = i - 1;
                break;
            }
        }

        // place targets one-by-one
        for (int n= 0; n< numberOfTargets; ++n)
        {
            // pick a random point
            int r = 1 + Random.Range(0, numberOfFreePoints); // we will pick <r>th free point as new target
            int j;
            for (j= 0; j < circle.points.Count; ++j)
            {
                if (isFree[j]) --r;
                if (r == 0) break;
            }

            if (j >= circle.points.Count)
            {
                // cannot proceed, no more space to put any extra target
                numberOfTargets = n;
                break;
            }
            selected[n] = j;
            isFree[j] = false;
            for (int i= 0; i< cannotFillAdjacent; ++i)
            {
                isFree[(j - i + circle.points.Count) % circle.points.Count] = false;
                isFree[(j + i) % circle.points.Count] = false;
            }
            numberOfFreePoints = 0;
            for (int i= 0; i< circle.points.Count; ++i)
            {
                if (isFree[i]) ++numberOfFreePoints;
            }
        }

        // sort selected array in ascending order
        // it will be necessary in consequent operations; not much efficieny is needed right now
        for (int j= 0; j< numberOfTargets; ++j)
        {
            for (int k= 1; k< numberOfTargets; ++k)
            {
                if (selected[j] > selected[k])
                {
                    int tmp = selected[j];
                    selected[j] = selected[k];
                    selected[k] = tmp;
                }
            }
        }

        for (int i= 0; i< numberOfTargets; ++i)
        {
            level.Targets.Add(new Target(TargetType.Success, circle.points[selected[i]]));
            Debug.Log("target " + i + ": " + (180.0 / Mathf.PI * Mathf.Atan2((float)circle.points[selected[i]].y, (float)circle.points[selected[i]].x)));
        }

        int numberOfFailTargets = numberOfTargets / 3;
        
        // TODO fail targets should not be adjacent
        // example: targets: 0, 1, 2, 3, 4, 5, 6, 7
        // assume that we pick 2 as the first fail target
        // 1, 2, and 3 are now not available for next
        // we should pick one of 0, 4, 5, 6, 7

        isFree = new bool[numberOfTargets];
        for (int k = 0; k < numberOfTargets; ++k) isFree[k] = true;
        for (int i= 0; i< numberOfFailTargets; ++i)
        {
            int numberOfFreePlaces = 0;
            for (int k = 0; k< numberOfTargets; ++k)
            {
                if (isFree[k]) ++numberOfFreePlaces;
            }

            int r = 1 + Random.Range(0, numberOfFreePlaces);
            int j = 0;
            for (; j < numberOfTargets; ++j)
            {
                if (isFree[j]) --r;
                if (r == 0) break;
            }

            if (j >= numberOfTargets)
            {
                numberOfFailTargets = i;
                Debug.LogError("SOME FAIL TARGETS CANNOT BE PLACED!"); // impossible ??
                break;
            }

            level.Targets[j].Type = TargetType.Fail;
            isFree[j] = false;
            isFree[(j - 1 + numberOfTargets) % numberOfTargets] = false;
            isFree[(j + 1) % numberOfTargets] = false;
        }

        // number of bullets is a function of level number and number of success targets
        level.NumberOfBullets = (numberOfTargets - numberOfFailTargets) + 50 - sublevel * 5;

        // position shooter on the screen center
        Position shooterPosition = new Position(0.0, 0.0);

        var shooterRotateDirection = RotateDirection.Clockwise;

        // shooter speed is selected according to level number
        // TODO improve
        double shooterSpeed = level.Number <= Constants.NumberOfNormalLevels
            ? 40.0 * (1.0 + (double)(level.Number / 10) / (Constants.NumberOfNormalLevels / 10))
            : 40.0 * (1.0 + (double)(level.Number - Constants.NumberOfNormalLevels) / (Constants.NumberOfLevels - Constants.NumberOfNormalLevels));

        level.WeaponConfiguration = new WeaponConfiguration(shooterSpeed, shooterRotateDirection, shooterPosition);

        // TODO make a function of level number
        var targetsMinSpeed = level.Number <= Constants.NumberOfNormalLevels ? 20.0 : -30.0 - sublevel * 2;

        var targetsMaxSpeed = level.Number <= Constants.NumberOfNormalLevels ? 20.0 : 30.0 + sublevel * 2;

        var targetsAcceleration = level.Number <= Constants.NumberOfNormalLevels ? 0.0 : 5.0 + sublevel;

        var targetsApproachEffectDuration = level.Number <= Constants.NumberOfNormalLevels ? 0.0
            : (level.Number <= Constants.NumberOfNormalLevels + 10 ?
                (sublevel <= 5 ? 0.0 : 3.0)
            : (sublevel <= 5 ? 0.0 : 2.0));

        level.TargetsConfiguration = new TargetsConfiguration(targetsMinSpeed, targetsMaxSpeed, targetsAcceleration, targetsApproachEffectDuration);

        return level;
    }

    private Level GenerateLevelRandomlyWithShapes(int passedLevel)
    {
        var level = new Level();

        // level numbers increment by 1
        level.Number = passedLevel + 1;

        // generate targets

        // density of targets; a number between 0.5 and 1; if 1 there is no space between adjacent targets
        double density = 1.0f;// Random.Range(0.6f, 0.85f);

        // we will rotate the shape
        double shapeRotationDegrees = Random.Range(0.0f, 360.0f);

        // initialize Targets
        level.Targets = new List<Target>();

        // pick a shape symbol and load related json file and deserialize it to get fragments and their positions
        string[] shapeSymbols = new string[] { "O" }; // "H", "L", "N", "O" };
        string shapeSymbol = shapeSymbols[Random.Range(0, shapeSymbols.Length)];
        string shapeJson = File.ReadAllText("Assets/Levels/Shapes/" + shapeSymbol + ".json");
        Shape shape = JsonConvert.DeserializeObject<Shape>(shapeJson);

        // rotate the shape
        shape.Rotate(shapeRotationDegrees);

        // fit shape into screen
        shape.FitIntoScreen();

        Debug.Log("shape: " + shapeSymbol + ", rotate: " + (int)shapeRotationDegrees);
        
        // for each fragment, insert targets to list as many as possible (according to density)
        foreach (Fragment fragment in shape.fragments)
        {
            int lastPoint = -1;
            int lastPointIdeal = -1;
            int firstPoint = -1;
            int firstTarget = level.Targets.Count - 1;

            for (int i= 0; i< fragment.points.Count; ++i)
            {
                if (TargetPositionIsInsideScreen(fragment.points[i]))
                {
                    var targetsOtherFragments = level.Targets.Where(t => t.Fragment != fragment.name);
                    var firstTargetSameFragment = level.Targets.FirstOrDefault(t => t.Fragment == fragment.name);
                    if (firstTargetSameFragment != null && level.Targets.Where(t => t.Fragment == fragment.name).Count() > 1)
                    {
                        targetsOtherFragments = targetsOtherFragments.Append(firstTargetSameFragment);
                    }
                    if (!TargetsCoincidePoint(targetsOtherFragments, fragment.points[i], density))
                    {
                        lastPointIdeal = i;
                        if (!TargetsCoincidePoint(level.Targets.Where(t => t.Fragment == fragment.name), fragment.points[i], density))
                        {
                            level.Targets.Add(new Target(TargetType.Success, new Position(fragment.points[i].x, fragment.points[i].y), fragment.name));
                            lastPoint = i;
                            if (firstPoint == -1)
                            {
                                firstPoint = i;
                            }
                        }
                    }
                }
            }
            int lastTarget = level.Targets.Count - 1;
            if (lastPoint > -1 && TargetPositionIsInsideScreen(fragment.points[fragment.points.Count - 1]))
            {
                // adjust targets from fragment
                Debug.Log("adjusting fragment " + fragment.name);

                // now.. targets should be between [firstPoint, lastPointIdeal] instead of [firstPoint, lastPoint]
                Debug.Log("firstPoint: " + firstPoint + ", lastPoint: " + lastPoint + ", lastPointIdeal: " + lastPointIdeal);

                double length, lengthIdeal, adjustedDensity = density;
                bool adjust = true;

                if (lastTarget - firstTarget == 1)
                {
                    if (firstPoint == 0)
                    {
                        Debug.Log("adjust <-- false");
                        adjust = false;
                    }
                    else
                    {
                        adjustedDensity = 1.005 * Mathf.Max(0.1f, (float)(density * firstPoint / ((lastPointIdeal + firstPoint) / 2)));
                        Debug.Log("Adjusted density (first case): " + adjustedDensity);
                    }
                }
                else
                {
                    Debug.Log("more than 1 targets...");
                    length = lastPoint - firstPoint;
                    Debug.Log("length: " + length);
                    lengthIdeal = lastPointIdeal - firstPoint;
                    Debug.Log("lengthIdeal: " + lengthIdeal);
                    // now adjust density accordingly
                    adjustedDensity = Mathf.Max(0.1f, (float)(density * length / lengthIdeal)) * 1.005;
                    Debug.Log("Adjusted density: " + adjustedDensity);
                }

                if (adjust)
                {
                    // reposition last <lastTarget - firstTarget> targets
                    level.Targets.RemoveRange(firstTarget + 1, lastTarget - firstTarget);
                    for (int i = 0; i < fragment.points.Count; ++i)
                    {
                        if (TargetPositionIsInsideScreen(fragment.points[i])
                            && !TargetsCoincidePoint(level.Targets, fragment.points[i], adjustedDensity))
                        {
                            level.Targets.Add(new Target(TargetType.Success, fragment.points[i], fragment.name));
                        }
                    }
                }                
            }
        }

        // number of bullets is a function of level number
        // TODO improve
        level.NumberOfBullets = 100 * level.Targets.Count * (1 + Constants.NumberOfLevels / 100 - level.Number / 100);

        // we position shooter randomly on the screen
        Position shooterPosition = new Position(0, 0);

        // pick one of the targets as fail target
        int indexFail = Random.Range(0, level.Targets.Count);
        level.Targets[indexFail].Type = TargetType.Fail;

        // rotate direction of shooter is selected randomly
        var shooterRotateDirection = Random.Range(0, 2) == 0 ? RotateDirection.Clockwise : RotateDirection.CounterClockwise;

        // shooter speed is selected according to level number
        // TODO improve
        double shooterSpeed = 90.0 * (1.0 + (double)level.Number / Constants.NumberOfLevels);

        level.WeaponConfiguration = new WeaponConfiguration(shooterSpeed, shooterRotateDirection, shooterPosition);

        return level;
    }

    private bool ShooterCanHitAllSuccessTargets(List<Target> targets, Position shooterPosition)
    {
        foreach (Target target in targets.Where(t => t.Type == TargetType.Success))
        {
            if (!ShooterCanHitSuccessTarget(targets, target, shooterPosition))
            {
                return false;
            }
        }
        return true;
    }

    private bool ShooterCanHitSuccessTarget(IEnumerable<Target> targets, Target target, Position shooterPosition)
    {
        IEnumerable<Target> failTargets = targets.Where(t => t.Type == TargetType.Fail);
        foreach (Target failTarget in failTargets)
        {
            if (GeometryUtil.IsOnTheWay(shooterPosition, target.Position, failTarget.Position))
            {
                return false;
            }
        }
        return true;
    }
}
