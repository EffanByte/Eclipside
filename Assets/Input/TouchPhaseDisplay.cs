using UnityEngine;
using UnityEngine.UI;   
public class TouchPhaseDisplay : MonoBehaviour
{

    public Text phaseDisplayText;
    private Touch theTouch;
    private float timeTouchEnded;
    private float displayTime = 0.5f; 

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.touchCount > 0 )
       {
           theTouch = Input.GetTouch(0);
           if(theTouch.phase == TouchPhase.Ended)
           {
               phaseDisplayText.text = theTouch.phase.ToString();
               timeTouchEnded = Time.time;
           }
           else if(Time.time - timeTouchEnded > displayTime)
           {
               phaseDisplayText.text = theTouch.phase.ToString();
               timeTouchEnded = Time.time;
           }            
       }
       else if (Time.time - timeTouchEnded > displayTime)
       {
           phaseDisplayText.text = "";
       }
    }
}
