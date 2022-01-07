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

        public ComputeBuffer grassBuffer;
        public ComputeBuffer farGrassBuffer;    //For the grass further away from the camera - Much lower LOD
        public ComputeBuffer subObjectSlot1;
        public ComputeBuffer subObjectSlot2;
        public ComputeBuffer subObjectSlot3;
        public ComputeBuffer subObjectSlot4;

        public PostCompute pc;

        public Mesh mesh;
        public int vertCount;
        public int triCount;

        public Vector3 _PlanetOrigin;

        public Scatter scatter;

        private int evaluatePoints;

        public float updateFPS = 1.0f;

        public int subObjectCount = 0;

        struct PositionData
        {
            public Vector3 pos;
            public Matrix4x4 mat;
            public static int Size()
            {
                return
                    sizeof(float) * 4 * 4 + // matrix;
                    sizeof(float) * 3;     // color
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
            pc = gameObject.GetComponent<PostCompute>();
            scatter = ScatterBodies.scatterBodies[FlightGlobals.currentMainBody.name].scatters["Grass"];
            distribute = Instantiate(ScatterShaderHolder.GetCompute("DistributePoints"));
            evaluate = Instantiate(ScatterShaderHolder.GetCompute("EvaluatePoints"));
            Debug.Log("Mesh count: " + vertCount + " / " + triCount);
            GeneratePositions();
            InitializeAllBuffers();
            
            //StartCoroutine(UpdatePositionFPS());
        }
        public void InitializeAllBuffers()
        {
            evaluatePoints = evaluate.FindKernel("EvaluatePoints");

            countBuffer = Utils.SetupComputeBufferSafe(6, sizeof(int), ComputeBufferType.IndirectArguments);
            grassBuffer = Utils.SetupComputeBufferSafe((triCount / 3) * (int)scatter.properties.scatterDistribution._PopulationMultiplier, GrassData.Size(), ComputeBufferType.Append);
            farGrassBuffer = Utils.SetupComputeBufferSafe((triCount / 3) * (int)scatter.properties.scatterDistribution._PopulationMultiplier, GrassData.Size(), ComputeBufferType.Append);

            subObjectSlot1 = Utils.SetupComputeBufferSafe((triCount / 3) * (int)scatter.properties.scatterDistribution._PopulationMultiplier, GrassData.Size(), ComputeBufferType.Append);
            subObjectSlot2 = Utils.SetupComputeBufferSafe((triCount / 3) * (int)scatter.properties.scatterDistribution._PopulationMultiplier, GrassData.Size(), ComputeBufferType.Append);
            subObjectSlot3 = Utils.SetupComputeBufferSafe((triCount / 3) * (int)scatter.properties.scatterDistribution._PopulationMultiplier, GrassData.Size(), ComputeBufferType.Append);
            subObjectSlot4 = Utils.SetupComputeBufferSafe((triCount / 3) * (int)scatter.properties.scatterDistribution._PopulationMultiplier, GrassData.Size(), ComputeBufferType.Append);

            evaluate.SetBuffer(evaluatePoints, "Grass", grassBuffer);
            evaluate.SetBuffer(evaluatePoints, "Positions", grassPositionBuffer);
            evaluate.SetBuffer(evaluatePoints, "FarGrass", farGrassBuffer);
            evaluate.SetBuffer(evaluatePoints, "SubObjects1", subObjectSlot1);
            evaluate.SetBuffer(evaluatePoints, "SubObjects2", subObjectSlot2);
            evaluate.SetBuffer(evaluatePoints, "SubObjects3", subObjectSlot3);
            evaluate.SetBuffer(evaluatePoints, "SubObjects4", subObjectSlot4);
        }
        void Update()
        {
            if (ScatterLibrary.requiresUpdate)
            {
                EvaluatePositions();
                timeSinceLastUpdate = 0;
            }
            bool isTime = CheckTheTime(updateFPS);
            if (isTime)
            {
                EvaluatePositions();
            }
        }
        float timeSinceLastUpdate = 0;
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
        void GeneratePositions()
        {
            Vector3[] verts = mesh.vertices;
            int[] tris = mesh.triangles;
            Utils.SafetyCheckRelease(positionBuffer, "position buffer");
            Utils.SafetyCheckRelease(grassPositionBuffer, "grass position buffer");
            Utils.SafetyCheckRelease(triangleBuffer, "mesh triangle buffer");
            positionBuffer = Utils.SetupComputeBufferSafe(vertCount, 12, ComputeBufferType.Structured);
            grassPositionBuffer = Utils.SetupComputeBufferSafe((tris.Length / 3) * (int)scatter.properties.scatterDistribution._PopulationMultiplier, PositionData.Size(), ComputeBufferType.Structured);
            triangleBuffer = Utils.SetupComputeBufferSafe(tris.Length, sizeof(int), ComputeBufferType.Structured);

            positionBuffer.SetData(verts);
            triangleBuffer.SetData(tris);

            int distributeKernel = distribute.FindKernel("DistributePoints");

            distribute.SetInt("_PopulationMultiplier", (int)scatter.properties.scatterDistribution._PopulationMultiplier);
            distribute.SetMatrix("_ObjectToWorld", transform.localToWorldMatrix);
            distribute.SetVector("_PlanetOrigin", FlightGlobals.currentMainBody.transform.position);
            distribute.SetInt("_VertCount", vertCount);
            distribute.SetVector("_ThisPos", transform.position);

            distribute.SetBuffer(distributeKernel, "Objects", positionBuffer);
            distribute.SetBuffer(distributeKernel, "Tris", triangleBuffer);
            distribute.SetBuffer(distributeKernel, "Positions", grassPositionBuffer);

            distribute.Dispatch(distributeKernel, Mathf.CeilToInt(((float)tris.Length / 3f) / 32f), 1, 1);

            //EvaluatePositions();

        }
        public void EvaluatePositions()
        {
            //ReleaseEvaluationBuffers();
            grassBuffer.SetCounterValue(0);
            farGrassBuffer.SetCounterValue(0);
            subObjectSlot1.SetCounterValue(0);
            subObjectSlot2.SetCounterValue(0);
            subObjectSlot3.SetCounterValue(0);
            subObjectSlot4.SetCounterValue(0);

            evaluate.SetVector("minScale", scatter.properties.scatterDistribution._MinScale);
            evaluate.SetVector("maxScale", scatter.properties.scatterDistribution._MaxScale);
            evaluate.SetVector("grassColorMain", scatter.properties.scatterMaterial._MainColor);
            evaluate.SetVector("grassColorSub", scatter.properties.scatterMaterial._SubColor);
            evaluate.SetFloat("grassColorNoiseStrength", scatter.properties.scatterMaterial._ColorNoiseStrength);
            evaluate.SetFloat("grassColorNoiseScale", scatter.properties.scatterMaterial._ColorNoiseScale);
            evaluate.SetFloat("grassCutoffScale", scatter.properties.scatterDistribution._CutoffScale);
            evaluate.SetFloat("grassSizeNoiseScale", scatter.properties.scatterDistribution._SizeNoiseScale);
            evaluate.SetFloat("grassSizeNoiseStrength", scatter.properties.scatterDistribution._SizeNoiseStrength);
            evaluate.SetFloat("grassSizeNoiseOffset", scatter.properties.scatterDistribution._SizeNoiseOffset.x);
            evaluate.SetFloat("range", scatter.properties.scatterDistribution._Range);
            evaluate.SetVector("_ThisPos", transform.position - ScatterLibrary.floatingOriginOffset);
            evaluate.SetVector("_CameraPos", FlightGlobals.ActiveVessel.transform.position);//Camera.allCameras.FirstOrDefault(_cam => _cam.name == "Camera 00").gameObject.transform.position - gameObject.transform.position);

            evaluate.SetFloat("subObjectWeight1", GetSubObjectProperty("subObjectWeight", 0));
            evaluate.SetFloat("subObjectNoiseScale1", GetSubObjectProperty("subObjectNoiseScale", 0));
            evaluate.SetFloat("subObjectSpawnChance1", GetSubObjectProperty("subObjectSpawnChance", 0));

            evaluate.SetFloat("subObjectWeight2", GetSubObjectProperty("subObjectWeight", 1));
            evaluate.SetFloat("subObjectNoiseScale2", GetSubObjectProperty("subObjectNoiseScale", 1));
            evaluate.SetFloat("subObjectSpawnChance2", GetSubObjectProperty("subObjectSpawnChance", 1));

            evaluate.SetFloat("subObjectWeight3", GetSubObjectProperty("subObjectWeight", 2));
            evaluate.SetFloat("subObjectNoiseScale3", GetSubObjectProperty("subObjectNoiseScale", 2));
            evaluate.SetFloat("subObjectSpawnChance3", GetSubObjectProperty("subObjectSpawnChance", 2));

            evaluate.SetFloat("subObjectWeight4", GetSubObjectProperty("subObjectWeight", 3));
            evaluate.SetFloat("subObjectNoiseScale4", GetSubObjectProperty("subObjectNoiseScale", 3));
            evaluate.SetFloat("subObjectSpawnChance4", GetSubObjectProperty("subObjectSpawnChance", 3));

            evaluate.Dispatch(evaluatePoints, ((triCount / 3) * (int)scatter.properties.scatterDistribution._PopulationMultiplier) / 32, 1, 1);
            ComputeBuffer.CopyCount(grassBuffer, countBuffer, 0);
            ComputeBuffer.CopyCount(farGrassBuffer, countBuffer, 4);
            ComputeBuffer.CopyCount(subObjectSlot1, countBuffer, 8);
            ComputeBuffer.CopyCount(subObjectSlot2, countBuffer, 12);
            ComputeBuffer.CopyCount(subObjectSlot3, countBuffer, 16);
            ComputeBuffer.CopyCount(subObjectSlot4, countBuffer, 20);
            int[] count = new int[] { 0, 0, 0, 0, 0, 0 };
            countBuffer.GetData(count);
            Debug.Log("Rendering " + count[0] + " objects and " + count[1] + " low LOD objects");
            pc.Setup(count, new ComputeBuffer[] { grassBuffer, farGrassBuffer, subObjectSlot1, subObjectSlot2, subObjectSlot3, subObjectSlot4 }, scatter);
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
            positionBuffer.Dispose();
            triangleBuffer.Dispose();
            grassPositionBuffer.Dispose();
            countBuffer.Dispose();
            grassBuffer.Dispose();
            Destroy(pc);
        }
        void OnDisable()
        {
            positionBuffer.Release();
            triangleBuffer.Release();
            grassPositionBuffer.Release();
            countBuffer.Release();
            grassBuffer.Release();
            Debug.Log("Compute Component disabled");
        }
    }

    [KSPAddon(KSPAddon.Startup.FlightAndKSC, false)]
    public class ShadowFixer : MonoBehaviour
    {
        public void Start()
        {
            QualitySettings.shadowDistance = 10000;
            QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
            QualitySettings.shadowProjection = ShadowProjection.StableFit;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowCascade4Split = new Vector3(0.002f, 0.022f, 0.178f);
            Camera.main.nearClipPlane = 0.1f;
            Camera.current.nearClipPlane = 0.1f;
        }
        void Update()
        {
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

        public Material material;
        public Material materialFar;
        public Material subObjectMat1;
        public Material subObjectMat2;
        public Material subObjectMat3;
        public Material subObjectMat4;

        public Mesh mesh;
        public Mesh farMesh;
        public Mesh subObjectMesh1;
        public Mesh subObjectMesh2;
        public Mesh subObjectMesh3;
        public Mesh subObjectMesh4;

        private ComputeBuffer argsBuffer;
        private ComputeBuffer farArgsBuffer;
        private ComputeBuffer subArgs1;
        private ComputeBuffer subArgs2;
        private ComputeBuffer subArgs3;
        private ComputeBuffer subArgs4;


        private ComputeBuffer mainNear;
        private ComputeBuffer mainFar;
        private ComputeBuffer sub1;
        private ComputeBuffer sub2;
        private ComputeBuffer sub3;
        private ComputeBuffer sub4;

        private Bounds bounds;
        bool setup = false;
        bool setupInitial = false;
        int countCheck;
        int farCountCheck;
        int subCount1;
        int subCount2;
        int subCount3;
        int subCount4;

        public Properties scatterProps;
        
        public void Setup(int[] counts, ComputeBuffer[] buffers, Scatter scatter)
        {
            if (!setupInitial)
            {
                GameObject go = GameDatabase.Instance.GetModel("Parallax_StockTextures/_Scatters/Models/grassclumpoptimized");
                Mesh mesh = Instantiate(go.GetComponent<MeshFilter>().mesh);
               
                this.mesh = mesh;
                go = GameDatabase.Instance.GetModel("Parallax_StockTextures/_Scatters/Models/trigrass");
                farMesh = Instantiate(go.GetComponent<MeshFilter>().mesh);
                material = new Material(ScatterShaderHolder.GetShader("Custom/InstancedIndirectColor"));
                subObjectMesh1 = Utils.GetSubObjectMesh(scatter, 0);
                subObjectMesh2 = Utils.GetSubObjectMesh(scatter, 1);
                subObjectMesh3 = Utils.GetSubObjectMesh(scatter, 2);
                subObjectMesh4 = Utils.GetSubObjectMesh(scatter, 3);

                //material.SetFloat("_WaveSpeed", 0);
                //material.SetFloat("_HeightCutoff", -1000);
                material.SetColor("_Color", new Color(1, 1, 1, 1));

                scatterProps = ScatterBodies.scatterBodies[FlightGlobals.currentMainBody.name].scatters["Grass"].properties;

                material.SetFloat("_MaxBrightness", 0.64f);

                material.SetFloat("_WaveSpeed", scatterProps.scatterWind._WaveSpeed);
                material.SetFloat("_WaveAmp", scatterProps.scatterWind._WaveAmp);
                material.SetFloat("_HeightCutoff", scatterProps.scatterWind._HeightCutoff);
                material.SetFloat("_HeightFactor", scatterProps.scatterWind._HeightFactor);

                material.SetVector("_PlanetOrigin", FlightGlobals.ActiveVessel.transform.position);
                material.SetVector("_WindSpeed", scatterProps.scatterWind._WindSpeed);

                material.SetColor("_SpecColor", new Color(0.2924f, 0.2924f, 0.2924f, 1));
                material.SetFloat("_Shininess", 15.9f);

                material.SetTexture("_WindMap", Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(t => t.name == "Parallax_StockTextures/_Scatters/grassuv2"));
                material.SetTexture("_BumpMap", Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(t => t.name == "Parallax_StockTextures/_Scatters/grassnrm"));
                //material.SetFloat("_Cutoff", 0);

                materialFar = Instantiate(material);
                subObjectMat1 = Utils.GetSubObjectMaterial(scatter, 0);
                subObjectMat2 = Utils.GetSubObjectMaterial(scatter, 1);
                subObjectMat3 = Utils.GetSubObjectMaterial(scatter, 2);
                subObjectMat4 = Utils.GetSubObjectMaterial(scatter, 3);
                //materialFar.SetFloat("_Cutoff", 0.5f);
                //materialFar.SetTexture("_MainTex", Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(t => t.name == "Parallax_StockTextures/_Scatters/grassuv5"));

                setupInitial = true;
            }
            int subdivisionRange = (int)(((2 * Mathf.PI * FlightGlobals.currentMainBody.Radius) / 4) / (Mathf.Pow(2, FlightGlobals.currentMainBody.pqsController.maxLevel)));
            bounds = new Bounds(transform.position, Vector3.one * (subdivisionRange + 1));
            countCheck = counts[0];
            farCountCheck = counts[1];
            subCount1 = counts[2];
            subCount2 = counts[3];
            subCount3 = counts[4];
            subCount4 = counts[5];

            mainNear = buffers[0];
            mainFar = buffers[1];
            sub1 = buffers[2];
            sub2 = buffers[3];
            sub3 = buffers[4];
            sub4 = buffers[5];

            Debug.Log("Sub1 count: " + subCount1);
            Debug.Log("Sub2 count: " + subCount2);
            Debug.Log("Sub3 count: " + subCount3);
            Debug.Log("Sub4 count: " + subCount4);

            InitializeBuffers();
        }

        private void InitializeBuffers()
        {
            if (mesh == null)
            {
                Debug.Log("Mesh null");
                return;
            }
            if (countCheck == 0)
            {
                Debug.Log("Count is 0");
                return;
            }
            Utils.SafetyCheckRelease(argsBuffer, "main args");
            Utils.SafetyCheckRelease(farArgsBuffer, "main args far");
            Utils.SafetyCheckRelease(subArgs1, "sub args 1");
            Utils.SafetyCheckRelease(subArgs2, "sub args 2");
            Utils.SafetyCheckRelease(subArgs3, "sub args 3");
            Utils.SafetyCheckRelease(subArgs4, "sub args 4");

            uint[] subArgsSlot1 = new uint[0];
            uint[] subArgsSlot2 = new uint[0];
            uint[] subArgsSlot3 = new uint[0];
            uint[] subArgsSlot4 = new uint[0];
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


            uint[] args = Utils.GenerateArgs(mesh, countCheck);
            uint[] farArgs = Utils.GenerateArgs(farMesh, farCountCheck);
            argsBuffer = Utils.SetupComputeBufferSafe(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            argsBuffer.SetData(args);
            farArgsBuffer = Utils.SetupComputeBufferSafe(1, farArgs.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            farArgsBuffer.SetData(farArgs);

            material.SetBuffer("_Properties", mainNear);
            materialFar.SetBuffer("_Properties", mainFar);


            setup = true;
            if (argsBuffer == null)
            {
                return;
            }
        }

        private void Start()
        {

        }
        private void Update()
        {

            if (mesh == null || setup == false || argsBuffer == null)
            {
                //Debug.Log("uh oh spaghettio");
                //Debug.Log(" - " + mesh);
                //Debug.Log(" - " + setup);
                //Debug.Log(" - " + argsBuffer);
                return;
            }
            //InitializeBuffers();
            Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer);
            Graphics.DrawMeshInstancedIndirect(farMesh, 0, materialFar, bounds, farArgsBuffer);
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
        }

        private void OnDestroy()
        {
            Debug.Log("OnDestroy called, forcing GPU finish");
            Utils.ForceGPUFinish(mainNear, typeof(Vector3), countCheck);
            Utils.SafetyCheckDispose(mainNear, "main scatter close");
            Utils.SafetyCheckDispose(mainFar, "main scatter far");
            Utils.SafetyCheckDispose(sub1, "sub object 1");
            Utils.SafetyCheckDispose(sub2, "sub object 2");
            Utils.SafetyCheckDispose(sub3, "sub object 3");
            Utils.SafetyCheckDispose(sub4, "sub object 4");
            Utils.SafetyCheckDispose(argsBuffer, "main scatter args");
            Utils.SafetyCheckDispose(farArgsBuffer, "main scatter far args");
            Utils.SafetyCheckDispose(subArgs1, "sub1 args");
            Utils.SafetyCheckDispose(subArgs2, "sub2 args");
            Utils.SafetyCheckDispose(subArgs3, "sub3 args");
            Utils.SafetyCheckDispose(subArgs4, "sub4 args");
        }
        private void OnDisable()
        {
            Debug.Log("OnDisable called, forcing GPU finish");
            Utils.ForceGPUFinish(mainNear, typeof(Vector3), countCheck);
            Utils.SafetyCheckDispose(mainNear, "main scatter close");
            Utils.SafetyCheckDispose(mainFar, "main scatter far");
            Utils.SafetyCheckDispose(sub1, "sub object 1");
            Utils.SafetyCheckDispose(sub2, "sub object 2");
            Utils.SafetyCheckDispose(sub3, "sub object 3");
            Utils.SafetyCheckDispose(sub4, "sub object 4");
            Utils.SafetyCheckDispose(argsBuffer, "main scatter args");
            Utils.SafetyCheckDispose(farArgsBuffer, "main scatter far args");
            Utils.SafetyCheckDispose(subArgs1, "sub1 args");
            Utils.SafetyCheckDispose(subArgs2, "sub2 args");
            Utils.SafetyCheckDispose(subArgs3, "sub3 args");
            Utils.SafetyCheckDispose(subArgs4, "sub4 args");
        }
    }
    public class QuadMeshes : MonoBehaviour
    {
        public PQ quad;
        public Material oldMaterial;
        bool alreadySubdivided = false;
        Material transparent = new Material(Shader.Find("Unlit/Transparent"));
        GameObject newQuad;
        void Start()
        {
            InvokeRepeating("CheckRange", 1f, 1f);
        }
        void CheckRange()
        {
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
                newQuadMeshRenderer.sharedMaterial = oldMaterial;//new Material(ScatterShaderHolder.GetShader("Custom/Wireframe"));//quadMeshRenderer.sharedMaterial;
                newQuadMeshRenderer.enabled = true;
                quadMeshRenderer.enabled = false;
                quadMeshRenderer.material = transparent;
                quadMeshRenderer.material.SetTexture("_MainTex", Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(t => t.name == "Parallax/BlankAlpha"));
                alreadySubdivided = true;

                ComputeComponent comp = newQuad.gameObject.AddComponent<ComputeComponent>();
                comp.mesh = mesh;
                comp.subObjectCount = ScatterBodies.scatterBodies[FlightGlobals.currentMainBody.name].scatters["Grass"].subObjectCount;
                PostCompute postComp = newQuad.gameObject.AddComponent<PostCompute>();
            }
            else if (distance >= limit && quad != null)
            {
            
                try
                {
                    Destroy(newQuad);
                }
                catch
                { Debug.Log("Unable to destroy newQuad"); }
                var quadMeshRenderer = quad.GetComponent<MeshRenderer>();
                if (oldMaterial != null)
                {
                    quadMeshRenderer.sharedMaterial = oldMaterial;
                }
                
                alreadySubdivided = false;
            }
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