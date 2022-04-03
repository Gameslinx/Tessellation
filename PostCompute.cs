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
            subObjectMat1 = Utils.GetSubObjectMaterial(scatter, 0);
            subObjectMat2 = Utils.GetSubObjectMaterial(scatter, 1);
            subObjectMat3 = Utils.GetSubObjectMaterial(scatter, 2);
            subObjectMat4 = Utils.GetSubObjectMaterial(scatter, 3);
            CreateBuffers();
            InitializeBuffers();
        }
        public void Setup(ComputeBuffer[] buffers, Scatter scatter)
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
                CreateBuffers();
            }
            try
            {
                bounds = new Bounds(transform.position, Vector3.one * (subdivisionRange));
            }
            catch(Exception ex) { Destroy(this); }
            mainNear = buffers[0];
            mainFar = buffers[1];
            mainFurther = buffers[2];
            sub1 = buffers[3];
            sub2 = buffers[4];
            sub3 = buffers[5];
            sub4 = buffers[6];
            InitializeBuffers();
        }
        private void CreateBuffers()
        {
            uint[] subArgsSlot1 = new uint[0];
            uint[] subArgsSlot2 = new uint[0];
            uint[] subArgsSlot3 = new uint[0];
            uint[] subArgsSlot4 = new uint[0];
            uint[] args = new uint[0];
            uint[] farArgs = new uint[0];
            uint[] furtherArgs = new uint[0];

            args = Utils.GenerateArgs(mesh);
            argsBuffer = Utils.SetupComputeBufferSafe(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            argsBuffer.SetData(args);
            

            farArgs = Utils.GenerateArgs(farMesh);
            farArgsBuffer = Utils.SetupComputeBufferSafe(1, farArgs.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            farArgsBuffer.SetData(farArgs);
            

            furtherArgs = Utils.GenerateArgs(furtherMesh);
            furtherArgsBuffer = Utils.SetupComputeBufferSafe(1, furtherArgs.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            furtherArgsBuffer.SetData(furtherArgs);
            

            subArgsSlot1 = Utils.GenerateArgs(subObjectMesh1);
            subArgs1 = Utils.SetupComputeBufferSafe(1, subArgsSlot1.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            subArgs1.SetData(subArgsSlot1);
            

            subArgsSlot2 = Utils.GenerateArgs(subObjectMesh2);
            subArgs2 = Utils.SetupComputeBufferSafe(1, subArgsSlot2.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            subArgs2.SetData(subArgsSlot2);
            

            subArgsSlot3 = Utils.GenerateArgs(subObjectMesh3);
            subArgs3 = Utils.SetupComputeBufferSafe(1, subArgsSlot3.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            subArgs3.SetData(subArgsSlot3);
            

            subArgsSlot4 = Utils.GenerateArgs(subObjectMesh4);
            subArgs4 = Utils.SetupComputeBufferSafe(1, subArgsSlot4.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            subArgs4.SetData(subArgsSlot4);
           
        }
        private void InitializeBuffers()
        {
            //Utils.SafetyCheckRelease(argsBuffer, "main args");
            //Utils.SafetyCheckRelease(farArgsBuffer, "main args far");
            //Utils.SafetyCheckRelease(furtherArgsBuffer, "main args far");
            //Utils.SafetyCheckRelease(subArgs1, "sub args 1");
            //Utils.SafetyCheckRelease(subArgs2, "sub args 2");
            //Utils.SafetyCheckRelease(subArgs3, "sub args 3");
            //Utils.SafetyCheckRelease(subArgs4, "sub args 4");

            ComputeBuffer.CopyCount(mainNear, argsBuffer, 4);
            ComputeBuffer.CopyCount(mainFar, farArgsBuffer, 4);
            ComputeBuffer.CopyCount(mainFurther, furtherArgsBuffer, 4);
            ComputeBuffer.CopyCount(sub1, subArgs1, 4);
            ComputeBuffer.CopyCount(sub2, subArgs2, 4);
            ComputeBuffer.CopyCount(sub3, subArgs3, 4);
            ComputeBuffer.CopyCount(sub4, subArgs4, 4);

            material.SetBuffer("_Properties", mainNear);
            materialFar.SetBuffer("_Properties", mainFar);
            materialFurther.SetBuffer("_Properties", mainFurther);
            subObjectMat1.SetBuffer("_Properties", sub1);
            subObjectMat2.SetBuffer("_Properties", sub2);
            subObjectMat3.SetBuffer("_Properties", sub3);
            subObjectMat4.SetBuffer("_Properties", sub4);

            setup = true;
        }

        
        
        private void Update()
        {
            if (farMesh != null)
            {
                Graphics.DrawMeshInstancedIndirect(farMesh, 0, materialFar, bounds, farArgsBuffer, 0, null, UnityEngine.Rendering.ShadowCastingMode.On, true, gameObject.layer);
            }
            if (furtherMesh != null)
            {
                Graphics.DrawMeshInstancedIndirect(furtherMesh, 0, materialFurther, bounds, furtherArgsBuffer, 0, null, shadowCastingMode, true, gameObject.layer);
            }
            if (subObjectMesh1 != null)
            {
                Graphics.DrawMeshInstancedIndirect(subObjectMesh1, 0, subObjectMat1, bounds, subArgs1, 0, null, UnityEngine.Rendering.ShadowCastingMode.On, true, gameObject.layer);
            }
            if (subObjectMesh2 != null)
            {
                Graphics.DrawMeshInstancedIndirect(subObjectMesh2, 0, subObjectMat2, bounds, subArgs2, 0, null, UnityEngine.Rendering.ShadowCastingMode.On, true, gameObject.layer);
            }
            if (subObjectMesh3 != null)
            {
                Graphics.DrawMeshInstancedIndirect(subObjectMesh3, 0, subObjectMat3, bounds, subArgs3, 0, null, UnityEngine.Rendering.ShadowCastingMode.On, true, gameObject.layer);
            }
            if (subObjectMesh4 != null)
            {
                Graphics.DrawMeshInstancedIndirect(subObjectMesh4, 0, subObjectMat4, bounds, subArgs4, 0, null, UnityEngine.Rendering.ShadowCastingMode.On, true, gameObject.layer);
            }
            if (mesh != null)
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
                material.SetFloat("_CurrentTime", Time.realtimeSinceStartup);
            }
            if (materialFar != null && materialFar.HasProperty("_PlanetOrigin"))
            {
                materialFar.SetVector("_PlanetOrigin", planetOrigin);
                materialFar.SetFloat("_CurrentTime", Time.realtimeSinceStartup);
            }
            if (materialFurther != null && materialFurther.HasProperty("_PlanetOrigin"))
            {
                materialFurther.SetVector("_PlanetOrigin", planetOrigin);
                materialFurther.SetFloat("_CurrentTime", Time.realtimeSinceStartup);
            }
            if (subObjectMat1 != null && subObjectMat1.HasProperty("_PlanetOrigin"))
            {
                subObjectMat1.SetVector("_PlanetOrigin", planetOrigin);
                subObjectMat1.SetFloat("_CurrentTime", Time.realtimeSinceStartup);
            }
            if (subObjectMat2 != null && subObjectMat2.HasProperty("_PlanetOrigin"))
            {
                subObjectMat2.SetVector("_PlanetOrigin", planetOrigin);
                subObjectMat2.SetFloat("_CurrentTime", Time.realtimeSinceStartup);
            }
            if (subObjectMat3 != null && subObjectMat3.HasProperty("_PlanetOrigin"))
            {
                subObjectMat3.SetVector("_PlanetOrigin", planetOrigin);
                subObjectMat3.SetFloat("_CurrentTime", Time.realtimeSinceStartup);
            }
            if (subObjectMat4 != null && subObjectMat4.HasProperty("_PlanetOrigin"))
            {
                subObjectMat4.SetVector("_PlanetOrigin", planetOrigin);
                subObjectMat4.SetFloat("_CurrentTime", Time.realtimeSinceStartup);
            }
        }
        private void UpdateBounds(Vector3d offset)
        {
            if (!active) { return; }
            bounds = new Bounds(transform.position, Vector3.one * (subdivisionRange + 1));
            SetPlanetOrigin();
        }
        private void OnEnable()
        {
            EventManager.OnShaderOffsetUpdated += UpdateBounds;
        }
        private void OnDestroy()
        {
            //Utils.ForceGPUFinish(mainNear, typeof(ComputeComponent.GrassData), countCheck);

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
            EventManager.OnShaderOffsetUpdated -= UpdateBounds;
            //Utils.ForceGPUFinish(mainNear, typeof(ComputeComponent.GrassData), countCheck);

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