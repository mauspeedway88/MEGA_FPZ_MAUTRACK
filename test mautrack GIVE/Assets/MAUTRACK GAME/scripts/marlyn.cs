using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class marlyn : MonoBehaviour
{

//public GameObject marlyn_Objc;

    public Animation anim;

    void Start()
    {
    //anim = gameObject.GetComponent<Animation>();
    //anim.Play("godown");     
    }
    
    
     private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "marlyn")
        {
            StartCoroutine(ExampleCoroutine());
        }
    }



     IEnumerator ExampleCoroutine()
        {
        yield return new WaitForSeconds(7);
        anim.Play("upup");
        yield return new WaitForSeconds(4);
        anim.Play("down00");
        }


}