using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIScript : MonoBehaviour
{
    public Slider gravityInput;
    public Slider viscosityInput;
    public Slider targetDensityInput;
    public Slider pressureMultiplierInput;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void ChangeGravity()
    {
        gameObject.GetComponent<Simulation3D>().gravity = gravityInput.value;
        gameObject.GetComponent<Simulation2D>().gravity = gravityInput.value;
    }

    public void ChangeViscosity()
    {
        gameObject.GetComponent<Simulation2D>().viscosityStrength = viscosityInput.value;
    }

    public void ChangeTargetDensity()
    {
        gameObject.GetComponent<Simulation2D>().targetDensity = targetDensityInput.value;
    }

    public void ChangePressureMultiplier()
    {
       gameObject.GetComponent<Simulation2D>().pressureMultiplier = pressureMultiplierInput.value;
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
