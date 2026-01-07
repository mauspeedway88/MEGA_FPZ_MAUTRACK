//using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleCollideScriptPlatform : MonoBehaviour
{
    public Transform childtObject;

    // bool rotation;


   void OnTriggerEnter(Collider col)
    {
       if (col.gameObject.tag == "Player")        {
           childtObject.transform.parent = this.transform;
           print("TriggerEnter");
           //rotation = true;
       }


}
void OnTriggerExit(Collider col)
{
    if (col.gameObject.tag == "Player")
    {
        childtObject.transform.parent = null;
        print("triggerexit");
        //rotation = false;

    }
}
}


