using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIScript : MonoBehaviour
{
    public TMP_InputField gravityInput;
    public TMP_InputField viscosityInput;
    public TMP_InputField targetDensityInput;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void ChangeGravity()
    {
        if(float.TryParse(gravityInput.text, out float result))
        {
            gameObject.GetComponent<Circle>().gravity = result;
        }
    }

    public void ChangeViscosity()
    {
        if (float.TryParse(viscosityInput.text, out float result))
        {
            gameObject.GetComponent<Circle>().viscosityStrength = result;
        }
    }

    public void ChangeTargetDensity()
    {
        if (float.TryParse(targetDensityInput.text, out float result))
        {
            gameObject.GetComponent<Circle>().targetDensity = result;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
