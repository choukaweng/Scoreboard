using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreController : MonoBehaviour {

    public Image mainScoreBoard;
    public Text mainNameText, mainScoreText;
    public AnimationCurve lerpCurve;

    private List<Score> scoreList;
    private Score mainScore;
    private bool startLerping = false;
    private float lerpScale = 0f, curveTime = 0f;

	// Use this for initialization
	void Start ()
    {
        scoreList = new List<Score>();
        mainScore = new Score();
        mainScore.SetScoreObject(mainScoreBoard, mainNameText, mainScoreText);
        mainScoreBoard.fillAmount = 0f;
    }
	
	// Update is called once per frame
	void Update ()
    {
		if(Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Lerp");
            Score newScore = new Score(80f, 100f, "First Team", Vector3.zero);
            mainScore.SetScoreData(newScore.actualMark, newScore.totalMark, newScore.name, Vector3.zero);
            mainScore.startLerping = true;
        }

        startLerping = mainScore.startLerping;
        if (!mainScore.completed && startLerping)
        {
            curveTime += Time.deltaTime;
            lerpScale = lerpCurve.Evaluate(curveTime);
            mainScore.ShowValue(lerpScale);
        }
        else
        {
            curveTime = 0f;
        }
	}

    private void StartLerping()
    {

    }
    
}


#region Score class
public class Score
{
    public Image scoreboard;

    private const string path = "Prefabs/ScorePrefab";
    private Text nameText, scoreText;
    private Vector3 position = Vector3.zero;

    public bool completed = false;

    public bool startLerping = false;
    public string name = "";

    public float currentMark = 0f, actualMark, totalMark;

    public Score() { }

    public Score(float actualMark, float totalMark, string name, Vector3 position)
    {
        this.actualMark = actualMark;
        this.totalMark = totalMark;
        this.name = name;
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

    public void SetScoreObject(Image scoreboard, Text nameText, Text scoreText)
    {
        this.scoreboard = scoreboard;
        this.nameText = nameText;
        this.scoreText = scoreText;
    }

    public void SetScoreData(float actualMark, float totalMark, string name, Vector3 position)
    {
        this.actualMark = actualMark;
        this.totalMark = totalMark;
        this.name = name;
        nameText.text = name;
    }

    public void ShowValue(float lerpScale)
    {
        if(startLerping)
        {
            float lerpTime = lerpScale * Time.deltaTime;
            currentMark = Mathf.Lerp(currentMark, actualMark, lerpTime);
            scoreboard.fillAmount = currentMark / totalMark;

            if(!completed)
            {
                if(actualMark - currentMark <= 0.1f)
                {
                    completed = true;
                    scoreText.text = actualMark.ToString();
                    startLerping = false;
                }
            }
        }
    }
}
#endregion