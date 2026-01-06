using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;
using UnityStandardAssets.Vehicles.Car;

public class CheckPoints : MonoBehaviour
{   
    private CheckPointsManager _checkPointsManager;
    private CheckPointNumber keepCheckPointNumber;
    private Collider[] collidersToIgnore;
    public GameObject messagePanel;
    public Slider progressBar;
    public Text nextCheckPointText;
    public CarController _carController;
    public float speedChanger;
    public float originalSpeed;
    public float maxSpeedLimit;
    
    // Start is called before the first frame update
    void Start()
    {
        _carController = GameObject.FindObjectOfType<CarController>();
        originalSpeed = _carController.m_Topspeed;
        _checkPointsManager = GameObject.FindObjectOfType<CheckPointsManager>();
        collidersToIgnore = new Collider[this.transform.GetChild(0).childCount];
        for(int i =0; i < collidersToIgnore.Length; i++)
        {
            collidersToIgnore[i] = this.transform.GetChild(0).GetChild(i).GetComponent<Collider>();
        }
        //progressBar.maxValue = _checkPointsManager.checkPointsArray.Length;
        //progressBar.value = CheckPointsManager.checkPointCounter;
        int checkPointTextValue;
        checkPointTextValue = CheckPointsManager.checkPointCounter + 1;
        nextCheckPointText.text = checkPointTextValue.ToString();

    }

    // Update is called once per frame
    void Update()
    {
      
    }

    private void OnTriggerEnter(Collider other)
    {
    	
    	/*if (other.gameObject.tag == "lastposition")
        {
        	Debug.Log("lastposition" );
        }*/	
        	
        if (other.gameObject.tag == "CheckPoint")
        {
            ignoreCollision(collidersToIgnore,other);
                
                keepCheckPointNumber = other.gameObject.GetComponent<CheckPointNumber>();
               if (keepCheckPointNumber.checkPointNumber == CheckPointsManager.checkPointCounter)
                { 
                    CheckPointsManager.checkPointCounter++;
                int checkPointTextValue;
                checkPointTextValue = CheckPointsManager.checkPointCounter + 1;
                if (checkPointTextValue> _checkPointsManager.checkPointsArray.Length)
                { nextCheckPointText.text = "FINISH"; }
                else { nextCheckPointText.text = checkPointTextValue.ToString(); }
                
                _checkPointsManager.changeMat(1);
                progressBar.value = CheckPointsManager.checkPointCounter;
                if(keepCheckPointNumber.checkPointNumber==_checkPointsManager.checkPointsArray.Length-1)
                {
                    _checkPointsManager.changeMat(2);
                }
                if (messagePanel.activeSelf) { messagePanel.SetActive(false); }

            }
            else
            {
                
                messagePanel.SetActive(true);
            }



        }

        if (other.gameObject.tag == "SpeedUPBump")
        {

            _carController.MaxSpeed  = _carController.CurrentSpeed+ maxSpeedLimit;
            _carController.speedChangeFactor = speedChanger;
            _carController.changeSpeed = true;
         
        }
    }

  public void ResetSpeed()
        {
         //   _carController.speedChangeFactor = _carController.speedChangeFactor /speedChanger;
       // _carController.m_Topspeed = originalSpeed;
        }
    private void OnTriggerExit(Collider other)
    {
        _carController.MaxSpeed = originalSpeed;
        _carController.speedChangeFactor = 1;
        _carController.changeSpeed = false;
    }
    public void ignoreCollision(Collider [] vehicleColliders, Collider checkPointCollider)
    {
        Collider tempCheckPointsCollider = checkPointCollider.GetComponent<Collider>();
        for(int i =0; i<vehicleColliders.Length;i++)
        {
            Collider temp = vehicleColliders[i].GetComponent<Collider>();
            Physics.IgnoreCollision(temp, tempCheckPointsCollider);
        }
    }



   
}
