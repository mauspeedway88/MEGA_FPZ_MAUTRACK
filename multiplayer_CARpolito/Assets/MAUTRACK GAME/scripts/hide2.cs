using UnityEngine;
using System.Collections;

public class hide2 : MonoBehaviour 
{
    public GameObject[] hideme3;
    public GameObject[] no_hideme4;
    
    void Start()
    {
        hideme3 = GameObject.FindGameObjectsWithTag("hideme00");

		foreach (GameObject hideme3 in hideme3)
        {
            hideme3.SetActive(false);
        }  
        //----------------------------------------------------
        no_hideme4 = GameObject.FindGameObjectsWithTag("NO_hide");

		foreach (GameObject no_hideme4 in no_hideme4)
        {
            no_hideme4.SetActive(true);
        } 
        
               
    }
}