using System.Linq;
using UnityEngine;
using Grass;
using ScatterConfiguratorUtils;
using System;

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
        private ComputeBuffer mainFar;
        private ComputeBuffer mainFurther;

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
        public void SetupAgain(Scatter scatter)
        {
            material = new Material(scatter.properties.scatterMaterial.shader);

            //material.SetFloat("_WaveSpeed", 0);
            //material.SetFloat("_HeightCutoff", -1000);
            material = Utils.SetShaderProperties(material, scatter.properties.scatterMaterial);
            scatterProps = scatter.properties; //ScatterBodies.scatterBodies[FlightGlobals.currentMainBody.name].scatters["Grass"].properties;
            shadowCastingMode = scatter.shadowCastingMode;

            materialFar = GameObject.Instantiate(material);
            materialFurther = GameObject.Instantiate(material);
            if (scatter.properties.scatterDistribution.lods.lods[0].isBillboard) { materialFar = new Material(ScatterShaderHolder.GetShader(material.shader.name + "Billboard")); }
            if (scatter.properties.scatterDistribution.lods.lods[1].isBillboard) { materialFurther = new Material(ScatterShaderHolder.GetShader(material.shader.name + "Billboard")); }
            materialFar.CopyPropertiesFromMaterial(material);
            materialFurther.CopyPropertiesFromMaterial(material);
            if (scatter.properties.scatterDistribution.lods.lods[0].mainTexName != "parent")
            {
                materialFar.SetTexture("_MainTex", Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(t => t.name == scatter.properties.scatterDistribution.lods.lods[0].mainTexName));
            }
            if (scatter.properties.scatterDistribution.lods.lods[1].mainTexName != "parent")
            {
                materialFurther.SetTexture("_MainTex", Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(t => t.name == scatter.properties.scatterDistribution.lods.lods[1].mainTexName));
            }
            CreateBuffers();
            InitializeBuffers();
        }
        public void Setup(ComputeBuffer[] buffers, Scatter scatter)
        {
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
                material = Utils.SetShaderProperties(material, scatter.properties.scatterMaterial);
                scatterProps = scatter.properties; //ScatterBodies.scatterBodies[FlightGlobals.currentMainBody.name].scatters["Grass"].properties;
                shadowCastingMode = scatter.shadowCastingMode;
                materialFar = GameObject.Instantiate(material);
                materialFurther = GameObject.Instantiate(material);
                if (scatter.properties.scatterDistribution.lods.lods[0].isBillboard) { materialFar = new Material(ScatterShaderHolder.GetShader(material.shader.name + "Billboard")); }
                if (scatter.properties.scatterDistribution.lods.lods[1].isBillboard) { materialFurther = new Material(ScatterShaderHolder.GetShader(material.shader.name + "Billboard")); }
                materialFar.CopyPropertiesFromMaterial(material);
                materialFurther.CopyPropertiesFromMaterial(material);
                if (scatter.properties.scatterDistribution.lods.lods[0].mainTexName != "parent")
                {
                    materialFar.SetTexture("_MainTex", Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(t => t.name == scatter.properties.scatterDistribution.lods.lods[0].mainTexName));
                }
                if (scatter.properties.scatterDistribution.lods.lods[1].mainTexName != "parent")
                {
                    materialFurther.SetTexture("_MainTex", Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(t => t.name == scatter.properties.scatterDistribution.lods.lods[1].mainTexName));
                }
                subdivisionRange = (int)(((2 * Mathf.PI * FlightGlobals.currentMainBody.Radius) / 4) / (Mathf.Pow(2, FlightGlobals.currentMainBody.pqsController.maxLevel)));
                subdivisionRange = Mathf.Sqrt(Mathf.Pow(subdivisionRange, 2) + Mathf.Pow(subdivisionRange, 2));
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
            mainNear = buffers[0];
            mainFar = buffers[1];
            mainFurther = buffers[2];
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



        public void Update()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                this.UpdateBounds(FloatingOrigin.TerrainShaderOffset);
                if (!this.active)
                {
                    return;
                }
                if (this.mesh != null)
                {
                    Graphics.DrawMeshInstancedIndirect(this.mesh, 0, this.material, this.bounds, this.argsBuffer, 0, null, UnityEngine.Rendering.ShadowCastingMode.On, true, 0);
                }
                if (this.farMesh != null)
                {
                    Graphics.DrawMeshInstancedIndirect(this.farMesh, 0, this.materialFar, this.bounds, this.farArgsBuffer, 0, null, UnityEngine.Rendering.ShadowCastingMode.On, true, 0);
                }
                if (this.furtherMesh != null)
                {
                    Graphics.DrawMeshInstancedIndirect(this.furtherMesh, 0, this.materialFurther, this.bounds, this.furtherArgsBuffer, 0, null, this.shadowCastingMode, true, 0);
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
                material.SetFloat("_CurrentTime", Time.realtimeSinceStartup);
                material.SetVector("_ShaderOffset", -((Vector3)FloatingOrigin.TerrainShaderOffset));
                //Debug.Log("Shader offset: " + FloatingOrigin.TerrainShaderOffset);
            }
            if (materialFar != null && materialFar.HasProperty("_PlanetOrigin"))
            {
                materialFar.SetVector("_PlanetOrigin", planetOrigin);
                materialFar.SetFloat("_CurrentTime", Time.realtimeSinceStartup);
                materialFar.SetVector("_ShaderOffset", -((Vector3)FloatingOrigin.TerrainShaderOffset));
            }
            if (materialFurther != null && materialFurther.HasProperty("_PlanetOrigin"))
            {
                materialFurther.SetVector("_PlanetOrigin", planetOrigin);
                materialFurther.SetFloat("_CurrentTime", Time.realtimeSinceStartup);
                materialFurther.SetVector("_ShaderOffset", -((Vector3)FloatingOrigin.TerrainShaderOffset));
            }
        }
        private void UpdateBounds(Vector3d offset)
        {
            if (!active) { return; }
            
            bounds = new Bounds(Vector3.zero, Vector3.one * (scatterProps.scatterDistribution._Range * 2.8f));
            if (scatterProps.scatterDistribution._Range < 5000) { bounds = new Bounds(Vector3.zero, Vector3.one * (scatterProps.scatterDistribution._Range * 30f)); }
            SetPlanetOrigin();
        }
        private void OnEnable()
        {

        }
        private void OnDestroy()
        {
            //Utils.ForceGPUFinish(mainNear, typeof(ComputeComponent.GrassData), countCheck);

            Utils.DestroyComputeBufferSafe(mainNear);
            Utils.DestroyComputeBufferSafe(mainFar);
            Utils.DestroyComputeBufferSafe(mainFurther);
            Utils.DestroyComputeBufferSafe(argsBuffer);
            Utils.DestroyComputeBufferSafe(farArgsBuffer);
            Utils.DestroyComputeBufferSafe(furtherArgsBuffer);
        }
        private void OnDisable()
        {
            //EventManager.OnShaderOffsetUpdated -= UpdateBounds;
            //Utils.ForceGPUFinish(mainNear, typeof(ComputeComponent.GrassData), countCheck);

            Utils.DestroyComputeBufferSafe(mainNear);
            Utils.DestroyComputeBufferSafe(mainFar);
            Utils.DestroyComputeBufferSafe(mainFurther);
            Utils.DestroyComputeBufferSafe(argsBuffer);
            Utils.DestroyComputeBufferSafe(farArgsBuffer);
            Utils.DestroyComputeBufferSafe(furtherArgsBuffer);
        }
    }
}