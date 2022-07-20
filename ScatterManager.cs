using ComputeLoader;
using ParallaxGrass;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Grass
{
    [KSPAddon(KSPAddon.Startup.PSystemSpawn, true)]
    public class ScatterManagerPlus : MonoBehaviour
    {
        public static Dictionary<string, List<ScatterComponent>> scatterComponents = new Dictionary<string, List<ScatterComponent>>();  //Planet name, component
        public static Dictionary<string, List<SharedScatterComponent>> sharedScatterComponents = new Dictionary<string, List<SharedScatterComponent>>();
        public static Dictionary<string, GameObject> gameObjects = new Dictionary<string, GameObject>();                                //GO to hold scatter components
        public static ScatterManagerPlus Instance;
        public string[] keys;
        void Awake()
        {
            GameObject.DontDestroyOnLoad(this);
            Instance = this;
        }
        void OnEnable()
        {
            BodySwitchManager.onBodyChange += OnPlanetChange;
            BodySwitchManager.onSceneChange += OnSceneChange;
        }
        void OnDisable()
        {
            BodySwitchManager.onBodyChange -= OnPlanetChange;
            BodySwitchManager.onSceneChange -= OnSceneChange;
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
                Debug.Log("Planet component name: " + keys[i]);
                scatterComponents.Add(keys[i], planetComponents);
                gameObjects.Add(keys[i], go);
            }
        }
        public void OnSceneChange(GameScenes from, GameScenes to)
        {
            Debug.Log("Scene change: " + from.ToString() + " -> " + to.ToString());
            string currentBody = FlightGlobals.currentMainBody != null ? FlightGlobals.currentMainBody.name : null;
            Debug.Log("CurrentBody: " + currentBody);
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

        int requiredMemory = 0;


        public void OnEnable()
        {
            Debug.Log("Scatter enabled: " + scatter.scatterName);
            CreateBuffers();
            CreateComputes(scatter.properties.scatterDistribution.noise.noiseMode, FlightGlobals.GetBodyByName(scatter.planetName).pqsController.maxLevel - scatter.properties.subdivisionSettings.minLevel + 1);
            co = StartCoroutine(OnUpdate());
            if (scatter.properties.subdivisionSettings.mode == SubdivisionMode.NearestQuads)
            {
                StartCoroutine(WaitForFlight());    //Wait until flight and regenerate. In some edge cases without this, the nearest quad scatters would disappear on scene change. No idea why. Yet
            }
        }
        public void OnDisable()
        {
            Debug.Log("Scatter Disabled: " + scatter.scatterName);
            StopCoroutine(co);
            DestroyBuffers();
            DestroyComputes();
        }
        public void CreateComputes(DistributionNoiseMode noiseMode, int subdivisionDifference)
        {
            //When subdivision difference is high, it means the scatter covers a lot of quads
            //so when doing camera changes we want them generating quickly, which needs more computes
            //but also results in a slightly higher memory usage. Unlucky, L + cope
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
        }
        public void DestroyBuffers()
        {
            if (Buffers.activeBuffers.ContainsKey(scatter.scatterName))
            {
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
            DestroyBuffers();
            GetMaxMemory();

            bufferList = new BufferList(requiredMemory, GrassData.Size());
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
        public IEnumerator WaitForFlight()  //Should only be used for NearestQuad scatters, then remove this method entirely when I fix body1 -> track -> body2 -> track -> body1 which makes them disappear
        {
            yield return new WaitUntil(() => FlightGlobals.ready);
            Debug.Log("FlightGlobals: Ready");
            foreach (KeyValuePair<PQ, QuadData> data in PQSMod_ParallaxScatter.quadList)
            {
                foreach (KeyValuePair<string, ScatterCompute> sc in data.Value.comps)
                {
                    if (sc.Value.active && sc.Value.scatter.scatterName == scatter.scatterName)
                    {
                        sc.Value.Start();
                    }

                }
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
