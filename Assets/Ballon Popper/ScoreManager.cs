using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public int score;
    public TextMeshProUGUI scoreText;

    void Start()
    {
        UpdateScoreTest();
    }

    public void IncreaseScore(int amount)
    {
        score += amount;
        UpdateScoreTest();
    }

    void UpdateScoreTest()
    {
        scoreText.text = "Score: " + score;
    }
}
