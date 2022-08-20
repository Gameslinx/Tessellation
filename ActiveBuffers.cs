using ParallaxGrass;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Grass
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class GlobalPoint : MonoBehaviour
    {
        public static Vector3 originPoint = new Vector3(0f, 0f, 0f);
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
                if (FlightGlobals.ActiveVessel != null)
                {
                    originPoint = FlightGlobals.ActiveVessel.transform.position;
                }
            }
            else if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
            {
                originPoint = Vector3.zero;
            }
        }
    }
    [KSPAddon(KSPAddon.Startup.PSystemSpawn, true)]
    public class ActiveBuffers : MonoBehaviour 
    {
        public static string currentPlanet = "";
        //String is scattername, we can retrieve these from the Compute.cs
        //public static List<ScatterComponent> mods = new List<ScatterComponent>();
        public static Vector3 cameraPos = Vector3.zero;
        public static Vector3 surfacePos = Vector3.zero;
        public static float[] planeNormals;
        public bool stopped = false;
        public CameraManager.CameraMode cameraMode;
        void Awake()
        {
            GameObject.DontDestroyOnLoad(this);
        }
        
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
                    foreach (KeyValuePair<Scatter, ScatterCompute> scatter in data.Value.comps)
                    {
                        //Debug.Log("Calling start on the following scatter: " + scatter.Value.scatter.scatterName + ", which is active? " + scatter.Value.active);
                        scatter.Value.Start();
                    }
                }
                cameraMode = CameraManager.Instance.currentCameraMode;
            }
            Camera cam = Camera.allCameras.FirstOrDefault(_cam => _cam.name == "Camera 00");
            if (cam == null) { return; }
            cameraPos = cam.gameObject.transform.position;

            ConstructFrustumPlanes(Camera.main, out planeNormals);
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
    public static class Buffers
    {
        public static Dictionary<string, BufferList> activeBuffers = new Dictionary<string, BufferList>();
    }
    public class BufferList //Holds the buffers for one scatter
    {
        public int frameCountCreated = 0;
        public BufferList(int memory, int stride, int frameCount)
        {
            Dispose();
            buffer = new ComputeBuffer(memory, stride, ComputeBufferType.Append);
            farBuffer = new ComputeBuffer(memory, stride, ComputeBufferType.Append);
            furtherBuffer = new ComputeBuffer(memory, stride, ComputeBufferType.Append);
            frameCountCreated = frameCount;
            Debug.Log("DEBUG INTERNAL: CREATE BUFFERS");
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
            Debug.Log("DEBUG INTERNAL: DISPOSE BUFFERS");
        }
        public float GetMemoryInMB()
        {
            int mem1 = buffer.count * buffer.stride;
            int mem2 = farBuffer.count * farBuffer.stride;
            int mem3 = furtherBuffer.count * furtherBuffer.stride;
            float total = mem1 + mem2 + mem3;
            Debug.Log(" - Compute Buffer: NearBuffer: Count - " + buffer.count + ", Stride - " + buffer.stride + " (" + ((float)(buffer.count * buffer.stride) / (1024f * 1024f)) + " MB)");
            Debug.Log(" - Compute Buffer: NearBuffer: Count - " + farBuffer.count + ", Stride - " + farBuffer.stride + " (" + ((float)(farBuffer.count * farBuffer.stride) / (1024f * 1024f)) + " MB)");
            Debug.Log(" - Compute Buffer: NearBuffer: Count - " + furtherBuffer.count + ", Stride - " + furtherBuffer.stride + " (" + ((float)(furtherBuffer.count * furtherBuffer.stride) / (1024f * 1024f)) + " MB)");
            return total / (1024f * 1024f);
        }
        public int GetObjectCount()
        {
            if (buffer == null || farBuffer == null || furtherBuffer == null)
            {
                return 0;
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
                return 0;
            }
            return buffer.count + farBuffer.count + furtherBuffer.count;
        }
    }
}