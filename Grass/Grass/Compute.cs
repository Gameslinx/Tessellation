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

namespace ComputeLoader
{

    public class ComputeComponent : MonoBehaviour
    {
        // Start is called before the first frame update
        public ComputeShader distribute;
        public ComputeShader evaluate;

        public ComputeBuffer positionBuffer;
        public ComputeBuffer grassPositionBuffer;
        public ComputeBuffer grassBuffer;
        public ComputeBuffer triangleBuffer;
        public ComputeBuffer countBuffer;
        public ComputeBuffer farGrassBuffer;    //For the grass further away from the camera - Much lower LOD
        public PostCompute pc;

        public Mesh mesh;
        public int vertCount;
        public int triCount;

        public Vector3 _PlanetOrigin;

        public Properties properties;

        private int evaluatePoints;

        public float updateFPS = 1.0f;

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
            if (distance > properties.scatterDistribution._Range * 2)
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
            properties = ScatterBodies.scatterBodies[FlightGlobals.currentMainBody.name].scatters["Grass"].properties;
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
            countBuffer = new ComputeBuffer(2, sizeof(int), ComputeBufferType.IndirectArguments);
            grassBuffer = new ComputeBuffer((triCount / 3) * (int)properties.scatterDistribution._PopulationMultiplier, GrassData.Size(), ComputeBufferType.Append);
            farGrassBuffer = new ComputeBuffer((triCount / 3) * (int)properties.scatterDistribution._PopulationMultiplier, GrassData.Size(), ComputeBufferType.Append);
            evaluate.SetBuffer(evaluatePoints, "Grass", grassBuffer);
            evaluate.SetBuffer(evaluatePoints, "Positions", grassPositionBuffer);
            evaluate.SetBuffer(evaluatePoints, "FarGrass", farGrassBuffer);
        }
        float time = 0;
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
            ReleasePositionBuffers();
            positionBuffer = new ComputeBuffer(vertCount, 12);
            grassPositionBuffer = new ComputeBuffer((tris.Length / 3) * (int)properties.scatterDistribution._PopulationMultiplier, PositionData.Size());
            triangleBuffer = new ComputeBuffer(tris.Length, sizeof(int));

            positionBuffer.SetData(verts);
            triangleBuffer.SetData(tris);

            int distributeKernel = distribute.FindKernel("DistributePoints");

            distribute.SetInt("_PopulationMultiplier", (int)properties.scatterDistribution._PopulationMultiplier);
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

            evaluate.SetVector("minScale", properties.scatterDistribution._MinScale);
            evaluate.SetVector("maxScale", properties.scatterDistribution._MaxScale);
            evaluate.SetVector("grassColorMain", properties.scatterMaterial._MainColor);
            evaluate.SetVector("grassColorSub", properties.scatterMaterial._SubColor);
            evaluate.SetFloat("grassColorNoiseStrength", properties.scatterMaterial._ColorNoiseStrength);
            evaluate.SetFloat("grassColorNoiseScale", properties.scatterMaterial._ColorNoiseScale);
            evaluate.SetFloat("grassCutoffScale", properties.scatterDistribution._CutoffScale);
            evaluate.SetFloat("grassSizeNoiseScale", properties.scatterDistribution._SizeNoiseScale);
            evaluate.SetFloat("grassSizeNoiseStrength", properties.scatterDistribution._SizeNoiseStrength);
            evaluate.SetFloat("grassSizeNoiseOffset", properties.scatterDistribution._SizeNoiseOffset.x);
            evaluate.SetFloat("range", properties.scatterDistribution._Range);
            evaluate.SetVector("_ThisPos", transform.position - ScatterLibrary.floatingOriginOffset);
            evaluate.SetVector("_CameraPos", FlightGlobals.ActiveVessel.transform.position);//Camera.allCameras.FirstOrDefault(_cam => _cam.name == "Camera 00").gameObject.transform.position - gameObject.transform.position);

            evaluate.Dispatch(evaluatePoints, ((triCount / 3) * (int)properties.scatterDistribution._PopulationMultiplier) / 32, 1, 1);
            ComputeBuffer.CopyCount(grassBuffer, countBuffer, 0);
            ComputeBuffer.CopyCount(farGrassBuffer, countBuffer, 4);
            int[] count = new int[] { 0, 0 };
            countBuffer.GetData(count);
            Debug.Log("Rendering " + count[0] + " objects and " + count[1] + " low LOD objects");
            pc.Setup(count[0], count[1], grassBuffer, farGrassBuffer);
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
        public int population;
        public float range = 100;
        public float viewDist;

        public Material material;
        public Material materialFar;
        public Mesh mesh;
        public Mesh farMesh;

        private ComputeBuffer argsBuffer;
        private ComputeBuffer farArgsBuffer;
        private ComputeBuffer meshPropertiesBuffer;
        private ComputeBuffer farMeshPropertiesBuffer;

        private Bounds bounds;
        bool setup = false;
        bool setupInitial = false;
        int countCheck;
        int farCountCheck;
        public Properties scatterProps;
        
        public void Setup(int count, int farCount, ComputeBuffer buffer, ComputeBuffer farBuffer)
        {
            if (!setupInitial)
            {
                GameObject go = GameDatabase.Instance.GetModel("Parallax_StockTextures/_Scatters/Models/grassclumpoptimized");
                go.transform.position += new Vector3(4,4,4);
                go.transform.up = Vector3.Normalize(gameObject.transform.position - FlightGlobals.currentMainBody.transform.position);
                Mesh mesh = Instantiate(go.GetComponent<MeshFilter>().mesh);
                this.mesh = mesh;
                go = GameDatabase.Instance.GetModel("Parallax_StockTextures/_Scatters/Models/trigrass");
                farMesh = Instantiate(go.GetComponent<MeshFilter>().mesh);
                material = new Material(ScatterShaderHolder.GetShader("Custom/InstancedIndirectColor"));
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
                //materialFar.SetFloat("_Cutoff", 0.5f);
                //materialFar.SetTexture("_MainTex", Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(t => t.name == "Parallax_StockTextures/_Scatters/grassuv5"));

                setupInitial = true;
            }
            int subdivisionRange = (int)(((2 * Mathf.PI * FlightGlobals.currentMainBody.Radius) / 4) / (Mathf.Pow(2, FlightGlobals.currentMainBody.pqsController.maxLevel)));
            bounds = new Bounds(transform.position, Vector3.one * (subdivisionRange + 1));
            countCheck = count;
            farCountCheck = farCount;
            InitializeBuffers(count, farCount, buffer, farBuffer);
        }

        private void InitializeBuffers(int count, int farCount, ComputeBuffer buffer, ComputeBuffer farBuffer)
        {

            if (mesh == null)
            {
                Debug.Log("Mesh null");
                return;
            }
            if (count == 0)
            {
                Debug.Log("Count is 0");
                return;
            }
            farMeshPropertiesBuffer = farBuffer;
            meshPropertiesBuffer = buffer;
            float time = Time.realtimeSinceStartup;
            uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
            args[0] = (uint)mesh.GetIndexCount(0);
            args[1] = (uint)count;
            args[2] = (uint)mesh.GetIndexStart(0);
            args[3] = (uint)mesh.GetBaseVertex(0);

            uint[] farArgs = new uint[5] { 0, 0, 0, 0, 0 };
            farArgs[0] = (uint)farMesh.GetIndexCount(0);
            farArgs[1] = (uint)farCount;
            farArgs[2] = (uint)farMesh.GetIndexStart(0);
            farArgs[3] = (uint)farMesh.GetBaseVertex(0);


            if (argsBuffer != null)
            {
                argsBuffer.Release();
            }
            if (farArgsBuffer != null)
            {
                farArgsBuffer.Release();
            }
            //if (meshPropertiesBuffer != null)
            //{
            //    meshPropertiesBuffer.Release();
            //}
            argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            argsBuffer.SetData(args);
            farArgsBuffer = new ComputeBuffer(1, farArgs.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            farArgsBuffer.SetData(farArgs);
            //meshPropertiesBuffer = new ComputeBuffer(props.Length, ComputeComponent.GrassData.Size());
            //meshPropertiesBuffer.SetData(props);
            material.SetBuffer("_Properties", buffer);
            materialFar.SetBuffer("_Properties", farBuffer);
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
                Debug.Log("uh oh spaghettio");
                Debug.Log(" - " + mesh);
                Debug.Log(" - " + setup);
                Debug.Log(" - " + argsBuffer);
                return;
            }
            //InitializeBuffers();
            Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer);
            Graphics.DrawMeshInstancedIndirect(farMesh, 0, materialFar, bounds, farArgsBuffer);
        }

        private void OnDestroy()
        {
            Debug.Log("OnDestroy called, forcing GPU finish");
            Vector3[] forceGPUFinish = new Vector3[countCheck];
            meshPropertiesBuffer.GetData(forceGPUFinish);
            Vector3[] forceGPUFinishFar = new Vector3[farCountCheck];
            farMeshPropertiesBuffer.GetData(forceGPUFinishFar);
            if (meshPropertiesBuffer != null)
            {
                meshPropertiesBuffer.Dispose();
            }
            if (farMeshPropertiesBuffer != null)
            {
                farMeshPropertiesBuffer.Dispose();
            }
            //meshPropertiesBuffer = null;

            if (argsBuffer != null)
            {
                argsBuffer.Dispose();
            }
            //argsBuffer = null;
        }
        private void OnDisable()
        {
            Debug.Log("OnDisable called, forcing GPU finish");
            Vector3[] forceGPUFinish = new Vector3[countCheck];
            meshPropertiesBuffer.GetData(forceGPUFinish);
            Vector3[] forceGPUFinishFar = new Vector3[farCountCheck];
            farMeshPropertiesBuffer.GetData(forceGPUFinishFar);
            if (meshPropertiesBuffer != null)
            {
                meshPropertiesBuffer.Dispose();
            }
            if (argsBuffer != null)
            {
                argsBuffer.Dispose();
            }
            if (farMeshPropertiesBuffer != null)
            {
                farMeshPropertiesBuffer.Dispose();
            }
            Debug.Log("Post Compute disabled");
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
            //quadMesh = Instantiate(gameObject.GetComponent<MeshFilter>().sharedMesh);
            //StartCoroutine(CheckRange());
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