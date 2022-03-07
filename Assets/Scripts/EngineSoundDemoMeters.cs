using System;
using UnityEngine;
using UnityEngine.UI;

namespace Yucatan.Scaff
{
    public class EngineSoundDemoMeters : MonoBehaviour
    {
        public Image RPMBar;
        public Text RPMText;

        public EngineSoundDemoController Car;

        public float MinRPM = 80, MaxRPM = 1000;

        public void Start()
        {
            Car = FindObjectOfType<EngineSoundDemoController>();
        }

        public void Update()
        {
            RPMBar.fillAmount = (Car.EngineRPM - MinRPM) / (MaxRPM - MinRPM);
            RPMText.text = Mathf.Round(Car.EngineRPM) + "";
        }
    }
}
