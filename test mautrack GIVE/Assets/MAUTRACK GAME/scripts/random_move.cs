using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class random_move : MonoBehaviour
{

    private Animation anim;    
    float animrandom = 1.444F;
    
    void Start()
    {
        anim = gameObject.GetComponent<Animation>();
        animrandom = Random.Range(0.8F, 1.2F);      
        //Debug.Log(animrandom);
        anim["moverse"].speed = animrandom;
    }


}
