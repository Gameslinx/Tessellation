using ComputeLoader;
using Grass;
using Kopernicus.ConfigParser.Attributes;
using Kopernicus.ConfigParser.BuiltinTypeParsers;
using Kopernicus.ConfigParser.Enumerations;
using Kopernicus.Configuration.ModLoader;
using ParallaxGrass;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;

namespace ParallaxGrass
{
    //[KSPAddon(KSPAddon.Startup.Flight, false)]
    //public class QuadQueue : MonoBehaviour
    //{
    //    
    //    public static Dictionary<string, QuadComp> quads = new Dictionary<string, QuadComp>();
    //    void Start()
    //    {
    //        FloatingOrigin.ResetTerrainShaderOffset();
    //        FloatingOrigin.TerrainShaderOffset = Vector3.zero;
    //    }
    //
    //}
    //public static class Timings
    //{
    //    public static WaitForSeconds framerate = new WaitForSeconds(1);//new WaitForSeconds(0.33f);
    //    public static WaitForEndOfFrame frameEnd = new WaitForEndOfFrame();
    //    public static WaitForSeconds shortWait = new WaitForSeconds(1);//new WaitForSeconds(0.025f);
    //}
    //public class QuadComp : MonoBehaviour
    //{
    //    int scatterCount = 0;
    //    public PQ quad;
    //    public GameObject go;
    //    public int subdiv;
    //    public ComputeComponent[] comps;
    //    public PostCompute[] postComps;
    //    public string[] keys;
    //    public float nearestSubdivisionLimit;
    //    float rangeLimit;
    //
    //    public ScatterBody thisScatterBody;
    //    public Mesh mesh;
    //    public Mesh subdividedMesh;
    //    public void Start()
    //    {
    //        useGUILayout = false;
    //        
    //    }
    //    public void Begin()
    //    {
    //        if (quad == null)
    //        {
    //            Debug.Log("Null quad");
    //            Destroy(this);
    //        }
    //        if (FlightGlobals.currentMainBody == null) { return; }
    //        scatterCount = ScatterBodies.scatterBodies[FlightGlobals.currentMainBody.name].scatters.Count;
    //        alreadyAdded = new bool[scatterCount];
    //        for (int i = 0; i < alreadyAdded.Length; i++)
    //        {
    //            alreadyAdded[i] = false;
    //        }
    //        keys = ScatterBodies.scatterBodies[FlightGlobals.currentMainBody.name].scatters.Keys.ToArray();
    //        comps = new ComputeComponent[scatterCount];
    //        postComps = new PostCompute[scatterCount];
    //        nearestSubdivisionLimit = (int)(((2 * Mathf.PI * FlightGlobals.currentMainBody.Radius) / 4) / (Mathf.Pow(2, FlightGlobals.currentMainBody.pqsController.maxLevel))) + 50;
    //        thisScatterBody = ScatterBodies.scatterBodies[FlightGlobals.currentMainBody.name];
    //        rangeLimit = (int)(((2 * Mathf.PI * FlightGlobals.currentMainBody.Radius) / 4) / (Mathf.Pow(2, FlightGlobals.currentMainBody.pqsController.maxLevel)));
    //        mesh = Instantiate(quad.GetComponent<MeshFilter>().sharedMesh);
    //        co = StartCoroutine(OnUpdate());
    //    }
    //    bool[] alreadyAdded;
    //    bool firstRun = true;
    //    int count = 0;
    //    public Coroutine co;
    //    public IEnumerator OnUpdate()
    //    {
    //        
    //        yield return Timings.frameEnd;
    //        
    //        
    //        while (true)
    //        {
    //            if (HighLogic.LoadedScene == GameScenes.TRACKSTATION) { StopCoroutine(co); yield return null; }
    //            //Profiler.BeginSample("Scatter Full OnUpdate");
    //            count++;
    //            if (quad == null)
    //            {
    //                Debug.Log("Quad is null tf");
    //            }
    //            if (!QuadQueue.quads.ContainsKey(quad.name))
    //            {
    //                Destroy(this);
    //            }
    //            //if (FlightGlobals.ActiveVessel != null)
    //            //{
    //                //Subdivide by the maximum subdivision setting
    //                
    //                SubdivideQuad();
    //                for (int i = 0; i < keys.Length; i++)
    //                {
    //                    Scatter thisScatter = thisScatterBody.scatters[keys[i]];
    //                    float distance = Vector3.Distance(GlobalPoint.originPoint, quad.transform.position);
    //                    float limit = GetRangeLimit(thisScatter);
    //                    if (alreadyAdded[i] == false && distance <= limit && quad.isVisible)
    //                    {
    //                        //Profiler.BeginSample("Setup Quad Scatter");
    //                        if (thisScatter.properties.subdivisionSettings.mode == SubdivisionMode.FixedRange)
    //                        {
    //                            SetupFixedScatter(thisScatter, quad, i);
    //                        }
    //                        else
    //                        {   
    //                            SetupNearestQuadScatter(thisScatter, quad, i);
    //                        }
    //                        alreadyAdded[i] = true;
    //                        //Profiler.EndSample();
    //                    }
    //                    if (distance > limit || quad.isVisible == false && alreadyAdded[i] == true)
    //                    {
    //                        RemoveFixedScatter(quad, i);
    //                        alreadyAdded[i] = false;
    //                    }
    //                }
    //            //}
    //            if (firstRun)
    //            {
    //                firstRun = false;
    //                yield return Timings.shortWait;
    //            }
    //            //Profiler.EndSample();
    //            yield return Timings.framerate;
    //        }
    //        
    //    }
    //    public void OnDestroy()
    //    {
    //        if (co != null) { StopCoroutine(co); }
    //        if (go != null) { Destroy(go); }
    //        if (mesh != null) { Destroy(mesh); }
    //        RemoveAllFixedScatters(quad);
    //    }
    //    public float GetRangeLimit(Scatter scatter)
    //    {
    //        if (scatter.properties.subdivisionSettings.mode == SubdivisionMode.FixedRange)
    //        {
    //            return scatter.properties.subdivisionSettings.range;
    //        }
    //        else
    //        {
    //            return rangeLimit;
    //        }
    //    }
    //    
    //    void SetupFixedScatter(Scatter thisScatter, PQ quad, int index)
    //    {
    //        if (quad == null)
    //        {
    //            Debug.Log("quad null innit lol");
    //        }
    //        ComputeComponent comp = quad.gameObject.AddComponent<ComputeComponent>();
    //        comp.mesh = mesh;
    //        float[] distributionData = GetDistributionData(thisScatter, quad); //PQSMod_ScatterDistribute.scatterData.distributionData[thisScatter.scatterName].data[quad.name];//[quad.GetHashCode().ToString()]; //quad.gameObject.GetComponent<ScatterData>();
    //        comp.distributionNoise = distributionData;
    //        comp.subObjectCount = thisScatter.subObjectCount;
    //        comp.scatter = thisScatter;
    //        comp.quadSubdivision = quad.subdivision;
    //        int maxLevel = quad.sphereRoot.maxLevel;
    //        int maxLevelDiff = maxLevel - quad.subdivision;
    //        comp.quadSubdivisionDifference = (maxLevelDiff * 3) + 1;
    //        comp.quad = quad;
    //        comps[index] = comp;
    //    }
    //    float[] GetDistributionData(Scatter thisScatter, PQ quad)
    //    {
    //        DistributionNoise noise = thisScatter.properties.scatterDistribution.noise;
    //
    //        if (noise.useNoiseProfile != null)
    //        {
    //            return PQSMod_ScatterDistribute.scatterData.distributionData[noise.useNoiseProfile].data[quad.name];
    //        }
    //        else
    //        {
    //            return PQSMod_ScatterDistribute.scatterData.distributionData[thisScatter.scatterName].data[quad.name];
    //        }
    //    }
    //    public bool alreadySubdivided = false;
    //    public GameObject newQuad;
    //    public void SubdivideQuad()
    //    {
    //        if (FlightGlobals.currentMainBody == null) { return; }
    //        if (quad.subdivision != FlightGlobals.currentMainBody.pqsController.maxLevel)
    //        {
    //            return;
    //        }
    //        //yield return new WaitForSeconds(1);
    //        float distance = Vector3.Distance(GlobalPoint.originPoint, quad.transform.position);
    //        
    //        Vector3 planetNormal = Vector3.Normalize(GlobalPoint.originPoint - FlightGlobals.currentMainBody.transform.position);
    //        if (distance < nearestSubdivisionLimit && alreadySubdivided == false && quad != null)
    //        {
    //            var quadMeshFilter = quad.GetComponent<MeshFilter>();
    //            var quadMeshRenderer = quad.GetComponent<MeshRenderer>();
    //            newQuad = new GameObject();
    //            newQuad.name = quad.name + "-Fake";
    //            newQuad.transform.position = quad.gameObject.transform.position;
    //            newQuad.transform.rotation = quad.gameObject.transform.rotation;
    //            newQuad.transform.parent = quad.gameObject.transform;
    //            newQuad.transform.localPosition = Vector3.zero;
    //            newQuad.transform.localRotation = Quaternion.identity;
    //            newQuad.transform.localScale = Vector3.one;
    //            newQuad.transform.parent = quad.gameObject.transform;
    //            newQuad.layer = quad.gameObject.layer;
    //            newQuad.SetActive(true);
    //            Mesh mesh = Instantiate(quadMeshFilter.sharedMesh);
    //            
    //            MeshHelper.Subdivide(mesh, 6);
    //            var newQuadMeshFilter = newQuad.AddComponent<MeshFilter>();
    //            newQuadMeshFilter.sharedMesh = mesh;
    //
    //
    //            quadMeshRenderer.enabled = true;   //Can use true here and just view the quad
    //            alreadySubdivided = true;
    //            newQuad.AddComponent<MeshRenderer>();
    //            newQuad.GetComponent<MeshRenderer>().enabled = false;
    //            //newQuad.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Standard"));
    //            subdividedMesh = mesh;
    //            //Profiler.EndSample();
    //        }
    //        else if (distance >= nearestSubdivisionLimit && quad != null && quad.subdivision == FlightGlobals.currentMainBody.pqsController.maxLevel)
    //        {
    //            Destroy(newQuad);
    //            //var quadMeshRenderer = quad.GetComponent<MeshRenderer>();
    //            alreadySubdivided = false;
    //        }
    //    }
    //    void SetupNearestQuadScatter(Scatter thisScatter, PQ quad, int index)
    //    {
    //        if (quad == null)
    //        {
    //            Debug.Log("quad null innit lol");
    //        }
    //        ComputeComponent comp = quad.gameObject.AddComponent<ComputeComponent>();
    //        comp.mesh = subdividedMesh;
    //        float[] distributionData = new float[subdividedMesh.vertexCount];//PQSMod_ScatterDistribute.scatterData.distributionData[thisScatter.scatterName].data[quad.name];//[quad.GetHashCode().ToString()]; //quad.gameObject.GetComponent<ScatterData>();
    //        for (int i = 0; i < distributionData.Length; i++) { distributionData[i] = 1; }
    //        comp.distributionNoise = distributionData;
    //        comp.subObjectCount = thisScatter.subObjectCount;
    //        comp.scatter = thisScatter;
    //        comp.quadSubdivision = quad.subdivision;
    //        int maxLevel = quad.sphereRoot.maxLevel;
    //        int maxLevelDiff = maxLevel - quad.subdivision;
    //        comp.quadSubdivisionDifference = (maxLevelDiff * 3) + 1;
    //        comp.quad = quad;
    //        comps[index] = comp;
    //    }
    //    public void RemoveFixedScatter(PQ quad, int index)
    //    {
    //        if (comps[index] != null)
    //        {
    //            if (comps[index].mesh != null) { Destroy(comps[index].mesh); }
    //            Destroy(comps[index]);
    //        }
    //    }
    //    public void RemoveAllFixedScatters(PQ quad)
    //    {
    //        //ComputeComponent comp = comps[i];// quad.gameObject.GetComponent<ComputeComponent>();
    //        //PostCompute postComp = postComps[i];// quad.gameObject.GetComponent<PostCompute>();
    //        //ComputeComponent comp = quad.gameObject.GetComponent<ComputeComponent>();
    //        //PostCompute postComp = quad.gameObject.GetComponent<PostCompute>();
    //        //Profiler.BeginSample("Remove All Fixed Scatter");
    //        for (int i = 0; i < scatterCount; i++)
    //        {
    //            int index = i;
    //            if (mesh != null) { Destroy(mesh); }
    //            if (comps[index] != null)
    //            {
    //                if (comps[index].mesh != null) { Destroy(comps[index].mesh); }
    //                Destroy(comps[index]);
    //            }
    //            if (postComps[index] != null)
    //            {
    //                //Destroy(postComps[index]);
    //            }
    //        }
    //        //Profiler.EndSample();
    //    }
    //    public void DestroyGO(GameObject go)
    //    {
    //        Destroy(go);
    //    }
    //}
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class Counter : MonoBehaviour
    {
        public void Update()
        {
            Debug.Log(PQSMod_ParallaxScatter.quadList.Count);
        }
    }
    public class PQSMod_ParallaxScatter : PQSMod
    {
        public static Dictionary<string, QuadData> quadList = new Dictionary<string, QuadData>(); //This WILL always reach 0 when no quads are in range
        public override void OnQuadBuilt(PQ quad)
        {
            if (quad.subdivision > ScatterBodies.scatterBodies[quad.sphereRoot.name].minimumSubdivision)       //Do everything scatter related within allowed subdivision
            {
                Profiler.BeginSample("Create Parallax Scatter Quad");
                QuadData data = new QuadData(quad);
                Profiler.EndSample();
                quadList.Add(quad.name, data);
            }
        }
        public override void OnQuadDestroy(PQ quad)
        {
            if (quad.subdivision > ScatterBodies.scatterBodies[quad.sphereRoot.name].minimumSubdivision)    //Remove from dictionary, remember to purge the buffers at least
            {
                if (quadList.ContainsKey(quad.name)) { quadList[quad.name].Cleanup(); }
                quadList.Remove(quad.name);     //Not all quads on destroy are in the dictionary, but it always falls down to 0
            }
        }
    }
    [RequireConfigType(ConfigType.Node)]
    public class ParallaxScatter : ModLoader<PQSMod_ParallaxScatter>
    {
        [ParserTarget("order", Optional = true)]
        public NumericParser<int> order
        {
            get { return Mod.order; }
            set { Mod.order = int.MaxValue - 1; }
        }
    }
}
