using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReviveCar : MonoBehaviour
{
    public GameObject Car;

    public void Revive()
    
    {
        //Rotates Car
        Car.transform.rotation = Quaternion.Euler(0, 0, 0);
        // Debug.Log("Revived");
    }
}  
