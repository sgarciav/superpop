using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectRegion : MonoBehaviour
{
    public GameObject cubePrefab;
    // Start is called before the first frame update

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = 2.0f;       // we want 2m away from the camera position
            Vector3 objectPos = Camera.current.ScreenToWorldPoint(mousePos);
            Instantiate(cubePrefab, objectPos, Quaternion.identity);
        }
    }
}
