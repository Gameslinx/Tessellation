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
                originPoint = Camera.allCameras.FirstOrDefault(_cam => _cam.name == "Camera 00").gameObject.transform.position;
            }
        }
    }
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
            if (HighLogic.LoadedScene != GameScenes.FLIGHT ) { return; }
            if (FlightGlobals.currentMainBody.name != currentPlanet)
            {
                foreach (PQSMod_ScatterManager mod in mods)
                {
                    mod.OnBodyChanged(currentPlanet, FlightGlobals.currentMainBody.name);
                }
                currentPlanet = FlightGlobals.currentMainBody.name;
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
            buffer.SetCounterValue(counter);
            farBuffer.SetCounterValue(counter);
            furtherBuffer.SetCounterValue(counter);
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
        public delegate void ForceEvaluate();
        public event ForceEvaluate OnForceEvaluate;
        public delegate void BufferLengthUpdated();
        public event BufferLengthUpdated OnBufferLengthUpdated;

        public PostCompute pc;
        public Coroutine co;
        public override void OnSetup()
        {
            //GameEvents.onDominantBodyChange.Add(OnBodyChanged);
            Start();    //So we can update from UI without starting another coroutine :D
        }

        public void OnBodyChanged(string from, string to)
        {
            if (to != scatter.planetName)
            {
                stop = true;
                pc.active = false;
                //Buffers.activeBuffers[scatterName].Dispose();
                if (co != null) { StopCoroutine(co); }
                //if (bufferList != null) { bufferList.Dispose(); }
            }
            if (to == scatter.planetName)
            {
                stop = false;
                pc.active = true;
                Start();
                co = StartCoroutine(OnUpdate());
            }
            FloatingOrigin.TerrainShaderOffset = Vector3.zero;

        }
        public void Start()
        {
            if (FlightGlobals.currentMainBody == null) { return; }
            scatter = ScatterBodies.scatterBodies[FlightGlobals.currentMainBody.name].scatters[scatterName];
            requiredMemory = subdivisionLevel * triCount * (int)scatter.properties.scatterDistribution._PopulationMultiplier * quadCount * scatter.properties.scatterDistribution.noise._MaxStacks * 50;
            if (!ActiveBuffers.mods.Contains(this))
            {
                ActiveBuffers.mods.Add(this);
            }
            CreateBuffers();
            if (pc == null) { pc = FlightGlobals.currentMainBody.gameObject.AddComponent<PostCompute>(); pc.scatterName = scatterName; pc.planetName = scatter.planetName; }
            framerate = new WaitForSeconds(updateRate);
        }
        public void CreateBuffers()
        {
            if (Buffers.activeBuffers.ContainsKey(scatterName))
            {
                Buffers.activeBuffers[scatterName].Dispose();
                Buffers.activeBuffers.Remove(scatterName);
            }
            requiredMemory = subdivisionLevel * triCount * (int)scatter.properties.scatterDistribution._PopulationMultiplier * quadCount * scatter.properties.scatterDistribution.noise._MaxStacks;
            if (requiredMemory == 0) { ScatterLog.Log("[Exception] Attempting to create a 0-length buffer, setting length to 10"); requiredMemory = 10; }
            bufferList = new BufferList(requiredMemory * 2, ComputeLoader.ComputeComponent.GrassData.Size());
            Buffers.activeBuffers.Add(scatterName, bufferList);
        }
        public bool stop = false;
        public WaitForSeconds framerate = new WaitForSeconds(1);
        public IEnumerator OnUpdate()
        {

            //timeUpdated = Time.realtimeSinceStartup;
            while (true)
            {
                if (HighLogic.LoadedScene != GameScenes.FLIGHT && HighLogic.LoadedScene != GameScenes.SPACECENTER) { StopCoroutine(co); yield return null; }
                if (!stop)
                {
                    Buffers.activeBuffers[scatterName].SetCounterValue(0);
                    if (OnForceEvaluate != null) { OnForceEvaluate(); }
                    pc.Setup(new ComputeBuffer[] { Buffers.activeBuffers[scatterName].buffer, Buffers.activeBuffers[scatterName].farBuffer, Buffers.activeBuffers[scatterName].furtherBuffer }, scatter);
                }
                yield return framerate;
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
            set { Mod.scatterName = value; }
        }

        [ParserTarget("updateRate", Optional = false)]
        public NumericParser<float> updateRate
        {
            get { return Mod.updateRate; }
            set { Mod.updateRate = value; Mod.framerate = new WaitForSeconds(value); }
        }
    }
}