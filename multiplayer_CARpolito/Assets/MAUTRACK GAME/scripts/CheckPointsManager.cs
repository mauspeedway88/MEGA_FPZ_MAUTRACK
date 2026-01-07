//using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckPointsManager : MonoBehaviour
{
    //int checkPointNumber;
    [HideInInspector]
    public Transform [] checkPointsArray;
    public Material _materialToChangeTo;
    public Material originalMaterial;
    public static int checkPointCounter;
   // public int showcounter;
    //public int perviousCheckPoint;
    // Start is called before the first frame update
    void Start()
    {
        //StartCoroutine(ExampleCoroutine());
        int totalChildern = this.transform.childCount;
        checkPointsArray = new Transform[totalChildern];
        for(int i =0;i<checkPointsArray.Length;i++)
        {
            checkPointsArray[i] = this.transform.GetChild(i);
            this.transform.GetChild(i).GetComponent<CheckPointNumber>().checkPointNumber = i;
        }
        checkPointCounter = 0;
        changeMat(0);

            Debug.Log("checkPointCounter = " + checkPointCounter);
            Debug.Log("checkPointsArray = " + checkPointsArray);

    }
   
    // Update is called once per frame
    void Update()
    {
       // showcounter = checkPointCounter;
       // print("checkPoint counter"+showcounter);
        //yield WaitForSeconds (4);
        //    Debug.Log("checkPointCounter = " + checkPointCounter);
        //    Debug.Log("checkPointsArray = " + checkPointsArray);
        //StartCoroutine(ExampleCoroutine());





    }

    public void changeMat(int value)
    {
        int holdCheckpointCounter;
        holdCheckpointCounter = checkPointCounter;

        switch (value) {
            case 0:
       // if (holdCheckpointCounter == 0)
       // {
            checkPointsArray[0].GetChild(0).GetChild(0).GetComponent<Renderer>().material = _materialToChangeTo;
                break;
            // }
            //  else
            // {
            case 1:
            if (holdCheckpointCounter >= checkPointsArray.Length)
            {
                holdCheckpointCounter = checkPointsArray.Length - 1;
            }
            checkPointsArray[holdCheckpointCounter].GetChild(0).GetChild(0).GetComponent<Renderer>().material = _materialToChangeTo;
            checkPointsArray[holdCheckpointCounter - 1].GetChild(0).GetChild(0).GetComponent<Renderer>().material = originalMaterial;
                break;
            case 2:
                holdCheckpointCounter = checkPointsArray.Length - 1;
                checkPointsArray[holdCheckpointCounter].GetChild(0).GetChild(0).GetComponent<Renderer>().material = originalMaterial;
                break;
     //   }
    }
    }
/*
        IEnumerator ExampleCoroutine()
        {
        while (true) {
        yield return new WaitForSeconds(5);
        Debug.Log("checkPointCounter = " + checkPointCounter);
         
        }

    } */

}
