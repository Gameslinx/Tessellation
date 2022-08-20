using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
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
}
