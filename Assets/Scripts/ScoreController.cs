using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Text;  

public class ScoreController : MonoBehaviour {

    public Image mainScoreBoard;
    public Text mainNameText, mainScoreText;
    public GameObject ScoreEntryPanel, Leaderboard;
    public AnimationCurve lerpCurve;

    private List<Score> scoreList;
    private Score mainScore;
    private float curveTime = 0f;
    private bool startLerping = false;
    private class LerpEvent : UnityEvent<float> { }
    private LerpEvent OnStartLerping;
    private UnityEvent OnResetScoreboard;

    //Score Entry Panel
    private InputField teamNameInputField, actualScoreInputField, totalScoreInputField;
    private bool showScoreEntryPanel = false;
    private Color gray;

    //Leaderboard
    private GameObject sampleData;
    private bool showLeaderboard = false;
    private Vector3 firstColumnPosition = new Vector3(-155f, -45f, 0f);
    private Vector3 secondColumnPosition = new Vector3(147f, -45f, 0f);
    private Vector3 offset = new Vector3(0f, 45f, 0f);

    //XML Database
    private string databaseName = "Data";
    private string databasePath = "";
    private ScoreContainer container;

	// Use this for initialization
	void Start ()
    {
        OnStartLerping = new LerpEvent();
        OnResetScoreboard = new UnityEvent();

        scoreList = new List<Score>();
        container = new ScoreContainer();

        sampleData = Resources.Load("Prefabs/SampleData") as GameObject;
        
        mainScore = new Score();
        mainScore.Initialize(this);
        mainScore.SetScoreObject(mainScoreBoard, mainNameText, mainScoreText);
        mainScoreBoard.fillAmount = 0f;

        SetupScoreEntryPanel();
        SetupDatabase();

        showScoreEntryPanel = ScoreEntryPanel.activeInHierarchy;
        showLeaderboard = Leaderboard.activeInHierarchy;

        gray = teamNameInputField.placeholder.GetComponent<Text>().color;
    }
	
	// Update is called once per frame
	void Update ()
    {
		if(Input.GetKeyDown(KeyCode.Space))
        {
            if(scoreList.Count > 0)
            {
                mainScoreText.text = "";
                mainNameText.text = "";
                OnResetScoreboard.Invoke();
                ClearEvents();

                Score newScore = scoreList[scoreList.Count - 1];
                newScore.SetScoreObject(mainScoreBoard, mainNameText, mainScoreText);
                mainScore.SetScoreData(newScore.scoreData.actualMark, newScore.scoreData.totalMark, newScore.scoreData.name, Vector3.zero);

                OnStartLerping.AddListener(newScore.ShowValue);
                OnResetScoreboard.AddListener(newScore.Reset);

                //foreach (Score score in scoreList)
                //{
                //    OnStartLerping.AddListener(score.ShowValue);
                //    OnResetScoreboard.AddListener(score.Reset);
                //}

                startLerping = true;
            }
            else
            {
                Debug.Log("Score list is empty");
            }
        }

        //Show Score Entry Panel
        if(Input.GetKeyDown(KeyCode.F1))
        {
            showScoreEntryPanel = !showScoreEntryPanel;
            ScoreEntryPanel.SetActive(showScoreEntryPanel);
        }

        //Show all scores
        if(Input.GetKeyDown(KeyCode.F2))
        {
            showLeaderboard = !showLeaderboard;
            Leaderboard.SetActive(showLeaderboard);
        }

       if(startLerping)
        {
            curveTime += Time.deltaTime;
            float lerpScale = lerpCurve.Evaluate(curveTime);

            OnStartLerping.Invoke(lerpScale);
        }
    }

    #region Score Control Methods
    private void ClearEvents()
    {
        OnStartLerping.RemoveAllListeners();
        OnResetScoreboard.RemoveAllListeners();
        startLerping = false;
        curveTime = 0f;
    }

    private void CreateScore()
    {
        bool valid = false;
        float _actualScore = 0f, _totalScore = 0f;
        float.TryParse(actualScoreInputField.text, out _actualScore);
        float.TryParse(totalScoreInputField.text, out _totalScore);

        if (teamNameInputField.text != "" && actualScoreInputField.text != "" && totalScoreInputField.text != "" && _actualScore < _totalScore)
        {
            valid = true;
        }
        else
        {
            if(teamNameInputField.text == "")
            {
                teamNameInputField.placeholder.GetComponent<Text>().color = Color.red;
            }
            if (actualScoreInputField.text == "")
            {
                actualScoreInputField.placeholder.GetComponent<Text>().color = Color.red;
            }
            if (totalScoreInputField.text == "")
            {
                totalScoreInputField.placeholder.GetComponent<Text>().color = Color.red;
            }
            if(_actualScore > _totalScore)
            {
                actualScoreInputField.textComponent.color = Color.red;
                totalScoreInputField.textComponent.color = Color.red;
            }
        }

        if (valid)
        {
            Score newScore = new Score(_actualScore, _totalScore, teamNameInputField.text, Vector3.zero);
            container.scoreDataList.Add(newScore.scoreData);
            UpdateLeaderboard(newScore.scoreData);
            scoreList.Add(newScore);

            teamNameInputField.text = "";
            actualScoreInputField.text = "";
            totalScoreInputField.text = "";
            teamNameInputField.textComponent.color = Color.black;
            teamNameInputField.placeholder.GetComponent<Text>().color = gray;
            actualScoreInputField.textComponent.color = Color.black;
            actualScoreInputField.placeholder.GetComponent<Text>().color = gray;
            totalScoreInputField.textComponent.color = Color.black;
            totalScoreInputField.placeholder.GetComponent<Text>().color = gray;

            showScoreEntryPanel = !showScoreEntryPanel;
            ScoreEntryPanel.SetActive(showScoreEntryPanel);

            XML_Serializer.Serialize(container);
        }
    }

    private void ShowInMainScore(Score score)
    {
        mainScore.SetScoreData(score.scoreData.actualMark, score.scoreData.totalMark, score.scoreData.name, Vector3.zero);
        OnStartLerping.AddListener(mainScore.ShowValue);
        OnResetScoreboard.AddListener(mainScore.Reset);
        startLerping = true;
    }

    private void SetupScoreEntryPanel()
    {
        Transform parent = ScoreEntryPanel.transform.GetChild(1);
        teamNameInputField = parent.Find("TeamName_InputField").GetComponent<InputField>();

        actualScoreInputField = parent.Find("ActualScore_InputField").GetComponent<InputField>();

        totalScoreInputField = parent.Find("TotalScore_InputField").GetComponent<InputField>();

        ScoreEntryPanel.GetComponentInChildren<Button>().onClick.AddListener(CreateScore);
    }

    private void SetupDatabase()
    {
        string _path = Application.streamingAssetsPath + "/" + databaseName + ".xml";
        if (!File.Exists(_path))
        {
            XmlDocument xml = new XmlDocument();
            XmlDeclaration xmlDeclaration = xml.CreateXmlDeclaration("1.0", "UTF-8", null);
            XmlElement root = xml.DocumentElement;
            xml.InsertBefore(xmlDeclaration, root);

            var stream = new FileStream(_path, FileMode.Create);
            xml.Save(stream);
            stream.Close();
        }
        else
        {
            if(container == null)
            {
                container = new ScoreContainer();
            }
            try
            {
                container = XML_Serializer.Deserialize<ScoreContainer>();
            }
            catch (Exception e)
            {
                Debug.Log(e.Data);
            }
            if (container.scoreDataList.Count > 0)
            {
                //string debug = "";
                foreach (ScoreData data in container.scoreDataList)
                {
                    Score newScore = new Score(data.actualMark, data.totalMark, data.name, Vector3.zero);
                    scoreList.Add(newScore);

                    UpdateLeaderboard(data);
                   // debug += data.name + " - " + data.actualMark + " / " + data.totalMark + "\n";
                }
            }
        }
    }

    private void UpdateLeaderboard(ScoreData newScore)
    {
        GameObject newData = Instantiate<GameObject>(sampleData, Leaderboard.transform.Find("Content"));
        newData.name = newScore.name;
        newData.transform.Find("TeamName").GetComponent<Text>().text = newScore.name;
        newData.transform.Find("Score").GetComponent<Text>().text = newScore.actualMark.ToString();

        Vector3 position = Vector3.zero;
        if(container.scoreDataList.IndexOf(newScore) <= 5)
        {
            position = firstColumnPosition;
            firstColumnPosition -= offset;
        }
        else
        {
            position = secondColumnPosition;
            secondColumnPosition -= offset;
        }

        newData.transform.localPosition = position;
        
    }

    #endregion
}

#region ScoreData class
public class ScoreData
{
    [XmlElement("Name")]
    public string name;

    [XmlElement("ActualMark")]
    public float actualMark;

    [XmlElement("TotalMark")]
    public float totalMark;
}
#endregion

#region ScoreContainer
[XmlRoot("Database")]
public class ScoreContainer
{
    [XmlArray("ScoreList")]
    [XmlArrayItem("ScoreData")]
    public List<ScoreData> scoreDataList;

    public ScoreContainer()
    {
        scoreDataList = new List<ScoreData>();
    }
}
#endregion

#region Score class
public class Score
{
    public Image scoreboard;
    public bool completed = false;
    public ScoreData scoreData;
    public float currentMark = 0f;
    public Score() { }

    private const string path = "Prefabs/ScorePrefab";
    private Text nameText, scoreText;
    private Vector3 position = Vector3.zero;
    private ScoreController controller;

    public Score(float actualMark, float totalMark, string name, Vector3 position)
    {
        scoreData = new ScoreData();
        scoreData.actualMark = actualMark;
        scoreData.totalMark = totalMark;
        scoreData.name = name;
        GameObject scorePrefab = Resources.Load<GameObject>(path);
        scorePrefab.transform.position = position;
        foreach(Transform t in scorePrefab.transform)
        {
            if(t.name == "Scale")
            {
                scoreboard = t.GetComponent<Image>();
            }
            else if(t.name == "Name")
            {
                nameText = t.GetComponentInChildren<Text>();
            }
            else if(t.name == "Score")
            {
                scoreText = t.GetComponentInChildren<Text>();
            }
        }
        
    }

    public void Initialize(ScoreController controller)
    {
        this.controller = controller;
    }

    public void SetScoreObject(Image scoreboard, Text nameText, Text scoreText)
    {
        this.scoreboard = scoreboard;
        this.nameText = nameText;
        this.scoreText = scoreText;
    }

    public void SetScoreData(float actualMark, float totalMark, string name, Vector3 position)
    {
        if(scoreData == null)
        {
            scoreData = new ScoreData();
        }

        scoreData.actualMark = actualMark;
        scoreData.totalMark = totalMark;
        scoreData.name = name;
        nameText.text = name;
    }

    public void SetPosition(Vector3 position)
    {
        this.position = position;
    }

    public void ShowValue(float lerpScale)
    {
        if (!completed)
        {
            float lerpTime = lerpScale * Time.deltaTime;
            currentMark = Mathf.Lerp(currentMark, scoreData.actualMark, lerpTime);
            scoreboard.fillAmount = currentMark / scoreData.totalMark;
        }

        if (scoreData.actualMark - currentMark <= 0.5f)
        {
            completed = true;
            scoreText.text = scoreData.actualMark.ToString();
            scoreboard.fillAmount = scoreData.actualMark / scoreData.totalMark;
            currentMark = scoreData.actualMark;
        }
    }

    public void Reset()
    {
        scoreboard.fillAmount = 0f;
        completed = false;
        currentMark = 0f;
    }
}
#endregion

#region XML Serializer
public static class XML_Serializer
{
    public static void Serialize<T>(T container)
    {
        string _path = Application.streamingAssetsPath + "/Data.xml";
        XmlSerializer serializer = new XmlSerializer(typeof(T));
        var stream = new FileStream(_path, FileMode.Create);
        XmlWriter writer = XmlWriter.Create(stream);
        writer.Settings.Encoding = Encoding.UTF8;

        serializer.Serialize(writer, container);
        stream.Close();
    }

    public static T Deserialize<T>()
    {
        string _path = Application.streamingAssetsPath + "/Data.xml";
        XmlSerializer serializer = new XmlSerializer(typeof(T));
        var stream = new FileStream(_path, FileMode.Open);

        T container = (T)serializer.Deserialize(stream);

        stream.Close();

        return container;
    }
}
#endregion