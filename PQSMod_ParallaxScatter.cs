using ComputeLoader;
using Grass;
using Kopernicus.ConfigParser.Attributes;
using Kopernicus.ConfigParser.BuiltinTypeParsers;
using Kopernicus.ConfigParser.Enumerations;
using Kopernicus.Configuration.ModLoader;
using ParallaxGrass;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;

namespace ParallaxGrass
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class Counter : MonoBehaviour
    {
        public void Update()
        {
            bool flag = Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.Alpha4);
            bool flag2 = Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.Alpha2);
            if (flag)
            {
                int count = PQSMod_ParallaxScatter.quadList.Count;
                int cleanedCount = 0;
                int actualCleanedCount = 0;
                float totalMem = 0;

                float bestCase = 0;
                float worstCase = 0;
                foreach (QuadData data in PQSMod_ParallaxScatter.quadList.Values)
                {
                    if (!data.quad.isVisible)
                    {
                        actualCleanedCount++;
                    }
                    foreach (ScatterCompute sc in data.comps.Values)
                    {
                        Vector3 usage = sc.GetTotalMemoryUsage();
                        Debug.Log(" - - - Best case: " + usage.z.ToString("F3") + " - - - Worst case: " + usage.y.ToString("F3"));
                        bestCase += usage.z;
                        worstCase += usage.y;
                        totalMem += usage.x; //(float)sc.totalMem / (1024f * 1024f);
                        if (!sc.quad.isVisible || sc.cleaned)
                        {
                            cleanedCount++;
                        }
                    }
                }
                Debug.Log("Total memory in use by compute buffers: " + totalMem + " MB");
                Debug.Log("Out of a total of " + count + " quads, " + actualCleanedCount + " were invisible and " + cleanedCount + " quadData components were inactive");

                //Get total object count in all compute buffers
                foreach (ScatterComponent scatterManager in ScatterManagerPlus.scatterComponents[FlightGlobals.currentMainBody.name])
                {
                    if (scatterManager.scatter.scatterName != null && Buffers.activeBuffers.ContainsKey(scatterManager.scatter.scatterName))
                    {
                        int objCount = Buffers.activeBuffers[scatterManager.scatter.scatterName].GetObjectCount();
                        int capacity = Buffers.activeBuffers[scatterManager.scatter.scatterName].GetCapacity();
                        Debug.Log("Object count of " + scatterManager.scatter.scatterName + " right now is " + objCount + " out of " + capacity + " which means " + (((float)objCount / (float)capacity) * 100) + "% of the buffer is in use right now");
                        float localTotal = Buffers.activeBuffers[scatterManager.scatter.scatterName].GetMemoryInMB();
                        Debug.Log(" - Total for this compute buffer: " + localTotal + " MB");
                        totalMem += localTotal;
                    }
                }
                Debug.Log("Absolute total memory usage of everything (in MB): " + totalMem);
                Debug.Log("Out of the PositionBuffers, best case object usage is " + bestCase + " MB and the actual memory footprint is " + worstCase + " MB");
                Debug.Log("This results in " + (100f - ((bestCase / worstCase) * 100f)) + "% memory that is being wasted and can be saved");
            }

            if (flag2)
            {

            }
        }
    }
    public class PQSMod_ParallaxScatter : PQSMod
    {
        public static Dictionary<PQ, QuadData> quadList = new Dictionary<PQ, QuadData>(); //This WILL always reach 0 when no quads are in range
        public override void OnSetup()
        {
            if (!ScatterGlobalSettings.enableScatters)
            {
                this.modEnabled = false;
            }
        }
        public override void OnQuadBuilt(PQ quad)
        {
            if (quad.subdivision > ScatterBodies.scatterBodies[quad.sphereRoot.name].minimumSubdivision)       //Do everything scatter related within allowed subdivision
            {
                QuadData data = new QuadData(quad);
                quadList.Add(quad, data);
            }
        }
        public override void OnQuadDestroy(PQ quad)
        {
            if (quadList.ContainsKey(quad)) { quadList[quad].Cleanup(); }
            quadList.Remove(quad);     //Not all quads on destroy are in the dictionary, but it always falls down to 0
        }
    }
    [RequireConfigType(ConfigType.Node)]
    public class ParallaxScatter : ModLoader<PQSMod_ParallaxScatter>
    {
        [ParserTarget("order", Optional = true)]
        public NumericParser<int> order
        {
            get { return Mod.order; }
            set { Mod.order = int.MaxValue - 1; }
        }
    }
}
