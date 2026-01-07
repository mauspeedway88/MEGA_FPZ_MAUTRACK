using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class triggercheckpointsound : MonoBehaviour
{
    public AudioSource PlaySound;
    void OnTriggerEnter(Collider other)
    {   
        PlaySound.Play();
    }
}
