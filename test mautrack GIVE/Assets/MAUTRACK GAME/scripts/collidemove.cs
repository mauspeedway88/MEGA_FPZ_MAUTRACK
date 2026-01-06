using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class collidemove : MonoBehaviour
{
/*	
     void OnTriggerStay(Collider other)
    {
                 if(other.gameObject.tag == "Player")
                 {
                 transform.parent = other.transform;
                  }
    }
    
        void OnTriggerExit(Collider other)
    {
				if(other.gameObject.tag == "Player")
				{
                 transform.parent = null;
                   }

    }
              
 }
 */

    // Applies an upwards force to all rigidbodies that enter the trigger.
    void OnTriggerStay(Collider other)
    {
        if (other.attachedRigidbody)
        Debug.Log("collide forse");
            other.attachedRigidbody.AddForce(Vector3.up * 50);
            transform.parent = other.transform;
    }
}


  