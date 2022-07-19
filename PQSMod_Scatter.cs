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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;

namespace Grass
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class GlobalPoint : MonoBehaviour
    {
        public static Vector3 originPoint = new Vector3(0f, 100f, 0f);
        void Start()
        {
            if (HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                PQSMod_ScatterDistribute.alreadySetupSpaceCenter = false;
            }
        }
        void Update()
        {
            if (HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                originPoint = FlightGlobals.ActiveVessel.transform.position;
            }
            else if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
            {
                originPoint = Vector3.zero;
            }
        }
    }
    public static class Buffers
    {
        public static Dictionary<string, BufferList> activeBuffers = new Dictionary<string, BufferList>();
    }
    [KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
    public class ActiveBuffers : MonoBehaviour 
    {
        public static string currentPlanet = "";
        //String is scattername, we can retrieve these from the Compute.cs
        public static List<PQSMod_ScatterManager> mods = new List<PQSMod_ScatterManager>();
        public static Vector3 cameraPos = Vector3.zero;
        public static Vector3 surfacePos = Vector3.zero;
        public static float[] planeNormals;
        public bool stopped = false;
        public CameraManager.CameraMode cameraMode;
        
        void Update()       //Might be worth changing this to an event in the future. If this is still here on release, recommend messaging me about it
        {
            if (HighLogic.LoadedScene != GameScenes.FLIGHT && !stopped)    //Stop coroutine otherwise they will double up lol
            {
                stopped = true;
                return; 
            }
            else if (HighLogic.LoadedScene == GameScenes.FLIGHT) { stopped = false; }
            if (stopped) { return; }
            if (CameraManager.Instance != null && CameraManager.Instance.currentCameraMode != cameraMode)
            {
                ScatterLog.Log("Camera mode changed! Regenerating scatters on " + currentPlanet);   //ShaderOffset changes when camera mode changes
                foreach (KeyValuePair<PQ, QuadData> data in PQSMod_ParallaxScatter.quadList)
                {
                    foreach (KeyValuePair<string, ScatterCompute> scatter in data.Value.comps)
                    {
                        //Debug.Log("Calling start on the following scatter: " + scatter.Value.scatter.scatterName + ", which is active? " + scatter.Value.active);
                        if (scatter.Value.active)
                        {
                            scatter.Value.Start();
                        }
                        
                    }
                }
                cameraMode = CameraManager.Instance.currentCameraMode;
            }
            Camera cam = Camera.allCameras.FirstOrDefault(_cam => _cam.name == "Camera 00");
            if (cam == null) { return; }
            cameraPos = cam.gameObject.transform.position;

            ConstructFrustumPlanes(Camera.main, out planeNormals);

            
        }
        //void FixedUpdate()
        //{
        //    RaycastHit hit;
        //    if (Physics.Raycast(cameraPos, -Vector3.Normalize(cameraPos - FlightGlobals.currentMainBody.transform.position), out hit, 5000.0f, 1 << 15))
        //    {
        //        surfacePos = hit.point;
        //    }
        //}
        private void ConstructFrustumPlanes(Camera camera, out float[] planeNormals)
        {
            const int floatPerNormal = 4;

            // https://docs.unity3d.com/ScriptReference/GeometryUtility.CalculateFrustumPlanes.html
            // Ordering: [0] = Left, [1] = Right, [2] = Down, [3] = Up, [4] = Near, [5] = Far
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);

            planeNormals = new float[planes.Length * floatPerNormal];
            for (int i = 0; i < planes.Length; ++i)
            {
                planeNormals[i * floatPerNormal + 0] = planes[i].normal.x;
                planeNormals[i * floatPerNormal + 1] = planes[i].normal.y;
                planeNormals[i * floatPerNormal + 2] = planes[i].normal.z;
                planeNormals[i * floatPerNormal + 3] = planes[i].distance;
            }
        }
    }
    public class BufferList //Holds the buffers for one scatter
    {

        public BufferList(int memory, int stride)
        {
            Dispose();
            buffer = new ComputeBuffer(memory, stride, ComputeBufferType.Append);
            farBuffer = new ComputeBuffer(memory, stride, ComputeBufferType.Append);
            furtherBuffer = new ComputeBuffer(memory, stride, ComputeBufferType.Append);
        }
        public ComputeBuffer buffer;
        public ComputeBuffer farBuffer;
        public ComputeBuffer furtherBuffer;
        public void SetCounterValue(uint counter)
        {
            if (buffer != null) { buffer.SetCounterValue(counter); }
            if (farBuffer != null) { farBuffer.SetCounterValue(counter); }
            if (furtherBuffer != null) { furtherBuffer.SetCounterValue(counter); }
        }
        public void Release()
        {
            if (buffer != null) { buffer.Release(); }
            if (farBuffer != null) { farBuffer.Release(); }
            if (furtherBuffer != null) { furtherBuffer.Release(); }
        }
        public void Dispose()
        {
            if (farBuffer != null) { farBuffer.Dispose(); }
            if (furtherBuffer != null) { furtherBuffer.Dispose(); }
            if (buffer != null) { buffer.Dispose(); }
            buffer = null;
            farBuffer = null;
            furtherBuffer = null;
        }
        public float GetMemoryInMB()
        {
            int mem1 = buffer.count * buffer.stride;
            int mem2 = farBuffer.count * farBuffer.stride;
            int mem3 = furtherBuffer.count * furtherBuffer.stride;
            float total = mem1 + mem2 + mem3;
            return total / (1024 * 1024);
        }
        public int GetObjectCount()
        {
            if (buffer == null || farBuffer == null || furtherBuffer == null)
            {
                return 0;  //The buffers are null
            }
            int[] data = new int[3];
            ComputeBuffer countBuffer = new ComputeBuffer(3, sizeof(int), ComputeBufferType.IndirectArguments);
            ComputeBuffer.CopyCount(buffer, countBuffer, 0);
            ComputeBuffer.CopyCount(farBuffer, countBuffer, 4);
            ComputeBuffer.CopyCount(furtherBuffer, countBuffer, 8);
            countBuffer.GetData(data);
            int count = data[0] + data[1] + data[2];
            countBuffer.Dispose();
            return count;
        }
        public int GetCapacity()
        {
            if (buffer == null || farBuffer == null || furtherBuffer == null)
            {
                return 0;  //The buffers are null
            }
            return buffer.count + farBuffer.count + furtherBuffer.count;
        }
    }
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
            if (!ActiveBuffers.mods.Contains(this)) { ActiveBuffers.mods.Add(this); }   //Add this PQSMod to the active buffer list so ScatterCompute can grab it
            CreateComputes(scatter.properties.scatterDistribution.noise.noiseMode, sphere.maxLevel - scatter.properties.subdivisionSettings.minLevel + 1);
            if (pc == null) { pc = FlightGlobals.GetBodyByName(sphere.name).gameObject.AddComponent<PostCompute>(); pc.scatterName = scatterName; pc.planetName = scatter.planetName; }
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
            Debug.Log("Processing body change in " + name + " for " + scatter.scatterName);
            Debug.Log("From: " + from + " to: " + to);
            FloatingOrigin.TerrainShaderOffset = Vector3.zero;
            
            if (to != scatter.planetName)
            {
                stop = true;
                pc.active = false;
                pc.setupInitial = false;    //Force setup again on body switch
                if (co != null) { StopCoroutine(co); }

                if (buffersCreated)
                {
                    bufferList.Dispose();
                    bufferList.SetCounterValue(0);
                    Buffers.activeBuffers[scatterName].Dispose();
                    Buffers.activeBuffers[scatterName].SetCounterValue(0);
                }
            }
            if (to == scatter.planetName)
            {
                stop = false;
                pc.active = true;
                CreateBuffers();
                Debug.Log("New body by name: " + to);
                Debug.Log("Successful body change for: " + to + " - " + FlightGlobals.GetBodyByName(to).name);
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
        }
        public void CreateBuffers()
        {
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