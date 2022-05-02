using ComputeLoader;
using Kopernicus.ConfigParser.Attributes;
using Kopernicus.ConfigParser.BuiltinTypeParsers;
using Kopernicus.ConfigParser.Enumerations;
using Kopernicus.Configuration.ModLoader;
using LibNoise;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Grass
{

    public static class Buffers
    {
        public static Dictionary<string, BufferList> activeBuffers = new Dictionary<string, BufferList>();
    }
    [KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
    public class Resetter : MonoBehaviour
    {
        public void Start()
        {
            ActiveBuffers.currentPlanet = "";
        }
    }
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class ActiveBuffers : MonoBehaviour
    {
        public static string currentPlanet = "";
          //String is scattername, we can retrieve these from the Compute.cs
        public static List<PQSMod_ScatterManager> mods = new List<PQSMod_ScatterManager>();
        public static Vector3 cameraPos = Vector3.zero;
        public static Vector3 surfacePos = Vector3.zero;
        public static float[] planeNormals;
        void Update()
        {
            Camera cam = Camera.allCameras.FirstOrDefault(_cam => _cam.name == "Camera 00");
            cameraPos = cam.gameObject.transform.position;

            ConstructFrustumPlanes(Camera.main, out planeNormals);

            if (FlightGlobals.currentMainBody.name != currentPlanet)
            {
                Debug.Log("Current main body changed from " + currentPlanet + " to " + FlightGlobals.currentMainBody.name);
                foreach (PQSMod_ScatterManager mod in mods)
                {
                    mod.OnBodyChanged(currentPlanet, FlightGlobals.currentMainBody.name);
                }
                currentPlanet = FlightGlobals.currentMainBody.name;

                //Debug.Log("Cleared active buffers as main body changed");
                //activeBuffers[currentPlanet].Dispose();
                //activeBuffers.Clear();
                //StopCoroutine(mods.Find(x => x.name == currentPlanet).OnUpdate());
                //currentPlanet = FlightGlobals.currentMainBody.name;

            }
        }
        void FixedUpdate()
        {
            RaycastHit hit;
            if (Physics.Raycast(cameraPos, -Vector3.Normalize(cameraPos - FlightGlobals.currentMainBody.transform.position), out hit, 5000.0f, 1 << 15))
            {
                surfacePos = hit.point;
            }
        }
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
            mergeBuffer = new ComputeBuffer(memory, stride, ComputeBufferType.Append);
            buffer = new ComputeBuffer(memory, stride, ComputeBufferType.Append);
            farBuffer = new ComputeBuffer(memory, stride, ComputeBufferType.Append);
            furtherBuffer = new ComputeBuffer(memory, stride, ComputeBufferType.Append);
        }
        public ComputeBuffer mergeBuffer;

        public ComputeBuffer buffer;
        public ComputeBuffer farBuffer;
        public ComputeBuffer furtherBuffer;
        public void SetCounterValue(uint counter)
        {
            buffer.SetCounterValue(counter);
            farBuffer.SetCounterValue(counter);
            furtherBuffer.SetCounterValue(counter);
        }
        public void SetMergeCounterValue(uint counter)
        {
            mergeBuffer.SetCounterValue(counter);
        }
        public void Release()
        {
            Debug.Log("Releasing buffers");
            if (mergeBuffer != null) { mergeBuffer.Release(); }
            if (buffer != null) { buffer.Release(); }
            if (farBuffer != null) { farBuffer.Release(); }
            if (furtherBuffer != null) { furtherBuffer.Release(); }
        }
        public void Dispose()
        {
            Debug.Log("Disposing buffers");
            if (mergeBuffer != null) { mergeBuffer.Dispose(); }
            if (buffer != null) { buffer.Dispose(); }
            if (farBuffer != null) { farBuffer.Dispose(); }
            if (furtherBuffer != null) { furtherBuffer.Dispose(); }
            mergeBuffer = null;
            buffer = null;
            farBuffer = null;
            furtherBuffer = null;
        }
    }
    public class PQSMod_ScatterManager : PQSMod
    {
        public string scatterName = "a";
        public int subdivisionLevel = 1;
        
        public int triCount = 392;
        public int quadCount = 100;

        public float updateRate = 1.0f;

        public int requiredMemory = 100;

        public BufferList bufferList;
        public Scatter scatter;

        float timeUpdated = 0;

        public delegate void ForceMerge();
        public event ForceMerge OnForceMerge;
        public delegate void BufferLengthUpdated();
        public event BufferLengthUpdated OnBufferLengthUpdated;

        public delegate void ForceEvaluate();
        public event ForceEvaluate OnForceEvaluate;
        public delegate void EvaluateBufferLengthUpdated();
        public event EvaluateBufferLengthUpdated OnEvaluateBufferLengthUpdated;

        public PostCompute pc;
        public Evaluate ev;
        public Coroutine co;

        public int objectCount = 0;
        public override void OnSetup()
        {
            //GameEvents.onDominantBodyChange.Add(OnBodyChanged);
            Start();    //So we can update from UI without starting another coroutine :D
        }

        public void OnBodyChanged(string from, string to)
        {
            Debug.Log("Body change: From " + from + " to " + to);
            if (to != scatter.planetName) 
            { 
                stop = true; 
                pc.active = false;
                ev.active = false;
                //Buffers.activeBuffers[scatterName].Dispose();
                if (co != null) { StopCoroutine(OnUpdate()); } 
                //if (bufferList != null) { bufferList.Dispose(); }
            }
            if (to == scatter.planetName) 
            {
                Debug.Log("Starting scatters for " + scatter.planetName);
                stop = false;
                pc.active = true;
                ev.active = true;
                Start();
                co = StartCoroutine(OnUpdate()); 
            }
            FloatingOrigin.TerrainShaderOffset = Vector3.zero;
            
            //We need to start if the planet becomes the dominant body again


            //if (data.to.name == scatter.planetName) { if (co != null) { co = StartCoroutine(OnUpdate()); } }
            //Debug.Log("Dominant body changed!");
            //Debug.Log("From: " + data.from.bodyName + ". To: " + data.to.bodyName);
            //if (co != null) { StopCoroutine(co); }
            //if (pc != null) { Destroy(pc); }

        }
        public void Start()
        {
            if (FlightGlobals.currentMainBody == null) { return; }
            Debug.Log("Name is: " + scatterName);
            Debug.Log("SubdivisionLevel is: " + subdivisionLevel);
            scatter = ScatterBodies.scatterBodies[FlightGlobals.currentMainBody.name].scatters[scatterName];
            requiredMemory = subdivisionLevel * triCount * (int)scatter.properties.scatterDistribution._PopulationMultiplier * quadCount * scatter.properties.scatterDistribution.noise._MaxStacks * 50;
            //if (!ActiveBuffers.activeBuffers.ContainsKey(scatterName))
            //{
            //    ActiveBuffers.activeBuffers.Add(scatterName, bufferList);
            //}
            if (!ActiveBuffers.mods.Contains(this))
            {
                ActiveBuffers.mods.Add(this);
            }
            //pc = new PostCompute();
            CreateBuffers();
            if (pc == null) { pc = FlightGlobals.currentMainBody.gameObject.AddComponent<PostCompute>(); pc.scatterName = scatterName; pc.planetName = scatter.planetName; }
            if (ev == null) { ev = FlightGlobals.currentMainBody.gameObject.AddComponent<Evaluate>(); ev.scatter = scatter; ev.planetName = scatter.planetName;}
            //if (co != null) { StopCoroutine(co); }
            //co = StartCoroutine(OnUpdate());
        }
        public void CreateBuffers()
        {
            Debug.Log("Creating buffers");
            if (Buffers.activeBuffers.ContainsKey(scatterName))
            {
                Buffers.activeBuffers[scatterName].Dispose();
                Buffers.activeBuffers.Remove(scatterName);
            }
            requiredMemory = subdivisionLevel * triCount * (int)scatter.properties.scatterDistribution._PopulationMultiplier * quadCount * scatter.properties.scatterDistribution.noise._MaxStacks;
            if (requiredMemory == 0) { ScatterLog.Log("[Exception] Attempting to create a 0-length buffer, setting length to 10"); requiredMemory = 10; }
            ScatterLog.Log("Requiring: " + ((float)requiredMemory * (float)ComputeComponent.GrassData.Size() / (1024f*1024f)).ToString("F4") + "Mb for " + scatterName);

            bufferList = new BufferList(requiredMemory, ComputeLoader.ComputeComponent.GrassData.Size());
            Buffers.activeBuffers.Add(scatterName, bufferList);
        }
        public bool stop = false;
        private int previousMaxMemory = -1;
        public IEnumerator OnUpdate()
        {

            //timeUpdated = Time.realtimeSinceStartup;
            //while (true)
            //{
            //    if (HighLogic.LoadedScene != GameScenes.FLIGHT) { yield return null; }
            //    //if (previousMaxMemory != requiredMemory) { previousMaxMemory = requiredMemory; CreateBuffers(); }
            //    if (Time.realtimeSinceStartup - timeUpdated > (updateRate * (1 / ScatterGlobalSettings.updateMult)) && !stop)
            //    {
            //        //if (OnEvaluateBufferLengthUpdated != null) { OnEvaluateBufferLengthUpdated(); } //Issue lies here
            //        Buffers.activeBuffers[scatterName].SetCounterValue(0);
            //        if (OnForceEvaluate != null) { OnForceEvaluate();  }
            //        pc.Setup(new ComputeBuffer[] { Buffers.activeBuffers[scatterName].buffer, Buffers.activeBuffers[scatterName].farBuffer, Buffers.activeBuffers[scatterName].furtherBuffer }, scatter);
            //        
            //        //Make this evaluate instead
            //        
            //        timeUpdated = Time.realtimeSinceStartup;
            //    }
            //    yield return new WaitForEndOfFrame();
            //}
            yield return null;
        }
        public void MergePoints()   //We need to force a merge on every quad each time a generate is completed
        {
            if (OnEvaluateBufferLengthUpdated != null) { OnEvaluateBufferLengthUpdated(); }
            if (OnBufferLengthUpdated != null) { OnBufferLengthUpdated(); }
            Buffers.activeBuffers[scatterName].SetMergeCounterValue(0);
            if (OnForceMerge != null) { OnForceMerge(); }
            //Should probably re-evaluate here as well
        }
        
        public override void OnSphereInactive()
        {
            Debug.Log("Sphere inactive");

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
            set { Mod.scatterName = value; }
        }
        
        [ParserTarget("updateRate", Optional = false)]
        public NumericParser<float> updateRate
        {
            get { return Mod.updateRate; }
            set { Mod.updateRate = value; }
        }
    }
}
