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

namespace ParallaxGrass
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class QuadQueue : MonoBehaviour
    {
        public static Dictionary<string, QuadComp> quads = new Dictionary<string, QuadComp>();
        int arrayIndex = 0;
        void Start()
        {
            InvokeRepeating("DoStart", 0f, 10f);
        }
        void DoStart()
        {
            QuadComp[] quadComps = Resources.FindObjectsOfTypeAll<QuadComp>();
            string[] keys = ScatterBodies.scatterBodies[FlightGlobals.currentMainBody.name].scatters.Keys.ToArray();
            float maxDist = 0;
            int count = 0;
            
            for (int i = 0; i < quadComps.Length; i++)
            {
                for (int b = 0; b < keys.Length; b++)
                {
                    string key = keys[b];
                    float distance = ScatterBodies.scatterBodies[FlightGlobals.currentMainBody.name].scatters[key].properties.subdivisionSettings.range;
                    float limit = distance;
                    if (Vector3.Distance(FlightGlobals.ActiveVessel.transform.position, quadComps[i].transform.position) < limit)
                    {
                        count++;
                    }
                }
                
            }
            Debug.Log("There are " + quadComps.Length + " quads loaded");
            Debug.Log("There are an estimated " + count + " CCs loaded");
            ComputeComponent[] ccs = Resources.FindObjectsOfTypeAll<ComputeComponent>();
            Debug.Log("There are " + ccs.Length + " CCs loaded");
            PostCompute[] pcs = Resources.FindObjectsOfTypeAll<PostCompute>();
            Debug.Log("There are " + pcs.Length + " PCs loaded");
            
        }
        void Update()
        {
            //UpdateQuads();
            //Iterate();
            //Iterate2();
        }
        public void UpdateQuads()
        {
            //while(true) //1 quad in the queue per frame, for eternity
            //{
                string[] keys = quads.Keys.ToArray();
                for (int i = 0; i < keys.Length; i++)
                {
                    if (quads.ContainsKey(keys[i]))
                    {
                        QuadComp quad = quads[keys[i]];
                        //quad.OnUpdate();
                        
                    }
                    if (i % 20 == 0)
                    {
                        //yield return null;
                    }
                }
            //}
        }
    }
    
    public class QuadComp : MonoBehaviour
    {
        int scatterCount = 0;
        public PQ quad;
        public GameObject go;
        public int subdiv;
        public ComputeComponent[] comps;
        public PostCompute[] postComps;
        public string[] keys;
        public float nearestSubdivisionLimit;
        public void Begin()
        {

            if (quad == null)
            {
                Debug.Log("Null quad");
                Destroy(this);
            }
            if (FlightGlobals.ActiveVessel == null)
            {
                return;
            }
            Debug.Log("Awake");
            scatterCount = ScatterBodies.scatterBodies[FlightGlobals.currentMainBody.name].scatters.Count;
            alreadyAdded = new bool[scatterCount];
            for (int i = 0; i < alreadyAdded.Length; i++)
            {
                alreadyAdded[i] = false;
            }
            keys = ScatterBodies.scatterBodies[FlightGlobals.currentMainBody.name].scatters.Keys.ToArray();
            comps = new ComputeComponent[scatterCount];
            postComps = new PostCompute[scatterCount];
            nearestSubdivisionLimit = (int)(((2 * Mathf.PI * FlightGlobals.currentMainBody.Radius) / 4) / (Mathf.Pow(2, FlightGlobals.currentMainBody.pqsController.maxLevel))) + 50;
            StartCoroutine(OnUpdate());
        }
        bool[] alreadyAdded;
        bool firstRun = true;
        public IEnumerator OnUpdate()
        {
            yield return new WaitForEndOfFrame();
            while (true)
            {
                if (quad == null)
                {
                    Debug.Log("Quad is null tf");
                }
                if (!QuadQueue.quads.ContainsKey(quad.name))
                {
                    Destroy(this);
                }
                if (FlightGlobals.ActiveVessel != null)
                {
                    //Subdivide by the maximum subdivision setting
                    SubdivideQuad();
                    for (int i = 0; i < keys.Length; i++)
                    {
                        Scatter thisScatter = ScatterBodies.scatterBodies[FlightGlobals.currentMainBody.name].scatters[keys[i]];
                        //Debug.Log("Scatter: " + thisScatter.scatterName);
                        float distance = Vector3.Distance(FlightGlobals.ActiveVessel.transform.position, quad.transform.position);
                        float limit = GetRangeLimit(thisScatter);
                        if (alreadyAdded[i] == false && distance <= limit && quad.isVisible)
                        {
                            mesh = Instantiate(quad.GetComponent<MeshFilter>().sharedMesh);
                            if (thisScatter.properties.subdivisionSettings.mode == SubdivisionMode.FixedRange)
                            {
                                SetupFixedScatter(thisScatter, quad, i);
                            }
                            else
                            {
                                SetupNearestQuadScatter(thisScatter, quad, i);
                            }
                            alreadyAdded[i] = true;
                        }
                        if (distance > limit || quad.isVisible == false && alreadyAdded[i] == true)
                        {
                            RemoveFixedScatter(quad, i);
                            alreadyAdded[i] = false;
                        }
                    }
                    

                }
                if (firstRun)
                {
                    firstRun = false;
                    yield return new WaitForSeconds(0.025f);
                }
                yield return new WaitForSeconds(0.333f);
            }

        }
        public void OnDestroy()
        {
            
            if (go != null) { Destroy(go); }
            if (mesh != null) { Destroy(mesh); }
            RemoveAllFixedScatters(quad);
        }
        public float GetRangeLimit(Scatter scatter)
        {
            if (scatter.properties.subdivisionSettings.mode == SubdivisionMode.FixedRange)
            {
                return scatter.properties.subdivisionSettings.range;
            }
            else
            { 
                return (int)(((2 * Mathf.PI * FlightGlobals.currentMainBody.Radius) / 4) / (Mathf.Pow(2, FlightGlobals.currentMainBody.pqsController.maxLevel)));
            }
        }
        public Mesh mesh;
        public Mesh subdividedMesh;
        void SetupFixedScatter(Scatter thisScatter, PQ quad, int index)
        {
            if (quad == null)
            {
                Debug.Log("quad null innit lol");
            }
            var quadMeshFilter = quad.GetComponent<MeshFilter>();
            var quadMeshRenderer = quad.GetComponent<MeshRenderer>();
            
            ComputeComponent comp = quad.gameObject.AddComponent<ComputeComponent>();
            comp.mesh = mesh;


            float[] distributionData = PQSMod_ScatterDistribute.scatterData.distributionData[thisScatter.scatterName].data[quad.name];//[quad.GetHashCode().ToString()]; //quad.gameObject.GetComponent<ScatterData>();
            comp.distributionNoise = distributionData;
            comp.subObjectCount = thisScatter.subObjectCount;
            comp.scatter = thisScatter;
            comp.quadSubdivision = quad.subdivision;
            int maxLevel = quad.sphereRoot.maxLevel;
            int maxLevelDiff = maxLevel - quad.subdivision;
            comp.quadSubdivisionDifference = (maxLevelDiff * 3) + 1;
            PostCompute postComp = quad.gameObject.AddComponent<PostCompute>();
            comp.pc = postComp;
            comp.quad = quad;
            comps[index] = comp;
            postComps[index] = postComp;
            
            //alreadyAdded[i] = true;
        }
        public bool alreadySubdivided = false;
        public GameObject newQuad;
        public void SubdivideQuad()
        {
            
            if (quad.subdivision != FlightGlobals.currentMainBody.pqsController.maxLevel)
            {
                return;
            }
            //yield return new WaitForSeconds(1);
            float distance = Vector3.Distance(FlightGlobals.ActiveVessel.transform.position, quad.transform.position);
            
            Vector3 planetNormal = Vector3.Normalize(FlightGlobals.ActiveVessel.transform.position - FlightGlobals.currentMainBody.transform.position);
            if (distance < nearestSubdivisionLimit && alreadySubdivided == false && quad != null)
            {
                Debug.Log("Subdividing");
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
                Mesh mesh = Instantiate(quadMeshFilter.sharedMesh);
                
                MeshHelper.Subdivide(mesh, 6);
                var newQuadMeshFilter = newQuad.AddComponent<MeshFilter>();
                newQuadMeshFilter.sharedMesh = mesh;


                quadMeshRenderer.enabled = true;   //Can use true here and just view the quad
                alreadySubdivided = true;
                newQuad.AddComponent<MeshRenderer>();
                newQuad.GetComponent<MeshRenderer>().enabled = false;
                //newQuad.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Standard"));
                subdividedMesh = mesh;
                
            }
            else if (distance >= nearestSubdivisionLimit && quad != null && quad.subdivision == FlightGlobals.currentMainBody.pqsController.maxLevel)
            {
                Destroy(newQuad);
                //var quadMeshRenderer = quad.GetComponent<MeshRenderer>();
                alreadySubdivided = false;
            }
        }
        void SetupNearestQuadScatter(Scatter thisScatter, PQ quad, int index)
        {
            if (quad == null)
            {
                Debug.Log("quad null innit lol");
            }
            var quadMeshFilter = quad.GetComponent<MeshFilter>();
            var quadMeshRenderer = quad.GetComponent<MeshRenderer>();

            ComputeComponent comp = quad.gameObject.AddComponent<ComputeComponent>();
            comp.mesh = subdividedMesh;


            float[] distributionData = new float[subdividedMesh.vertexCount];//PQSMod_ScatterDistribute.scatterData.distributionData[thisScatter.scatterName].data[quad.name];//[quad.GetHashCode().ToString()]; //quad.gameObject.GetComponent<ScatterData>();
            for (int i = 0; i < distributionData.Length; i++) { distributionData[i] = 1; }

            comp.distributionNoise = distributionData;
            comp.subObjectCount = thisScatter.subObjectCount;
            comp.scatter = thisScatter;
            comp.quadSubdivision = quad.subdivision;
            int maxLevel = quad.sphereRoot.maxLevel;
            int maxLevelDiff = maxLevel - quad.subdivision;
            comp.quadSubdivisionDifference = (maxLevelDiff * 3) + 1;
            PostCompute postComp = quad.gameObject.AddComponent<PostCompute>();
            comp.pc = postComp;
            comp.quad = quad;
            comps[index] = comp;
            postComps[index] = postComp;
        }
        public void RemoveFixedScatter(PQ quad, int index)
        {
            //ComputeComponent comp = comps[i];// quad.gameObject.GetComponent<ComputeComponent>();
            //PostCompute postComp = postComps[i];// quad.gameObject.GetComponent<PostCompute>();
            //ComputeComponent comp = quad.gameObject.GetComponent<ComputeComponent>();
            //PostCompute postComp = quad.gameObject.GetComponent<PostCompute>();
            //if (mesh != null) { Destroy(mesh); }
            if (comps[index] != null)
            {
                if (comps[index].mesh != null) { Destroy(comps[index].mesh); }
                Debug.Log("Destroying comp with scatter: " + comps[index].scatter.scatterName);
                Destroy(comps[index]);
            }
            if (postComps[index] != null)
            {
                Destroy(postComps[index]);
            }
        }
        public void RemoveAllFixedScatters(PQ quad)
        {
            //ComputeComponent comp = comps[i];// quad.gameObject.GetComponent<ComputeComponent>();
            //PostCompute postComp = postComps[i];// quad.gameObject.GetComponent<PostCompute>();
            //ComputeComponent comp = quad.gameObject.GetComponent<ComputeComponent>();
            //PostCompute postComp = quad.gameObject.GetComponent<PostCompute>();
            for (int i = 0; i < scatterCount; i++)
            {
                int index = i;
                if (mesh != null) { Destroy(mesh); }
                if (comps[index] != null)
                {
                    if (comps[index].mesh != null) { Destroy(comps[index].mesh); }
                    Destroy(comps[index]);
                }
                if (postComps[index] != null)
                {
                    Destroy(postComps[index]);
                }
            }
        }
        public void DestroyGO(GameObject go)
        {
            Destroy(go);
        }
    }
    public class PQSMod_ParallaxScatter : PQSMod
    {
        public override void OnQuadBuilt(PQ quad)
        {
            if (quad.subdivision > ScatterBodies.scatterBodies[quad.sphereRoot.name].minimumSubdivision)
            {
                QuadComp qComp = quad.gameObject.AddComponent<QuadComp>();
                qComp.quad = quad;
                qComp.subdiv = quad.subdivision;
                qComp.Begin();
                //qComp.Start();
                if (!QuadQueue.quads.ContainsKey(quad.name)) { QuadQueue.quads.Add(quad.name, quad.GetComponent<QuadComp>()); }
            }
        }
        public override void OnQuadDestroy(PQ quad)
        {
            if (QuadQueue.quads.ContainsKey(quad.name)) 
            { 
                QuadQueue.quads[quad.name].RemoveAllFixedScatters(quad);
                Destroy(QuadQueue.quads[quad.name]);
                QuadQueue.quads.Remove(quad.name); 
                
            }
        }
        //public override void OnQuadBuilt(PQ quad)
        //{
        //    quads.Add(quad.GetHashCode().ToString(), quad);
        //       
        //    //if (quad == null || FlightGlobals.currentMainBody == null || FlightGlobals.currentMainBody.pqsController == null)
        //    //{
        //    //    return;
        //    //}
        //    //if (quad != null && HighLogic.LoadedScene == GameScenes.FLIGHT && quad.subdivision >= FlightGlobals.currentMainBody.pqsController.maxLevel - 4)
        //    //{
        //    //    QuadMeshes sm = quad.gameObject.AddComponent<QuadMeshes>();
        //    //    sm.body = ScatterBodies.scatterBodies[FlightGlobals.currentMainBody.name];
        //    //    sm.quad = quad;
        //    //    //quad.onDestroy += DestroyComps;
        //    //}
        //}
        //public override void OnQuadDestroy(PQ quad)
        //{
        //    quads.Remove(quad.GetHashCode().ToString());
        //    //QuadMeshes subComp = quad.gameObject.GetComponent<QuadMeshes>();
        //    //if (subComp != null)
        //    //{
        //    //    Destroy(subComp);
        //    //}
        //}
        //public void DestroyComps(PQ quad)
        //{
        //    Debug.Log("OnDestroy called via delegate");
        //    GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //    go.transform.parent = quad.transform;
        //    go.transform.localScale = new Vector3(200, 200, 200);
        //    go.transform.localPosition = Vector3.zero;
        //    go.transform.localRotation = Quaternion.identity;
        //
        //}
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
    //public class QuadMeshes : MonoBehaviour
    //{
    //    public PQ quad;
    //    public Material oldMaterial;
    //    bool alreadySubdivided = false;
    //    Material transparent = new Material(Shader.Find("Unlit/Transparent"));
    //    GameObject newQuad;
    //    public ScatterBody body;
    //    bool wasEverSubdivided = false;
    //    public ComputeComponent[] comps;
    //    public PostCompute[] postComps;
    //    
    //    public void DestroyComps(PQ quad)
    //    {
    //        OnDestroy();
    //        Debug.Log("OnDestroy called via delegate");
    //        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
    //        go.transform.parent = quad.transform;
    //        go.transform.localScale = new Vector3(200, 200, 200);
    //        go.transform.localPosition = Vector3.zero;
    //        go.transform.localRotation = Quaternion.identity;
    //
    //    }
    //    void Start()
    //    {
    //        comps = new ComputeComponent[body.scatters.Values.Count];
    //        postComps = new PostCompute[body.scatters.Values.Count];
    //        alreadyAdded = new bool[body.scatters.Values.Count];
    //        //quad.onDestroy += DestroyComps;
    //        
    //        InvokeRepeating("CheckRange", 0f, 1f);
    //        InvokeRepeating("CheckFixedRange", 0f, 1f);
    //    }
    //
    //    void CheckRange()
    //    {
    //        return;
    //        if (quad.subdivision != FlightGlobals.currentMainBody.pqsController.maxLevel)
    //        {
    //            return;
    //        }
    //        //yield return new WaitForSeconds(1);
    //        float distance = Vector3.Distance(FlightGlobals.ActiveVessel.transform.position, quad.transform.position);
    //        float limit = (int)(((2 * Mathf.PI * FlightGlobals.currentMainBody.Radius) / 4) / (Mathf.Pow(2, FlightGlobals.currentMainBody.pqsController.maxLevel)));
    //        Vector3 planetNormal = Vector3.Normalize(FlightGlobals.ActiveVessel.transform.position - FlightGlobals.currentMainBody.transform.position);
    //        if (distance < limit && alreadySubdivided == false && quad != null)
    //        {
    //            var quadMeshFilter = quad.GetComponent<MeshFilter>();
    //            var quadMeshRenderer = quad.GetComponent<MeshRenderer>();
    //
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
    //
    //            oldMaterial = quad.GetComponent<MeshRenderer>().sharedMaterial;
    //            Mesh mesh = Instantiate(quadMeshFilter.sharedMesh);
    //            //Mesh mesh = GameDatabase.Instance.GetModel("Parallax_StockTextures/_Scatters/Models/tallgrass").GetComponent<MeshFilter>().mesh;
    //            MeshHelper.Subdivide(mesh, 6);
    //            var newQuadMeshFilter = newQuad.AddComponent<MeshFilter>();
    //            newQuadMeshFilter.sharedMesh = mesh;
    //            //var newQuadMeshRenderer = newQuad.AddComponent<MeshRenderer>();
    //            //newQuadMeshRenderer.sharedMaterial = Instantiate(oldMaterial);//new Material(ScatterShaderHolder.GetShader("Custom/Wireframe"));//quadMeshRenderer.sharedMaterial;
    //            //newQuadMeshRenderer.enabled = false;
    //            
    //
    //            quadMeshRenderer.enabled = true;
    //            //quadMeshRenderer.material = transparent;
    //            //quadMeshRenderer.material.SetTexture("_MainTex", Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(t => t.name == "Parallax/BlankAlpha"));
    //            alreadySubdivided = true;
    //            wasEverSubdivided = true;
    //            string[] keys = body.scatters.Keys.ToArray();
    //
    //            for (int i = 0; i < body.scatters.Count; i++)
    //            {
    //                Scatter thisScatter = body.scatters[keys[i]];
    //                if (thisScatter.properties.subdivisionSettings.mode == SubdivisionMode.NearestQuads)
    //                {
    //                    ComputeComponent comp = newQuad.gameObject.AddComponent<ComputeComponent>();
    //                    comp.mesh = mesh;
    //                    comp.subObjectCount = thisScatter.subObjectCount;
    //                    comp.scatter = thisScatter;
    //                    PostCompute postComp = newQuad.gameObject.AddComponent<PostCompute>();
    //                    comp.pc = postComp;
    //                    float[] distributionData = PQSMod_ScatterDistribute.scatterData.distributionData[thisScatter.scatterName].data[quad.GetHashCode().ToString()];//[quad.GetHashCode().ToString()]; //quad.gameObject.GetComponent<ScatterData>();
    //                    comp.distributionNoise = distributionData;
    //                }
    //            }
    //        }
    //        else if (distance >= limit && quad != null && quad.subdivision == FlightGlobals.currentMainBody.pqsController.maxLevel)
    //        {
    //            if (wasEverSubdivided)
    //            {
    //                ComputeComponent comp = null;
    //                PostCompute postComp = null;
    //                if (newQuad != null)
    //                {
    //                    comp = newQuad.GetComponent<ComputeComponent>();
    //                    postComp = newQuad.GetComponent<PostCompute>();
    //                }
    //
    //                if (comp != null)
    //                {
    //                    Destroy(comp);
    //                }
    //                if (postComp != null)
    //                {
    //                    Destroy(postComp);
    //                }
    //                Destroy(newQuad);
    //                var quadMeshRenderer = quad.GetComponent<MeshRenderer>();
    //                if (oldMaterial != null)
    //                {
    //                    quadMeshRenderer.sharedMaterial = FlightGlobals.currentMainBody.pqsController.surfaceMaterial;
    //                    quadMeshRenderer.enabled = true;
    //                    Debug.Log("Swapped out old material");
    //                }
    //                else
    //                {
    //                    Debug.Log("Old material is null");
    //                }
    //                alreadySubdivided = false;
    //                wasEverSubdivided = false;
    //            }
    //
    //
    //
    //        }
    //    }
    //    bool[] alreadyAdded;
    //    int initialSubdivision = 0;
    //    bool notBuilt = false;
    //    void CheckFixedRange()
    //    {
    //
    //        int maxLevel = FlightGlobals.currentMainBody.pqsController.maxLevel;
    //        int maxLevelDiff = maxLevel - quad.subdivision;
    //        string[] keys = body.scatters.Keys.ToArray();
    //        for (int i = 0; i < body.scatters.Count; i++)
    //        {
    //            Scatter thisScatter = body.scatters[keys[i]];
    //            if (thisScatter.properties.subdivisionSettings.mode == SubdivisionMode.FixedRange)
    //            {
    //                float distance = Vector3.Distance(FlightGlobals.ActiveVessel.transform.position, quad.transform.position);
    //                float limit = thisScatter.properties.subdivisionSettings.range;
    //                Vector3 planetNormal = Vector3.Normalize(FlightGlobals.ActiveVessel.transform.position - FlightGlobals.currentMainBody.transform.position);
    //                if (distance < limit && alreadyAdded[i] == false && quad != null)
    //                {
    //                    var quadMeshFilter = quad.GetComponent<MeshFilter>();
    //                    var quadMeshRenderer = quad.GetComponent<MeshRenderer>();
    //                    Mesh mesh = quadMeshFilter.sharedMesh;
    //                    ComputeComponent comp = quad.gameObject.AddComponent<ComputeComponent>();
    //                    comp.mesh = mesh;
    //
    //
    //                    float[] distributionData = PQSMod_ScatterDistribute.scatterData.distributionData[thisScatter.scatterName].data[quad.GetHashCode().ToString()];//[quad.GetHashCode().ToString()]; //quad.gameObject.GetComponent<ScatterData>();
    //                    comp.distributionNoise = distributionData;
    //                    comp.subObjectCount = thisScatter.subObjectCount;
    //                    comp.scatter = thisScatter;
    //                    comp.quadSubdivisionDifference = (maxLevelDiff * 2) + 1;
    //                    PostCompute postComp = quad.gameObject.AddComponent<PostCompute>();
    //                    if (notBuilt) { postComp.notBuilt = true; }
    //                    comp.pc = postComp;
    //
    //                    comps[i] = comp;
    //                    postComps[i] = postComp;
    //
    //                    alreadyAdded[i] = true;
    //                }
    //                else if (distance > limit && quad != null)
    //                {
    //                    ComputeComponent comp = comps[i];// quad.gameObject.GetComponent<ComputeComponent>();
    //                    PostCompute postComp = postComps[i];// quad.gameObject.GetComponent<PostCompute>();
    //                    if (comp != null)
    //                    {
    //                        Destroy(comp);
    //                    }
    //                    if (postComp != null)
    //                    {
    //                        Destroy(postComp);
    //                    }
    //                    alreadyAdded[i] = false;
    //                }
    //            }
    //        }
    //
    //
    //    }
    //    void DetermineDistanceLimit()
    //    {
    //
    //    }
    //    void OnDestroy()
    //    {
    //        Debug.Log("OnDestroy called");
    //        if (newQuad != null)
    //        {
    //            Destroy(newQuad);
    //            Destroy(transparent);
    //        }
    //        var computeComp = quad.gameObject.GetComponent<ComputeComponent>();
    //        var postComp = quad.gameObject.GetComponent<PostCompute>();
    //        if (computeComp != null)
    //        {
    //            Debug.Log("Destroyed computecomp");
    //            Destroy(computeComp);
    //        }
    //        if (postComp != null)
    //        {
    //            Debug.Log("Destroyed postcomp");
    //            Destroy(postComp);
    //        }
    //    }
    //}
}
