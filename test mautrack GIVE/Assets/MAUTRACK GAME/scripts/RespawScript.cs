using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RespawScript : MonoBehaviour
{
	

		[SerializeField] private Transform player; 
		[SerializeField] private Transform respawnPoint; 

		private void OnTriggerEnter(Collider other) 
{ 
		if (other.CompareTag("Player")) 
		{ 
			player.transform.position = respawnPoint.transform.position; 
			player.transform.rotation = respawnPoint.transform.rotation;
			player.GetComponent<Rigidbody>().isKinematic = true;
			player.GetComponent<Rigidbody>().isKinematic = false;
			Physics.SyncTransforms(); 			
		}	
}

}
