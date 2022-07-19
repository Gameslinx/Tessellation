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
                foreach (QuadData data in PQSMod_ParallaxScatter.quadList.Values)
                {
                    if (!data.quad.isVisible)
                    {
                        actualCleanedCount++;
                    }
                    foreach (ScatterCompute sc in data.comps.Values)
                    {
                        totalMem += sc.totalMem / (1024f * 1024f);
                        if (!sc.quad.isVisible || sc.cleaned)
                        {
                            cleanedCount++;
                        }
                    }
                }
                Debug.Log("Total memory in use by compute buffers: " + totalMem);
                Debug.Log("Total memory in use by compute buffers: " + ((float)totalMem / (1024f * 1024f)) + "mb");
                Debug.Log("Out of a total of " + count + " quads, " + actualCleanedCount + " were invisible and " + cleanedCount + " quadData components were inactive");

                //Get total object count in all compute buffers
                foreach (PQSMod_ScatterManager scatterManager in ActiveBuffers.mods)
                {
                    if (scatterManager.scatterName != null && Buffers.activeBuffers.ContainsKey(scatterManager.scatterName))
                    {
                        int objCount = Buffers.activeBuffers[scatterManager.scatterName].GetObjectCount();
                        int capacity = Buffers.activeBuffers[scatterManager.scatterName].GetCapacity();
                        Debug.Log("Object count of " + scatterManager.scatterName + " right now is " + objCount + " out of " + capacity + " which means " + (((float)objCount / (float)capacity) * 100) + "% of the buffer is in use right now");
                        totalMem += Buffers.activeBuffers[scatterManager.scatterName].GetMemoryInMB();
                    }
                }
                Debug.Log("Absolute total memory usage of everything (in MB): " + totalMem);
            }

            if (flag2)
            {
                Debug.Log("Number of textures loaded right now: " + LoadOnDemand.activeTextures.Count);
                float memImpact = 0;
                foreach(KeyValuePair<string, Texture> activeTexture in LoadOnDemand.activeTextures)
                {
                    memImpact += Profiler.GetRuntimeMemorySizeLong(activeTexture.Value);
                    Debug.Log("Memory impact of " + activeTexture.Key + " is " + (Profiler.GetRuntimeMemorySizeLong(activeTexture.Value) / (1024L * 1024L)));
                    Debug.Log("\t - GraphicsFormat: " + activeTexture.Value.graphicsFormat.ToString());
                    Debug.Log("\t - IsReadable: " + activeTexture.Value.isReadable.ToString());
                    Debug.Log("\t - Mip Count: " + activeTexture.Value.mipmapCount.ToString());
                }
                Debug.Log("Total memory usage by Scatter textures: " + memImpact);
            }
        }
    }
    public class PQSMod_ParallaxScatter : PQSMod
    {
        public static Dictionary<PQ, QuadData> quadList = new Dictionary<PQ, QuadData>(); //This WILL always reach 0 when no quads are in range
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
            if (quad.subdivision > ScatterBodies.scatterBodies[quad.sphereRoot.name].minimumSubdivision)    //Remove from dictionary, remember to purge the buffers at least
            {
                if (quadList.ContainsKey(quad)) { quadList[quad].Cleanup(); }
                quadList.Remove(quad);     //Not all quads on destroy are in the dictionary, but it always falls down to 0
            }
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
