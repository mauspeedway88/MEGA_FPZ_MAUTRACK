//using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleRotationScript : MonoBehaviour
{
   
    void OnTriggerEnter(Collider col)
    {
        if (col.gameObject.tag == "Plataform") {
           this.transform.parent = col.gameObject.transform;
           print("TriggerEnter");
         
       }


}
void OnTriggerExit(Collider col)
{
        if (col.gameObject.tag == "Plataform")
        {
            this.transform.parent = null;
             print("TriggerExit");
            
        }

    }
}


