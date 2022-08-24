using ComputeLoader;
using ParallaxGrass;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;

namespace Grass
{
    [KSPAddon(KSPAddon.Startup.PSystemSpawn, true)]
    public class ScatterManagerPlus : MonoBehaviour
    {
        public static Dictionary<string, List<ScatterComponent>> scatterComponents = new Dictionary<string, List<ScatterComponent>>();  //Planet name, component
        public static Dictionary<string, List<SharedScatterComponent>> sharedScatterComponents = new Dictionary<string, List<SharedScatterComponent>>();
        public static Dictionary<string, GameObject> gameObjects = new Dictionary<string, GameObject>();                                //GO to hold scatter components
        public static ScatterManagerPlus Instance;
        public delegate void QuadRangeCheck();
        public static event QuadRangeCheck OnQuadRangeCheck;
        public delegate void QuadPhysicsCheck();
        public static event QuadPhysicsCheck OnQuadPhysicsCheck;

        public string[] keys;
        public bool suppressPlanetChange = false;
        void Awake()
        {
            GameObject.DontDestroyOnLoad(this);
            Instance = this;
        }
        void OnEnable()
        {
            BodySwitchManager.onBodyChange += OnPlanetChange;
            BodySwitchManager.onSceneChange += OnSceneChange;
            GameEvents.onVesselGoOffRails.Add(OnOffRails);
            GameEvents.onFlightReady.Add(OnFlightReady);
            GameEvents.OnFlightGlobalsReady.Add(FlightGlobalsReady);
        }
        void OnDisable()
        {
            BodySwitchManager.onBodyChange -= OnPlanetChange;
            BodySwitchManager.onSceneChange -= OnSceneChange;
            GameEvents.onVesselGoOffRails.Remove(OnOffRails);
            GameEvents.onFlightReady.Remove(OnFlightReady);
            GameEvents.OnFlightGlobalsReady.Remove(FlightGlobalsReady);
        }
        public void FlightGlobalsReady(bool ready)
        {
            if (!ready)
            {
                Debug.Log("Flight globals not ready - Setting active vessel at frame " + Time.frameCount);
            }
        }
        void Start()
        {
            keys = ScatterBodies.scatterBodies.Keys.ToArray();
            for (int i = 0; i < ScatterBodies.scatterBodies.Count; i++)
            {
                int scatterCount = ScatterBodies.scatterBodies[keys[i]].scatters.Count;
                List<ScatterComponent> planetComponents = new List<ScatterComponent>();
                List<SharedScatterComponent> sharedPlanetComponents = new List<SharedScatterComponent>();
                GameObject go = new GameObject(keys[i] + " (Scatter Manager)");
                GameObject.DontDestroyOnLoad(go);
                go.SetActive(false);
                for (int b = 0; b < scatterCount; b++)
                {
                    string[] scatterKeys = ScatterBodies.scatterBodies[keys[i]].scatters.Keys.ToArray();
                    Scatter scatter = ScatterBodies.scatterBodies[keys[i]].scatters[scatterKeys[b]];
                    if (!scatter.shared)
                    {
                        ScatterComponent comp = go.gameObject.AddComponent<ScatterComponent>();
                        comp.name = scatter.scatterName + " Manager";
                        comp.scatter = scatter;
                        comp.pc = go.AddComponent<PostCompute>();
                        comp.pc.scatterName = scatter.scatterName;
                        planetComponents.Add(comp);
                    }
                    else
                    {
                        SharedScatterComponent comp = go.AddComponent<SharedScatterComponent>();
                        comp.name = scatter.scatterName + " Shared Mananger";
                        comp.scatter = scatter;
                        comp.pc = go.AddComponent<PostCompute>();
                        comp.pc.scatterName = scatter.scatterName;
                        comp.parentName = scatter.planetName + "-" + scatter.sharedParent;
                        sharedPlanetComponents.Add(comp);
                    }
                }
                scatterComponents.Add(keys[i], planetComponents);
                gameObjects.Add(keys[i], go);
            }
        }
        public void FixedUpdate()   //Maybe make a coroutine to space out range checks :D
        {
            if (HighLogic.LoadedScene != GameScenes.FLIGHT) { return; }
            if (OnQuadRangeCheck != null)
            {
                OnQuadRangeCheck();
            }
            if (OnQuadPhysicsCheck != null && ScatterGlobalSettings.enableCollisions)
            {
                OnQuadPhysicsCheck();
            }
        }
        
        //public void Update()
        //{
        //    
        //}
        public void Restart()
        {
            foreach(KeyValuePair<string, GameObject> pair in gameObjects)
            {
                UnityEngine.Object.Destroy(pair.Value);
            }
            gameObjects.Clear();
            scatterComponents.Clear();
            sharedScatterComponents.Clear();
            Start();
            foreach (KeyValuePair<string, GameObject> pair in gameObjects)
            {
                if (pair.Key == FlightGlobals.currentMainBody.name)
                {
                    pair.Value.SetActive(true);
                }
                
            }
        }
        public void OnSceneChange(GameScenes from, GameScenes to)
        {
            Debug.Log("Scene change: " + from.ToString() + " -> " + to.ToString());
            StartCoroutine(WaitForDominantBody());
        }
        public IEnumerator WaitForDominantBody()    //wdym by that O_o
        {
            ScatterLog.Log("Scene change logged, awaiting planet change before starting scatters...");
            yield return new WaitUntil(() => FlightGlobals.currentMainBody != null);
            ScatterLog.Log("Planet found, starting scatters");
            string currentBody = FlightGlobals.currentMainBody.name;
            foreach (KeyValuePair<string, GameObject> go in gameObjects)
            {
                if (go.Key == currentBody)
                {
                    go.Value.SetActive(true);
                }
                else
                {
                    go.Value.SetActive(false);
                }
            }
        }
        public void OnPlanetChange(string from, string to)
        {
            
            Debug.Log("Planet change: " + from + " -> " + to);
            if (suppressPlanetChange)                           //Will be called again by the switch manager
            { 
                suppressPlanetChange = false;
                Debug.Log(" - Blocked by previous request");
                return; 
            } 
            foreach (KeyValuePair<string, GameObject> go in gameObjects)
            {
                if (go.Key == to)
                {
                    go.Value.SetActive(true);
                }
                else
                {
                    go.Value.SetActive(false);
                }
            }
        }
        public void RequestEarlyInitialization(string name)    //Called from harmony patched PQS to get the buffers created before the terrain builds, as KSP does not provide a method for this itself
        {
            foreach (KeyValuePair<string, List<ScatterComponent>> go in scatterComponents)
            {
                if (go.Key == name)
                {
                    foreach(ScatterComponent component in go.Value)
                    {
                        component.DestroyComputes();
                        component.CreateComputes(component.scatter.properties.scatterDistribution.noise.noiseMode, FlightGlobals.GetBodyByName(component.scatter.planetName).pqsController.maxLevel - component.scatter.properties.subdivisionSettings.minLevel + 1);
                        component.CreateBuffers();
                    }
                }
            }
        }
        public void OnFlightReady()
        {
            Debug.Log("Flight ready at frame " + Time.frameCount);
            foreach (KeyValuePair<string, List<ScatterComponent>> go in scatterComponents)
            {
                if (go.Key == FlightGlobals.currentMainBody.name)
                {
                    for (int i = 0; i < go.Value.Count; i++)
                    {
                        go.Value[i].CheckQueue();
                    }
                }
            }
            foreach (KeyValuePair<PQ, QuadData> data in PQSMod_ParallaxScatter.quadList)
            {
                foreach (KeyValuePair<Scatter, ScatterCompute> sc in data.Value.comps)
                {
                    sc.Value.Start();
                }
            }
            //foreach (KeyValuePair<string, GameObject> go in gameObjects)
            //{
            //    if (go.Key == FlightGlobals.currentMainBody.name && !go.Value.activeSelf)
            //    {
            //        go.Value.SetActive(true);
            //    }
            //    else if (go.Key != FlightGlobals.currentMainBody.name)
            //    {
            //        go.Value.SetActive(false);
            //    }
            //}
            //foreach (QuadData qd in PQSMod_ParallaxScatter.quadList.Values)             //So the reason for this is annoying. For all scatters, they are generated, evaluated and work properly
            //{                                                                           //But for SOME reason, SOME scatters regardless of their type won't show up on flight scene start
            //    foreach (ScatterCompute sc in qd.comps.Values)                          //So this forces them to regenerate at the start of the flight scene and they all show up
            //    {                                                                       //Idk, I spent hours and hours trying to work this out, it's a hacky but a working solution. If someone else
            //        if (sc.scatter.planetName == FlightGlobals.currentMainBody.name)    //Wants to help me out by investigating this (just remove the double foreach and see for yourself)
            //        {                                                                   //I will sell you my firstborn or something idk but yeah i cba to do more on this
            //            sc.Start();
            //        }
            //    }
            //}
        }
        public void OnOffRails(Vessel v)
        {
            if (v.Landed && !v.parts[0].isKerbalEVA() && (v.parts.Where((x) => x.name == "groundAnchor").Count() == 0))
            {
                Debug.Log("Vessel terrain height: " + v.heightFromTerrain);
                Debug.Log("manager off rails");
                Vector3d up = Vector3.Normalize(v.vesselTransform.position - v.mainBody.transform.position);
                v.SetPosition(v.vesselTransform.position + up * 0.1d); //Avoid spawning inside scatter colliders. It's a pretty dog fix but I've given up on this
                v.GetHeightFromTerrain();
                List<Part> parts = v.parts;
                for (int i = 0; i < parts.Count; i++)
                {
                }
                Debug.Log("New vessel terrain height: " + v.heightFromTerrain);
            }
        }
    }
    public class ScatterComponent : MonoBehaviour
    {
        public Scatter scatter;
        public int triCount = 131;

        public BufferList bufferList;

        public delegate void ForceEvaluate();
        public event ForceEvaluate OnForceEvaluate;
        public delegate void RangeCheck();          //Check if quad is close enough to subdivide
        public event RangeCheck OnRangeCheck;

        public PostCompute pc;
        public Coroutine co;

        public Queue<ScatterCompute> scatterQueue = new Queue<ScatterCompute>();    //Queue up quads to be processed by the distribute compute shaders
        public List<ComputeShader> computePool = new List<ComputeShader>();        //Pool of compute shaders that are currently awaiting instructions

        public int maxObjects = 0;  //Max amount of objects for this scatter - Massively contributes to memory usage
        public bool buffersCreated = false;
        public bool computesCreated = false;

        int requiredMemory = 0;

        public void OnEnable()
        {
            Debug.Log("Scatter enabled: " + scatter.scatterName);
            CreateBuffers();
            CreateComputes(scatter.properties.scatterDistribution.noise.noiseMode, FlightGlobals.GetBodyByName(scatter.planetName).pqsController.maxLevel - scatter.properties.subdivisionSettings.minLevel + 1);
            co = StartCoroutine(OnUpdate());
        }
        public void OnDisable()
        {
            Debug.Log("Scatter Disabled: " + scatter.scatterName);
            buffersCreated = false;
            StopCoroutine(co);
            DestroyBuffers();
            DestroyComputes();
        }
        public ComputeShader InstantiateNew(DistributionNoiseMode noiseMode)
        {
            if (noiseMode == DistributionNoiseMode.NonPersistent)
            {
                return GameObject.Instantiate(ScatterShaderHolder.GetCompute("DistributeNearest"));
            }
            if (noiseMode == DistributionNoiseMode.Persistent)
            {
                return GameObject.Instantiate(ScatterShaderHolder.GetCompute("DistributeFixed"));
            }
            if (noiseMode == DistributionNoiseMode.VerticalStack)
            {
                return GameObject.Instantiate(ScatterShaderHolder.GetCompute("DistFTH"));
            }
            Debug.Log("[Exception] Unrecognized DistributionNoiseMode");
            return null;
        }
        public void CreateComputes(DistributionNoiseMode noiseMode, int subdivisionDifference)
        {
            //When subdivision difference is high, it means the scatter covers a lot of quads
            //so when doing camera changes we want them generating quickly, which needs more computes
            //but also results in a slightly higher memory usage. Unlucky, L + cope
            if (computesCreated) { return; }
            computePool.Clear();
            
            if (noiseMode == DistributionNoiseMode.NonPersistent)
            {
                for (int i = 0; i < 2 * subdivisionDifference; i++)
                {
                    ComputeShader cs = GameObject.Instantiate(ScatterShaderHolder.GetCompute("DistributeNearest"));
                    DontDestroyOnLoad(cs);  //this doesn't work
                    computePool.Add(cs);
                   
                }
            }
            if (noiseMode == DistributionNoiseMode.Persistent)
            {
                for (int i = 0; i < 10 * subdivisionDifference; i++)
                {
                    ComputeShader cs = GameObject.Instantiate(ScatterShaderHolder.GetCompute("DistributeFixed"));
                    DontDestroyOnLoad(cs);  //this doesn't work
                    computePool.Add(cs);
                }
            }
            if (noiseMode == DistributionNoiseMode.VerticalStack)
            {
                for (int i = 0; i < 10 * subdivisionDifference; i++)
                {
                    ComputeShader cs = GameObject.Instantiate(ScatterShaderHolder.GetCompute("DistFTH"));
                    DontDestroyOnLoad(cs);  //this doesn't work
                    computePool.Add(cs);
                }
            }
            computesCreated = true;
            CheckQueue();
            
        }
        public void CheckQueue()    //There may be items waiting in the queue that were added by QuadData/ScatterCompute before the compute pool was initialized - Process them now
        {
            if (scatterQueue.Count > 0)
            {
                int max = Mathf.Min(computePool.Count, scatterQueue.Count);
                for (int i = 0; i < max; i++)
                {
                    if (scatterQueue.Count > 0) //Dequeue CAN happen inside QuadData if the GPU finishes early
                    {
                        ScatterCompute comp = scatterQueue.Dequeue();
                        comp.Start();
                    }
                    
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
            computesCreated = false;
        }
        public void DestroyBuffers()
        {
            if (Buffers.activeBuffers.ContainsKey(scatter.scatterName))
            {
                buffersCreated = false;
                Buffers.activeBuffers[scatter.scatterName].Dispose();
                Buffers.activeBuffers.Remove(scatter.scatterName);
            }
            
        }
        public void GetMaxMemory()
        {
            maxObjects = scatter.maxObjects;
            if (maxObjects == 0)
            {
                requiredMemory = triCount * (int)scatter.properties.scatterDistribution._PopulationMultiplier * 100 * scatter.properties.scatterDistribution.noise._MaxStacks;
            }
            else
            {
                requiredMemory = (int)((float)maxObjects * ScatterGlobalSettings.densityMult * ScatterGlobalSettings.rangeMult);
            }
            if (requiredMemory == 0) { ScatterLog.Log("[Exception] Attempting to create a 0-length buffer, setting length to 10"); requiredMemory = 10; }
        }
        public void CreateBuffers()
        {
            
            if (buffersCreated) { Debug.Log("Buffers already created externally"); return; }
            DestroyBuffers();
            GetMaxMemory();

            
            bufferList = new BufferList(requiredMemory, GrassData.Size(), Time.frameCount);
            Buffers.activeBuffers.Add(scatter.scatterName, bufferList);
            buffersCreated = true;
        }
        WaitForSeconds rapidWait = new WaitForSeconds(0.0606f);
        public IEnumerator OnUpdate()
        {
            while (true)
            {
                Buffers.activeBuffers[scatter.scatterName].SetCounterValue(0);
                if (scatter.properties.subdivisionSettings.mode == SubdivisionMode.NearestQuads && OnRangeCheck != null) { OnRangeCheck(); }
                if (OnForceEvaluate != null) { OnForceEvaluate(); }
                pc.Setup(Buffers.activeBuffers[scatter.scatterName].buffer, Buffers.activeBuffers[scatter.scatterName].farBuffer, Buffers.activeBuffers[scatter.scatterName].furtherBuffer, scatter);
                
                yield return rapidWait;
            }
        }
        
    }
    public class SharedScatterComponent : MonoBehaviour
    {
        public Scatter scatter;
        public string parentName;
        public PostCompute pc;
        WaitForSeconds rapidWait = new WaitForSeconds(0.0606f);
        Coroutine co;
        void OnEnable()
        {
            Debug.Log("Shared scatter enabled: " + scatter.scatterName);
            co = StartCoroutine(OnUpdate());
        }
        void OnDisable()
        {
            Debug.Log("Shared scatter disabled: " + scatter.scatterName);
            StopCoroutine(co);
        }
        public IEnumerator OnUpdate()
        {
            while (true)
            {
                pc.Setup(Buffers.activeBuffers[parentName].buffer, Buffers.activeBuffers[parentName].farBuffer, Buffers.activeBuffers[parentName].furtherBuffer, scatter);
                yield return rapidWait;
            }
        }
    }
}
