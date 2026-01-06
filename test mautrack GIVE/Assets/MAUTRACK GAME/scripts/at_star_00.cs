using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class at_star_00 : MonoBehaviour
{

public GameObject activame;
public GameObject desactivame;
public GameObject activame_button;
public GameObject desactivame_button;
public GameObject canvasscreen;

    void Start()
    {
       // rend = GetComponent<Renderer>();
       // rend.enabled = false; 
       desactivame.SetActive(false);
       activame.SetActive(true);
       desactivame_button.SetActive(false);
       activame_button.SetActive(true);
       canvasscreen.SetActive(true);
    }
    
	
}
