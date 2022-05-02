using Grass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ComputeLoader
{
    public class Evaluate : MonoBehaviour
    {
        public ComputeShader evaluate;
        public ComputeBuffer indirectArgs;
        private int evaluatePoints;
        public bool doEvaluate = true;
        public Scatter scatter;
        public PQSMod_ScatterManager pqsMod;
        public string planetName = "";

        //public int objectCount = 1000; //Copy count from merge buffer
        public bool active = false;
        public void OnEnable()
        {
            
        }
        public void OnDisable()
        {
            pqsMod.OnForceEvaluate -= DispatchEvaluate;
            pqsMod.OnEvaluateBufferLengthUpdated -= ReInitializeAllBuffers;
        }
        public void DeterminePQSMod()
        {
            Debug.Log("Determining mod");
            if (scatter == null) { Debug.Log("Scatter null"); }
            for (int i = 0; i < ActiveBuffers.mods.Count; i++)
            {
                Debug.Log(i);
                if (ActiveBuffers.mods[i] == null) { Debug.Log("Is null??"); }
                Debug.Log("a");
                if (ActiveBuffers.mods[i].scatterName == null) { Debug.Log("what"); }
                if (ActiveBuffers.mods[i].scatterName == scatter.scatterName)
                {
                    Debug.Log("b");
                    pqsMod = ActiveBuffers.mods[i];
                    Debug.Log("c");
                    if (pqsMod == null) { Debug.Log("Null PQSMod"); }
                    Debug.Log("d");

                }
            }
        }
        public void Start()
        {
            //Debug.Log("OnEnable");
            //Debug.Log("Length is " + ActiveBuffers.mods.Count);
            DeterminePQSMod();
            pqsMod.OnForceEvaluate += DispatchEvaluate;
            pqsMod.OnEvaluateBufferLengthUpdated += ReInitializeAllBuffers;
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
        void Update()
        {
            if (active)
            {
                //Debug.Log("1");
                Buffers.activeBuffers[scatter.scatterName].SetCounterValue(0);
                DispatchEvaluate();
                pqsMod.pc.Setup(new ComputeBuffer[] { Buffers.activeBuffers[scatter.scatterName].buffer, Buffers.activeBuffers[scatter.scatterName].farBuffer, Buffers.activeBuffers[scatter.scatterName].furtherBuffer }, scatter);
            }
        }
        void RealStart()
        {
            
            evaluate = Instantiate(ScatterShaderHolder.GetCompute("EvaluatePoints"));
            InitializeAllBuffers();
            ReInitializeAllBuffers();
            EvaluatePositions();
        }
        public void InitializeAllBuffers()
        {
            evaluatePoints = evaluate.FindKernel("EvaluatePoints");

            int maxStacks = scatter.properties.scatterDistribution.noise._MaxStacks;
            if (!Buffers.activeBuffers.ContainsKey(scatter.scatterName))
            {
                Debug.Log("Not contained");
            }
            if (Buffers.activeBuffers[scatter.scatterName].buffer == null) { Debug.Log("Buffer is null lol"); }
            evaluate.SetBuffer(evaluatePoints, "Grass", Buffers.activeBuffers[scatter.scatterName].buffer);
            evaluate.SetBuffer(evaluatePoints, "Positions", Buffers.activeBuffers[scatter.scatterName].mergeBuffer);
            evaluate.SetBuffer(evaluatePoints, "FarGrass", Buffers.activeBuffers[scatter.scatterName].farBuffer);
            evaluate.SetBuffer(evaluatePoints, "FurtherGrass", Buffers.activeBuffers[scatter.scatterName].furtherBuffer);
        }
        public void ReInitializeAllBuffers()
        {
            evaluate.SetBuffer(evaluatePoints, "Positions", Buffers.activeBuffers[scatter.scatterName].mergeBuffer);
            evaluate.SetBuffer(evaluatePoints, "Grass", Buffers.activeBuffers[scatter.scatterName].buffer);
            evaluate.SetBuffer(evaluatePoints, "FarGrass", Buffers.activeBuffers[scatter.scatterName].farBuffer);
            evaluate.SetBuffer(evaluatePoints, "FurtherGrass", Buffers.activeBuffers[scatter.scatterName].furtherBuffer);
            if (indirectArgs != null) { indirectArgs.Release(); }
            indirectArgs = new ComputeBuffer(1, sizeof(int) * 3, ComputeBufferType.IndirectArguments);
            int[] workGroups = new int[] { Mathf.CeilToInt(((float)pqsMod.objectCount) / 32f), 1, 1 };
            indirectArgs.SetData(workGroups);
        }
        public void EvaluatePositions()
        {
            Debug.Log("Beginning evaluate");
            //if (objectCount == 0) { return; }
            if (!doEvaluate) { return; }
            
            if (Buffers.activeBuffers[scatter.scatterName].buffer == null)
            {
                Debug.Log("Buffer null");
            }
            
            if (!Buffers.activeBuffers[scatter.scatterName].buffer.IsValid())
            {
                Debug.Log("Invalid, destroying");
                Destroy(this);
                return;
            }
            evaluate.SetFloat("range", scatter.properties.scatterDistribution._Range);
            evaluate.SetVector("_CameraPos", ActiveBuffers.cameraPos);// FlightGlobals.ActiveVessel.transform.position);//Camera.allCameras.FirstOrDefault(_cam => _cam.name == "Camera 00").gameObject.transform.position - gameObject.transform.position);
            evaluate.SetVector("_CraftPos", Vector3.zero);  //Set in scene changes here
            if (scatter.useSurfacePos) { evaluate.SetVector("_CameraPos", ActiveBuffers.surfacePos); }
            
            evaluate.SetFloat("_LODPerc", scatter.properties.scatterDistribution.lods.lods[0].range / scatter.properties.scatterDistribution._Range);    //At what range does the LOD change to the low one?
            evaluate.SetFloat("_LOD2Perc", scatter.properties.scatterDistribution.lods.lods[1].range / scatter.properties.scatterDistribution._Range);
            
            evaluate.SetVector("_ShaderOffset", -((Vector3)FloatingOrigin.TerrainShaderOffset));
            evaluate.SetVector("_ThisPos", transform.position);
            evaluate.SetInt("_MaxCount", pqsMod.objectCount);  //quadsubdif?
                                                  //and V
            evaluate.SetFloat("_CurrentTime", Time.timeSinceLevelLoad);
            evaluate.SetFloat("_Pow", scatter.properties.scatterDistribution._RangePow);
            evaluate.SetFloats("_CameraFrustumPlanes", ActiveBuffers.planeNormals);             //Frustum culling
            evaluate.SetFloat("_CullLimit", scatter.cullingLimit);
            
            float cullingRangePerc = scatter.cullingRange / scatter.properties.scatterDistribution._Range;
            if (ScatterGlobalSettings.frustumCull == false) { cullingRangePerc = 1; }   //Disable cull
            evaluate.SetFloat("_CullStartRange", cullingRangePerc);
            


            //evaluate.DispatchIndirect(evaluatePoints, indirectArgs, 0);
            
        }
        public void DispatchEvaluate()
        {
            evaluate.SetVector("_ShaderOffset", -((Vector3)FloatingOrigin.TerrainShaderOffset));
            evaluate.SetVector("_CameraPos", ActiveBuffers.cameraPos);
            evaluate.SetVector("_CraftPos", FlightGlobals.ActiveVessel.transform.position);
            evaluate.SetFloat("_CurrentTime", Time.timeSinceLevelLoad);
            evaluate.SetInt("_MaxCount", pqsMod.objectCount);
            if (scatter.useSurfacePos) { evaluate.SetVector("_CameraPos", ActiveBuffers.surfacePos); }
            //Debug.Log("Evaluating");
            evaluate.SetFloats("_CameraFrustumPlanes", ActiveBuffers.planeNormals);
            evaluate.DispatchIndirect(evaluatePoints, indirectArgs, 0);
        }
    }
}
