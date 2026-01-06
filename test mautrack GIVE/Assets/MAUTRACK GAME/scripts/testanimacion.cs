using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class testanimacion : MonoBehaviour
{
    
    public Animation anim2;

    private void OnTriggerEnter(Collider other)
    {
     if (other.gameObject.tag == "marlyn")
     {
     anim2.Play("sube");
     }
    }

    private void OnTriggerExit(Collider other)
    {
     if (other.gameObject.tag == "marlyn")
     {
     anim2.Play("baja");
     }
    }

}
