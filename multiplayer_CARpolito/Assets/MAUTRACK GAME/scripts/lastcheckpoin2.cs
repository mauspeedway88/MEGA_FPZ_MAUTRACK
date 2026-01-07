using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class lastcheckpoint2 : MonoBehaviour
{
    
	[SerializeField] private Transform player; 
	[SerializeField] private Transform respawnPoint;
	
	GameObject[] f1 = GameObject.FindGameObjectsWithTag("hideme00");
		
	//public GameObject Car2;
    public void Revive2()
    
    {
        
			foreach(GameObject f in f1)
 		{
     		f.SetActive(false);
 		}			
			
    }
} 












