using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public GameObject greenBubblePreFab;
    public GameObject redBubblePreFab;
    public GameObject greenCubePreFab;
    public GameObject redCubePreFab;
    public GameObject pandaPreFab;
    public GameObject lionPreFab;
    public GameObject elephantPreFab;
    public GameObject cowPreFab;

    public ScoreManager scoreManager;
    public SoundManager soundManager;
    public GameObject UI_Setting;
    public GameObject UI_Menu;
    public GameObject UI_GameUI;
    public GameObject UI_Summary;
    public GameObject CollectArea;
    //public Camera myCam;
    public GameObject calibratorLeft;
    public GameObject calibratorRight;
    public GameObject calibratorTop;
    public GameObject calibratorBottom;

    public GameObject leftHand;
    public GameObject rightHand;
    public bool isStarted = false;
    //public bool isInCalibrationMode = false;

    private List<Bubble> GoodBubbles = new List<Bubble>();
    private List<Bubble> BadBubbles = new List<Bubble>();

    private bool executedOnce = false;

    public TextMeshProUGUI countdownTimer;
    public TextMeshProUGUI summary;
    public TMP_InputField m_targetMT;
    public TMP_Dropdown m_size;
    public TMP_Dropdown m_object;
    public TMP_Dropdown m_gamelength;
    public TMP_Dropdown m_sound;
    public TMP_Dropdown m_gameselection;
    public TMP_InputField m_username;
    public TMP_Dropdown m_currentuser;
    public TMP_Dropdown m_hand;

    // Specific setting for the popping game
    public TMP_Dropdown m_numberObjects;
    public TMP_Dropdown m_redRatio;
    public TMP_Dropdown m_bubbleSpeed;
    public TMP_Dropdown m_appearInterval;

    // Specific setting for the hitting game
    public TMP_Dropdown m_bubbledistance;

    // Specific setting for the cating game
    public TMP_Dropdown m_timedelay;

    public GameObject UI_Robot_left;
    public GameObject UI_Robot_right;
    //timer
    float currentTime = 0f;
    float startingTime = 30f;

    float bubbleScale = 0.2f;
    public int gameType = 0;
    public int bubbleCount = 0;
    int maxBubbleCount;
    int redBubbleProp;
    int objectType = 0;
    float distance;
    bool moveRight = true;
    int timeDelay = 0;
    int appearingStart;
    public int bubbleSpeed;

    public bool withRobot = false;
    public float targetMT = 0;
    public ClientSocket clientSocket;

    public float topLimit = 2f;
    public float bottomLimit = 1f;
    public float leftLimit = -1f;
    public float rightLimit = 1f;
        
    void Start()
    {
        topLimit = PlayerPrefs.GetFloat("topLimit", topLimit);
        bottomLimit = PlayerPrefs.GetFloat("bottomLimit", bottomLimit);
        leftLimit = PlayerPrefs.GetFloat("leftLimit", leftLimit);
        rightLimit = PlayerPrefs.GetFloat("rightLimit", rightLimit);

    }

    // Update is called once per frame
    void Update()
    {
        if (isStarted && !executedOnce)
        {
            if (m_gameselection.value == 0)
            {
                if (bubbleCount < maxBubbleCount)
                {
                    StartCoroutine(CreateBubbles());
                    //executedOnce = true;
                }
            }
            else if (m_gameselection.value == 1)
            {
                if (bubbleCount < 1)
                {
                    StartCoroutine(CreateHitingGameObjects());
                }
            }
            else if (m_gameselection.value == 2)
            {
                if (bubbleCount < 1)
                {
                    StartCoroutine(CreateCatchingGameObjects());
                }
            }
            else if (m_gameselection.value == 3)
            {
                if (bubbleCount < 1)
                {
                    StartCoroutine(CreateGrabAndDropGameObjects());
                }
            }



        }

        if (isStarted)
        {
            currentTime -= 1 * Time.deltaTime;
            if (currentTime < 10)
            {
                countdownTimer.color = Color.red;
            }

            if (currentTime >= 10 && currentTime < 20)
            {
                countdownTimer.color = Color.yellow;
            }
            if (currentTime.ToString("0") == "0")
            {
                Time.timeScale = 0f;
                isStarted = false;
                SummaryPage();
                //GameOver();
            }

            countdownTimer.text = currentTime.ToString("0");
        }

    }

    public void SetRobot()
    {
        withRobot = true;
        if (string.IsNullOrEmpty(m_targetMT.text))
        {
            targetMT = 0f; // Default value
            UI_Robot_left.SetActive(true);
            UI_Robot_right.SetActive(true);
        }
        else
        {
            targetMT = float.Parse(m_targetMT.text);
            UI_Robot_left.SetActive(false);
            UI_Robot_right.SetActive(false);
        }

    }

    public void TestRobot()
    {
       clientSocket.SendCommand(0);
    }
    public void StartGame()
    {
        SelectGame();
        isStarted = true;
        currentTime = startingTime;
        Time.timeScale = 1f;

        int currID = PlayerPrefs.GetInt("currID");

        //Object Type
        objectType = PlayerPrefs.GetInt("user" + currID.ToString() + "ObjectType");

        //Set sound
        SoundManager.sound = PlayerPrefs.GetInt("user" + currID.ToString() + "Sound");
        SoundManager.count_song = 0;

        //Object Size
        switch (PlayerPrefs.GetInt("user" + currID.ToString() + "Size"))
        {
            case 0:
                bubbleScale = 0.2f;
                CollectArea.transform.localScale = new Vector3(bubbleScale*1.5f, bubbleScale*1.5f, 0.1f);
                break;
            case 1:
                bubbleScale = 0.1f;
                CollectArea.transform.localScale = new Vector3(bubbleScale*1.5f, bubbleScale*1.5f, 0.1f);
                break;
            case 2:
                bubbleScale = 0.3f;
                CollectArea.transform.localScale = new Vector3(bubbleScale*1.5f, bubbleScale*1.5f, 0.1f);
                break;
            default:
                print("Incorrect object size.");
                break;
        }

        //Game Length
        switch (PlayerPrefs.GetInt("user" + currID.ToString() + "Length"))
        {
            case 0:
                startingTime = 30f;
                break;
            case 1:
                startingTime = 60f;
                break;
            case 2:
                startingTime = 120f;
                break;
            case 3:
                startingTime = 180f;
                break;
            default:
                print("Incorrect game length level.");
                break;
        }
        currentTime = startingTime;


        // set up left/right/both hands
        SetHand();
        // poping game
        maxBubbleCount =  10 - PlayerPrefs.GetInt("user" + currID.ToString() + "NumObjects");

        switch (PlayerPrefs.GetInt("user" + currID.ToString() + "RedRatio"))
        {
            case 0:
                redBubbleProp = 0;
                break;
            case 1:
                redBubbleProp = 1;
                break;
            case 2:
                redBubbleProp = 2;
                break;
            case 3:
                redBubbleProp = 3;
                break;
            case 4:
                redBubbleProp = 4;
                break;
            case 5:
                redBubbleProp = 5;
                break;
            case 6:
                redBubbleProp = 6;
                break;
            case 7:
                redBubbleProp = 7;
                break;
            case 8:
                redBubbleProp = 8;
                break;
            case 9:
                redBubbleProp = 9;
                break;
            case 10:
                redBubbleProp = 10;
                break;
            default:
                print("Incorrect red bubble ratio option.");
                break;
        }

        bubbleSpeed = PlayerPrefs.GetInt("user" + currID.ToString() + "BubbleSpeed");
       
        switch (PlayerPrefs.GetInt("user" + currID.ToString() + "AppearInterval"))
        {
            case 0:
                appearingStart = 0;
                break;
            case 1:
                appearingStart = 5;
                break;
            case 2:
                appearingStart = 10;
                break;
            default:
                print("Incorrect appearance interval.");
                break;
        }
        // hitting game
        switch (PlayerPrefs.GetInt("user" + currID.ToString() + "Distance"))
        {
            case 0:
                distance = 0.25f;
                break;
            case 1:
                distance = 0.2f;
                break;
            case 2:
                distance = 0.3f;
                break;
            default:
                print("Incorrect distance.");
                break;
        }

        // catching game
        timeDelay = PlayerPrefs.GetInt("user" + currID.ToString() + "Timedelay");

    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void ExitCurrentGame()
    {
        GameOver();
    }

    void SummaryPage()
    {   
        float[] intervals = scoreManager.GetPopIntervals();
        //Debug.Log("Pop Intervals:");
        //foreach (float interval in intervals)
        //{
        //    Debug.Log(interval);
        //}

        int score = scoreManager.GetScore();
        int bubblePoppedCount = intervals.Length;
        float movementTime = scoreManager.getAveragePopInterval();
        float targetMovementTime = movementTime * 0.85f;

        string formattedOutput = "Summary: \n" +
                                 $"Score: {score}\n" +
                                 $"Total Bubble Popped: {bubblePoppedCount}\n" +
                                 $"Average Movement Time: {movementTime:F2}\n" +
                                 $"Target Movement Time: {targetMovementTime:F2}";

        UI_Summary.SetActive(true);
        summary.text = formattedOutput;

        if (withRobot)
        {
            clientSocket.SendCommand(3);
        }

        scoreManager.WriteDataToCSV();
    }
    
    public void GameOver()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    //void DropdownValueChanged(TMP_Dropdown change)
    //{
    //    Debug.Log(change.value);
    //}

    public void SelectGame()
    {
        gameType = m_gameselection.value;
    }

    public IEnumerator CreateBubbles()
    {
        bubbleCount++;
        //Rate of spawn
        yield return new WaitForSeconds(Random.Range(appearingStart, appearingStart+5));

        //generate random bubbles
        int randomNum = Random.Range(1, 10);
        Vector3 randomPosition = new Vector3(Random.Range(leftLimit, rightLimit), Random.Range(bottomLimit, topLimit), greenBubblePreFab.gameObject.transform.position.z);

        GameObject greenObjectPreFab = greenBubblePreFab;
        GameObject redObjectPreFab = redBubblePreFab;
        Quaternion rotation = Quaternion.identity;
        if (objectType == 0)
        {
            greenObjectPreFab = greenBubblePreFab;
            redObjectPreFab = redBubblePreFab;
        }
        else if (objectType == 1)
        {
            greenObjectPreFab = greenCubePreFab;
            redObjectPreFab = redCubePreFab;
        }
        else if (objectType == 2)
        {
            greenObjectPreFab = pandaPreFab;
            // if it's animal head, then we only have good one
            randomNum = 10;
            rotation = new Quaternion(0f, 180f, 0, 1f);
        }
        else if (objectType == 3)
        {
            greenObjectPreFab = lionPreFab;
            // if it's animal head, then we only have good one
            randomNum = 10;
            rotation = new Quaternion(0f, 180f, 0, 1f);
        }
        else if (objectType == 4)
        {
            greenObjectPreFab = elephantPreFab;
            // if it's animal head, then we only have good one
            randomNum = 10;
            rotation = new Quaternion(0f, 180f, 0, 1f);
        }
        else if (objectType == 5)
        {
            greenObjectPreFab = cowPreFab;
            // if it's animal head, then we only have good one
            randomNum = 10;
            rotation = new Quaternion(0f, 180f, 0, 1f);
        }

        if (randomNum > redBubbleProp)
        {

            GameObject newBubbleObject = Instantiate(greenObjectPreFab, randomPosition, rotation, transform);
            newBubbleObject.transform.localScale = new Vector3(bubbleScale, bubbleScale, bubbleScale);
            Bubble newBubble = newBubbleObject.GetComponent<Bubble>();
            newBubble.isGoodBubble = true;
            newBubble.scoreManager = scoreManager;
            newBubble.soundManager = soundManager;
            newBubble.clientSocket = clientSocket;
            newBubble.mBubbleManager = this;
            //GoodBubbles.Add(newBubble);

        }
        else
        {
            GameObject newBubbleObject = Instantiate(redObjectPreFab, randomPosition, rotation, transform);
            newBubbleObject.transform.localScale = new Vector3(bubbleScale, bubbleScale, bubbleScale);
            Bubble newBubble = newBubbleObject.GetComponent<Bubble>();
            newBubble.isGoodBubble = false;
            newBubble.scoreManager = scoreManager;
            newBubble.soundManager = soundManager;
            newBubble.clientSocket = clientSocket;
            newBubble.mBubbleManager = this;
            //BadBubbles.Add(newBubble);
        }

        
    }

    public IEnumerator CreateHitingGameObjects()
    {
        GameObject greenObjectPreFab = greenBubblePreFab;
        Quaternion rotation = Quaternion.identity;
        //Select Size - need to abstract this function
        if (objectType == 0)
        {
            greenObjectPreFab = greenBubblePreFab;
        }
        else if (objectType == 1)
        {
            greenObjectPreFab = greenCubePreFab;
        }
        else if (objectType == 2)
        {
            greenObjectPreFab = pandaPreFab;
            rotation = new Quaternion(0f, 180f, 0, 1f);
        }
        else if (objectType == 3)
        {
            greenObjectPreFab = lionPreFab;
            rotation = new Quaternion(0f, 180f, 0, 1f);
        }
        else if (objectType == 4)
        {
            greenObjectPreFab = elephantPreFab;
            rotation = new Quaternion(0f, 180f, 0, 1f);
        }
        else if (objectType == 5)
        {
            greenObjectPreFab = cowPreFab;
            rotation = new Quaternion(0f, 180f, 0, 1f);
        }

        Vector3[] randomPositions = new Vector3[3];
        float randomX = Random.Range(leftLimit, rightLimit);
        float randomY = Random.Range(bottomLimit, topLimit);
        float fixedZ = greenBubblePreFab.gameObject.transform.position.z;
        //randomPositions[0] = new Vector3(randomX, randomY, fixedY);

        for (int i = 0; i < 3; i++)
        {
            randomPositions[i] = new Vector3(randomX + distance * i, randomY, fixedZ);
            GameObject newBubbleObject = Instantiate(greenObjectPreFab, randomPositions[i], rotation, transform);
            newBubbleObject.transform.localScale = new Vector3(bubbleScale, bubbleScale, bubbleScale);
            Bubble newBubble = newBubbleObject.GetComponent<Bubble>();
            newBubble.isGoodBubble = true;
            newBubble.scoreManager = scoreManager;
            newBubble.soundManager = soundManager;
            newBubble.mBubbleManager = this;
            GoodBubbles.Add(newBubble);
            //Debug.Log(GoodBubbles.Count);
            bubbleCount++;
        }

        //Rate of spawn
        yield return new WaitForSeconds(0.3f);


    }

    public IEnumerator CreateCatchingGameObjects()
    {
        bubbleCount++;
        //Rate of spawn
        yield return new WaitForSeconds(timeDelay);

        GameObject greenObjectPreFab = greenBubblePreFab;
        Quaternion rotation = Quaternion.identity;
        //Select Size - need to abstract this function
        if (objectType == 0)
        {
            greenObjectPreFab = greenBubblePreFab;
        }
        else if (objectType == 1)
        {
            greenObjectPreFab = greenCubePreFab;
        }
        else if (objectType == 2)
        {
            greenObjectPreFab = pandaPreFab;
            rotation = new Quaternion(0f, 180f, 0, 1f);
        }
        else if (objectType == 3)
        {
            greenObjectPreFab = lionPreFab;
            rotation = new Quaternion(0f, 180f, 0, 1f);
        }
        else if (objectType == 4)
        {
            greenObjectPreFab = elephantPreFab;
            rotation = new Quaternion(0f, 180f, 0, 1f);
        }
        else if (objectType == 5)
        {
            greenObjectPreFab = cowPreFab;
            rotation = new Quaternion(0f, 180f, 0, 1f);
        }

        //Vector3 randomPosition = new Vector3;
        //float randomX = -1.0f;
        //float randomY = Random.Range(1.0f, 2.0f);
        //float randomX = Random.Range(leftLimit, rightLimit);

        float randomX = 0;
        if (moveRight)
        {
            randomX = leftLimit;
        }
        else
        {
            randomX = rightLimit;
        }
        
        float randomY = Random.Range(bottomLimit, topLimit);
        float fixedZ = greenBubblePreFab.gameObject.transform.position.z;
        //randomPositions[0] = new Vector3(randomX, randomY, fixedY);

        Vector3 randomPosition = new Vector3(randomX, randomY, fixedZ);
        GameObject newBubbleObject = Instantiate(greenObjectPreFab, randomPosition, rotation, transform);
        newBubbleObject.transform.localScale = new Vector3(bubbleScale, bubbleScale, bubbleScale);
        Bubble newBubble = newBubbleObject.GetComponent<Bubble>();
        newBubble.moveRight = moveRight;
        newBubble.isGoodBubble = true;
        newBubble.scoreManager = scoreManager;
        newBubble.soundManager = soundManager;
        newBubble.mBubbleManager = this;
        GoodBubbles.Add(newBubble);
        moveRight = !moveRight;
    }

    public IEnumerator CreateGrabAndDropGameObjects()
    {
        if (!CollectArea.activeSelf) { CollectArea.SetActive(true); }
        bubbleCount++;
        //Rate of spawn
        yield return new WaitForSeconds(timeDelay);

        GameObject greenObjectPreFab = greenBubblePreFab;
        Quaternion rotation = Quaternion.identity;
        //Select Size - need to abstract this function
        if (objectType == 0)
        {
            greenObjectPreFab = greenBubblePreFab;
        }
        else if (objectType == 1)
        {
            greenObjectPreFab = greenCubePreFab;
        }
        else if (objectType == 2)
        {
            greenObjectPreFab = pandaPreFab;
            rotation = new Quaternion(0f, 180f, 0, 1f);
        }
        else if (objectType == 3)
        {
            greenObjectPreFab = lionPreFab;
            rotation = new Quaternion(0f, 180f, 0, 1f);
        }
        else if (objectType == 4)
        {
            greenObjectPreFab = elephantPreFab;
            rotation = new Quaternion(0f, 180f, 0, 1f);
        }
        else if (objectType == 5)
        {
            greenObjectPreFab = cowPreFab;
            rotation = new Quaternion(0f, 180f, 0, 1f);
        }

        //float randomX = Random.Range(leftLimit, rightLimit);
        //float randomY = Random.Range(bottomLimit, topLimit);
        float randomX = leftLimit;
        float randomY = topLimit;
        float fixedZ = greenBubblePreFab.gameObject.transform.position.z;
        //randomPositions[0] = new Vector3(randomX, randomY, fixedY);

        Vector3 randomPosition = new Vector3(randomX, randomY, fixedZ);
        GameObject newBubbleObject = Instantiate(greenObjectPreFab, randomPosition, rotation, transform);
        newBubbleObject.transform.localScale = new Vector3(bubbleScale, bubbleScale, bubbleScale);
        Bubble newBubble = newBubbleObject.GetComponent<Bubble>();
        newBubble.isGoodBubble = true;
        newBubble.scoreManager = scoreManager;
        newBubble.soundManager = soundManager;
        newBubble.mBubbleManager = this;
        GoodBubbles.Add(newBubble);
    
    }
    public void SaveBubbleRegion()
    {
        topLimit = calibratorTop.transform.position.y;
        bottomLimit = calibratorBottom.transform.position.y;
        leftLimit = calibratorLeft.transform.position.x;
        rightLimit = calibratorRight.transform.position.x;

        //To do: Save this to user profile
        PlayerPrefs.SetFloat("topLimit", topLimit);
        PlayerPrefs.SetFloat("bottomLimit", bottomLimit);
        PlayerPrefs.SetFloat("leftLimit", leftLimit);
        PlayerPrefs.SetFloat("rightLimit", rightLimit);
    }

    public void ResetBubbleRegion()
    {
        calibratorTop.transform.position = new Vector3(0f, 2f, 2f);
        calibratorBottom.transform.position = new Vector3(0f, 1f, 2f);
        calibratorLeft.transform.position = new Vector3(-1f, 1.5f, 2f);
        calibratorRight.transform.position = new Vector3(1f, 1.5f, 2f);
    }

    public void SetCalibratorRegion()
    {
        calibratorTop.transform.position = new Vector3((leftLimit + rightLimit) / 2.0f, topLimit, 2f);
        calibratorBottom.transform.position = new Vector3((leftLimit + rightLimit) / 2.0f, bottomLimit, 2f);
        calibratorLeft.transform.position = new Vector3(leftLimit, (topLimit + bottomLimit) / 2.0f, 2f);
        calibratorRight.transform.position = new Vector3(rightLimit, (topLimit + bottomLimit) / 2.0f, 2f);
    }
    public void GetUserSettings()
    {
        UpdateUserList();
        m_currentuser.value = PlayerPrefs.GetInt("currID"); ;
        //UpdateUserSettings
        UpdateGameSettingsByUserID();
    }

    public void AddUser()
    {
        string newUserName = m_username.text;

        int userCount = PlayerPrefs.GetInt("userCount", 0);
        userCount++;
        PlayerPrefs.SetInt("userCount", userCount);
        PlayerPrefs.SetString("user" + userCount.ToString(), newUserName);
        UpdateUserList();
    }

    public void UpdateUserList()
    {

        int userCount = PlayerPrefs.GetInt("userCount", 0);
        List<string> userNames = new List<string> { };
        for (int i = 1; i <= userCount; i++)
        {
            string userName = PlayerPrefs.GetString("user" + i.ToString());
            userNames.Add(userName);
        }
        m_currentuser.ClearOptions();
        m_currentuser.AddOptions(userNames);
    }


    public void SaveGameSettings()
    {
        int currID = m_currentuser.value;
        //Save Curr User
        PlayerPrefs.SetInt("currID", currID);
        //Object Type
        PlayerPrefs.SetInt("user" + currID.ToString() + "ObjectType", m_object.value);
        //Object Size
        PlayerPrefs.SetInt("user" + currID.ToString() + "Size", m_size.value);
        //Game Length
        PlayerPrefs.SetInt("user" + currID.ToString() + "Length", m_gamelength.value);
        //Sound
        PlayerPrefs.SetInt("user" + currID.ToString() + "Sound", m_sound.value);
        // hand
        PlayerPrefs.SetInt("user" + currID.ToString() + "Hand", m_hand.value);
        SetHand();
        //specific settings for the poping game
        PlayerPrefs.SetInt("user" + currID.ToString() + "NumObjects", m_numberObjects.value);
        PlayerPrefs.SetInt("user" + currID.ToString() + "RedRatio", m_redRatio.value);
        PlayerPrefs.GetInt("user" + currID.ToString() + "AppearInterval", m_appearInterval.value);
        PlayerPrefs.GetInt("user" + currID.ToString() + "BubbleSpeed", m_bubbleSpeed.value);
        //specific settigns for the hitting game
        PlayerPrefs.SetInt("user" + currID.ToString() + "Distance", m_bubbledistance.value);
        //specific settings for the catching game
        PlayerPrefs.SetInt("user" + currID.ToString() + "Timedelay", m_timedelay.value);
    }   

    public void SetHand()
    {
        int currID = m_currentuser.value;
        //hand
        int hand = PlayerPrefs.GetInt("user" + currID.ToString() + "Hand");
        //both
        if (hand == 0)
        {
            leftHand.SetActive(true);
            rightHand.SetActive(true);
        }
        else if (hand == 1)
        {
            leftHand.SetActive(true);
            rightHand.SetActive(false);
        }
        else if (hand == 2)
        {
            leftHand.SetActive(false);
            rightHand.SetActive(true);
        }
    }
    public void UpdateGameSettingsByUserID()
    {
        int currID = m_currentuser.value;
        //Object Type
        m_object.value = PlayerPrefs.GetInt("user" + currID.ToString() + "ObjectType");
        //Object Size
        m_size.value = PlayerPrefs.GetInt("user" + currID.ToString() + "Size");
        //Game Length
        m_gamelength.value = PlayerPrefs.GetInt("user" + currID.ToString() + "Length");
        // Sound
        m_sound.value = PlayerPrefs.GetInt("user" + currID.ToString() + "Sound");
        // Hand
        m_hand.value = PlayerPrefs.GetInt("user" + currID.ToString() + "Hand");

        m_numberObjects.value = PlayerPrefs.GetInt("user" + currID.ToString() + "NumObjects");
        m_redRatio.value = PlayerPrefs.GetInt("user" + currID.ToString() + "RedRatio");
        m_appearInterval.value = PlayerPrefs.GetInt("user" + currID.ToString() + "AppearInterval");
        m_bubbleSpeed.value = PlayerPrefs.GetInt("user" + currID.ToString() + "BubbleSpeed");

        m_bubbledistance.value = PlayerPrefs.GetInt("user" + currID.ToString() + "Distance");

        m_timedelay.value = PlayerPrefs.GetInt("user" + currID.ToString() + "Timedelay");
    }

    public void Robot_Keep() 
    {
        clientSocket.SendCommand(1);
    }

    public void Robot_Faster()
    {
        clientSocket.SendCommand(2);
    }
}