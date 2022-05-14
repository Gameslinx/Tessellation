using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Grass
{
    public class GlobalSettings
    {
        public static float rangeMult = 1.0f;
        public static float densityMult = 1.0f;
        public static bool frustumCull = true;
        public static float computeUpdateRate = 1.0f;
    }
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class ScatterGlobalSettings : MonoBehaviour
    {
        void Start()
        {

        }
    }
}
