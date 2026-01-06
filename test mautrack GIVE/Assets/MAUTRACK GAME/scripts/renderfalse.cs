using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class renderfalse : MonoBehaviour
{
    private Renderer rend;
    // Start is called before the first frame update
    void Start()
    {
        rend = GetComponent<Renderer>();
        rend.enabled = false; 
    }


}






