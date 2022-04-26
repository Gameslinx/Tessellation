using System.Collections.Generic;
using UnityEngine;
using ParallaxGrass;
using Kopernicus.ConfigParser.Attributes;
using Kopernicus.ConfigParser.Enumerations;
using Kopernicus.ConfigParser.BuiltinTypeParsers;
using System.Collections;
using Kopernicus.Configuration.ModLoader;
using Grass;
using ScatterConfiguratorUtils;
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
        public ComputeBuffer positionCountBuffer;
        public ComputeBuffer normalBuffer;

        public ComputeBuffer indirectArgs;
        //public PostCompute pc;  //Assign this OUTSIDE of this

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
        public int maxMemory = 0;
        public bool doEvaluate = true;

        PQSMod_ScatterManager pqsMod;
        
        struct PositionData
        {
            public Vector3 pos;
            public Matrix4x4 mat;
            public Vector4 color;
            public int mode;
            public float timeUpdated;
            public static int Size()
            {
                return
                    sizeof(float) * 4 * 4 + // matrix;
                    sizeof(float) * 3 + // pos
                    sizeof(float) * 4 + // color
                    sizeof(int) +
                    sizeof(float);     
            }
        }
        public struct GrassData
        {
            public Matrix4x4 mat;
            public Vector4 color;
            public float initialTime;
            public static int Size()
            {
                return
                    sizeof(float) * 4 * 4 + // matrix;
                    sizeof(float) * 4 +     // color
                    sizeof(float);          // time
            }
        }
        CameraManager.CameraMode lastCameraMode;
        public void OnCameraChange(CameraManager.CameraMode mode)
        {
            Debug.Log("Camera mode changed");
            if (mode == CameraManager.CameraMode.IVA)
            {
                GeneratePositions();    //Must regenerate based on terrain shader offset, which is reset for some reason
                lastCameraMode = CameraManager.CameraMode.IVA;
                
            }
            if (mode == CameraManager.CameraMode.Flight && lastCameraMode == CameraManager.CameraMode.IVA)
            {
                GeneratePositions();
            }
        }
        public void OnEnable()
        {
            GameEvents.OnCameraChange.Add(OnCameraChange);
        }
        public void Start()
        {
            //Debug.Log("OnEnable");
            //Debug.Log("Length is " + ActiveBuffers.mods.Count);
            for (int i = 0; i < ActiveBuffers.mods.Count; i++)
            {
                if (ActiveBuffers.mods[i] == null) { Debug.Log("Is null??"); }
                if (ActiveBuffers.mods[i].scatterName == scatter.scatterName)
                {
                    pqsMod = ActiveBuffers.mods[i];
                    if (pqsMod == null) { Debug.Log("Null PQSMod"); }
                    
                }
            }
            if (scatter == null) { Debug.Log("Scatter null?"); }
            if (scatter.scatterName == null) { Debug.Log("Name null?"); }
            //PQSMod_ScatterManager pqsMod = ActiveBuffers.mods.Find(x => x.scatterName == scatter.scatterName);  //Get corresponding mod here
            //Debug.Log("the bruh");
            //if (pqsMod == null) { Debug.Log("Null PQSMod"); }

            
            //Debug.Log("the bruh 2");
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
                Destroy(this);
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

            int maxStacks = scatter.properties.scatterDistribution.noise._MaxStacks;
            if (!Buffers.activeBuffers.ContainsKey(scatter.scatterName))
            {
                Debug.Log("Not contained");
            }

            int count = (triCount / 3) * (int)scatter.properties.scatterDistribution._PopulationMultiplier * quadSubdivisionDifference * maxStacks;

            float totalMemory = (GrassData.Size() * count * 8) + (7 * sizeof(int)) + (vertCount * 12) + (vertCount * 12) + (distributionNoise.Length * sizeof(float));
            vRAMinMb = totalMemory / (1024 * 1024);
            if (Buffers.activeBuffers[scatter.scatterName].buffer == null) { Debug.Log("Buffer is null lol"); }
            evaluate.SetBuffer(evaluatePoints, "Grass", Buffers.activeBuffers[scatter.scatterName].buffer);
            evaluate.SetBuffer(evaluatePoints, "Positions", grassPositionBuffer);
            evaluate.SetBuffer(evaluatePoints, "FarGrass", Buffers.activeBuffers[scatter.scatterName].farBuffer);
            evaluate.SetBuffer(evaluatePoints, "FurtherGrass", Buffers.activeBuffers[scatter.scatterName].furtherBuffer);
           
            initialized = true;
        }
        public void ReInitializeAllBuffers()
        {
            evaluate.SetBuffer(evaluatePoints, "Grass", Buffers.activeBuffers[scatter.scatterName].buffer);
            evaluate.SetBuffer(evaluatePoints, "FarGrass", Buffers.activeBuffers[scatter.scatterName].farBuffer);
            evaluate.SetBuffer(evaluatePoints, "FurtherGrass", Buffers.activeBuffers[scatter.scatterName].furtherBuffer);
        }
        Vector3d previousTerrainOffset = Vector3d.zero;
        float timeSinceLastRead = 0;
        int count = 0;
        float timeSinceLastSoftUpdate = 0;
        //void FixedUpdate()
        //{
        //    return;
        //    if (scatter.properties.subdivisionSettings.mode == SubdivisionMode.FixedRange)
        //    {
        //        float targetDeltaTime = 1.0f / softUpdateRate;
        //        float deltaTime = Time.deltaTime;
        //        if (timeSinceLastSoftUpdate >= targetDeltaTime)
        //        {
        //            timeSinceLastSoftUpdate = 0;
        //            bool isTimeForSoft = CheckTheSoftTime();
        //            if (isTimeForSoft && FlightGlobals.ActiveVessel.speed < 100f && objectCount > 0)
        //            {
        //
        //                EvaluatePositions();
        //
        //            }
        //        }
        //        else
        //        {
        //            timeSinceLastSoftUpdate += Time.deltaTime;
        //        }
        //        
        //        
        //
        //    }
        //}
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
        bool alreadyIncreasedMemory = false;
        
        public void GeneratePositions()
        {
            if (mesh == null) { return; }
            Vector3[] verts = mesh.vertices;
            int[] tris = mesh.triangles;
            Vector3[] normals = mesh.normals;

            //First we need to adjust the memory usage for the output buffer

            maxMemory = (tris.Length / 3) * (int)scatter.properties.scatterDistribution._PopulationMultiplier * quadSubdivisionDifference * scatter.properties.scatterDistribution.noise._MaxStacks;
            //if (!alreadyIncreasedMemory) { pqsMod.requiredMemory += maxMemory; }
            alreadyIncreasedMemory = true;
            //pqsMod.OnBufferLengthUpdated();

            //Now generate

            
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
            distribute.SetVector("_ShaderOffset", -((Vector3)FloatingOrigin.TerrainShaderOffset));

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
            distribute.SetFloat("seed", scatter.properties.scatterDistribution._Seed);
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

            distribute.SetFloat("rotationMult", 1);

           
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
                currentlyReadingDist = false;
                return;
            }
            currentlyReadingDist = false;

            //ComputeBuffer.CopyCount(grassPositionBuffer, indirectArgs, 0);
            pqsMod.OnForceEvaluate += DispatchEvaluate;
            pqsMod.OnBufferLengthUpdated += ReInitializeAllBuffers;
            EvaluatePositions();
        }
        bool currentlyReadingDist = false;
        //bool currentlyReadingEv = false;
        public void EvaluatePositions()
        {
            if (initialized == false) { Debug.Log("Not initialized yet..."); }
            if (currentlyReadingDist) { Debug.Log("Currently reading something"); return; }
            if (objectCount == 0) { return; }
            if (!doEvaluate) { return; }

            if (Buffers.activeBuffers[scatter.scatterName].buffer == null)
            {
                Debug.Log("Buffer null");
            }
            if (!Buffers.activeBuffers[scatter.scatterName].buffer.IsValid())             //Someone else is gonna need to help me on this. Buffers are initialized without error but fail here on some seemingly random quads
            {
                Debug.Log("Invalid, destroying");
                Destroy(this);
                return;
            }
            if (indirectArgs != null) { indirectArgs.Release(); }
            indirectArgs = new ComputeBuffer(1, sizeof(int) * 3, ComputeBufferType.IndirectArguments);
            int[] workGroups = new int[] { Mathf.CeilToInt(((float)objectCount) / 32f), 1, 1 };
            indirectArgs.SetData(workGroups);

            evaluate.SetFloat("range", scatter.properties.scatterDistribution._Range);
            evaluate.SetVector("_CameraPos", ActiveBuffers.cameraPos);// FlightGlobals.ActiveVessel.transform.position);//Camera.allCameras.FirstOrDefault(_cam => _cam.name == "Camera 00").gameObject.transform.position - gameObject.transform.position);
            evaluate.SetVector("_CraftPos", FlightGlobals.ActiveVessel.transform.position);
            if (scatter.useSurfacePos) { evaluate.SetVector("_CameraPos", ActiveBuffers.surfacePos); }

            evaluate.SetFloat("_LODPerc", scatter.properties.scatterDistribution.lods.lods[0].range / scatter.properties.scatterDistribution._Range);    //At what range does the LOD change to the low one?
            evaluate.SetFloat("_LOD2Perc", scatter.properties.scatterDistribution.lods.lods[1].range / scatter.properties.scatterDistribution._Range);

            evaluate.SetVector("_ShaderOffset", -((Vector3)FloatingOrigin.TerrainShaderOffset));
            evaluate.SetVector("_ThisPos", transform.position);
            evaluate.SetInt("_MaxCount", objectCount);  //quadsubdif?
                                                        //and V
            evaluate.SetFloat("_CurrentTime", Time.timeSinceLevelLoad);
            evaluate.SetFloat("_Pow", scatter.properties.scatterDistribution._RangePow);
            evaluate.SetFloats("_CameraFrustumPlanes", ActiveBuffers.planeNormals);             //Frustum culling
            evaluate.SetFloat("_CullLimit", scatter.cullingLimit);
            float cullingRangePerc = scatter.cullingRange / scatter.properties.scatterDistribution._Range;
            evaluate.SetFloat("_CullStartRange", cullingRangePerc);


            evaluate.DispatchIndirect(evaluatePoints, indirectArgs, 0);
            //evaluate.Dispatch(evaluatePoints, Mathf.CeilToInt(((float)objectCount) / 32f), 1, 1);

            //ComputeBuffer.CopyCount(grassBuffer, countBuffer, 0);
            //int[] count = new int[1] { 0 };
            //countBuffer.GetData(count);
            //int counter = count[0];
            //GrassData[] data = new GrassData[counter];
            //grassBuffer.GetData(data);
            //Debug.Log("Evaluate retrieved - Length of data: " + data.Length);
            //Debug.Log("Matrix from data " + data[0].mat.ToString("F3"));
            //
            //Debug.Log("Evaluate dispatched");
            //ActiveBuffers.activeBuffers[scatter.scatterName].buffer = grassBuffer;
            //ActiveBuffers.activeBuffers[scatter.scatterName].farBuffer = farGrassBuffer;
            //ActiveBuffers.activeBuffers[scatter.scatterName].furtherBuffer = furtherGrassBuffer;
            //pc.Setup(new ComputeBuffer[] { grassBuffer, farGrassBuffer, furtherGrassBuffer, subObjectSlot1, subObjectSlot2, subObjectSlot3, subObjectSlot4 }, scatter);
        }
        public void DispatchEvaluate()
        {
            if (initialized == false) { Debug.Log("Not initialized yet..."); }
            if (currentlyReadingDist) { Debug.Log("Currently reading something"); return; }
            if (objectCount == 0) { return; }
            evaluate.SetVector("_ShaderOffset", -((Vector3)FloatingOrigin.TerrainShaderOffset));
            evaluate.SetVector("_CameraPos", ActiveBuffers.cameraPos);
            evaluate.SetVector("_CraftPos", FlightGlobals.ActiveVessel.transform.position);
            evaluate.SetFloat("_CurrentTime", Time.timeSinceLevelLoad);
            if (scatter.useSurfacePos) { evaluate.SetVector("_CameraPos", ActiveBuffers.surfacePos); }
            //Debug.Log("Evaluating");
            evaluate.SetFloats("_CameraFrustumPlanes", ActiveBuffers.planeNormals);
            evaluate.DispatchIndirect(evaluatePoints, indirectArgs, 0);
        }
        //private void AwaitEvaluateReadback(AsyncGPUReadbackRequest req)
        //{
        //    if (req.hasError)
        //    {
        //        ScatterLog.Log("[Exception] Async GPU Readback error! (In EvaluatePositions())");
        //        return;
        //    }
        //    int[] count = req.GetData<int>(0).ToArray();
        //    currentlyReadingEv = false;
        //    pc.Setup(new ComputeBuffer[] { grassBuffer, farGrassBuffer, furtherGrassBuffer, subObjectSlot1, subObjectSlot2, subObjectSlot3, subObjectSlot4 }, scatter);
        //}
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
        void OnDestroy()
        {

            //Utils.ForceGPUFinish(grassBuffer, typeof(GrassData), (triCount / 3) * (int)scatter.properties.scatterDistribution._PopulationMultiplier);
            Utils.DestroyComputeBufferSafe(positionBuffer);
            Utils.DestroyComputeBufferSafe(normalBuffer);
            Utils.DestroyComputeBufferSafe(triangleBuffer);
            Utils.DestroyComputeBufferSafe(noiseBuffer);
            Utils.DestroyComputeBufferSafe(grassPositionBuffer);
            Utils.DestroyComputeBufferSafe(positionCountBuffer);
            Utils.DestroyComputeBufferSafe(indirectArgs);
        }
        bool everDisabled = false;
        void OnDisable()
        {
            //Utils.ForceGPUFinish(grassBuffer, typeof(GrassData), (triCount / 3) * (int)scatter.properties.scatterDistribution._PopulationMultiplier);
            Utils.DestroyComputeBufferSafe(positionBuffer);
            Utils.DestroyComputeBufferSafe(normalBuffer);
            Utils.DestroyComputeBufferSafe(triangleBuffer);
            Utils.DestroyComputeBufferSafe(noiseBuffer);
            Utils.DestroyComputeBufferSafe(grassPositionBuffer);
            Utils.DestroyComputeBufferSafe(positionCountBuffer);
            Utils.DestroyComputeBufferSafe(indirectArgs);
            //PQSMod_ScatterManager pqsMod = ActiveBuffers.mods.Find(x => x.scatterName == scatter.scatterName);  //Get corresponding mod here
            pqsMod.OnForceEvaluate -= DispatchEvaluate;
            pqsMod.OnBufferLengthUpdated -= ReInitializeAllBuffers;
            //pqsMod.requiredMemory -= maxMemory;
            GameEvents.OnCameraChange.Remove(OnCameraChange);
        }
    }
    
}