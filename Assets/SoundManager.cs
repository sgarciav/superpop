using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SoundManager : MonoBehaviour
{
    //this implementation is based on the original SuperPop sound effects
    //ref: https://github.com/jxu81/SuperPop/blob/main/SuperPop_OriginalGame/HapticSimulation/BubblePopGameSound.cs#L136
    public AudioSource audioSrc;
    public AudioClip[] Notes;
    public TMP_Dropdown m_sound;
    public static int count_song = 0;
    public static int sound = 0;

    // Start is called before the first frame update
    void Start()
    {
        audioSrc = gameObject.GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void PlayClip()
    {
        if (sound == 0)
        {
            PlayFurElise();
        }
        else if (sound == 1)
        {
            PlayTetris();
        }
        else if (sound == 2)
        {
            PlayRowYourBoat();
        }
        else if (sound == 3)
        {
            PlayLittleStar();
        }
    }

    void PlayFurElise()
    {
        count_song++;
        switch (count_song)
        {
            // first verse
            case 1: audioSrc.PlayOneShot(Notes[21]); break;
            case 2: audioSrc.PlayOneShot(Notes[20]); break;
            case 3: audioSrc.PlayOneShot(Notes[21]); break;
            case 4: audioSrc.PlayOneShot(Notes[20]); break;
            case 5: audioSrc.PlayOneShot(Notes[21]); break;
            case 6: audioSrc.PlayOneShot(Notes[16]); break;
            case 7: audioSrc.PlayOneShot(Notes[19]); break;
            case 8: audioSrc.PlayOneShot(Notes[17]); break;
            case 9: audioSrc.PlayOneShot(Notes[14]); break;
            case 10: audioSrc.PlayOneShot(Notes[5]); break;
            case 11: audioSrc.PlayOneShot(Notes[9]); break;
            case 12: audioSrc.PlayOneShot(Notes[14]); break;
            case 13: audioSrc.PlayOneShot(Notes[16]); break;
            case 14: audioSrc.PlayOneShot(Notes[9]); break;
            case 15: audioSrc.PlayOneShot(Notes[14]); break;
            case 16: audioSrc.PlayOneShot(Notes[16]); break;
            case 17: audioSrc.PlayOneShot(Notes[17]); break;
            case 18: audioSrc.PlayOneShot(Notes[9]); break;
            // second verse
            case 19: audioSrc.PlayOneShot(Notes[21]); break;
            case 20: audioSrc.PlayOneShot(Notes[20]); break;
            case 21: audioSrc.PlayOneShot(Notes[21]); break;
            case 22: audioSrc.PlayOneShot(Notes[20]); break;
            case 23: audioSrc.PlayOneShot(Notes[21]); break;
            case 24: audioSrc.PlayOneShot(Notes[16]); break;
            case 25: audioSrc.PlayOneShot(Notes[19]); break;
            case 26: audioSrc.PlayOneShot(Notes[17]); break;
            case 27: audioSrc.PlayOneShot(Notes[14]); break;
            case 28: audioSrc.PlayOneShot(Notes[5]); break;
            case 29: audioSrc.PlayOneShot(Notes[9]); break;
            case 30: audioSrc.PlayOneShot(Notes[14]); break;
            case 31: audioSrc.PlayOneShot(Notes[16]); break;
            case 32: audioSrc.PlayOneShot(Notes[9]); break;
            case 33: audioSrc.PlayOneShot(Notes[17]); break;
            case 34: audioSrc.PlayOneShot(Notes[16]); break;
            case 35: audioSrc.PlayOneShot(Notes[14]); break;
            // third verse
            case 36: audioSrc.PlayOneShot(Notes[16]); break;
            case 37: audioSrc.PlayOneShot(Notes[17]); break;
            case 38: audioSrc.PlayOneShot(Notes[19]); break;
            case 39: audioSrc.PlayOneShot(Notes[21]); break;
            case 40: audioSrc.PlayOneShot(Notes[14]); break;
            case 41: audioSrc.PlayOneShot(Notes[22]); break;
            case 42: audioSrc.PlayOneShot(Notes[21]); break;
            case 43: audioSrc.PlayOneShot(Notes[19]); break;
            case 44: audioSrc.PlayOneShot(Notes[12]); break;
            case 45: audioSrc.PlayOneShot(Notes[21]); break;
            case 46: audioSrc.PlayOneShot(Notes[19]); break;
            case 47: audioSrc.PlayOneShot(Notes[17]); break;
            case 48: audioSrc.PlayOneShot(Notes[9]); break;
            case 49: audioSrc.PlayOneShot(Notes[19]); break;
            case 50: audioSrc.PlayOneShot(Notes[17]); break;
            case 51: audioSrc.PlayOneShot(Notes[16]); break;
            // repeat first verse
            case 52: audioSrc.PlayOneShot(Notes[21]); break;
            case 53: audioSrc.PlayOneShot(Notes[20]); break;
            case 54: audioSrc.PlayOneShot(Notes[21]); break;
            case 55: audioSrc.PlayOneShot(Notes[20]); break;
            case 56: audioSrc.PlayOneShot(Notes[21]); break;
            case 57: audioSrc.PlayOneShot(Notes[16]); break;
            case 58: audioSrc.PlayOneShot(Notes[19]); break;
            case 59: audioSrc.PlayOneShot(Notes[17]); break;
            case 60: audioSrc.PlayOneShot(Notes[14]); break;
            case 61: audioSrc.PlayOneShot(Notes[5]); break;
            case 62: audioSrc.PlayOneShot(Notes[9]); break;
            case 63: audioSrc.PlayOneShot(Notes[14]); break;
            case 64: audioSrc.PlayOneShot(Notes[16]); break;
            case 65: audioSrc.PlayOneShot(Notes[9]); break;
            case 66: audioSrc.PlayOneShot(Notes[14]); break;
            case 67: audioSrc.PlayOneShot(Notes[16]); break;
            case 68: audioSrc.PlayOneShot(Notes[17]); break;
            case 69: audioSrc.PlayOneShot(Notes[9]); break;
            // repeat second verse
            case 70: audioSrc.PlayOneShot(Notes[21]); break;
            case 71: audioSrc.PlayOneShot(Notes[20]); break;
            case 72: audioSrc.PlayOneShot(Notes[21]); break;
            case 73: audioSrc.PlayOneShot(Notes[20]); break;
            case 74: audioSrc.PlayOneShot(Notes[21]); break;
            case 75: audioSrc.PlayOneShot(Notes[16]); break;
            case 76: audioSrc.PlayOneShot(Notes[19]); break;
            case 77: audioSrc.PlayOneShot(Notes[17]); break;
            case 78: audioSrc.PlayOneShot(Notes[14]); break;
            case 79: audioSrc.PlayOneShot(Notes[5]); break;
            case 80: audioSrc.PlayOneShot(Notes[9]); break;
            case 81: audioSrc.PlayOneShot(Notes[14]); break;
            case 82: audioSrc.PlayOneShot(Notes[16]); break;
            case 83: audioSrc.PlayOneShot(Notes[9]); break;
            case 84: audioSrc.PlayOneShot(Notes[17]); break;
            case 85: audioSrc.PlayOneShot(Notes[16]); break;
            case 86:
                {
                    count_song = 0; // start song from beginning
                    audioSrc.PlayOneShot(Notes[14]);
                    break;
                }

            default: audioSrc.PlayOneShot(Notes[0]); break;
        }
    }

    void PlayTetris()
    {
        count_song++;
        switch (count_song)
        {
            // first verse
            case 1: audioSrc.PlayOneShot(Notes[9]); break;
            case 2: audioSrc.PlayOneShot(Notes[4]); break;
            case 3: audioSrc.PlayOneShot(Notes[5]); break;
            case 4: audioSrc.PlayOneShot(Notes[7]); break;
            case 5: audioSrc.PlayOneShot(Notes[5]); break;
            case 6: audioSrc.PlayOneShot(Notes[4]); break;
            case 7:
            case 8: audioSrc.PlayOneShot(Notes[2]); break;
            case 9: audioSrc.PlayOneShot(Notes[5]); break;
            case 10: audioSrc.PlayOneShot(Notes[9]); break;
            case 11: audioSrc.PlayOneShot(Notes[7]); break;
            case 12: audioSrc.PlayOneShot(Notes[5]); break;
            case 13:
            case 14: audioSrc.PlayOneShot(Notes[4]); break;
            case 15: audioSrc.PlayOneShot(Notes[5]); break;
            case 16: audioSrc.PlayOneShot(Notes[7]); break;
            case 17: audioSrc.PlayOneShot(Notes[9]); break;
            case 18: audioSrc.PlayOneShot(Notes[5]); break;
            case 19:
            case 20: audioSrc.PlayOneShot(Notes[2]); break;
            // second verse
            case 21: audioSrc.PlayOneShot(Notes[7]); break;
            case 22: audioSrc.PlayOneShot(Notes[10]); break;
            case 23: audioSrc.PlayOneShot(Notes[14]); break;
            case 24: audioSrc.PlayOneShot(Notes[12]); break;
            case 25: audioSrc.PlayOneShot(Notes[10]); break;
            case 26: audioSrc.PlayOneShot(Notes[9]); break;
            case 27: audioSrc.PlayOneShot(Notes[5]); break;
            case 28: audioSrc.PlayOneShot(Notes[9]); break;
            case 29: audioSrc.PlayOneShot(Notes[7]); break;
            case 30: audioSrc.PlayOneShot(Notes[5]); break;
            case 31: audioSrc.PlayOneShot(Notes[4]); break;
            case 32: audioSrc.PlayOneShot(Notes[5]); break;
            case 33: audioSrc.PlayOneShot(Notes[7]); break;
            case 34: audioSrc.PlayOneShot(Notes[9]); break;
            case 35: audioSrc.PlayOneShot(Notes[5]); break;
            case 36: audioSrc.PlayOneShot(Notes[2]); break;
            case 37:
                {
                    count_song = 0; // start song from beginning
                    audioSrc.PlayOneShot(Notes[2]); break;
                }

            default: audioSrc.PlayOneShot(Notes[0]); break;
        }
    }

    void PlayRowYourBoat()
    {
        count_song++;
        switch (count_song)
        {
            // first verse
            case 1:
            case 2:
            case 3: audioSrc.PlayOneShot(Notes[10]); break;
            case 4: audioSrc.PlayOneShot(Notes[12]); break;
            case 5:
            case 6: audioSrc.PlayOneShot(Notes[14]); break;
            case 7: audioSrc.PlayOneShot(Notes[12]); break;
            case 8: audioSrc.PlayOneShot(Notes[14]); break;
            case 9: audioSrc.PlayOneShot(Notes[15]); break;
            case 10: audioSrc.PlayOneShot(Notes[17]); break;
            case 11:
            case 12: audioSrc.PlayOneShot(Notes[22]); break;
            case 13:
            case 14: audioSrc.PlayOneShot(Notes[17]); break;
            case 15:
            case 16: audioSrc.PlayOneShot(Notes[14]); break;
            case 17:
            case 18: audioSrc.PlayOneShot(Notes[10]); break;
            case 19: audioSrc.PlayOneShot(Notes[17]); break;
            case 20: audioSrc.PlayOneShot(Notes[15]); break;
            case 21: audioSrc.PlayOneShot(Notes[14]); break;
            case 22: audioSrc.PlayOneShot(Notes[12]); break;
            case 23:
                {
                    count_song = 0; // start song from beginning
                    audioSrc.PlayOneShot(Notes[10]); break;
                }

            default: audioSrc.PlayOneShot(Notes[0]); break;
        }
    }

    void PlayLittleStar()
    {
        count_song++;
        switch (count_song)
        {
            // first verse
            case 1:
            case 2: audioSrc.PlayOneShot(Notes[5]); break;
            case 3:
            case 4: audioSrc.PlayOneShot(Notes[12]); break;
            case 5:
            case 6: audioSrc.PlayOneShot(Notes[14]); break;
            case 7: audioSrc.PlayOneShot(Notes[12]); break;
            case 8:
            case 9: audioSrc.PlayOneShot(Notes[10]); break;
            case 10:
            case 11: audioSrc.PlayOneShot(Notes[9]); break;
            case 12:
            case 13: audioSrc.PlayOneShot(Notes[7]); break;
            case 14: audioSrc.PlayOneShot(Notes[5]); break;
            // second verse
            case 15:
            case 16: audioSrc.PlayOneShot(Notes[12]); break;
            case 17:
            case 18: audioSrc.PlayOneShot(Notes[10]); break;
            case 19:
            case 20: audioSrc.PlayOneShot(Notes[9]); break;
            case 21: audioSrc.PlayOneShot(Notes[7]); break;
            // repeat second verse
            case 22:
            case 23: audioSrc.PlayOneShot(Notes[12]); break;
            case 24:
            case 25: audioSrc.PlayOneShot(Notes[10]); break;
            case 26:
            case 27: audioSrc.PlayOneShot(Notes[9]); break;
            case 28: audioSrc.PlayOneShot(Notes[7]); break;
            // repeat first verse to finish song
            case 29:
            case 30: audioSrc.PlayOneShot(Notes[5]); break;
            case 31:
            case 32: audioSrc.PlayOneShot(Notes[12]); break;
            case 33:
            case 34: audioSrc.PlayOneShot(Notes[14]); break;
            case 35: audioSrc.PlayOneShot(Notes[12]); break;
            case 36:
            case 37: audioSrc.PlayOneShot(Notes[10]); break;
            case 38:
            case 39: audioSrc.PlayOneShot(Notes[9]); break;
            case 40:
            case 41: audioSrc.PlayOneShot(Notes[7]); break;
            case 42:
                {
                    count_song = 0; // start song from beginning
                    audioSrc.PlayOneShot(Notes[5]); break;
                }

            default: audioSrc.PlayOneShot(Notes[0]); break;
        }
    }
}
