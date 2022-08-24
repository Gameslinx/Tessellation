using Grass.DebugStuff;
using ParallaxGrass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Grass
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class BodySwitchManager : MonoBehaviour   //Seriously screw KSP's OnDominantBodyChange event, useless pile of garbage that's cost me 2 days straight of my time
    {
        public delegate void BodyChange(string from, string to);
        public static event BodyChange onBodyChange;

        public delegate void SceneChange(GameScenes from, GameScenes to);
        public static event SceneChange onSceneChange;

        public string currentBody = "NoBody";
        public GameScenes currentScene = GameScenes.LOADING;

        public static BodySwitchManager Instance;

        public bool scaledWasActive = false;
        public float lastOpacity = 0;
        void Awake()
        {
            GameObject.DontDestroyOnLoad(this);
            Instance = this;
        }
        void Update()       //Tie all body change events into this. Explicitly load the new textures first BEFORE processing any events
        {
            if (FlightGlobals.currentMainBody != null)
            {
                if (currentBody != FlightGlobals.currentMainBody.name)
                {
                    LoadOnDemand.OnBodyChange(FlightGlobals.currentMainBody.name);
                    ScatterLog.Log("Processing a body change from " + currentBody + " to " + FlightGlobals.currentMainBody);
                    if (onBodyChange != null) { onBodyChange(currentBody, FlightGlobals.currentMainBody.name); }    //Dominant body changed
                                                                                                                    //Submissive body when? o_O wdym by that
                    currentBody = FlightGlobals.currentMainBody.name;
                }       
            }
            if (currentScene != HighLogic.LoadedScene)
            {
                //SpaceCenter to Flight, for example, is skipped by the body change code. Update the scatter managers accordingly
                ScatterLog.Log("Processing a scene change from " + currentScene.ToString() + " to " + HighLogic.LoadedScene.ToString());
                if (onSceneChange != null) { onSceneChange(currentScene, HighLogic.LoadedScene); }
                currentScene = HighLogic.LoadedScene;
            }
            if (FlightGlobals.currentMainBody != null)
            {
                float opacity = FlightGlobals.currentMainBody.pqsController.surfaceMaterial.GetFloat("_PlanetOpacity");
                if (opacity > 0)
                {
                    scaledWasActive = true;
                    lastOpacity = opacity;
                }
                if (opacity == 0 && lastOpacity > 0)    //Must regenerate the scatters, since the shader offset was changed
                {
                    foreach (KeyValuePair<PQ, QuadData> data in PQSMod_ParallaxScatter.quadList)
                    {
                        foreach (KeyValuePair<Scatter, ScatterCompute> scatter in data.Value.comps)
                        {
                            scatter.Value.Start();
                        }
                    }
                    lastOpacity = opacity;
                    scaledWasActive = false;
                }
            }
            
        }
    }
}
