using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveableCube : MonoBehaviour
{
    bool graspMode = false;
    GameObject hand;
    void OnTriggerEnter(Collider other){
        graspMode = true;
        hand = other.gameObject.transform.parent.gameObject;
    }

    void Update(){
        if(graspMode){
            gameObject.transform.position = hand.transform.position;
            Debug.Log(hand.transform.position);
        }

        
    }
}
