using UnityEngine;
using System.Collections;

public class WaitSeconds : MonoBehaviour
{
	
	public GameObject arrow;
	
	
    void Update()
    {
        //Start the coroutine we define below named ExampleCoroutine.
        StartCoroutine(ExampleCoroutine());
    }

    IEnumerator ExampleCoroutine()
    {
        //Print the time of when the function is first called.
        Debug.Log("Started Coroutine at timestamp : " + Time.time);

        //yield on a new YieldInstruction that waits for 5 seconds.
        yield return new WaitForSeconds(2);
        arrow.SetActive(false);
        yield return new WaitForSeconds(2);
        arrow.SetActive(true);

        //After we have waited 5 seconds print the time again.
        Debug.Log("Finished Coroutine at timestamp : " + Time.time);
    }
}





