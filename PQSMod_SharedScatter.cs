using ComputeLoader;
using Kopernicus.ConfigParser.Attributes;
using Kopernicus.ConfigParser.BuiltinTypeParsers;
using Kopernicus.ConfigParser.Enumerations;
using Kopernicus.Configuration.ModLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Grass
{
    public class PQSMod_SharedScatter : PQSMod
    {
        public string scatterName = "a";
        public string parentName = "b";

        public Scatter scatter;

        public PostCompute pc;

        bool eventAlreadyAdded = false; //OnEnable is broken. OnSetup is called more than once. I want to die

        public void Awake()
        {
            Kopernicus.Events.OnPostBodyFixing.Add(InitialSetup);
        }
        public void InitialSetup(CelestialBody body)
        {
            if (eventAlreadyAdded) { return; }                                          //Don't want event added multiple times
            if (body.name != sphere.name) { return; }                                   //Body that has just been added is not this one
            ScatterLog.Log("Initial setup for " + scatterName + " shared manager");
            scatter = ScatterBodies.scatterBodies[sphere.name].scatters[scatterName];
            BodySwitchManager.onBodyChange += OnBodyChanged;
            BodySwitchManager.onSceneChange += OnSceneChanged;
            if (pc == null) { pc = FlightGlobals.GetBodyByName(sphere.name).gameObject.AddComponent<PostCompute>(); pc.scatterName = scatterName; pc.planetName = scatter.planetName; }
            eventAlreadyAdded = true;
        }
        public void OnDestroy()
        {
            BodySwitchManager.onBodyChange -= OnBodyChanged;
            BodySwitchManager.onSceneChange -= OnSceneChanged;
            Kopernicus.Events.OnPostBodyFixing.Remove(InitialSetup);
            eventAlreadyAdded = false;
        }
        public void OnBodyChanged(string from, string to)
        {
            if (scatter == null) { Debug.Log("Scatter is null"); Debug.Log("Name should be " + scatterName); }
            Debug.Log("Processing body change in " + name + " for " + scatter.scatterName);
            Debug.Log("From: " + from + " to: " + to);
            FloatingOrigin.TerrainShaderOffset = Vector3.zero;

            if (to != scatter.planetName)
            {
                stop = true;
                pc.active = false;
                pc.setupInitial = false;    //Force setup again on body switch
            }
            if (to == scatter.planetName)
            {
                stop = false;
                pc.active = true;
                Debug.Log("New body by name: " + to);
                Debug.Log("Successful body change for: " + to + " - " + FlightGlobals.GetBodyByName(to).name);
            }
        }
        public void OnSceneChanged(GameScenes from, GameScenes to)
        {
            ScatterLog.Log("OnSceneChanged: From " + from.ToString() + ", to " + to.ToString());
            if (to == GameScenes.FLIGHT)
            {
                OnBodyChanged("SceneChanged", FlightGlobals.ActiveVessel.mainBody.name);
            }
            else
            {
                OnBodyChanged("SceneChanged", "SceneChanged");  //No planets are active right now
            }
        }
        public bool stop = false;
        public WaitForSeconds framerate = new WaitForSeconds(1);
        WaitForSeconds rapidWait = new WaitForSeconds(0.0606f);

        public void Update()
        {
            if (HighLogic.LoadedScene != GameScenes.FLIGHT) { return; }
            if (!stop)
            {
                pc.Setup(Buffers.activeBuffers[parentName].buffer, Buffers.activeBuffers[parentName].farBuffer, Buffers.activeBuffers[parentName].furtherBuffer, scatter);
            }

        }
    }
    [RequireConfigType(ConfigType.Node)]
    public class SharedScatter : ModLoader<PQSMod_SharedScatter>
    {
        [ParserTarget("order", Optional = true)]
        public NumericParser<int> order
        {
            get { return Mod.order; }
            set { Mod.order = int.MaxValue - 2; }
        }
        [ParserTarget("scatterName", Optional = false)]
        public String scatterName
        {
            get { return Mod.scatterName; }
            set
            {
                Mod.scatterName = Mod.sphere.name + "-" + value;
            }
        }
        [ParserTarget("parentName", Optional = false)]
        public String parentName
        {
            get { return Mod.parentName; }
            set
            {
                Mod.parentName = Mod.sphere.name + "-" + value;
            }
        }
    }
}
