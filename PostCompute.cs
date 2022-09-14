using System.Linq;
using UnityEngine;
using Grass;
using ScatterConfiguratorUtils;
using System;
using System.Collections;

namespace ComputeLoader
{
    public class PostCompute : MonoBehaviour
    {
        public bool active = true;
        public Material material;
        public Material materialFar;
        public Material materialFurther;

        public Mesh mesh;
        public Mesh farMesh;
        public Mesh furtherMesh;

        private ComputeBuffer argsBuffer;
        private ComputeBuffer farArgsBuffer;
        private ComputeBuffer furtherArgsBuffer;


        public ComputeBuffer mainNear;
        public ComputeBuffer mainFar;
        public ComputeBuffer mainFurther;

        private Bounds bounds;
        public bool setupInitial = false;

        public int vertexCount;
        public int farVertexCount;
        public int furtherVertexCount;
        public int subVertexCount1;
        public int subVertexCount2;
        public int subVertexCount3;
        public int subVertexCount4;

        float subdivisionRange = 0;

        public string quadName;
        public string scatterName;
        public string planetName;

        public Properties scatterProps;
        UnityEngine.Rendering.ShadowCastingMode shadowCastingMode;
        void OnEnable()
        {

        }
        public void SetupAgain(Scatter scatter)
        {
            material = new Material(scatter.properties.scatterMaterial.shader);

            //material.SetFloat("_WaveSpeed", 0);
            //material.SetFloat("_HeightCutoff", -1000);
            material = new Material(scatter.properties.scatterMaterial.shader);
            Utils.SetShaderProperties(ref material, ref scatter.properties.scatterMaterial, scatter.scatterName);
            scatterProps = scatter.properties; //ScatterBodies.scatterBodies[FlightGlobals.currentMainBody.name].scatters["Grass"].properties;
            materialFar = GameObject.Instantiate(material);
            materialFurther = GameObject.Instantiate(material);
            if (scatter.properties.scatterDistribution.lods.lods[0].isBillboard) { materialFar = new Material(ScatterShaderHolder.GetShader("Custom/InstancedCutoutBillboard")); materialFar.SetFloat("_Cutoff", 0.5f); }
            if (scatter.properties.scatterDistribution.lods.lods[1].isBillboard) { materialFurther = new Material(ScatterShaderHolder.GetShader("Custom/InstancedCutoutBillboard")); materialFurther.SetFloat("_Cutoff", 0.5f); }
            materialFar.CopyPropertiesFromMaterial(material);
            materialFurther.CopyPropertiesFromMaterial(material);
            if (scatter.properties.scatterDistribution.lods.lods[0].mainTexName != "parent")
            {
                Debug.Log("Attempting load: " + scatter.scatterName + "-" + scatter.properties.scatterDistribution.lods.lods[1].mainTexName);
                materialFar.SetTexture("_MainTex", LoadOnDemand.activeTextures[scatter.scatterName + "-" + scatter.properties.scatterDistribution.lods.lods[0].mainTexName]);
            }
            if (scatter.properties.scatterDistribution.lods.lods[0].normalName != "parent")
            {
                materialFar.SetTexture("_BumpMap", LoadOnDemand.activeTextures[scatter.scatterName + "-" + scatter.properties.scatterDistribution.lods.lods[0].normalName]);
            }
            if (scatter.properties.scatterDistribution.lods.lods[1].mainTexName != "parent")
            {
                Debug.Log("Attempting load: " + scatter.scatterName + "-" + scatter.properties.scatterDistribution.lods.lods[1].mainTexName);
                materialFurther.SetTexture("_MainTex", LoadOnDemand.activeTextures[scatter.scatterName + "-" + scatter.properties.scatterDistribution.lods.lods[1].mainTexName]);
            }
            if (scatter.properties.scatterDistribution.lods.lods[1].normalName != "parent")
            {
                materialFurther.SetTexture("_BumpMap", LoadOnDemand.activeTextures[scatter.scatterName + "-" + scatter.properties.scatterDistribution.lods.lods[1].normalName]);
            }
            CreateBuffers();
            InitializeBuffers();
        }
        public void Setup(ComputeBuffer buffer, ComputeBuffer farBuffer, ComputeBuffer furtherBuffer, Scatter scatter)
        {
            if (buffer == null) { Debug.Log("The buffer is null"); }
            if (!setupInitial)
            {
                GameObject go = GameDatabase.Instance.GetModel(scatter.model);
                Mesh mesh = GameObject.Instantiate(go.GetComponent<MeshFilter>().mesh);
                this.mesh = mesh;
                go = GameDatabase.Instance.GetModel(scatter.properties.scatterDistribution.lods.lods[0].modelName);
                farMesh = GameObject.Instantiate(go.GetComponent<MeshFilter>().mesh);
                go = GameDatabase.Instance.GetModel(scatter.properties.scatterDistribution.lods.lods[1].modelName);
                furtherMesh = GameObject.Instantiate(go.GetComponent<MeshFilter>().mesh);
                material = new Material(scatter.properties.scatterMaterial.shader);
                Utils.SetShaderProperties(ref material, ref scatter.properties.scatterMaterial, scatter.scatterName);

                scatterProps = scatter.properties; //ScatterBodies.scatterBodies[FlightGlobals.currentMainBody.name].scatters["Grass"].properties;
                shadowCastingMode = scatter.shadowCastingMode;
                materialFar = GameObject.Instantiate(material);
                materialFurther = GameObject.Instantiate(material);
                if (scatter.properties.scatterDistribution.lods.lods[0].isBillboard) { materialFar = new Material(ScatterShaderHolder.GetShader("Custom/InstancedCutoutBillboard")); materialFar.SetFloat("_Cutoff", 0.5f); }
                if (scatter.properties.scatterDistribution.lods.lods[1].isBillboard) { materialFurther = new Material(ScatterShaderHolder.GetShader("Custom/InstancedCutoutBillboard")); materialFurther.SetFloat("_Cutoff", 0.5f); }
                materialFar.CopyPropertiesFromMaterial(material);
                materialFurther.CopyPropertiesFromMaterial(material);
                if (scatter.properties.scatterDistribution.lods.lods[0].mainTexName != "parent")
                {
                    Debug.Log("Attempting load: " + scatter.scatterName + "-" + scatter.properties.scatterDistribution.lods.lods[1].mainTexName);
                    materialFar.SetTexture("_MainTex", LoadOnDemand.activeTextures[scatter.scatterName + "-" + scatter.properties.scatterDistribution.lods.lods[0].mainTexName]);
                }
                if (scatter.properties.scatterDistribution.lods.lods[0].normalName != "parent")
                {
                    materialFar.SetTexture("_BumpMap", LoadOnDemand.activeTextures[scatter.scatterName + "-" + scatter.properties.scatterDistribution.lods.lods[0].normalName]);
                }
                if (scatter.properties.scatterDistribution.lods.lods[1].mainTexName != "parent")
                {
                    Debug.Log("Attempting load: " + scatter.scatterName + "-" + scatter.properties.scatterDistribution.lods.lods[1].mainTexName);
                    materialFurther.SetTexture("_MainTex", LoadOnDemand.activeTextures[scatter.scatterName + "-" + scatter.properties.scatterDistribution.lods.lods[1].mainTexName]);
                }
                if (scatter.properties.scatterDistribution.lods.lods[1].normalName != "parent")
                {
                    materialFurther.SetTexture("_BumpMap", LoadOnDemand.activeTextures[scatter.scatterName + "-" + scatter.properties.scatterDistribution.lods.lods[1].normalName]);
                }
                //subdivisionRange = (int)(((2 * Mathf.PI * FlightGlobals.currentMainBody.Radius) / 4) / (Mathf.Pow(2, FlightGlobals.currentMainBody.pqsController.maxLevel)));
                //subdivisionRange = Mathf.Sqrt(Mathf.Pow(subdivisionRange, 2) + Mathf.Pow(subdivisionRange, 2));

                if (!ScatterGlobalSettings.castShadows)
                {
                    shadowMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }

                vertexCount = mesh.vertexCount;
                farVertexCount = farMesh.vertexCount;
                furtherVertexCount = furtherMesh.vertexCount;
                setupInitial = true;
                CreateBuffers();
            }
            //try
           // {
                UpdateBounds(Vector3.zero);
            //}
            //catch { }
            mainNear = buffer;
            mainFar = farBuffer;
            mainFurther = furtherBuffer;
            InitializeBuffers();
        }
        public void ReInitializeBuffers()
        {
            ScreenMessages.PostScreenMessage("[PostCompute] Reinitialized buffers externally");
            CreateBuffers();
            InitializeBuffers();
        }
        private void CreateBuffers()
        {
            uint[] args = new uint[0];
            uint[] farArgs = new uint[0];
            uint[] furtherArgs = new uint[0];
            if (argsBuffer != null) { argsBuffer.Release(); }
            args = Utils.GenerateArgs(mesh);
            argsBuffer = Utils.SetupComputeBufferSafe(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            argsBuffer.SetData(args);

            if (farArgsBuffer != null) { farArgsBuffer.Release(); }
            farArgs = Utils.GenerateArgs(farMesh);
            farArgsBuffer = Utils.SetupComputeBufferSafe(1, farArgs.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            farArgsBuffer.SetData(farArgs);

            if (furtherArgsBuffer != null) { furtherArgsBuffer.Release(); }
            furtherArgs = Utils.GenerateArgs(furtherMesh);
            furtherArgsBuffer = Utils.SetupComputeBufferSafe(1, furtherArgs.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            furtherArgsBuffer.SetData(furtherArgs);

        }
        private void InitializeBuffers()
        {
            ComputeBuffer.CopyCount(mainNear, argsBuffer, 4);
            ComputeBuffer.CopyCount(mainFar, farArgsBuffer, 4);
            ComputeBuffer.CopyCount(mainFurther, furtherArgsBuffer, 4);

            material.SetBuffer("_Properties", mainNear);
            materialFar.SetBuffer("_Properties", mainFar);
            materialFurther.SetBuffer("_Properties", mainFurther);
        }

        public UnityEngine.Rendering.ShadowCastingMode shadowMode = UnityEngine.Rendering.ShadowCastingMode.On;

        public void Update()
        {
            if (!active) { return; }
            
            if (HighLogic.LoadedSceneIsFlight)
            {
                this.UpdateBounds(FloatingOrigin.TerrainShaderOffset);
                if (this.mesh != null)
                {
                    Graphics.DrawMeshInstancedIndirect(this.mesh, 0, this.material, this.bounds, this.argsBuffer, 0, null, this.shadowMode, true, 15);
                }
                if (this.farMesh != null)
                {
                    Graphics.DrawMeshInstancedIndirect(this.farMesh, 0, this.materialFar, this.bounds, this.farArgsBuffer, 0, null, this.shadowMode, true, 15);
                }
                if (this.furtherMesh != null)
                {
                    Graphics.DrawMeshInstancedIndirect(this.furtherMesh, 0, this.materialFurther, this.bounds, this.furtherArgsBuffer, 0, null, this.shadowCastingMode, true, 15);
                    return;
                }
            }
            
        }
        private void SetPlanetOrigin()
        {
            if (FlightGlobals.currentMainBody == null) { return; }
            Vector3 planetOrigin = FlightGlobals.currentMainBody.transform.position;//Vector3.zero;
            if (material != null && material.HasProperty("_PlanetOrigin"))
            {
                material.SetVector("_PlanetOrigin", planetOrigin);
                material.SetFloat("_CurrentTime", Time.timeSinceLevelLoad);
                material.SetVector("_ShaderOffset", -((Vector3)FloatingOrigin.TerrainShaderOffset));
            }
            if (materialFar != null && materialFar.HasProperty("_PlanetOrigin"))
            {
                materialFar.SetVector("_PlanetOrigin", planetOrigin);
                materialFar.SetFloat("_CurrentTime", Time.timeSinceLevelLoad);
                materialFar.SetVector("_ShaderOffset", -((Vector3)FloatingOrigin.TerrainShaderOffset));
            }
            if (materialFurther != null && materialFurther.HasProperty("_PlanetOrigin"))
            {
                materialFurther.SetVector("_PlanetOrigin", planetOrigin);
                materialFurther.SetFloat("_CurrentTime", Time.timeSinceLevelLoad);
                materialFurther.SetVector("_ShaderOffset", -((Vector3)FloatingOrigin.TerrainShaderOffset));
            }
        }
        private void UpdateBounds(Vector3d offset)
        {
            bounds = new Bounds(Vector3.zero, Vector3.one * (scatterProps.scatterDistribution._Range * 2.8f));
            if (scatterProps.scatterDistribution._Range < 5000) { bounds = new Bounds(Vector3.zero, Vector3.one * (scatterProps.scatterDistribution._Range * 30f)); }

            SetPlanetOrigin();
        }
        private void OnDestroy()
        {
            //Utils.ForceGPUFinish(mainNear, typeof(ComputeComponent.GrassData), countCheck);

            //Utils.DestroyComputeBufferSafe(ref mainNear);
            //Utils.DestroyComputeBufferSafe(ref mainFar);
            //Utils.DestroyComputeBufferSafe(ref mainFurther);
            Utils.DestroyComputeBufferSafe(ref argsBuffer);
            Utils.DestroyComputeBufferSafe(ref farArgsBuffer);
            Utils.DestroyComputeBufferSafe(ref furtherArgsBuffer);
        }
        private void OnDisable()
        {
            setupInitial = false;
            //Utils.DestroyComputeBufferSafe(ref mainNear);
            //Utils.DestroyComputeBufferSafe(ref mainFar);
            //Utils.DestroyComputeBufferSafe(ref mainFurther);
            Utils.DestroyComputeBufferSafe(ref argsBuffer);
            Utils.DestroyComputeBufferSafe(ref farArgsBuffer);
            Utils.DestroyComputeBufferSafe(ref furtherArgsBuffer);
        }
    }
}