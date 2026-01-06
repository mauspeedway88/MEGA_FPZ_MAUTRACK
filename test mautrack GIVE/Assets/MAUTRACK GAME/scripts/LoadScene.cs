// SceneA.
// SceneA is given the sceneName which will
// load SceneB from the Build Settings

using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadScene : MonoBehaviour
{
    void Start()
    {
        
    }

    public void LoadA(string scenename_01)
    {
        Debug.Log("sceneName to load: " + scenename_01);
        SceneManager.LoadScene(scenename_01);
        //Debug.Log(scenename);
    }



}