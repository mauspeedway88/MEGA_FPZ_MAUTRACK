using UnityEngine;
public class rendermaterial : MonoBehaviour
{
    public Material [] material;
    public int x;
    Renderer rend;
    
    void Start()
    {
        x=0;
        rend = GetComponent<Renderer>();
        rend.enabled = true;
        rend.sharedMaterial = material[0];

    }
    

  void OnTriggerEnter(Collider col)
    	{
       if (col.gameObject.tag == "camara1")        
       {
		rend.sharedMaterial = material[1];
        Debug.Log("hola");
       }
    	}
       
  void OnTriggerExit(Collider col)
    	{
       if (col.gameObject.tag == "camara1")        
       {
		rend.sharedMaterial = material[0];
       }
    	}
       

    
}
