using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class ShowMessage : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
       
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnEnable()
    {
        StartCoroutine(TypeMessage());
    }

    private void OnDisable()
    {
        foreach (char letter in text.ToCharArray())
        {
            label.text = "";
        }
    }

    [TextArea]
    public string text;

   // RangeAttribute[(0.01f,0.1f)]
    public float characterInterval;
    private Text label;

    private void Awake()
    {
        label = GetComponent<Text>();

    }

   
    IEnumerator TypeMessage()
    {

        foreach(char letter in text.ToCharArray())
        {
            label.text += letter;
            yield return new WaitForSeconds(.01f);

        }
    }
}
