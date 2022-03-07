using UnityEngine;
using System;

public class EngineSoundDemoController : MonoBehaviour, ISoundsLikeACar
{
    public float accelerator = 1;
    public bool LiveInput = false;

    public float engineRPM;
    public float EngineRPM
    {
        get { return engineRPM * accelerator; }
    }

    public int pistonCount = 6;
    public int PistonCount
    {
        get { return pistonCount; }
    }

    public bool AllStopped
    {
        get { return false; }
    }

    public void Update()
    {
        if (LiveInput)
        {
            accelerator = Input.mousePosition.y / (float)Screen.height;
            //accelerator = Input.GetAxis("Accelerator") + 0.1f;
        }
    }

    public void SetRPM(float rpm)
    {
        engineRPM = rpm*10f;
    }
}
