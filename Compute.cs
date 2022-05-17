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
using System;

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
            public static int Size()
            {
                return
                    sizeof(float) * 4 * 4 + // matrix;
                    sizeof(float) * 4;     // color
            }
        }
        public void OnCameraChange(CameraManager.CameraMode mode)
        {
            if (HighLogic.LoadedScene != GameScenes.FLIGHT) { return; }
            if (!started) { return; }
            if (FlightGlobals.currentMainBody == null) { return; }
            FloatingOrigin.ResetTerrainShaderOffset();
            GeneratePositions();    //Must regenerate based on terrain shader offset, which is reset for some reason

        }
        public void OnEnable()
        {
            
            
        }
        public void Start()
        {
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

            RealStart();
        }
        void RealStart()
        {
            pqsMod.OnForceEvaluate += DispatchEvaluate;
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
            
            GameEvents.OnCameraChange.Add(OnCameraChange);
            GeneratePositions();
            started = true;
        }
        bool initialized = false;
        public void InitializeAllBuffers()
        {
            Debug.Log("Initializing all buffers for " + scatter.scatterName);
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
        public void GeneratePositions()
        {
            if (mesh == null) { Destroy(this); return; }
            Vector3[] verts = mesh.vertices;
            int[] tris = mesh.triangles;
            Vector3[] normals = mesh.normals;
            //First we need to adjust the memory usage for the output buffer

            //maxMemory = (tris.Length / 3) * (int)scatter.properties.scatterDistribution._PopulationMultiplier * quadSubdivisionDifference * scatter.properties.scatterDistribution.noise._MaxStacks;

            //We need a latitude and longitude multiplier to make sure densities are correctly adjusted
            

            Utils.SafetyCheckDispose(positionBuffer, "position buffer");
            Utils.SafetyCheckDispose(grassPositionBuffer, "grass position buffer");
            Utils.SafetyCheckDispose(normalBuffer, "normal buffer");
            Utils.SafetyCheckDispose(triangleBuffer, "mesh triangle buffer");
            Utils.SafetyCheckDispose(noiseBuffer, "mesh noise buffer");
            Utils.SafetyCheckDispose(positionCountBuffer, "mesh triangle buffer");
            
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
            distribute.SetVector("_PlanetNormal", Vector3.Normalize(GlobalPoint.originPoint - FlightGlobals.currentMainBody.transform.position));
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
            distribute.SetFloat("rotationMult", 1);
            distribute.SetFloat("_MaxNormalDeviance", scatter.properties.scatterDistribution._MaxNormalDeviance);
            distribute.SetFloat("_PlanetRadius", (float)FlightGlobals.currentMainBody.Radius);
            distribute.SetVector("_PlanetRelative", Utils.initialPlanetRelative);
            distribute.SetMatrix("_WorldToPlanet", FlightGlobals.currentMainBody.gameObject.transform.worldToLocalMatrix);
            distribute.SetFloat("spawnChance", scatter.properties.scatterDistribution._SpawnChance);

            Vector2d latlon = LatLon.GetLatitudeAndLongitude(FlightGlobals.currentMainBody.BodyFrame, FlightGlobals.currentMainBody.transform.position, transform.position);
            double lat = Math.Abs(latlon.x) % 45.0 - 22.5;
            double lon = Math.Abs(latlon.y) % 45.0 - 22.5;   //From -22.5 to 22.5 where 0 we want the highest density and -22.5 we want 1/3 density
            lat /= 22.5;
            lon /= 22.5;    //Now from -1 to 1, with 0 being where we want most density and -1 where we want 1/3

            lat = Math.Abs(lat);
            lon = Math.Abs(lon);    //Now from 0 to 1. 1 when at a corner

            double factor = (lat + lon) / 2;
            float multiplier = Mathf.Clamp01(Mathf.Lerp(1.0f, 0.333333f, Mathf.Pow((float)factor, 3)));
            if (scatter.properties.scatterDistribution._PopulationMultiplier > 2 && multiplier < 0.65f)
            {
                //scatter.properties.scatterDistribution._PopulationMultiplier = (int)(scatter.properties.scatterDistribution._PopulationMultiplier * factor);
                distribute.SetInt("_PopulationMultiplier", (int)(Mathf.Round((scatter.properties.scatterDistribution._PopulationMultiplier * quadSubdivisionDifference) * multiplier)));
            }
            if (scatter.properties.scatterDistribution._PopulationMultiplier < 3 && multiplier < 0.65f)
            {
                distribute.SetFloat("spawnChance", scatter.properties.scatterDistribution._SpawnChance * multiplier);
            }


            
            distribute.SetVector("_PlanetRelative", Utils.initialPlanetRelative);
            if (scatter.alignToTerrainNormal) { distribute.SetInt("_AlignToNormal", 1); } else { distribute.SetInt("_AlignToNormal", 0); }
            distribute.SetBuffer(distributeKernel, "Objects", positionBuffer);
            distribute.SetBuffer(distributeKernel, "Tris", triangleBuffer);
            distribute.SetBuffer(distributeKernel, "Noise", noiseBuffer);
            distribute.SetBuffer(distributeKernel, "Positions", grassPositionBuffer);
            distribute.SetBuffer(distributeKernel, "Normals", normalBuffer);
            distribute.Dispatch(distributeKernel, Mathf.CeilToInt((((float)tris.Length) / 3f) / 32f), scatter.properties.scatterDistribution.noise._MaxStacks, 1);
            ComputeBuffer.CopyCount(grassPositionBuffer, positionCountBuffer, 0);
            AsyncGPUReadback.Request(positionCountBuffer, AwaitDistributeReadback);
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
            InitializeAllBuffers();
            if (objectCount == 0)
            {
                currentlyReadingDist = false;
                return;
            }
            currentlyReadingDist = false;

            //ComputeBuffer.CopyCount(grassPositionBuffer, indirectArgs, 0);
            
            
            EvaluatePositions();
        }
        bool currentlyReadingDist = false;
        //bool currentlyReadingEv = false;
        public void EvaluatePositions()
        {
            if (initialized == false) { return; }
            if (currentlyReadingDist) { return; }
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
            evaluate.SetVector("_CameraPos", ActiveBuffers.cameraPos);// GlobalPoint.originPoint);//Camera.allCameras.FirstOrDefault(_cam => _cam.name == "Camera 00").gameObject.transform.position - gameObject.transform.position);
            evaluate.SetVector("_CraftPos", GlobalPoint.originPoint);
            if (scatter.useSurfacePos) { evaluate.SetVector("_CameraPos", ActiveBuffers.surfacePos); }

            evaluate.SetFloat("_LODPerc", scatter.properties.scatterDistribution.lods.lods[0].range / scatter.properties.scatterDistribution._Range);    //At what range does the LOD change to the low one?
            evaluate.SetFloat("_LOD2Perc", scatter.properties.scatterDistribution.lods.lods[1].range / scatter.properties.scatterDistribution._Range);

            evaluate.SetVector("_ShaderOffset", -((Vector3)FloatingOrigin.TerrainShaderOffset));
            evaluate.SetVector("_ThisPos", transform.position);
            evaluate.SetInt("_MaxCount", objectCount);
                                                       
            evaluate.SetFloat("_CurrentTime", Time.timeSinceLevelLoad);
            evaluate.SetFloat("_Pow", scatter.properties.scatterDistribution._RangePow);
            evaluate.SetFloats("_CameraFrustumPlanes", ActiveBuffers.planeNormals);             //Frustum culling
            evaluate.SetFloat("_CullLimit", scatter.cullingLimit);
            float cullingRangePerc = scatter.cullingRange / scatter.properties.scatterDistribution._Range;
            
            evaluate.SetFloat("_CullStartRange", cullingRangePerc);
            if (!ScatterGlobalSettings.frustumCull)
            {
                evaluate.SetFloat("_CullStartRange", 1);
            }

            evaluate.DispatchIndirect(evaluatePoints, indirectArgs, 0);
        }
        public void DispatchEvaluate()
        {
            if (initialized == false) { return; }
            if (currentlyReadingDist) { return; }
            if (objectCount == 0) { return; }
            if (!doEvaluate) { return; }
            evaluate.SetVector("_ShaderOffset", -((Vector3)FloatingOrigin.TerrainShaderOffset));
            evaluate.SetVector("_CameraPos", ActiveBuffers.cameraPos);
            evaluate.SetVector("_CraftPos", GlobalPoint.originPoint);
            evaluate.SetFloat("_CurrentTime", Time.timeSinceLevelLoad);
            if (scatter.useSurfacePos) { evaluate.SetVector("_CameraPos", ActiveBuffers.surfacePos); }
            evaluate.SetFloats("_CameraFrustumPlanes", ActiveBuffers.planeNormals);
            evaluate.DispatchIndirect(evaluatePoints, indirectArgs, 0);
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
            //pqsMod.requiredMemory -= maxMemory;
            GameEvents.OnCameraChange.Remove(OnCameraChange);
        }
    }

}