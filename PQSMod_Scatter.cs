using ComputeLoader;
using Kopernicus.ConfigParser.Attributes;
using Kopernicus.ConfigParser.BuiltinTypeParsers;
using Kopernicus.ConfigParser.Enumerations;
using Kopernicus.Configuration.ModLoader;
using LibNoise;
using ParallaxGrass;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;

namespace Grass
{
    
    
    public class PQSMod_ScatterManager : PQSMod
    {
        public string scatterName = "a";
        public int subdivisionLevel = 1;

        public int triCount = 131;
        public int quadCount = 100;

        public float updateRate = 1.0f;

        public int requiredMemory = 100;

        public BufferList bufferList;
        public Scatter scatter;

        public delegate void ForceEvaluate();
        public event ForceEvaluate OnForceEvaluate;
        public delegate void RangeCheck();          //Check if quad is close enough to subdivide
        public event RangeCheck OnRangeCheck;   

        public PostCompute pc;
        public Coroutine co;

        bool eventAlreadyAdded = false; //OnEnable is broken. OnSetup is called more than once. I want to die

        public Queue<ScatterCompute> scatterQueue = new Queue<ScatterCompute>();    //Queue up quads to be processed by the distribute compute shaders
        public List<ComputeShader> computePool = new List<ComputeShader>();        //Pool of compute shaders that are currently awaiting instructions

        public int maxObjects = 0;  //Max amount of objects for this scatter - Massively contributes to memory usage
        public bool buffersCreated = false;

        public void Awake()
        {

            Kopernicus.Events.OnPostBodyFixing.Add(InitialSetup);
        }
        public void InitialSetup(CelestialBody body)
        {
            if (eventAlreadyAdded) { return; }                                          //Don't want event added multiple times
            if (body.name != sphere.name) { return; }                                   //Body that has just been added is not this one
            ScatterLog.Log("Initial setup for " + scatterName + " manager");
            scatter = ScatterBodies.scatterBodies[sphere.name].scatters[scatterName];
            BodySwitchManager.onBodyChange += OnBodyChanged;
            BodySwitchManager.onSceneChange += OnSceneChanged;
            //if (!ActiveBuffers.mods.Contains(this)) { ActiveBuffers.mods.Add(this); }   //Add this PQSMod to the active buffer list so ScatterCompute can grab it
            
            
            framerate = new WaitForSeconds(updateRate * ScatterGlobalSettings.updateMult);
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
            FloatingOrigin.TerrainShaderOffset = Vector3.zero;
            
            if (to != scatter.planetName)
            {
                stop = true;
                if (co != null) { StopCoroutine(co); }
                if (pc != null) { Destroy(pc); }
                if (buffersCreated && Buffers.activeBuffers.ContainsKey(scatterName))
                {
                    bufferList.Dispose();
                    bufferList.SetCounterValue(0);
                    Buffers.activeBuffers[scatterName].Dispose();
                    Buffers.activeBuffers[scatterName].SetCounterValue(0);
                    Buffers.activeBuffers.Remove(scatterName);
                    buffersCreated = false;
                }
                DestroyComputes();
            }
            if (to == scatter.planetName)
            {
                if (pc == null) { pc = FlightGlobals.GetBodyByName(sphere.name).gameObject.AddComponent<PostCompute>(); pc.scatterName = scatterName; pc.planetName = scatter.planetName; }
                stop = false;
                pc.active = true;
                CreateComputes(scatter.properties.scatterDistribution.noise.noiseMode, sphere.maxLevel - scatter.properties.subdivisionSettings.minLevel + 1);
                CreateBuffers();
                if (co != null) { StopCoroutine(co); }
                co = StartCoroutine(OnUpdate());
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
        public void StopRoutine()
        {
            StopCoroutine(co);
        }
        
        public void CreateComputes(DistributionNoiseMode noiseMode, int subdivisionDifference)
        {
            //When subdivision difference is high, it means the scatter covers a lot of quads
            //so when doing camera changes we want them generating quickly, which needs more computes
            //but also results in a slightly higher memory usage. Unlucky
            computePool.Clear();
            if (noiseMode == DistributionNoiseMode.NonPersistent)
            {
                for (int i = 0; i < 10 * subdivisionDifference; i++)
                {
                    computePool.Add(GameObject.Instantiate(ScatterShaderHolder.GetCompute("DistributeNearest")));
                }
            }
            if (noiseMode == DistributionNoiseMode.Persistent)
            {
                for (int i = 0; i < 10 * subdivisionDifference; i++)
                {
                    computePool.Add(GameObject.Instantiate(ScatterShaderHolder.GetCompute("DistributeFixed")));
                }
            }
            if (noiseMode == DistributionNoiseMode.VerticalStack)
            {
                for (int i = 0; i < 10 * subdivisionDifference; i++)
                {
                    computePool.Add(GameObject.Instantiate(ScatterShaderHolder.GetCompute("DistFTH")));
                }
            }
            CheckQueue();
        }
        public void CheckQueue()    //There may be items waiting in the queue that were added by QuadData/ScatterCompute before the compute pool was initialized - Process them now
        {
            if (scatterQueue.Count > 0)
            {
                for (int i = 0; i < computePool.Count; i++)
                {
                    ScatterCompute comp = scatterQueue.Dequeue();
                    comp.Start();
                }
            }
        }
        public void DestroyComputes()
        {
            for (int i = 0; i < computePool.Count; i++)
            {
                Destroy(computePool[i]);
            }
            computePool.Clear();
        }
        public void CreateBuffers()
        {
            Debug.Log("Create buffers: " + scatterName);
            if (Buffers.activeBuffers.ContainsKey(scatterName))
            {
                Buffers.activeBuffers[scatterName].Dispose();
                Buffers.activeBuffers.Remove(scatterName);
            }
            if (maxObjects == 0)
            {
                requiredMemory = subdivisionLevel * triCount * (int)scatter.properties.scatterDistribution._PopulationMultiplier * quadCount * scatter.properties.scatterDistribution.noise._MaxStacks;
            }
            else
            {
                requiredMemory = (int)((float)maxObjects * ScatterGlobalSettings.densityMult * ScatterGlobalSettings.rangeMult);
            }
            if (requiredMemory == 0) { ScatterLog.Log("[Exception] Attempting to create a 0-length buffer, setting length to 10"); requiredMemory = 10; }

            bufferList = new BufferList(requiredMemory, GrassData.Size());
            Buffers.activeBuffers.Add(scatterName, bufferList);
            buffersCreated = true;
        }
        public bool stop = false;
        public WaitForSeconds framerate = new WaitForSeconds(1);
        WaitForSeconds rapidWait = new WaitForSeconds(0.0606f);
        public IEnumerator OnUpdate()
        {
            while (true)
            {
                if (HighLogic.LoadedScene != GameScenes.FLIGHT) { if (co != null) { StopCoroutine(co); } yield return null; }
                if (!stop)
                {
                    //Debug.Log("Memory usage of evaluate destination buffer for " + scatterName + ": " + Buffers.activeBuffers[scatterName].GetMemoryInMB());
                    Buffers.activeBuffers[scatterName].SetCounterValue(0);
                    if (scatter.properties.subdivisionSettings.mode == SubdivisionMode.NearestQuads && OnRangeCheck != null) { OnRangeCheck(); }
                    if (OnForceEvaluate != null) { OnForceEvaluate(); }
                    pc.Setup(Buffers.activeBuffers[scatterName].buffer, Buffers.activeBuffers[scatterName].farBuffer, Buffers.activeBuffers[scatterName].furtherBuffer, scatter);
                }
                yield return rapidWait;
            }
        }
    }
    [RequireConfigType(ConfigType.Node)]
    public class ScatterManager : ModLoader<PQSMod_ScatterManager>
    {
        [ParserTarget("order", Optional = true)]
        public NumericParser<int> order
        {
            get { return Mod.order; }
            set { Mod.order = int.MaxValue - 2; }
        }
        [ParserTarget("subdivisionLevel", Optional = false)]
        public NumericParser<int> subdivisionLevel
        {
            get { return Mod.subdivisionLevel; }
            set { Mod.subdivisionLevel = value; }
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

        [ParserTarget("updateRate", Optional = false)]
        public NumericParser<float> updateRate
        {
            get { return Mod.updateRate; }
            set { Mod.updateRate = value; Mod.framerate = new WaitForSeconds(value); }
        }

        [ParserTarget("maxObjects", Optional = true)]
        public NumericParser<float> maxObjects
        {
            get { return Mod.maxObjects; }
            set { Mod.maxObjects = (int)value; }
        }
    }
}