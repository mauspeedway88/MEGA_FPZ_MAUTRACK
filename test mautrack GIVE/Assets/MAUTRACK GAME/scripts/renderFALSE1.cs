using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class renderFALSE1 : MonoBehaviour
{
    public Material [] material;
    Renderer rend;


void Start()
    {
        rend = GetComponent<Renderer>();
        rend.enabled = false; 
    } 
  
}
