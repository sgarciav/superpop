using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using System.IO;

public class ScoreManager : MonoBehaviour
{
    public int score;
    public TextMeshProUGUI scoreText;
    public TMP_Dropdown n;
    public List<float> popIntervals = new List<float>();
    public List<float> popTimes = new List<float>();
    public int N = 3;

    void Start()
    {
        UpdateScoreTest();

    }

    public void ChangeN()
    {
        if (n.value == 0)
        {
            N = 3;
        }
        else if(n.value == 1) {
            N = 5;
        }
        else if(n.value == 2){
            N = 8;
        }
        else if(n.value == 3) {
            N = 10;
        }
       
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

    public int GetScore()
    {
        return score;
    }

    public void AddPopInterval(float interval)
    {
        popIntervals.Add(interval);
    }

    public void AddPopTime(float time)
    {
        popTimes.Add(time);
    }

    public float getLastPopTime()
    {

        return popTimes[popTimes.Count - 1];
    }

    public int GetPopCount()
    {
        return popIntervals.Count;
    }


    public float[] GetPopIntervals()
    {
        return popIntervals.ToArray();
    }

    public float getAveragePopInterval()
    {
        if (popIntervals.Count == 0)
            return 0f;

        float sum = 0f;
        foreach (float interval in popIntervals)
        {
            sum += interval;
        }

        return sum / popIntervals.Count;
    }

    public float CalculateAverageOfLastN()
    {
        if (N <= popIntervals.Count)
        {
            // Take the last n elements and calculate their average
            return popIntervals.Skip(popIntervals.Count - N).Average();
        }
        else
        {
            return popIntervals.Average();
        }

        
    }

    public void WriteDataToCSV()
    {
        string filePath = "data.csv";

        // Set append to true to avoid overwriting the file
        using (StreamWriter writer = new StreamWriter(filePath, append: true))
        {
            string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // Write data for popTimes
            string linePopTimes = $"{currentTime},popTime,{string.Join(",", popTimes)}";
            writer.WriteLine(linePopTimes);

            // Write data for popIntervals
            string linePopIntervals = $"{currentTime},popInterval,{string.Join(",", popIntervals)}";
            writer.WriteLine(linePopIntervals);
        }

        Console.WriteLine($"Data written to {filePath}");
    }


    //public void WriteDataToCSV()
    //{
    //    string filePath = "data.csv";

    //    // Set append to true to avoid overwriting the file

    //    using (StreamWriter writer = new StreamWriter(filePath, append: true))
    //    {
    //        // Get the current time as the first column
    //        string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

    //        // Create a line with the current time followed by the values in popIntervals
    //        string line = currentTime + "," + string.Join(",", popIntervals);

    //        // Write the line to the CSV file
    //        writer.WriteLine(line);
    //    }



    //    Console.WriteLine($"Data written to {filePath}");
    //}
}
