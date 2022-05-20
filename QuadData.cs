using Grass;
using ScatterConfiguratorUtils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace ParallaxGrass
{
    struct PositionData
    {
        public Vector3 pos;
        public Matrix4x4 mat;
        public Vector4 color;
        public float timeUpdated;
        public static int Size()
        {
            return
                sizeof(float) * 4 * 4 + // matrix;
                sizeof(float) * 3 + // pos
                sizeof(float) * 4 + // color
                sizeof(float);
        }
    }
    public struct GrassData
    {
        public Matrix4x4 mat;
        public Vector4 color;
        public static int Size()
        {
            return
                sizeof(float) * 4 * 4 + // matrix;
                sizeof(float) * 4;     // color
        }
    }
    public class QuadData
    {
        public PQ quad;
        public Mesh mesh;
        public Dictionary<string, ScatterCompute> comps = new Dictionary<string, ScatterCompute>();

        public QuadData(PQ pq)
        {
            quad = pq;
            mesh = quad.mesh;
            InitializeScatters();
        }
        public void InitializeScatters()    //Setup fixed and nearest scatters and assign them ScatterComputes
        {
            Scatter[] scatters = ScatterBodies.scatterBodies[quad.sphereRoot.name].scatters.Values.ToArray();
            for (int i = 0; i < scatters.Length; i++)
            {
                PQSMod_ScatterManager manager = ActiveBuffers.mods.Find(x => x.scatterName == scatters[i].scatterName);
                ScatterCompute comp = new ScatterCompute(scatters[i], quad, mesh, manager);
                comps.Add(scatters[i].scatterName, comp);
            }
        }
        public void Cleanup()   //Called when the quad is destroyed. Purge anything memory intensive, because this won't be destroyed for a while
        {
            foreach (ScatterCompute scatter in comps.Values)
            {
                scatter.Cleanup();
            }
        }
    }
    public class ScatterCompute : MonoBehaviour
    {
        public Scatter scatter;
        public Properties properties;
        public PQ quad;
        public Mesh mesh;
        public PQSMod_ScatterManager pqsMod;

        ComputeShader distribute;
        ComputeShader evaluate;

        int distributeIndex;
        int evaluateIndex;

        public ComputeBuffer vertexBuffer;
        public ComputeBuffer normalBuffer;
        public ComputeBuffer triBuffer;
        public ComputeBuffer noiseBuffer;
        public ComputeBuffer positionBuffer;

        public ComputeBuffer distributeCountBuffer;
        public ComputeBuffer indirectArgs;

        int memoryUsage = 0;
        int quadSubdivDifference = 1;
        int triCount = 1;
        float[] distributionNoise;

        bool currentlyReadingDist = true;
        bool initializedGenerate = false;
        bool initializedEvaluate = false;
        int objectCount = 0;

        public ScatterCompute(Scatter scatter, PQ quad, Mesh mesh, PQSMod_ScatterManager pqsMod)
        {
            this.scatter = scatter;
            this.quad = quad;
            this.mesh = mesh;
            this.pqsMod = pqsMod;
            Start();
        }
        public void Start()  //Subscribe events here. Determine subdivision level, if the quad needs subdividing, etc
        {
            pqsMod.OnForceEvaluate += EvaluatePositions;
            GameEvents.OnCameraChange.Add(OnCameraChange);
            distributionNoise = Utils.GetDistributionData(scatter, quad);
            SetupComputeShaders();
            InitializeGenerate();
            PrepareGenerate();
            DispatchGenerate();
        }
        //public Mesh Subdivide()
        //{
        //    if (properties.subdivisionSettings.mode == SubdivisionMode.NearestQuads)
        //    {
        //
        //    }
        //}
        public void OnCameraChange(CameraManager.CameraMode data)
        {
            //if (initializedGenerate && FlightGlobals.ready)    //OnCameraChange is called before the position buffer is set up. Make sure it's ready before regenerating
            //{
            //    PrepareGenerate();
            //    DispatchGenerate();
            //}
        }
        public void SetupComputeShaders() 
        {
            distribute = Utils.GetCorrectComputeShader(scatter);
            evaluate = GameObject.Instantiate(ScatterShaderHolder.GetCompute("EvaluatePoints"));
            distributeIndex = distribute.FindKernel("DistributePoints");
            evaluateIndex = evaluate.FindKernel("EvaluatePoints");

            int maxLevel = quad.sphereRoot.maxLevel;
            triCount = mesh.triangles.Length;
            quadSubdivDifference = ((maxLevel - quad.subdivision) * 3) + 1;
            memoryUsage = (int)(triCount * properties.scatterDistribution._PopulationMultiplier * quadSubdivDifference * properties.scatterDistribution.noise._MaxStacks);
        }
        public void InitializeGenerate()
        {
            initializedGenerate = false;
            //Utils.SafetyCheckDispose(ref vertexBuffer, "vertex buffer");
            //Utils.SafetyCheckDispose(ref positionBuffer, "grass position buffer");
            //Utils.SafetyCheckDispose(ref normalBuffer, "normal buffer");
            //Utils.SafetyCheckDispose(ref triBuffer, "mesh triangle buffer");
            //Utils.SafetyCheckDispose(ref noiseBuffer, "mesh noise buffer");
            //Utils.SafetyCheckDispose(ref distributeCountBuffer, "count buffer");

            //vertexBuffer = Utils.SetupComputeBufferSafe(mesh.vertexCount, 12, ComputeBufferType.Structured);
            //normalBuffer = Utils.SetupComputeBufferSafe(mesh.normals.Length, 12, ComputeBufferType.Structured);
            //positionBuffer = Utils.SetupComputeBufferSafe((mesh.triangles.Length / 3) * (int)scatter.properties.scatterDistribution._PopulationMultiplier * quadSubdivDifference * scatter.properties.scatterDistribution.noise._MaxStacks, PositionData.Size(), ComputeBufferType.Append);
            //triBuffer = Utils.SetupComputeBufferSafe(mesh.triangles.Length, sizeof(int), ComputeBufferType.Structured);
            //noiseBuffer = Utils.SetupComputeBufferSafe(distributionNoise.Length, sizeof(float), ComputeBufferType.Structured);
            //distributeCountBuffer = Utils.SetupComputeBufferSafe(1, sizeof(int), ComputeBufferType.IndirectArguments);

            //vertexBuffer.SetData(mesh.vertices);
            //triBuffer.SetData(mesh.triangles);
            //noiseBuffer.SetData(distributionNoise);
            //normalBuffer.SetData(mesh.normals);

            //distribute.SetBuffer(distributeIndex, "Objects", vertexBuffer);
            //distribute.SetBuffer(distributeIndex, "Tris", triBuffer);
            //distribute.SetBuffer(distributeIndex, "Noise", noiseBuffer);
            //distribute.SetBuffer(distributeIndex, "Positions", positionBuffer);
            //distribute.SetBuffer(distributeIndex, "Normals", normalBuffer);

            initializedGenerate = true;
        }
        public void PrepareGenerate() //Each time a quad is built, generate scatter positions
        {
            Utils.SetDistributionVars(ref distribute, scatter, quad.gameObject.transform, quadSubdivDifference, triCount, quad.sphereRoot.name);
        }
        public void DispatchGenerate()
        {
            //positionBuffer.SetCounterValue(0);
            //distribute.Dispatch(distributeIndex, Mathf.CeilToInt((((float)triCount) / 3f) / 32f), scatter.properties.scatterDistribution.noise._MaxStacks, 1);
            //ComputeBuffer.CopyCount(positionBuffer, distributeCountBuffer, 0);
            //AsyncGPUReadback.Request(distributeCountBuffer, AwaitDistributeReadback);
            currentlyReadingDist = true;
        }
        private void AwaitDistributeReadback(AsyncGPUReadbackRequest req)
        {
            if (req.hasError)
            {
                ScatterLog.Log("[Exception] Async GPU Readback error! (In GeneratePositions())");
                return;
            }
            objectCount = req.GetData<int>(0).ToArray()[0];
            //if (objectCount == 0)
            //{
            //    currentlyReadingDist = false;
            //    Cleanup(); //No need to evaluate anything on this quad scatter. There's nothing there
            //    return;
            //}
            currentlyReadingDist = false;
            InitializeEvaluate();
        }
        public void InitializeEvaluate()
        {
            if (currentlyReadingDist) { return; }
            if (objectCount == 0) { return; }

            evaluate.SetBuffer(evaluateIndex, "Grass", Buffers.activeBuffers[scatter.scatterName].buffer);
            evaluate.SetBuffer(evaluateIndex, "Positions", positionBuffer);
            evaluate.SetBuffer(evaluateIndex, "FarGrass", Buffers.activeBuffers[scatter.scatterName].farBuffer);
            evaluate.SetBuffer(evaluateIndex, "FurtherGrass", Buffers.activeBuffers[scatter.scatterName].furtherBuffer);

            if (indirectArgs != null) { indirectArgs.Dispose(); indirectArgs = null; }
            indirectArgs = new ComputeBuffer(1, sizeof(int) * 3, ComputeBufferType.IndirectArguments);
            int[] workGroups = new int[] { Mathf.CeilToInt(((float)objectCount) / 32f), 1, 1 };
            indirectArgs.SetData(workGroups);

            Utils.SetEvaluationVars(ref evaluate, scatter, quad.gameObject.transform, objectCount);
            initializedEvaluate = true;

            EvaluatePositions();
        }
        public void EvaluatePositions()
        {
            if (!quad.isVisible) { return; }
            if (currentlyReadingDist) { return; }
            if (!initializedEvaluate) { return; }
            if (objectCount == 0) { return; }
            //Debug.Log("Dispatching evaluate indirectly");

            evaluate.SetVector("_ShaderOffset", -((Vector3)FloatingOrigin.TerrainShaderOffset));
            evaluate.SetVector("_CameraPos", ActiveBuffers.cameraPos);
            evaluate.SetVector("_CraftPos", Vector3.zero);
            evaluate.SetFloat("_CurrentTime", Time.timeSinceLevelLoad);
            evaluate.SetFloats("_CameraFrustumPlanes", ActiveBuffers.planeNormals);
            evaluate.DispatchIndirect(evaluateIndex, indirectArgs, 0);
        }
        public void Cleanup()
        {
            //Debug.Log("Cleanup called for scatter: " + scatter.scatterName);   
           
            //Events
            pqsMod.OnForceEvaluate -= EvaluatePositions;
            GameEvents.OnCameraChange.Remove(OnCameraChange);
            Utils.DestroyComputeBufferSafe(ref vertexBuffer);
            Utils.DestroyComputeBufferSafe(ref normalBuffer);
            Utils.DestroyComputeBufferSafe(ref triBuffer);
            Utils.DestroyComputeBufferSafe(ref noiseBuffer);
            Utils.DestroyComputeBufferSafe(ref positionBuffer);
            Utils.DestroyComputeBufferSafe(ref distributeCountBuffer);
            Utils.DestroyComputeBufferSafe(ref indirectArgs);
        }
    }
}
