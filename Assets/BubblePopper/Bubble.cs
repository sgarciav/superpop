using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Bubble : MonoBehaviour
{
    public int scoreToGive = 1;
    public int clicksToPop = 5;
    public float scaleIncreasePerClick = 0.1f;

    [HideInInspector]
    public GameManager mBubbleManager = null;
    public ScoreManager scoreManager;
    public SoundManager soundManager;
    public ClientSocket clientSocket;
    public bool isGoodBubble;

    public Vector3 movementDirection = Vector3.zero;
    GameObject hand;
    bool isMoveable = false;
    public bool moveRight = true;
    float prevPopTime = 0;
    float popInterval = 0;
    float lastNAverage = 0;
    //float targetMT;

    //void OnMouseDown(){
    //    clicksToPop -= 1;
    //    transform.localScale += Vector3.one * scaleIncreasePerClick;

    //    if(clicksToPop == 0)
    //    {
    //        scoreManager.IncreaseScore(scoreToGive);
    //        Destroy(gameObject);
    //    }
        
    //}

    void Start()
    {
        if (mBubbleManager.gameType == 0)
        {
            StartCoroutine(WaitThenDie());
        }
            
    }


    IEnumerator WaitThenDie()
    {
        int random_time;
        if (mBubbleManager.bubbleSpeed == 0)//medium
        {
           random_time = Random.Range(5, 15);
        }
        else if (mBubbleManager.bubbleSpeed == 1)//slow
        {
           random_time = Random.Range(15, 25);
        }
        else //fast
        {
           random_time = Random.Range(1, 5);
        }
         
        yield return new WaitForSeconds(random_time);
        mBubbleManager.bubbleCount -= 1;
        Destroy(gameObject);
        //gameObject.transform.position = new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(1.0f, 2.0f), gameObject.transform.position.z);
    }

    void Update()
    {
        if (mBubbleManager.gameType == 2)
        {
            if (moveRight)
            {
                movementDirection = Vector3.right;
            }
            else {
                movementDirection = Vector3.left;
            }

            gameObject.transform.position += movementDirection * Time.deltaTime * 0.25f;
            int randomNum = Random.Range(0, 10);
            if (randomNum > 5)
            {
                gameObject.transform.position += Vector3.up * Time.deltaTime * 0.2f;
            }
            else
            {
                gameObject.transform.position += Vector3.down * Time.deltaTime * 0.2f;
            }

            //gameObject.transform.Rotate(Vector3.forward * Time.deltaTime * movementDirection.x * 20, Space.Self);

            //destroy the bubble if the bubble out of the apperance region
            if (this.gameObject.transform.position.x < mBubbleManager.leftLimit || this.gameObject.transform.position.x > mBubbleManager.rightLimit || this.gameObject.transform.position.y > mBubbleManager.topLimit || this.gameObject.transform.position.y < mBubbleManager.bottomLimit){
                mBubbleManager.bubbleCount -= 1;
                Destroy(this.gameObject);
            }
        }

        if (mBubbleManager.gameType == 3 && isMoveable)
        {
            gameObject.transform.position = hand.transform.position;
            //if gameObject.transform.position.x 
        }
    }

    void OnTriggerEnter(Collider other){
        if (other.tag == "Player") 
        {
            // play the audio clip
            //AudioSource audioSrc = gameObject.GetComponent<AudioSource>();
            //if (audioSrc != null && !audioSrc.isPlaying)
            //{
            //    audioSrc.Play();
            //}
            soundManager.PlayClip();
            //Destroy the bubble
            //Destroy(this.gameObject);

           
            if (mBubbleManager.gameType == 0)
            {
                //gameObject.transform.position = new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(1.0f, 2.0f), gameObject.transform.position.z);
                mBubbleManager.bubbleCount -= 1;
                Destroy(this.gameObject);
            }
            else if ((mBubbleManager.gameType == 1) || (mBubbleManager.gameType == 2))
            {
                mBubbleManager.bubbleCount -= 1;
                Destroy(this.gameObject);
                //Debug.Log(mBubbleManager.bubbleCount);
            }
            else if (mBubbleManager.gameType == 3)
            {
                  hand = other.transform.gameObject;
                  isMoveable = true;
            }
        }

        if (other.tag == "Finish")
        {
            mBubbleManager.bubbleCount -= 1;
            soundManager.PlayClip();
            Destroy(this.gameObject);
        }


        if (isGoodBubble)
        {
            scoreManager.IncreaseScore(scoreToGive);
            if (scoreManager.popTimes.Count > 0)
            {
                prevPopTime = scoreManager.getLastPopTime();
                popInterval = Time.time - prevPopTime;
                scoreManager.AddPopInterval(popInterval);

                lastNAverage = scoreManager.CalculateAverageOfLastN();

                if (mBubbleManager.withRobot && (scoreManager.popIntervals.Count % scoreManager.N == 0) && (mBubbleManager.targetMT != 0))
                {
                    //Debug.Log(scoreManager.popIntervals.Count);
                    if (lastNAverage <= mBubbleManager.targetMT)
                    {
                        clientSocket.SendCommand(1);
                        Debug.Log("Keep up the good work");
                    }
                    else
                    {
                        clientSocket.SendCommand(2);
                        Debug.Log("Move a little faster");
                    }
                }
            }

            scoreManager.AddPopTime(Time.time);
            
            //Debug.Log('say a good word');


        } 
        else
        {
            scoreManager.IncreaseScore(-scoreToGive);
        }


    }
}
