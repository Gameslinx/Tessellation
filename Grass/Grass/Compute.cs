using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ParallaxGrass;
using Kopernicus.ConfigParser.Attributes;
using Kopernicus.ConfigParser.Enumerations;
using Kopernicus.ConfigParser.BuiltinTypeParsers;
using System.Collections;
using Kopernicus.Configuration.ModLoader;
using Grass;
using ScatterConfiguratorUtils;
using System;
using UnityEngine.Rendering;

namespace ComputeLoader
{
    public class ComputeComponent : MonoBehaviour
    {
        public PQ quad;
        // Start is called before the first frame update
        public ComputeShader distribute;
        public ComputeShader evaluate;

        public ComputeBuffer positionBuffer;
        public ComputeBuffer grassPositionBuffer;
        public ComputeBuffer triangleBuffer;
        public ComputeBuffer noiseBuffer;
        public ComputeBuffer countBuffer;
        public ComputeBuffer positionCountBuffer;
        public ComputeBuffer normalBuffer;

        public ComputeBuffer grassBuffer;
        public ComputeBuffer farGrassBuffer;    //For the grass further away from the camera - Much lower LOD
        public ComputeBuffer furtherGrassBuffer;
        public ComputeBuffer subObjectSlot1;
        public ComputeBuffer subObjectSlot2;
        public ComputeBuffer subObjectSlot3;
        public ComputeBuffer subObjectSlot4;

        public PostCompute pc;  //Assign this OUTSIDE of this

        public Mesh mesh;
        public int vertCount;
        public int triCount;

        public Vector3 _PlanetOrigin;

        public Scatter scatter;
        public int quadSubdivision; //MARKED FOR REMOVE
        public bool isVisible = true; //   MARKED FOR REMOVE

        private int evaluatePoints;

        public float updateFPS; //1.0f;

        public int subObjectCount = 0;
        public int quadSubdivisionDifference = 1;  //Using this, increase population as quad subdivision is reduced to balance out
        public int objectCount = 0;
        public bool started = false;
        public float[] distributionNoise;
        public float vRAMinMb = 0;
        
        struct PositionData
        {
            public Vector3 pos;
            public Matrix4x4 mat;
            public Vector4 color;
            public int mode;
            public static int Size()
            {
                return
                    sizeof(float) * 4 * 4 + // matrix;
                    sizeof(float) * 3 +
                    sizeof(float) * 4 +
                    sizeof(int);     // color
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
        public void Start()
        {
            pc.quadName = quad.name;
            //quadSubdivisionDifference = 1;
            RealStart();
            //mesh = Instantiate(gameObject.GetComponent<MeshFilter>().mesh);
            
            
            //StartCoroutine(UpdatePositionFPS());
        }
        void RealStart()
        {
            //yield return new WaitForEndOfFrame();  //Prevent floating objects while quad is still repositioning
            if (mesh == null)
            {
                Debug.Log("[Exception] Quad mesh is null (ComputeComponent RealStart())");
                quad.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Standard"));
                quad.GetComponent<MeshRenderer>().material.SetColor("_Color", new Color(1, 0, 1));
                return;
            }
            vertCount = mesh.vertexCount;
            triCount = mesh.triangles.Length;
            if (scatter.properties.scatterDistribution.noise.noiseMode == DistributionNoiseMode.NonPersistent)
            {
                distribute = Instantiate(ScatterShaderHolder.GetCompute("DistributeNearest"));
            }
            else if (scatter.properties.scatterDistribution.noise.noiseMode == DistributionNoiseMode.Persistent)
            {
                distribute = Instantiate(ScatterShaderHolder.GetCompute("DistributeFixed"));
            }
            else
            {
                distribute = Instantiate(ScatterShaderHolder.GetCompute("DistFTH"));
            }
            evaluate = Instantiate(ScatterShaderHolder.GetCompute("EvaluatePoints"));
            GeneratePositions();
            started = true;
        }
        bool initialized = false;
        public void InitializeAllBuffers()
        {
            evaluatePoints = evaluate.FindKernel("EvaluatePoints");

            Utils.SafetyCheckRelease(countBuffer, "countBuffer");
            Utils.SafetyCheckRelease(grassBuffer, "grassBuffer");
            Utils.SafetyCheckRelease(farGrassBuffer, "farGrassBuffer");
            Utils.SafetyCheckRelease(furtherGrassBuffer, "furtherGrassBuffer");
            Utils.SafetyCheckRelease(subObjectSlot1, "subObjectSlot1");
            Utils.SafetyCheckRelease(subObjectSlot2, "subObjectSlot2");
            Utils.SafetyCheckRelease(subObjectSlot3, "subObjectSlot3");
            Utils.SafetyCheckRelease(subObjectSlot4, "subObjectSlot4");

            int maxStacks = scatter.properties.scatterDistribution.noise._MaxStacks;
            countBuffer = Utils.SetupComputeBufferSafe(7, sizeof(int), ComputeBufferType.IndirectArguments);
            grassBuffer = Utils.SetupComputeBufferSafe((triCount / 3) * (int)scatter.properties.scatterDistribution._PopulationMultiplier * quadSubdivisionDifference * maxStacks, GrassData.Size(), ComputeBufferType.Append);
            farGrassBuffer = Utils.SetupComputeBufferSafe((triCount / 3) * (int)scatter.properties.scatterDistribution._PopulationMultiplier * quadSubdivisionDifference * maxStacks, GrassData.Size(), ComputeBufferType.Append);
            furtherGrassBuffer = Utils.SetupComputeBufferSafe((triCount / 3) * (int)scatter.properties.scatterDistribution._PopulationMultiplier * quadSubdivisionDifference * maxStacks, GrassData.Size(), ComputeBufferType.Append);
            int count = (triCount / 3) * (int)scatter.properties.scatterDistribution._PopulationMultiplier * quadSubdivisionDifference * maxStacks;
            //Debug.Log("Counts: " + count);
            //Debug.Log("Tricount: " + triCount);
            //Debug.Log("PopMult: " + scatter.properties.scatterDistribution._PopulationMultiplier);
            //Debug.Log("QSD: " + quadSubdivisionDifference);
            //Debug.Log("Scatter: " + scatter.scatterName);
            //Debug.Log("Memory footprint per buffer: " + (((float)count * GrassData.Size()) / (1024 * 1024)) + " mb");
            subObjectSlot1 = Utils.SetupComputeBufferSafe((triCount / 3) * (int)scatter.properties.scatterDistribution._PopulationMultiplier * quadSubdivisionDifference * maxStacks, GrassData.Size(), ComputeBufferType.Append);
            subObjectSlot2 = Utils.SetupComputeBufferSafe((triCount / 3) * (int)scatter.properties.scatterDistribution._PopulationMultiplier * quadSubdivisionDifference * maxStacks, GrassData.Size(), ComputeBufferType.Append);
            subObjectSlot3 = Utils.SetupComputeBufferSafe((triCount / 3) * (int)scatter.properties.scatterDistribution._PopulationMultiplier * quadSubdivisionDifference * maxStacks, GrassData.Size(), ComputeBufferType.Append);
            subObjectSlot4 = Utils.SetupComputeBufferSafe((triCount / 3) * (int)scatter.properties.scatterDistribution._PopulationMultiplier * quadSubdivisionDifference * maxStacks, GrassData.Size(), ComputeBufferType.Append);

            float totalMemory = (GrassData.Size() * count * 8) + (7 * sizeof(int)) + (vertCount * 12) + (vertCount * 12) + (distributionNoise.Length * sizeof(float));
            vRAMinMb = totalMemory / (1024 * 1024);

            evaluate.SetBuffer(evaluatePoints, "Grass", grassBuffer);
            evaluate.SetBuffer(evaluatePoints, "Positions", grassPositionBuffer);
            evaluate.SetBuffer(evaluatePoints, "FarGrass", farGrassBuffer);
            evaluate.SetBuffer(evaluatePoints, "FurtherGrass", furtherGrassBuffer);
            evaluate.SetBuffer(evaluatePoints, "SubObjects1", subObjectSlot1);
            evaluate.SetBuffer(evaluatePoints, "SubObjects2", subObjectSlot2);
            evaluate.SetBuffer(evaluatePoints, "SubObjects3", subObjectSlot3);
            evaluate.SetBuffer(evaluatePoints, "SubObjects4", subObjectSlot4);
           
            initialized = true;
        }
        Vector3d previousTerrainOffset = Vector3d.zero;
        void Update()
        {
            //return;
            //return;
            //if (FloatingOrigin.TerrainShaderOffset != previousTerrainOffset)
            //{
            //    EvaluatePositions();
            //    timeSinceLastUpdate = 0;
            //    previousTerrainOffset = FloatingOrigin.TerrainShaderOffset;
            //}
            if (quad == null)
            {
                Debug.Log("Quad null");
            }
            if (scatter == null)
            {
                Debug.Log("Scatter is null");
                quad.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Standard"));
                Debug.Log("Swapped material");
            }
            bool isTime = CheckTheTime(scatter.updateFPS);
            if (isTime && FlightGlobals.ActiveVessel.speed > 0.05f && started && objectCount > 0)
            {
                //GeneratePositions();
                EvaluatePositions();
            }
            
        }
        float timeSinceLastSoftUpdate = 0;
        void FixedUpdate()
        {
            return;
            if (scatter.properties.subdivisionSettings.mode == SubdivisionMode.FixedRange)
            {
                float targetDeltaTime = 1.0f / softUpdateRate;
                float deltaTime = Time.deltaTime;
                if (timeSinceLastSoftUpdate >= targetDeltaTime)
                {
                    timeSinceLastSoftUpdate = 0;
                    bool isTimeForSoft = CheckTheSoftTime();
                    if (isTimeForSoft && FlightGlobals.ActiveVessel.speed < 100f && objectCount > 0)
                    {

                        EvaluatePositions();

                    }
                }
                else
                {
                    timeSinceLastSoftUpdate += Time.deltaTime;
                }
                
                

            }
        }
        float softUpdateRate = 1;
        float timeSinceLastUpdate = 0;
        float distanceSinceLastUpdate = 0;
        Vector3 lastPos = Vector3.zero;
        public bool CheckTheTime(float TargetFPS) //Grass framerate
        {
            float targetDeltaTime = 1.0f / TargetFPS;
            float deltaTime = Time.deltaTime;
            
            if (deltaTime > targetDeltaTime * 4)
            {
                ScatterLog.Log("Warning: The time since the last frame is vastly exceeding the target framerate for Compute Shader updates. Consider lowering the scatter update rate in your settings!");
            }
            if (timeSinceLastUpdate >= targetDeltaTime)
            {
                timeSinceLastUpdate = 0;
                return true;
            }
            else
            {
                timeSinceLastUpdate += Time.deltaTime;
                return false;
            }
        }
        public bool CheckTheSoftTime() //Grass framerate
        {
            if (Vector3.Distance(FlightGlobals.ActiveVessel.transform.position, transform.position) > scatter.properties.scatterDistribution.lods.lods[0].range + (int)(((2 * Mathf.PI * FlightGlobals.currentMainBody.Radius) / 4) / (Mathf.Pow(2, FlightGlobals.currentMainBody.pqsController.maxLevel))) )
            {
                return false;
            }
            Vector3 thisPos = LatLon.GetRelSurfacePosition(FlightGlobals.currentMainBody.BodyFrame, FlightGlobals.currentMainBody.transform.position, FlightGlobals.ActiveVessel.transform.position);
            float distance = Vector3.Distance(thisPos, lastPos);
            lastPos = thisPos;
            distanceSinceLastUpdate += distance;
            if (distanceSinceLastUpdate > scatter.properties.scatterDistribution.lods.lods[0].range)
            {
                distanceSinceLastUpdate = 0;
                return true;
            }
            else { return false; }
        }
        public void GeneratePositions()
        {
            Vector3[] verts = mesh.vertices;
            int[] tris = mesh.triangles;
            Vector3[] normals = mesh.normals;
            Utils.SafetyCheckRelease(positionBuffer, "position buffer");
            Utils.SafetyCheckRelease(grassPositionBuffer, "grass position buffer");
            Utils.SafetyCheckRelease(normalBuffer, "normal buffer");
            Utils.SafetyCheckRelease(triangleBuffer, "mesh triangle buffer");
            Utils.SafetyCheckRelease(noiseBuffer, "mesh noise buffer");
            Utils.SafetyCheckRelease(positionCountBuffer, "mesh triangle buffer");
            positionBuffer = Utils.SetupComputeBufferSafe(vertCount, 12, ComputeBufferType.Structured);
            normalBuffer = Utils.SetupComputeBufferSafe(normals.Length, 12, ComputeBufferType.Structured);
            grassPositionBuffer = Utils.SetupComputeBufferSafe((tris.Length / 3) * (int)scatter.properties.scatterDistribution._PopulationMultiplier * quadSubdivisionDifference * scatter.properties.scatterDistribution.noise._MaxStacks, PositionData.Size(), ComputeBufferType.Append);
            triangleBuffer = Utils.SetupComputeBufferSafe(tris.Length, sizeof(int), ComputeBufferType.Structured);                  //quad subdiv diff ^
            noiseBuffer = Utils.SetupComputeBufferSafe(distributionNoise.Length, sizeof(float), ComputeBufferType.Structured);
            positionCountBuffer = Utils.SetupComputeBufferSafe(1, sizeof(int), ComputeBufferType.IndirectArguments);

            positionBuffer.SetData(verts);
            triangleBuffer.SetData(tris);
            noiseBuffer.SetData(distributionNoise);
            normalBuffer.SetData(normals);

            int distributeKernel = distribute.FindKernel("DistributePoints");
            grassPositionBuffer.SetCounterValue(0);
            distribute.SetInt("_PopulationMultiplier", (int)scatter.properties.scatterDistribution._PopulationMultiplier * quadSubdivisionDifference); //quadsubdiv diff
            distribute.SetMatrix("_ObjectToWorld", transform.localToWorldMatrix);
            distribute.SetVector("_PlanetOrigin", FlightGlobals.currentMainBody.transform.position);
            distribute.SetInt("_VertCount", vertCount);
            distribute.SetVector("_PlanetNormal", Vector3.Normalize(FlightGlobals.ActiveVessel.transform.position - FlightGlobals.currentMainBody.transform.position));
            distribute.SetVector("_ThisPos", transform.position);
            distribute.SetVector("_ShaderOffset", -(Vector3)FloatingOrigin.TerrainShaderOffset);
            distribute.SetInt("_MaxCount", (triCount / 3) * (int)scatter.properties.scatterDistribution._PopulationMultiplier * quadSubdivisionDifference);
            distribute.SetVector("minScale", scatter.properties.scatterDistribution._MinScale);
            distribute.SetVector("maxScale", scatter.properties.scatterDistribution._MaxScale);
            distribute.SetFloat("minAltitude", scatter.properties.scatterDistribution._MinAltitude);
            distribute.SetFloat("maxAltitude", scatter.properties.scatterDistribution._MaxAltitude);
            distribute.SetFloat("grassSizeNoiseScale", scatter.properties.scatterDistribution.noise._SizeNoiseScale);
            distribute.SetFloat("grassSizeNoiseOffset", scatter.properties.scatterDistribution.noise._SizeNoiseOffset);
            distribute.SetVector("grassColorMain", scatter.properties.scatterMaterial._MainColor);//scatter.properties.scatterMaterial._MainColor);
            distribute.SetVector("grassColorSub", scatter.properties.scatterMaterial._SubColor);
            distribute.SetFloat("grassColorNoiseStrength", scatter.properties.scatterMaterial._ColorNoiseStrength);
            distribute.SetFloat("grassColorNoiseScale", scatter.properties.scatterDistribution.noise._ColorNoiseScale);
            if (scatter.properties.scatterDistribution.noise.noiseMode == DistributionNoiseMode.VerticalStack)
            {
                distribute.SetFloat("_StackSeparation", scatter.properties.scatterDistribution.noise._StackSeparation);
                distribute.SetInt("_VerticalMult", scatter.properties.scatterDistribution.noise._MaxStacks);
            }


            distribute.SetFloat("grassCutoffScale", scatter.properties.scatterDistribution._CutoffScale);
            distribute.SetFloat("grassSizeNoiseStrength", scatter.properties.scatterDistribution._SizeNoiseStrength);
            distribute.SetFloat("_SteepPower", scatter.properties.scatterDistribution._SteepPower);
            distribute.SetFloat("_SteepContrast", scatter.properties.scatterDistribution._SteepContrast);
            distribute.SetFloat("_SteepMidpoint", scatter.properties.scatterDistribution._SteepMidpoint);
            distribute.SetFloat("subObjectPatchChance1", GetSubObjectProperty("subObjectPatchChance", 0));
            distribute.SetFloat("subObjectSpawnRadius1", GetSubObjectProperty("subObjectSpawnRadius", 0));
            distribute.SetFloat("subObjectSpawnChance1", GetSubObjectProperty("subObjectSpawnChance", 0));

            distribute.SetFloat("subObjectPatchChance2", GetSubObjectProperty("subObjectPatchChance", 1));
            distribute.SetFloat("subObjectSpawnRadius2", GetSubObjectProperty("subObjectSpawnRadius", 1));
            distribute.SetFloat("subObjectSpawnChance2", GetSubObjectProperty("subObjectSpawnChance", 1));

            distribute.SetFloat("subObjectPatchChance3", GetSubObjectProperty("subObjectPatchChance", 2));
            distribute.SetFloat("subObjectSpawnRadius3", GetSubObjectProperty("subObjectSpawnRadius", 2));
            distribute.SetFloat("subObjectSpawnChance3", GetSubObjectProperty("subObjectSpawnChance", 2));


           
            distribute.SetFloat("_MaxNormalDeviance", scatter.properties.scatterDistribution._MaxNormalDeviance);

            distribute.SetFloat("_PlanetRadius", (float)FlightGlobals.currentMainBody.Radius);
            distribute.SetVector("_PlanetRelative", Utils.initialPlanetRelative);
            distribute.SetMatrix("_WorldToPlanet", FlightGlobals.currentMainBody.gameObject.transform.worldToLocalMatrix);

            distribute.SetFloat("spawnChance", scatter.properties.scatterDistribution._SpawnChance);
            double lat = 0;
            double lon = 0;
            double alt = 0;
            //LatLon.GetLatLongAlt(FlightGlobals.currentMainBody.BodyFrame, FlightGlobals.currentMainBody.transform.position, FlightGlobals.currentMainBody.Radius, FlightGlobals.currentMainBody.transform.position, out lat, out lon, out alt);
            
            distribute.SetVector("_PlanetRelative", Utils.initialPlanetRelative);
            if (scatter.alignToTerrainNormal){ distribute.SetInt("_AlignToNormal", 1); }else{ distribute.SetInt("_AlignToNormal", 0); }


            distribute.SetBuffer(distributeKernel, "Objects", positionBuffer);
            distribute.SetBuffer(distributeKernel, "Tris", triangleBuffer);
            distribute.SetBuffer(distributeKernel, "Noise", noiseBuffer);
            distribute.SetBuffer(distributeKernel, "Positions", grassPositionBuffer);
            distribute.SetBuffer(distributeKernel, "Normals", normalBuffer);
           


            distribute.Dispatch(distributeKernel, Mathf.CeilToInt((((float)tris.Length) / 3f) / 32f), scatter.properties.scatterDistribution.noise._MaxStacks, 1);
            ComputeBuffer.CopyCount(grassPositionBuffer, positionCountBuffer, 0);
            AsyncGPUReadback.Request(positionCountBuffer, AwaitDistributeReadback);
            currentlyReadingDist = true;
            //int[] count = new int[] { 0 };
            //positionCountBuffer.GetData(count);
            //objectCount = count[0];
            //EvaluatePositions();

        }
        private void AwaitDistributeReadback(AsyncGPUReadbackRequest req)
        {
            if (req.hasError)
            {
                ScatterLog.Log("[Exception] Async GPU Readback error! (In GeneratePositions())");
                return;
            }
            objectCount = req.GetData<int>(0).ToArray()[0];
            InitializeAllBuffers();
            if (objectCount == 0)
            {
                return;
            }
            currentlyReadingDist = false;
            EvaluatePositions();
        }
        bool currentlyReadingDist = false;
        bool currentlyReadingEv = false;
        public void EvaluatePositions()
        {
            if (initialized == false) { Debug.Log("Not initialized yet..."); }
            if (currentlyReadingDist || currentlyReadingEv) { Debug.Log("Currently reading something"); return; }
            if (objectCount == 0) { return; }
            if (grassBuffer == null)
            {
                Debug.Log("Buffer null");
            }
            if (!grassBuffer.IsValid())             //Someone else is gonna need to help me on this. Buffers are initialized without error but fail here on some seemingly random quads
            {
                Destroy(this);
                return;
            }
            grassBuffer.SetCounterValue(0);
            farGrassBuffer.SetCounterValue(0);
            furtherGrassBuffer.SetCounterValue(0);
            subObjectSlot1.SetCounterValue(0);
            subObjectSlot2.SetCounterValue(0);
            subObjectSlot3.SetCounterValue(0);
            subObjectSlot4.SetCounterValue(0);
            
            
            evaluate.SetFloat("range", scatter.properties.scatterDistribution._Range);
            evaluate.SetVector("_CameraPos", FlightGlobals.ActiveVessel.transform.position);//Camera.allCameras.FirstOrDefault(_cam => _cam.name == "Camera 00").gameObject.transform.position - gameObject.transform.position);


            evaluate.SetFloat("_LODPerc", scatter.properties.scatterDistribution.lods.lods[0].range / scatter.properties.scatterDistribution._Range);    //At what range does the LOD change to the low one?
            evaluate.SetFloat("_LOD2Perc", scatter.properties.scatterDistribution.lods.lods[1].range / scatter.properties.scatterDistribution._Range);

            evaluate.SetVector("_ShaderOffset", -(Vector3)FloatingOrigin.TerrainShaderOffset);
            evaluate.SetVector("_ThisPos", transform.position);
            evaluate.SetInt("_MaxCount", objectCount);  //quadsubdif?
                                                        //and V
            evaluate.Dispatch(evaluatePoints, Mathf.CeilToInt(((float)objectCount) / 32f), 1, 1);

            ComputeBuffer.CopyCount(grassBuffer, countBuffer, 0);
            ComputeBuffer.CopyCount(farGrassBuffer, countBuffer, 4);
            ComputeBuffer.CopyCount(furtherGrassBuffer, countBuffer, 8);
            ComputeBuffer.CopyCount(subObjectSlot1, countBuffer, 12);
            ComputeBuffer.CopyCount(subObjectSlot2, countBuffer, 16);
            ComputeBuffer.CopyCount(subObjectSlot3, countBuffer, 20);
            ComputeBuffer.CopyCount(subObjectSlot4, countBuffer, 24);
            AsyncGPUReadback.Request(countBuffer, AwaitEvaluateReadback);
            currentlyReadingEv = true;
            //int[] count = new int[] { 0, 0, 0, 0, 0, 0, 0 };
            //countBuffer.GetData(count);
            //pc.Setup(count, new ComputeBuffer[] { grassBuffer, farGrassBuffer, furtherGrassBuffer, subObjectSlot1, subObjectSlot2, subObjectSlot3, subObjectSlot4 }, scatter);
        }
        private void AwaitEvaluateReadback(AsyncGPUReadbackRequest req)
        {
            if (req.hasError)
            {
                ScatterLog.Log("[Exception] Async GPU Readback error! (In EvaluatePositions())");
                return;
            }
            int[] count = req.GetData<int>(0).ToArray();
            currentlyReadingEv = false;
            pc.Setup(count, new ComputeBuffer[] { grassBuffer, farGrassBuffer, furtherGrassBuffer, subObjectSlot1, subObjectSlot2, subObjectSlot3, subObjectSlot4 }, scatter);
        }
        public float GetSubObjectProperty(string property, int index)
        {
            if (index >= subObjectCount)    //0, 1, 2, 3
            {                               //1, 2, 3, 4
                return 0;
            }
            else
            {
                if (property == "subObjectPatchChance")
                {
                    return scatter.subObjects[index].properties._NoiseAmount;
                }
                else if (property == "subObjectSpawnRadius")
                {
                    return scatter.subObjects[index].properties._NoiseScale;
                }
                else if (property == "subObjectSpawnChance")
                {
                    return scatter.subObjects[index].properties._Density;
                }
                else
                {
                    ScatterLog.Log("Exception getting SubObject property: " + property);
                    return 0;
                }
            }
        }
        void ReleasePositionBuffers()
        {
            if (positionBuffer != null && triangleBuffer != null && grassPositionBuffer != null)
            {
                positionBuffer.Release();
                triangleBuffer.Release();
                grassPositionBuffer.Release();
            }
        }
        void ReleaseEvaluationBuffers()
        {
            if (countBuffer != null && grassBuffer != null)
            {
                countBuffer.Release();
                grassBuffer.Release();
            }
        }
        bool everDestroyed = false;
        void OnDestroy()
        {

            Utils.ForceGPUFinish(grassBuffer, typeof(GrassData), (triCount / 3) * (int)scatter.properties.scatterDistribution._PopulationMultiplier);
            Utils.DestroyComputeBufferSafe(positionBuffer);
            Utils.DestroyComputeBufferSafe(normalBuffer);
            Utils.DestroyComputeBufferSafe(triangleBuffer);
            Utils.DestroyComputeBufferSafe(noiseBuffer);
            Utils.DestroyComputeBufferSafe(grassPositionBuffer);
            Utils.DestroyComputeBufferSafe(countBuffer);
            Utils.DestroyComputeBufferSafe(grassBuffer);
            Utils.DestroyComputeBufferSafe(farGrassBuffer);
            Utils.DestroyComputeBufferSafe(furtherGrassBuffer);
            Utils.DestroyComputeBufferSafe(subObjectSlot1);
            Utils.DestroyComputeBufferSafe(subObjectSlot2);
            Utils.DestroyComputeBufferSafe(subObjectSlot3);
            Utils.DestroyComputeBufferSafe(subObjectSlot4);
            Utils.DestroyComputeBufferSafe(positionCountBuffer);
            Destroy(pc);
        }
        bool everDisabled = false;
        void OnDisable()
        {
            Utils.ForceGPUFinish(grassBuffer, typeof(GrassData), (triCount / 3) * (int)scatter.properties.scatterDistribution._PopulationMultiplier);
            Utils.DestroyComputeBufferSafe(positionBuffer);
            Utils.DestroyComputeBufferSafe(normalBuffer);
            Utils.DestroyComputeBufferSafe(triangleBuffer);
            Utils.DestroyComputeBufferSafe(noiseBuffer);
            Utils.DestroyComputeBufferSafe(grassPositionBuffer);
            Utils.DestroyComputeBufferSafe(countBuffer);
            Utils.DestroyComputeBufferSafe(grassBuffer);
            Utils.DestroyComputeBufferSafe(farGrassBuffer);
            Utils.DestroyComputeBufferSafe(furtherGrassBuffer);
            Utils.DestroyComputeBufferSafe(subObjectSlot1);
            Utils.DestroyComputeBufferSafe(subObjectSlot2);
            Utils.DestroyComputeBufferSafe(subObjectSlot3);
            Utils.DestroyComputeBufferSafe(subObjectSlot4);
            Utils.DestroyComputeBufferSafe(positionCountBuffer);
            Destroy(pc);
        }
    }

    public class PostCompute : MonoBehaviour
    {
        public bool active = true;
        public Material material;
        public Material materialFar;
        public Material materialFurther;
        public Material subObjectMat1;
        public Material subObjectMat2;
        public Material subObjectMat3;
        public Material subObjectMat4;

        public Mesh mesh;
        public Mesh farMesh;
        public Mesh furtherMesh;
        public Mesh subObjectMesh1;
        public Mesh subObjectMesh2;
        public Mesh subObjectMesh3;
        public Mesh subObjectMesh4;

        private ComputeBuffer argsBuffer;
        private ComputeBuffer farArgsBuffer;
        private ComputeBuffer furtherArgsBuffer;
        private ComputeBuffer subArgs1;
        private ComputeBuffer subArgs2;
        private ComputeBuffer subArgs3;
        private ComputeBuffer subArgs4;


        private ComputeBuffer mainNear;
        private ComputeBuffer mainFar;
        private ComputeBuffer mainFurther;
        private ComputeBuffer sub1;
        private ComputeBuffer sub2;
        private ComputeBuffer sub3;
        private ComputeBuffer sub4;

        private Bounds bounds;
        bool setup = false;
        public bool setupInitial = false;
        public int countCheck = 0;
        public int farCountCheck = 0;
        public int furtherCountCheck = 0;
        public int subCount1 = 0;
        public int subCount2 = 0;
        public int subCount3 = 0;
        public int subCount4 = 0;

        public int vertexCount;
        public int farVertexCount;
        public int furtherVertexCount;
        public int subVertexCount1;
        public int subVertexCount2;
        public int subVertexCount3;
        public int subVertexCount4;

        float subdivisionRange = 0;

        public string quadName;

        public Properties scatterProps;
        UnityEngine.Rendering.ShadowCastingMode shadowCastingMode;
        public void SetupAgain(Scatter scatter)
        {
            material = new Material(scatter.properties.scatterMaterial.shader);

            //material.SetFloat("_WaveSpeed", 0);
            //material.SetFloat("_HeightCutoff", -1000);
            material = Utils.SetShaderProperties(material, scatter.properties.scatterMaterial);
            scatterProps = scatter.properties; //ScatterBodies.scatterBodies[FlightGlobals.currentMainBody.name].scatters["Grass"].properties;
            shadowCastingMode = scatter.shadowCastingMode;
            materialFar = Instantiate(material);
            materialFurther = Instantiate(material);
            if (scatter.properties.scatterDistribution.lods.lods[0].mainTexName != "parent")
            {
                materialFar.SetTexture("_MainTex", Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(t => t.name == scatter.properties.scatterDistribution.lods.lods[0].mainTexName));
            }
            if (scatter.properties.scatterDistribution.lods.lods[1].mainTexName != "parent")
            {
                materialFurther.SetTexture("_MainTex", Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(t => t.name == scatter.properties.scatterDistribution.lods.lods[1].mainTexName));
            }
            subObjectMat1 = Utils.GetSubObjectMaterial(scatter, 0);
            subObjectMat2 = Utils.GetSubObjectMaterial(scatter, 1);
            subObjectMat3 = Utils.GetSubObjectMaterial(scatter, 2);
            subObjectMat4 = Utils.GetSubObjectMaterial(scatter, 3);
            InitializeBuffers();
        }
        public void Setup(int[] counts, ComputeBuffer[] buffers, Scatter scatter)
        {
            if (!setupInitial)
            {
                GameObject go = GameDatabase.Instance.GetModel(scatter.model);
                Mesh mesh = Instantiate(go.GetComponent<MeshFilter>().mesh);
                this.mesh = mesh;
                go = GameDatabase.Instance.GetModel(scatter.properties.scatterDistribution.lods.lods[0].modelName);
                farMesh = Instantiate(go.GetComponent<MeshFilter>().mesh);
                go = GameDatabase.Instance.GetModel(scatter.properties.scatterDistribution.lods.lods[1].modelName);
                furtherMesh = Instantiate(go.GetComponent<MeshFilter>().mesh);
                material = new Material(scatter.properties.scatterMaterial.shader);
                subObjectMesh1 = Utils.GetSubObjectMesh(scatter, 0, out subVertexCount1);
                subObjectMesh2 = Utils.GetSubObjectMesh(scatter, 1, out subVertexCount2);
                subObjectMesh3 = Utils.GetSubObjectMesh(scatter, 2, out subVertexCount3);
                subObjectMesh4 = Utils.GetSubObjectMesh(scatter, 3, out subVertexCount4);
                material = Utils.SetShaderProperties(material, scatter.properties.scatterMaterial);
                scatterProps = scatter.properties; //ScatterBodies.scatterBodies[FlightGlobals.currentMainBody.name].scatters["Grass"].properties;
                shadowCastingMode = scatter.shadowCastingMode;
                materialFar = Instantiate(material);
                materialFurther = Instantiate(material);
                if (scatter.properties.scatterDistribution.lods.lods[0].mainTexName != "parent")
                {
                    materialFar.SetTexture("_MainTex", Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(t => t.name == scatter.properties.scatterDistribution.lods.lods[0].mainTexName));
                }
                if (scatter.properties.scatterDistribution.lods.lods[1].mainTexName != "parent")
                {
                    materialFurther.SetTexture("_MainTex", Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(t => t.name == scatter.properties.scatterDistribution.lods.lods[1].mainTexName));
                }
                subObjectMat1 = Utils.GetSubObjectMaterial(scatter, 0);
                subObjectMat2 = Utils.GetSubObjectMaterial(scatter, 1);
                subObjectMat3 = Utils.GetSubObjectMaterial(scatter, 2);
                subObjectMat4 = Utils.GetSubObjectMaterial(scatter, 3);
                subdivisionRange = (int)(((2 * Mathf.PI * FlightGlobals.currentMainBody.Radius) / 4) / (Mathf.Pow(2, FlightGlobals.currentMainBody.pqsController.maxLevel)));
                subdivisionRange = Mathf.Sqrt(Mathf.Pow(subdivisionRange, 2) + Mathf.Pow(subdivisionRange, 2));
                vertexCount = mesh.vertexCount;
                farVertexCount = farMesh.vertexCount;
                furtherVertexCount = furtherMesh.vertexCount;
                setupInitial = true;
            }
            try
            {
                bounds = new Bounds(transform.position, Vector3.one * (subdivisionRange));
            }
            catch(Exception ex) { Destroy(this); }
            countCheck = counts[0];
            farCountCheck = counts[1];
            furtherCountCheck = counts[2];
            subCount1 = counts[3];
            subCount2 = counts[4];
            subCount3 = counts[5];
            subCount4 = counts[6];
            mainNear = buffers[0];
            mainFar = buffers[1];
            mainFurther = buffers[2];
            sub1 = buffers[3];
            sub2 = buffers[4];
            sub3 = buffers[5];
            sub4 = buffers[6];
            InitializeBuffers();
        }

        private void InitializeBuffers()
        {
            Utils.SafetyCheckRelease(argsBuffer, "main args");
            Utils.SafetyCheckRelease(farArgsBuffer, "main args far");
            Utils.SafetyCheckRelease(furtherArgsBuffer, "main args far");
            Utils.SafetyCheckRelease(subArgs1, "sub args 1");
            Utils.SafetyCheckRelease(subArgs2, "sub args 2");
            Utils.SafetyCheckRelease(subArgs3, "sub args 3");
            Utils.SafetyCheckRelease(subArgs4, "sub args 4");

            uint[] subArgsSlot1 = new uint[0];
            uint[] subArgsSlot2 = new uint[0];
            uint[] subArgsSlot3 = new uint[0];
            uint[] subArgsSlot4 = new uint[0];
            uint[] args = new uint[0];// Utils.GenerateArgs(mesh, countCheck);
            uint[] farArgs = new uint[0];//Utils.GenerateArgs(farMesh, farCountCheck);
            uint[] furtherArgs = new uint[0];
            if (subCount1 != 0)
            {
                subArgsSlot1 = Utils.GenerateArgs(subObjectMesh1, subCount1);
                subArgs1 = Utils.SetupComputeBufferSafe(1, subArgsSlot1.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
                subArgs1.SetData(subArgsSlot1);
                subObjectMat1.SetBuffer("_Properties", sub1);
            }
            if (subCount2 != 0)
            {
                subArgsSlot2 = Utils.GenerateArgs(subObjectMesh2, subCount2);
                subArgs2 = Utils.SetupComputeBufferSafe(1, subArgsSlot2.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
                subArgs2.SetData(subArgsSlot2);
                subObjectMat2.SetBuffer("_Properties", sub2);
            }
            if (subCount3 != 0)
            {
                subArgsSlot3 = Utils.GenerateArgs(subObjectMesh3, subCount3);
                subArgs3 = Utils.SetupComputeBufferSafe(1, subArgsSlot3.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
                subArgs3.SetData(subArgsSlot3);
                subObjectMat3.SetBuffer("_Properties", sub3);
            }
            if (subCount4 != 0)
            {
                subArgsSlot4 = Utils.GenerateArgs(subObjectMesh4, subCount4);
                subArgs4 = Utils.SetupComputeBufferSafe(1, subArgsSlot4.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
                subArgs4.SetData(subArgsSlot4);
                subObjectMat4.SetBuffer("_Properties", sub4);
            }
            if (countCheck != 0)
            {
                args = Utils.GenerateArgs(mesh, countCheck);
                argsBuffer = Utils.SetupComputeBufferSafe(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
                argsBuffer.SetData(args);
                material.SetBuffer("_Properties", mainNear);
            }
            if (farCountCheck != 0)
            {
                farArgs = Utils.GenerateArgs(mesh, farCountCheck);
                farArgsBuffer = Utils.SetupComputeBufferSafe(1, farArgs.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
                farArgsBuffer.SetData(farArgs);
                materialFar.SetBuffer("_Properties", mainFar);
            }
            if (furtherCountCheck != 0)
            {
                furtherArgs = Utils.GenerateArgs(mesh, furtherCountCheck);
                furtherArgsBuffer = Utils.SetupComputeBufferSafe(1, furtherArgs.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
                furtherArgsBuffer.SetData(furtherArgs);
                materialFurther.SetBuffer("_Properties", mainFurther);
            }
            setup = true;
        }

        private void Start()
        {

        }
        private void Update()
        {
            if (!active)
            {
                return;
            }
            //UpdateMaterialOffsets();
            UpdateBounds();
            SetPlanetOrigin();
            if (farCountCheck != 0)
            {
                Graphics.DrawMeshInstancedIndirect(farMesh, 0, materialFar, bounds, farArgsBuffer, 0, null, UnityEngine.Rendering.ShadowCastingMode.On, true, gameObject.layer);
            }
            if (furtherCountCheck != 0)
            {
                Graphics.DrawMeshInstancedIndirect(furtherMesh, 0, materialFurther, bounds, furtherArgsBuffer, 0, null, shadowCastingMode, true, gameObject.layer);
            }
            if (subCount1 != 0)
            {
                Graphics.DrawMeshInstancedIndirect(subObjectMesh1, 0, subObjectMat1, bounds, subArgs1, 0, null, UnityEngine.Rendering.ShadowCastingMode.On, true, gameObject.layer);
            }
            if (subCount2 != 0)
            {
                Graphics.DrawMeshInstancedIndirect(subObjectMesh2, 0, subObjectMat2, bounds, subArgs2, 0, null, UnityEngine.Rendering.ShadowCastingMode.On, true, gameObject.layer);
            }
            if (subCount3 != 0)
            {
                Graphics.DrawMeshInstancedIndirect(subObjectMesh3, 0, subObjectMat3, bounds, subArgs3, 0, null, UnityEngine.Rendering.ShadowCastingMode.On, true, gameObject.layer);
            }
            if (subCount4 != 0)
            {
                Graphics.DrawMeshInstancedIndirect(subObjectMesh4, 0, subObjectMat4, bounds, subArgs4, 0, null, UnityEngine.Rendering.ShadowCastingMode.On, true, gameObject.layer);
            }
            if (countCheck != 0)
            {
                Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer, 0, null, UnityEngine.Rendering.ShadowCastingMode.On, true, gameObject.layer);
            }
        }
        private void SetPlanetOrigin()
        {
            Vector3 planetOrigin = FlightGlobals.currentMainBody.transform.position;//Vector3.zero;
            if (material != null && material.HasProperty("_PlanetOrigin"))
            {
                material.SetVector("_PlanetOrigin", planetOrigin);
            }
            if (materialFar != null && materialFar.HasProperty("_PlanetOrigin"))
            {
                materialFar.SetVector("_PlanetOrigin", planetOrigin);
            }
            if (materialFurther != null && materialFurther.HasProperty("_PlanetOrigin"))
            {
                materialFurther.SetVector("_PlanetOrigin", planetOrigin);
            }
            if (subObjectMat1 != null && subObjectMat1.HasProperty("_PlanetOrigin"))
            {
                subObjectMat1.SetVector("_PlanetOrigin", planetOrigin);
            }
            if (subObjectMat2 != null && subObjectMat2.HasProperty("_PlanetOrigin"))
            {
                subObjectMat2.SetVector("_PlanetOrigin", planetOrigin);
            }
            if (subObjectMat3 != null && subObjectMat3.HasProperty("_PlanetOrigin"))
            {
                subObjectMat3.SetVector("_PlanetOrigin", planetOrigin);
            }
            if (subObjectMat4 != null && subObjectMat4.HasProperty("_PlanetOrigin"))
            {
                subObjectMat4.SetVector("_PlanetOrigin", planetOrigin);
            }
        }
        private void UpdateBounds()
        {
            bounds = new Bounds(transform.position, Vector3.one * (subdivisionRange + 1));
        }
        private void OnDestroy()
        {
            Utils.ForceGPUFinish(mainNear, typeof(ComputeComponent.GrassData), countCheck);

            Utils.DestroyComputeBufferSafe(mainNear);
            Utils.DestroyComputeBufferSafe(mainFar);
            Utils.DestroyComputeBufferSafe(mainFurther);
            Utils.DestroyComputeBufferSafe(sub1);
            Utils.DestroyComputeBufferSafe(sub2);
            Utils.DestroyComputeBufferSafe(sub3);
            Utils.DestroyComputeBufferSafe(sub4);
            Utils.DestroyComputeBufferSafe(argsBuffer);
            Utils.DestroyComputeBufferSafe(farArgsBuffer);
            Utils.DestroyComputeBufferSafe(furtherArgsBuffer);
            Utils.DestroyComputeBufferSafe(subArgs1);
            Utils.DestroyComputeBufferSafe(subArgs2);
            Utils.DestroyComputeBufferSafe(subArgs3);
            Utils.DestroyComputeBufferSafe(subArgs4);
        }
        private void OnDisable()
        {
            Utils.ForceGPUFinish(mainNear, typeof(ComputeComponent.GrassData), countCheck);

            Utils.DestroyComputeBufferSafe(mainNear);
            Utils.DestroyComputeBufferSafe(mainFar);
            Utils.DestroyComputeBufferSafe(mainFurther);
            Utils.DestroyComputeBufferSafe(sub1);
            Utils.DestroyComputeBufferSafe(sub2);
            Utils.DestroyComputeBufferSafe(sub3);
            Utils.DestroyComputeBufferSafe(sub4);
            Utils.DestroyComputeBufferSafe(argsBuffer);
            Utils.DestroyComputeBufferSafe(farArgsBuffer);
            Utils.DestroyComputeBufferSafe(furtherArgsBuffer);
            Utils.DestroyComputeBufferSafe(subArgs1);
            Utils.DestroyComputeBufferSafe(subArgs2);
            Utils.DestroyComputeBufferSafe(subArgs3);
            Utils.DestroyComputeBufferSafe(subArgs4);
        }
    }
    
}