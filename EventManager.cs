using UnityEngine;
using Grass;
using System.Collections.Generic;
using System.Linq;

namespace ComputeLoader
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class EventManager : MonoBehaviour
    {
        public delegate void ParallaxEvent(Vector3d shaderOffset);
        public static event ParallaxEvent OnShaderOffsetUpdated;
        public Vector3d lastShaderOffset = Vector3.zero;

        public delegate void ComputeEvent();
        public static event ComputeEvent OnEvaluateRequired;
        float lastTime = 0;
        void Start()
        {
            lastTime = Time.realtimeSinceStartup;
        }
        void Update()
        {
            if (FloatingOrigin.TerrainShaderOffset != lastShaderOffset) 
            { 
                lastShaderOffset = FloatingOrigin.TerrainShaderOffset;
                if (OnShaderOffsetUpdated != null)
                {
                    OnShaderOffsetUpdated(lastShaderOffset);
                }
            }

            float timeElapsed = Time.realtimeSinceStartup - lastTime;   //Force an evaluate update
            if (timeElapsed > 1 && OnEvaluateRequired != null)
            {
                lastTime = Time.realtimeSinceStartup;
                OnEvaluateRequired();
            }
        }
    }
}