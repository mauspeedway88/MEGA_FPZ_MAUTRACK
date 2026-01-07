using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckPointDetector : MonoBehaviour
{
    public GameObject ReviveCheckpoint;
    public GameObject car;
    private int Counter;
    // Start is called before the first frame update


    // Update is called once per frame
    void Update()
    {
        if (car.transform.rotation.eulerAngles.z == -180)
        {
           Debug.Log("WorkingPerfect");
        }
    }
    
}
