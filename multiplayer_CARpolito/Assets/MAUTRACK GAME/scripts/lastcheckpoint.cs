using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class lastcheckpoint : MonoBehaviour
{
    
	[SerializeField] private Transform player; 
	[SerializeField] private Transform respawnPoint;
		
	//public GameObject Car2;
    public void Revive2()
    
    {
        
			player.transform.position = respawnPoint.transform.position; 
			player.transform.rotation = respawnPoint.transform.rotation;
			player.GetComponent<Rigidbody>().isKinematic = true;
			player.GetComponent<Rigidbody>().isKinematic = false;
			Physics.SyncTransforms(); 			
			
    }
} 


/*


GameObject[] f1 = GameObject.FindGameObjectsWithTag("hideme00");

foreach(GameObject f in f1)
 {
     hideme3.SetActuve(false);
 }
Perdón creo que es...
f.SetActive(false);


*/