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

namespace ComputeLoader
{

    public class ComputeComponent : MonoBehaviour
    {
        // Start is called before the first frame update
        public ComputeShader distribute;
        public ComputeShader evaluate;

        public ComputeBuffer positionBuffer;
        public ComputeBuffer grassPositionBuffer;
        public ComputeBuffer triangleBuffer;
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

        private int evaluatePoints;

        public float updateFPS; //1.0f;

        public int subObjectCount = 0;
        public int quadSubdivisionDifference = 1;  //Using this, increase population as quad subdivision is reduced to balance out
        int objectCount = 0;

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
        IEnumerator UpdatePositionFPS()
        {
            yield return new WaitForSeconds(0.5f);
            float distance = Vector3.Distance(gameObject.transform.position, FlightGlobals.ActiveVessel.transform.position);
            if (distance > scatter.properties.scatterDistribution._Range * 2)
            {
                StartCoroutine(UpdatePositionFPS());
                yield break;
            }
            evaluate.SetVector("_CameraPos", FlightGlobals.ActiveVessel.transform.position);//Camera.allCameras.FirstOrDefault(_cam => _cam.name == "Camera 00").gameObject.transform.position - gameObject.transform.position);

            EvaluatePositions();
            StartCoroutine(UpdatePositionFPS());
        }
        public void Start()
        {
            //mesh = Instantiate(gameObject.GetComponent<MeshFilter>().mesh);
            vertCount = mesh.vertexCount;
            triCount = mesh.triangles.Length;
            distribute = Instantiate(ScatterShaderHolder.GetCompute("DistributePoints"));
            evaluate = Instantiate(ScatterShaderHolder.GetCompute("EvaluatePoints"));
            GeneratePositions();
            InitializeAllBuffers();
            EvaluatePositions();
            //StartCoroutine(UpdatePositionFPS());
        }
        public void InitializeAllBuffers()
        {
            Debug.Log("Initializing for " + scatter.scatterName);
            evaluatePoints = evaluate.FindKernel("EvaluatePoints");

            Utils.SafetyCheckRelease(countBuffer, "countBuffer");
            Utils.SafetyCheckRelease(grassBuffer, "grassBuffer");
            Utils.SafetyCheckRelease(farGrassBuffer, "farGrassBuffer");
            Utils.SafetyCheckRelease(furtherGrassBuffer, "furtherGrassBuffer");
            Utils.SafetyCheckRelease(subObjectSlot1, "subObjectSlot1");
            Utils.SafetyCheckRelease(subObjectSlot2, "subObjectSlot2");
            Utils.SafetyCheckRelease(subObjectSlot3, "subObjectSlot3");
            Utils.SafetyCheckRelease(subObjectSlot4, "subObjectSlot4");


            countBuffer = Utils.SetupComputeBufferSafe(7, sizeof(int), ComputeBufferType.IndirectArguments);
            grassBuffer = Utils.SetupComputeBufferSafe((triCount / 3) * (int)scatter.properties.scatterDistribution._PopulationMultiplier * quadSubdivisionDifference, GrassData.Size(), ComputeBufferType.Append);
            farGrassBuffer = Utils.SetupComputeBufferSafe((triCount / 3) * (int)scatter.properties.scatterDistribution._PopulationMultiplier * quadSubdivisionDifference, GrassData.Size(), ComputeBufferType.Append);
            furtherGrassBuffer = Utils.SetupComputeBufferSafe((triCount / 3) * (int)scatter.properties.scatterDistribution._PopulationMultiplier * quadSubdivisionDifference, GrassData.Size(), ComputeBufferType.Append);


            subObjectSlot1 = Utils.SetupComputeBufferSafe((triCount / 3) * (int)scatter.properties.scatterDistribution._PopulationMultiplier, GrassData.Size(), ComputeBufferType.Append);
            subObjectSlot2 = Utils.SetupComputeBufferSafe((triCount / 3) * (int)scatter.properties.scatterDistribution._PopulationMultiplier, GrassData.Size(), ComputeBufferType.Append);
            subObjectSlot3 = Utils.SetupComputeBufferSafe((triCount / 3) * (int)scatter.properties.scatterDistribution._PopulationMultiplier, GrassData.Size(), ComputeBufferType.Append);
            subObjectSlot4 = Utils.SetupComputeBufferSafe((triCount / 3) * (int)scatter.properties.scatterDistribution._PopulationMultiplier, GrassData.Size(), ComputeBufferType.Append);

            evaluate.SetBuffer(evaluatePoints, "Grass", grassBuffer);
            evaluate.SetBuffer(evaluatePoints, "Positions", grassPositionBuffer);
            evaluate.SetBuffer(evaluatePoints, "FarGrass", farGrassBuffer);
            evaluate.SetBuffer(evaluatePoints, "FurtherGrass", furtherGrassBuffer);
            evaluate.SetBuffer(evaluatePoints, "SubObjects1", subObjectSlot1);
            evaluate.SetBuffer(evaluatePoints, "SubObjects2", subObjectSlot2);
            evaluate.SetBuffer(evaluatePoints, "SubObjects3", subObjectSlot3);
            evaluate.SetBuffer(evaluatePoints, "SubObjects4", subObjectSlot4);
        }
        Vector3d previousTerrainOffset = Vector3d.zero;
        void Update()
        {
            //if (FloatingOrigin.TerrainShaderOffset != previousTerrainOffset)
            //{
            //    EvaluatePositions();
            //    timeSinceLastUpdate = 0;
            //    previousTerrainOffset = FloatingOrigin.TerrainShaderOffset;
            //}
            bool isTime = CheckTheTime(scatter.updateFPS);
            if (isTime && FlightGlobals.ActiveVessel.speed > 0.05f)
            {
                
                EvaluatePositions();
            }
            
        }
        float timeSinceLastSoftUpdate = 0;
        void FixedUpdate()
        {
            
            if (scatter.properties.subdivisionSettings.mode == SubdivisionMode.FixedRange)
            {
                float targetDeltaTime = 1.0f / softUpdateRate;
                float deltaTime = Time.deltaTime;
                if (timeSinceLastSoftUpdate >= targetDeltaTime)
                {
                    timeSinceLastSoftUpdate = 0;
                    bool isTimeForSoft = CheckTheSoftTime();
                    if (isTimeForSoft && FlightGlobals.ActiveVessel.speed < 100f)
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
                ScatterLog.Log("Consider enabling variable framerate to lower the update rate as your FPS falls");
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
            //Debug.Log(distanceSinceLastUpdate);
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
            Utils.SafetyCheckRelease(positionCountBuffer, "mesh triangle buffer");
            positionBuffer = Utils.SetupComputeBufferSafe(vertCount, 12, ComputeBufferType.Structured);
            normalBuffer = Utils.SetupComputeBufferSafe(normals.Length, 12, ComputeBufferType.Structured);
            grassPositionBuffer = Utils.SetupComputeBufferSafe((tris.Length / 3) * (int)scatter.properties.scatterDistribution._PopulationMultiplier * quadSubdivisionDifference, PositionData.Size(), ComputeBufferType.Append);
            triangleBuffer = Utils.SetupComputeBufferSafe(tris.Length, sizeof(int), ComputeBufferType.Structured);                  //quad subdiv diff ^
            positionCountBuffer = Utils.SetupComputeBufferSafe(1, sizeof(int), ComputeBufferType.IndirectArguments);

            positionBuffer.SetData(verts);
            triangleBuffer.SetData(tris);
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
            distribute.SetVector("grassColorMain", scatter.properties.scatterMaterial._MainColor);
            distribute.SetVector("grassColorSub", scatter.properties.scatterMaterial._SubColor);
            distribute.SetFloat("grassColorNoiseStrength", scatter.properties.scatterMaterial._ColorNoiseStrength);
            distribute.SetFloat("grassColorNoiseScale", scatter.properties.scatterMaterial._ColorNoiseScale);
            distribute.SetFloat("grassCutoffScale", scatter.properties.scatterDistribution._CutoffScale);
            distribute.SetFloat("grassSizeNoiseScale", scatter.properties.scatterDistribution._SizeNoiseScale);
            distribute.SetFloat("grassSizeNoiseStrength", scatter.properties.scatterDistribution._SizeNoiseStrength);
            distribute.SetFloat("grassSizeNoiseOffset", scatter.properties.scatterDistribution._SizeNoiseOffset.x);
            distribute.SetFloat("_SteepPower", scatter.properties.scatterDistribution._SteepPower);
            distribute.SetFloat("_SteepContrast", scatter.properties.scatterDistribution._SteepContrast);
            distribute.SetFloat("_SteepMidpoint", scatter.properties.scatterDistribution._SteepMidpoint);
            distribute.SetFloat("subObjectWeight1", GetSubObjectProperty("subObjectWeight", 0));
            distribute.SetFloat("subObjectNoiseScale1", GetSubObjectProperty("subObjectNoiseScale", 0));
            distribute.SetFloat("subObjectSpawnChance1", GetSubObjectProperty("subObjectSpawnChance", 0));

            distribute.SetFloat("subObjectWeight2", GetSubObjectProperty("subObjectWeight", 1));
            distribute.SetFloat("subObjectNoiseScale2", GetSubObjectProperty("subObjectNoiseScale", 1));
            distribute.SetFloat("subObjectSpawnChance2", GetSubObjectProperty("subObjectSpawnChance", 1));

            distribute.SetFloat("subObjectWeight3", GetSubObjectProperty("subObjectWeight", 2));
            distribute.SetFloat("subObjectNoiseScale3", GetSubObjectProperty("subObjectNoiseScale", 2));
            distribute.SetFloat("subObjectSpawnChance3", GetSubObjectProperty("subObjectSpawnChance", 2));

            distribute.SetFloat("subObjectWeight4", GetSubObjectProperty("subObjectWeight", 3));
            distribute.SetFloat("subObjectNoiseScale4", GetSubObjectProperty("subObjectNoiseScale", 3));
            distribute.SetFloat("subObjectSpawnChance4", GetSubObjectProperty("subObjectSpawnChance", 3));

            distribute.SetFloat("spawnChance", scatter.properties.scatterDistribution._SpawnChance);

            distribute.SetBuffer(distributeKernel, "Objects", positionBuffer);
            distribute.SetBuffer(distributeKernel, "Tris", triangleBuffer);
            distribute.SetBuffer(distributeKernel, "Positions", grassPositionBuffer);
            distribute.SetBuffer(distributeKernel, "Normals", normalBuffer);
            


            distribute.Dispatch(distributeKernel, Mathf.CeilToInt((((float)tris.Length * (float)quadSubdivisionDifference) / 3f) / 32f), 1, 1);
            ComputeBuffer.CopyCount(grassPositionBuffer, positionCountBuffer, 0);
            int[] count = new int[] { 0 };
            positionCountBuffer.GetData(count);
            objectCount = count[0];
            //EvaluatePositions();

        }
        public void EvaluatePositions()
        {
            //ReleaseEvaluationBuffers();
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
            evaluate.SetInt("_MaxCount", objectCount * quadSubdivisionDifference);  //quadsubdif?
                                                        //and V
            evaluate.Dispatch(evaluatePoints, Mathf.CeilToInt(((float)objectCount) / 32f) * quadSubdivisionDifference, 1, 1);

            ComputeBuffer.CopyCount(grassBuffer, countBuffer, 0);
            ComputeBuffer.CopyCount(farGrassBuffer, countBuffer, 4);
            ComputeBuffer.CopyCount(furtherGrassBuffer, countBuffer, 8);
            ComputeBuffer.CopyCount(subObjectSlot1, countBuffer, 12);
            ComputeBuffer.CopyCount(subObjectSlot2, countBuffer, 16);
            ComputeBuffer.CopyCount(subObjectSlot3, countBuffer, 20);
            ComputeBuffer.CopyCount(subObjectSlot4, countBuffer, 24);
            int[] count = new int[] { 0, 0, 0, 0, 0, 0, 0 };
            countBuffer.GetData(count);
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
                if (property == "subObjectWeight")
                {
                    Debug.Log(property + " = " + scatter.subObjects[index].properties._NoiseAmount);
                    return scatter.subObjects[index].properties._NoiseAmount;
                }
                else if (property == "subObjectNoiseScale")
                {
                    Debug.Log(property + " = " + scatter.subObjects[index].properties._NoiseScale);
                    return scatter.subObjects[index].properties._NoiseScale;
                }
                else if (property == "subObjectSpawnChance")
                {
                    Debug.Log(property + " = " + scatter.subObjects[index].properties._Density);
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
        void OnDestroy()
        {

            Utils.ForceGPUFinish(grassBuffer, typeof(GrassData), (triCount / 3) * (int)scatter.properties.scatterDistribution._PopulationMultiplier);
            Utils.DestroyComputeBufferSafe(positionBuffer);
            Utils.DestroyComputeBufferSafe(normalBuffer);
            Utils.DestroyComputeBufferSafe(triangleBuffer);
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
            Debug.Log("Compute component DESTROYED");
        }
        void OnDisable()
        {
            Utils.ForceGPUFinish(grassBuffer, typeof(GrassData), (triCount / 3) * (int)scatter.properties.scatterDistribution._PopulationMultiplier);
            Utils.DestroyComputeBufferSafe(positionBuffer);
            Utils.DestroyComputeBufferSafe(normalBuffer);
            Utils.DestroyComputeBufferSafe(triangleBuffer);
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
            Debug.Log("Compute component DISABLED");
        }
    }

    [KSPAddon(KSPAddon.Startup.FlightAndKSC, false)]
    public class ShadowFixer : MonoBehaviour
    {
        GameObject go;
        GameObject sphere;
        public void Start()
        {
            QualitySettings.shadowDistance = 10000;
            QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
            QualitySettings.shadowProjection = ShadowProjection.StableFit;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowCascade4Split = new Vector3(0.002f, 0.022f, 0.178f);
            Camera.main.nearClipPlane = 0.1f;
            Camera.current.nearClipPlane = 0.1f;
           
            if (HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                //go = GameObject.CreatePrimitive(PrimitiveType.Plane);
                //go.transform.localScale = new Vector3(250, 250, 250);
                //go.GetComponent<MeshRenderer>().material = new Material(ScatterShaderHolder.GetShader("Custom/NoisePosTest"));
                //
                //sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                //sphere.transform.localScale = new Vector3(25, 25, 25);
                //sphere.GetComponent<MeshRenderer>().material = new Material(ScatterShaderHolder.GetShader("Custom/NoisePosTest"));
                //
                //
                //Destroy(go.GetComponent<Collider>());
                //Destroy(sphere.GetComponent<Collider>());
            }
            

        }
        void Update()
        {
            
            if (HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                //Vector3 a = LatLon.GetRelSurfacePosition(FlightGlobals.currentMainBody.BodyFrame, FlightGlobals.currentMainBody.transform.position, FlightGlobals.ActiveVessel.transform.position);
                //Vector3 b = LatLon.GetWorldSurfacePosition(FlightGlobals.currentMainBody.BodyFrame, FlightGlobals.currentMainBody.transform.position, FlightGlobals.currentMainBody.Radius, LatLon.GetLatitude(FlightGlobals.currentMainBody.BodyFrame, FlightGlobals.currentMainBody.transform.position, FlightGlobals.ActiveVessel.transform.position), LatLon.GetLongitude(FlightGlobals.currentMainBody.BodyFrame, FlightGlobals.currentMainBody.transform.position, FlightGlobals.ActiveVessel.transform.position), FlightGlobals.ActiveVessel.altitude);
                //Debug.Log("Relative planet position is " + a.ToString("F3"));
                //Debug.Log(" - - - - World Surface planet position is " + a.ToString("F3"));
                //go.transform.position = FlightGlobals.ActiveVessel.transform.position + Vector3.Normalize(FlightGlobals.ActiveVessel.transform.position - FlightGlobals.currentMainBody.transform.position);
                //go.GetComponent<MeshRenderer>().material.SetVector("_ShaderOffset", -(Vector3)FloatingOrigin.TerrainShaderOffset);
                //go.transform.up = Vector3.Normalize(FlightGlobals.ActiveVessel.transform.position - FlightGlobals.currentMainBody.transform.position);
                //
                //sphere.transform.position = -(Vector3)FloatingOrigin.TerrainShaderOffset;
            }
            

            QualitySettings.shadowDistance = 10000;
            QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
            QualitySettings.shadowProjection = ShadowProjection.StableFit;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowCascade4Split = new Vector3(0.002f, 0.022f, 0.178f);
            Camera.main.nearClipPlane = 0.1f;
            Camera.current.nearClipPlane = 0.1f;
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

        public Properties scatterProps;
        public void SetupAgain(Scatter scatter)
        {
            material = new Material(scatter.properties.scatterMaterial.shader);

            //material.SetFloat("_WaveSpeed", 0);
            //material.SetFloat("_HeightCutoff", -1000);
            material = Utils.SetShaderProperties(material, scatter.properties.scatterMaterial);
            scatterProps = scatter.properties; //ScatterBodies.scatterBodies[FlightGlobals.currentMainBody.name].scatters["Grass"].properties;


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
                Debug.Log("Using model: " + scatter.model);
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

                //material.SetFloat("_WaveSpeed", 0);
                //material.SetFloat("_HeightCutoff", -1000);
                material = Utils.SetShaderProperties(material, scatter.properties.scatterMaterial);
                scatterProps = scatter.properties; //ScatterBodies.scatterBodies[FlightGlobals.currentMainBody.name].scatters["Grass"].properties;

                
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
                //materialFar.SetFloat("_Cutoff", 0.5f);
                //materialFar.SetTexture("_MainTex", Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(t => t.name == "Parallax_StockTextures/_Scatters/grassuv5"));

                subdivisionRange = (int)(((2 * Mathf.PI * FlightGlobals.currentMainBody.Radius) / 4) / (Mathf.Pow(2, FlightGlobals.currentMainBody.pqsController.maxLevel)));

                vertexCount = mesh.vertexCount;
                farVertexCount = farMesh.vertexCount;
                furtherVertexCount = furtherMesh.vertexCount;

                setupInitial = true;
            }
            
            bounds = new Bounds(transform.position, Vector3.one * (subdivisionRange + 1));
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
                Graphics.DrawMeshInstancedIndirect(farMesh, 0, materialFar, bounds, farArgsBuffer);
            }
            if (furtherCountCheck != 0)
            {
                Graphics.DrawMeshInstancedIndirect(furtherMesh, 0, materialFurther, bounds, furtherArgsBuffer);
            }
            if (subCount1 != 0)
            {
                Graphics.DrawMeshInstancedIndirect(subObjectMesh1, 0, subObjectMat1, bounds, subArgs1);
            }
            if (subCount2 != 0)
            {
                Graphics.DrawMeshInstancedIndirect(subObjectMesh2, 0, subObjectMat2, bounds, subArgs2);
            }
            if (subCount3 != 0)
            {
                Graphics.DrawMeshInstancedIndirect(subObjectMesh3, 0, subObjectMat3, bounds, subArgs3);
            }
            if (subCount4 != 0)
            {
                Graphics.DrawMeshInstancedIndirect(subObjectMesh4, 0, subObjectMat4, bounds, subArgs4);
            }
            if (countCheck != 0)
            {
                Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer);
            }
        }
        private void SetPlanetOrigin()
        {
            Vector3 planetOrigin = FlightGlobals.currentMainBody.transform.position;//Vector3.zero;
            if (material != null && material.HasProperty("_PlanetOrigin"))
            {
                //Debug.Log("Updated near offset: " + floatingOffset.ToString("F3"));
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
            Debug.Log("PostCompute DESTROYED");
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
            Debug.Log("PostCompute DISABLED");
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
    public class QuadMeshes : MonoBehaviour
    {
        public PQ quad;
        public Material oldMaterial;
        bool alreadySubdivided = false;
        Material transparent = new Material(Shader.Find("Unlit/Transparent"));
        GameObject newQuad;
        public ScatterBody body;
        bool wasEverSubdivided = false;
        public ComputeComponent[] comps;
        public PostCompute[] postComps;

        void Start()
        {
            comps = new ComputeComponent[body.scatters.Values.Count];
            postComps = new PostCompute[body.scatters.Values.Count];
            alreadyAdded = new bool[body.scatters.Values.Count];
            InvokeRepeating("CheckRange", 1f, 1f);
            InvokeRepeating("CheckFixedRange", 1f, 1f);
        }
        void CheckRange()
        {
            if (quad.subdivision != FlightGlobals.currentMainBody.pqsController.maxLevel)
            {
                return;
            }
            //yield return new WaitForSeconds(1);
            float distance = Vector3.Distance(FlightGlobals.ActiveVessel.transform.position, quad.transform.position);
            float limit = (int)(((2 * Mathf.PI * FlightGlobals.currentMainBody.Radius) / 4) / (Mathf.Pow(2, FlightGlobals.currentMainBody.pqsController.maxLevel)));
            Vector3 planetNormal = Vector3.Normalize(FlightGlobals.ActiveVessel.transform.position - FlightGlobals.currentMainBody.transform.position);
            if (distance < limit && alreadySubdivided == false && quad != null)
            {
                var quadMeshFilter = quad.GetComponent<MeshFilter>();
                var quadMeshRenderer = quad.GetComponent<MeshRenderer>();
                
                newQuad = new GameObject();
                newQuad.name = quad.name + "-Fake";
                newQuad.transform.position = quad.gameObject.transform.position;
                newQuad.transform.rotation = quad.gameObject.transform.rotation;
                newQuad.transform.parent = quad.gameObject.transform;
                newQuad.transform.localPosition = Vector3.zero;
                newQuad.transform.localRotation = Quaternion.identity;
                newQuad.transform.localScale = Vector3.one;
                newQuad.transform.parent = quad.gameObject.transform;
                newQuad.layer = quad.gameObject.layer;
                newQuad.SetActive(true);
                
                oldMaterial = quad.GetComponent<MeshRenderer>().sharedMaterial;
                Mesh mesh = Instantiate(quadMeshFilter.sharedMesh);
                //Mesh mesh = GameDatabase.Instance.GetModel("Parallax_StockTextures/_Scatters/Models/tallgrass").GetComponent<MeshFilter>().mesh;
                MeshHelper.Subdivide(mesh, 6);
                var newQuadMeshFilter = newQuad.AddComponent<MeshFilter>();
                newQuadMeshFilter.sharedMesh = mesh;
                var newQuadMeshRenderer = newQuad.AddComponent<MeshRenderer>();
                newQuadMeshRenderer.sharedMaterial = Instantiate(oldMaterial);//new Material(ScatterShaderHolder.GetShader("Custom/Wireframe"));//quadMeshRenderer.sharedMaterial;
                newQuadMeshRenderer.enabled = false;

                quadMeshRenderer.enabled = true;
                //quadMeshRenderer.material = transparent;
                //quadMeshRenderer.material.SetTexture("_MainTex", Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(t => t.name == "Parallax/BlankAlpha"));
                alreadySubdivided = true;
                wasEverSubdivided = true;
                string[] keys = body.scatters.Keys.ToArray();

                for (int i = 0; i < body.scatters.Count; i++)
                {
                    Scatter thisScatter = body.scatters[keys[i]];
                    if (thisScatter.properties.subdivisionSettings.mode == SubdivisionMode.NearestQuads)
                    {
                        ComputeComponent comp = newQuad.gameObject.AddComponent<ComputeComponent>();
                        comp.mesh = mesh;
                        comp.subObjectCount = thisScatter.subObjectCount;
                        comp.scatter = thisScatter;
                        PostCompute postComp = newQuad.gameObject.AddComponent<PostCompute>();
                        comp.pc = postComp;
                    }
                }
            }
            else if (distance >= limit && quad != null && quad.subdivision == FlightGlobals.currentMainBody.pqsController.maxLevel)
            {
                if (wasEverSubdivided)
                {
                    Debug.Log("Expecting destroy messages:");
                    ComputeComponent comp = null;
                    PostCompute postComp = null;
                    if (newQuad != null)
                    {
                        comp = newQuad.GetComponent<ComputeComponent>();
                        postComp = newQuad.GetComponent<PostCompute>();
                    }
                        
                    if (comp != null)
                    {
                        Destroy(comp);
                    }
                    if (postComp != null)
                    {
                        Destroy(postComp);
                    }
                    Debug.Log("Shoud be here ^");
                    Destroy(newQuad);
                    var quadMeshRenderer = quad.GetComponent<MeshRenderer>();
                    if (oldMaterial != null)
                    {
                        quadMeshRenderer.sharedMaterial = FlightGlobals.currentMainBody.pqsController.surfaceMaterial;
                        quadMeshRenderer.enabled = true;
                        Debug.Log("Swapped out old material");
                    }
                    else
                    {
                        Debug.Log("Old material is null");
                    }
                    alreadySubdivided = false;
                    wasEverSubdivided = false;
                }
                
                
                
            }
        }
        bool[] alreadyAdded;
        void CheckFixedRange()
        {
            

            int maxLevel = FlightGlobals.currentMainBody.pqsController.maxLevel;
            int maxLevelDiff = maxLevel - quad.subdivision;
            string[] keys = body.scatters.Keys.ToArray();
            for (int i = 0; i < body.scatters.Count; i++)
            {
                Scatter thisScatter = body.scatters[keys[i]];
                if (thisScatter.properties.subdivisionSettings.mode == SubdivisionMode.FixedRange)
                {
                    float distance = Vector3.Distance(FlightGlobals.ActiveVessel.transform.position, quad.transform.position);
                    float limit = thisScatter.properties.subdivisionSettings.range;
                    Vector3 planetNormal = Vector3.Normalize(FlightGlobals.ActiveVessel.transform.position - FlightGlobals.currentMainBody.transform.position);
                    if (distance < limit && alreadyAdded[i] == false && quad != null)
                    {
                        var quadMeshFilter = quad.GetComponent<MeshFilter>();
                        var quadMeshRenderer = quad.GetComponent<MeshRenderer>();
                        Mesh mesh = Instantiate(quadMeshFilter.sharedMesh);
                        ComputeComponent comp = quad.gameObject.AddComponent<ComputeComponent>();
                        comp.mesh = mesh;
                        comp.subObjectCount = thisScatter.subObjectCount;
                        comp.scatter = thisScatter;
                        comp.quadSubdivisionDifference = (maxLevelDiff * 2) + 1;
                        PostCompute postComp = quad.gameObject.AddComponent<PostCompute>();
                        comp.pc = postComp;

                        comps[i] = comp;
                        postComps[i] = postComp;

                        Debug.Log("Added " + thisScatter.scatterName);
                        //quadMeshRenderer.enabled = false;
                        alreadyAdded[i] = true;
                    }
                    else if (distance > limit && quad != null)
                    {
                        ComputeComponent comp = comps[i];// quad.gameObject.GetComponent<ComputeComponent>();
                        PostCompute postComp = postComps[i];// quad.gameObject.GetComponent<PostCompute>();
                        if (comp != null)
                        {
                            Destroy(comp);
                        }
                        if (postComp != null)
                        {
                            Destroy(postComp);
                        }
                        alreadyAdded[i] = false;
                    }
                }
            }

            
        }
        void DetermineDistanceLimit()
        {

        }
        void OnDestroy()
        {
            if (newQuad != null)
            {
                Destroy(newQuad);
                Destroy(transparent);
                var computeComp = quad.gameObject.GetComponent<ComputeComponent>();
                var postComp = quad.gameObject.GetComponent<PostCompute>();
                if (computeComp != null)
                {
                    Destroy(computeComp);
                }
                if (postComp != null)
                {
                    Destroy(postComp);
                }
            }
            
        }
    }
}