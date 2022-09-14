using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using KSP.UI.Screens.DebugToolbar.Screens.Cheats;
using ParallaxGrass;
using UnityEngine;

namespace Grass
{
    public class PQSBodyChangeEvent
    {
        public static void Fire(string name)
        {
            ScatterManagerPlus.Instance.RequestEarlyInitialization(name);
        }
    }

    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class ParallaxHarmonyPatcher : MonoBehaviour
    {
        public void Start()
        {
            ScatterLog.Log("Starting Harmony patching...");
            var harmony = new Harmony("Parallax");
            harmony.PatchAll();
            ScatterLog.Log("Harmony patching complete");
        }
    }

    [HarmonyPatch(typeof(PSystemSetup), nameof(PSystemSetup.SetPQSActive), new Type[] {typeof(PQS)})]
    class PQS_ForceStart
    {
        static void Prefix(PSystemSetup __instance, PQS pqs)
        {
            if (FlightGlobals.ActiveVessel != null)
            {
                Debug.Log("[Parallax Override] Terrain shader offset set to active vessel position prematurely");
                FloatingOrigin.ResetTerrainShaderOffset();
                FloatingOrigin.SetOffset(FlightGlobals.ActiveVessel.transform.position);
            }
            else
            {
                FloatingOrigin.ResetTerrainShaderOffset();
                FloatingOrigin.SetOffset(Vector3.zero);
            }
            PQS[] pqsArray = Traverse.Create(__instance).Field("pqsArray").GetValue() as PQS[];
            foreach (PQS gclass in pqsArray)
            {
                gclass.isDisabled = false;
                if (gclass != pqs)
                {
                    gclass.ResetAndWait();
                }
                else
                {
                    PQSBodyChangeEvent.Fire(gclass.name);
                    gclass.ForceStart();
                }
            }
        }
    }

    [HarmonyPatch(typeof(FlightGlobals), nameof(FlightGlobals.SetVesselPosition), new Type[] { typeof(int), typeof(double), typeof(double), typeof(double), typeof(double), typeof(double), typeof(bool), typeof(bool), typeof(double) })]
    class SetVesselPosShaderOffset
    {
        static void Prefix(FlightGlobals __instance, int selBodyIndex, double latitude, double longitude, double altitude, double inclination, double heading, bool asl, bool easeToSurface = false, double gravityMultiplier = 0.1)
        {
            Debug.Log("[Parallax Override] Cheating setting position, regenerating scatters and resetting shader offset");
            
            __instance.SetVesselPosition(selBodyIndex, latitude, longitude, altitude, new Vector3((float)inclination, 0f, (float)heading), asl, easeToSurface, gravityMultiplier);
            FloatingOrigin.ResetTerrainShaderOffset();
            foreach (KeyValuePair<PQ, QuadData> data in PQSMod_ParallaxScatter.quadList)
            {
                foreach (KeyValuePair<Scatter, ScatterCompute> scatter in data.Value.comps)
                {
                    scatter.Value.Start();
                }
            }
        }
    }
}
