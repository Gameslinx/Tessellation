using System;
using UnityEngine;
using Kopernicus.Configuration.ModLoader;
using Kopernicus.Configuration;
using Kopernicus;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Kopernicus.ConfigParser.Attributes;
using Kopernicus.ConfigParser.BuiltinTypeParsers;
using Kopernicus.ConfigParser.Enumerations;
using Kopernicus.ConfigParser.Interfaces;
using Kopernicus.Configuration.Parsing;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using UnityEngine.Rendering;
using Parallax;
using ParallaxQualityLibrary;
using KSP.UI.Screens.DebugToolbar;
using Smooth.Delegates;
using Kopernicus.Components;
using AdvancedSubdivision;

[assembly: KSPAssembly("ParallaxSubdivisionMod", 1, 0)]
[assembly: KSPAssemblyDependency("Kopernicus", 1, 0)]
[assembly: KSPAssemblyDependency("Parallax", 1, 0)]
[assembly: KSPAssemblyDependency("ParallaxQualityLibrary", 1, 0)]
[assembly: KSPAssemblyDependency("AdvancedSubdivision", 1, 0)]
//[assembly: KSPAssemblyDependency("AdvancedSubdivision", 1, 0)]
namespace PQSModExpansion
{

    public static class QuadMeshDictionary
    {
        public static Dictionary<string, GameObject> subdividedQuadList = new Dictionary<string, GameObject>();
        public static Dictionary<float, GameObject> quadsByDistance = new Dictionary<float, GameObject>();
    }
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class AdvancedSubdivisionManager : MonoBehaviour
    {
        
        List<GameObject> lastQuadList;
        string nameCheck = "";
        bool force = false;
        public void Update()
        {
            if (FlightGlobals.ActiveVessel == null)
            {
                return;
            }
            //return;
            if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.Alpha2))
            {
                force = true;
            }
            if (FlightGlobals.ActiveVessel == null)
            {
                return;
            }
            List<GameObject> quads = QuadMeshDictionary.subdividedQuadList.Values.ToList();
            quads = quads.OrderBy(x => Vector3.Distance(FlightGlobals.ActiveVessel.transform.position, x.transform.position)).ToList();
            string newNameCheck = "";
            foreach (GameObject quad in quads)
            {
                newNameCheck += quad.name.ToString();
            }


            if (nameCheck != newNameCheck || force == true)
            {
                nameCheck = newNameCheck;
                AdvancedSubdivisionGlobal.subQuads = quads.ToArray();
            }
            else
            {
                return;
            }

            for (int i = 0; i < quads.Count; i++)
            {
                if (i == 0)
                {
                    MakePrimary(quads[i], i, quads.Count);
                }
                else
                {
                    MakeSecondary(quads[i], i, quads.Count);
                }
            }
            for (int i = 1; i < quads.Count; i++)
            {
                quads[i].GetComponent<SubQuad>().ForceUpdate();
            }
            force = false;
        }
        public void MakePrimary(GameObject quad, int quadNumber, int quadCount)
        {
            if (quad.GetComponent<SubQuad>() != null)
            {
                var comp = quad.GetComponent<SubQuad>();
                if (comp.newQuad != null)
                {
                    Destroy(comp.newQuad);
                    quad.GetComponent<MeshFilter>().sharedMesh = Instantiate(comp.originalMesh);
                }
                Destroy(quad.GetComponent<SubQuad>());
            }
            if (quad.GetComponent<AdvancedSubdivision.AdvancedSubdivision>() != null)
            {
                return;
            }
            quad.AddComponent<AdvancedSubdivision.AdvancedSubdivision>();
        }
        public void MakeSecondary(GameObject quad, int quadNumber, int quadCount)
        {
            if (quad.GetComponent<AdvancedSubdivision.AdvancedSubdivision>() != null)
            {
                var comp = quad.GetComponent<AdvancedSubdivision.AdvancedSubdivision>();
                if (comp.newQuad != null)
                {
                    Destroy(comp.newQuad);
                    quad.GetComponent<MeshFilter>().sharedMesh = Instantiate(comp.originalMesh);
                }
                Destroy(quad.GetComponent<AdvancedSubdivision.AdvancedSubdivision>());
            }
            if (quad.GetComponent<SubQuad>() == null)
            {
                quad.AddComponent<SubQuad>();
            }
            
            //Initialize ASSubQuad
            var component = quad.GetComponent<SubQuad>();
            //component.centreWorldPos = quad.transform.position;
            component.nearest = true;
            component.furthest = false;
            if (quadNumber == 1)
            {
                component.nearest = true;
            }
            if (quadNumber == quadCount - 1)
            {
                component.furthest = true;
                component.nearest = false;
            }
            if (component.nearest == true && component.furthest == true)
            {
                component.furthest = false;
            }
            if (quadCount == 2)
            {
                component.nearest = true; //Only 2 quads. It's both nearest and furthest but we need it to be set to nearest
                component.furthest = false;
            }
        }
    }

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class ClearDictionary2 : MonoBehaviour
    {
        public void Start()
        {
            foreach (KeyValuePair<string, GameObject> quad in QuadMeshDictionary.subdividedQuadList)
            {
                quad.Value.GetComponent<MeshRenderer>().enabled = false;
                Destroy(quad.Value);
                Debug.Log("Destroyed a tile on scene change");
            }
            QuadMeshDictionary.subdividedQuadList.Clear();
            Debug.Log("Cleared the quad mesh dictionary");
        }
    }
    public static class GrassMaterial
    {
        public static Material grassMaterial;
    }
    public class QuadMeshes : MonoBehaviour
    {
        public bool subdivided = false;
        public PQ quad;
        public GameObject newQuad;
        public GameObject collisionQuad;
        public int subdivisionLevel = 1;
        Material transparent = new Material(Shader.Find("Unlit/Transparent"));
        private float distance = 0;
        public bool overrideDistLimit = false;
        public int customDistLimit = 1000;
        Material trueMaterial;

        public void OnDestroy()
        {
            Destroy(newQuad);
            Destroy(collisionQuad);
            Debug.Log("[SubdivMod] Destroyed");
        }
        public void Start()
        {
            InvokeRepeating("CheckSubdivision", 1f, 1f);
        }
        public void CheckSubdivision()
        {
            if (quad != null && FlightGlobals.ActiveVessel != null)
            {

                distance = Vector3.Distance(FlightGlobals.ActiveVessel.transform.position, quad.transform.position);
                int distLimit = (int)(((2 * Mathf.PI * FlightGlobals.currentMainBody.Radius) / 4) / (Mathf.Pow(2, FlightGlobals.currentMainBody.pqsController.maxLevel)));
                if (overrideDistLimit)
                {
                    distLimit = customDistLimit;
                }
                if (distance < distLimit && subdivided == false)
                {
                    subdivided = true;

                    var quadMeshFilter = quad.GetComponent<MeshFilter>();
                    var quadMeshRenderer = quad.GetComponent<MeshRenderer>();



                    newQuad = new GameObject();
                    newQuad.name = quad.name + "FAKE";
                    newQuad.transform.position = quad.gameObject.transform.position;
                    newQuad.transform.rotation = quad.gameObject.transform.rotation;
                    newQuad.transform.parent = quad.gameObject.transform;
                    newQuad.transform.localPosition = Vector3.zero;
                    newQuad.transform.localRotation = Quaternion.identity;
                    newQuad.transform.localScale = Vector3.one;
                    newQuad.transform.parent = quad.gameObject.transform;
                    newQuad.layer = 15;

                    collisionQuad = new GameObject();   //Raycast for collisions are done on this layer
                    collisionQuad.name = quad.name + "FAKE COLLIDER";
                    collisionQuad.transform.position = quad.gameObject.transform.position;
                    collisionQuad.transform.rotation = quad.gameObject.transform.rotation;
                    collisionQuad.transform.parent = quad.gameObject.transform;
                    collisionQuad.transform.localPosition = Vector3.zero;
                    collisionQuad.transform.localRotation = Quaternion.identity;
                    collisionQuad.transform.localScale = Vector3.one;
                    collisionQuad.transform.parent = quad.gameObject.transform;
                    collisionQuad.layer = 29;
                    Debug.Log("1");

                    Physics.IgnoreLayerCollision(0, 14);
                    Debug.Log("1");

                    trueMaterial = quad.GetComponent<MeshRenderer>().sharedMaterial;    //reference to sharedMaterial

                    Mesh mesh = Instantiate(quadMeshFilter.sharedMesh);
                    if (subdivisionLevel > 1)
                    {
                        mesh = MeshHandler.Subdivide(mesh, 14, 14);
                    }
                    
                    Debug.Log("1");
                    newQuad.AddComponent<MeshFilter>();
                    var newQuadMeshFilter = newQuad.GetComponent<MeshFilter>();
                    collisionQuad.AddComponent<MeshFilter>();
                    collisionQuad.GetComponent<MeshFilter>().sharedMesh = mesh;
                    Debug.Log("1");
                    collisionQuad.AddComponent<MeshRenderer>();
                    collisionQuad.GetComponent<MeshRenderer>().enabled = false;
                    Debug.Log("1");
                    newQuadMeshFilter.sharedMesh = mesh;
                    newQuad.AddComponent<MeshRenderer>();
                    var newQuadMeshRenderer = newQuad.GetComponent<MeshRenderer>();
                    //Material[] newMaterials = new Material[1];
                    collisionQuad.AddComponent<MeshCollider>();
                    collisionQuad.GetComponent<MeshCollider>().sharedMesh = mesh;
                    Debug.Log("1");
                    //newQuadMeshRenderer.materials = new Material[2];
                    //newQuadMeshRenderer.materials[0] = FlightGlobals.currentMainBody.pqsController.surfaceMaterial;
                    //newQuadMeshRenderer.materials[1] = GetGrassMaterial();
                    newQuadMeshRenderer.sharedMaterial = quad.GetComponent<MeshRenderer>().sharedMaterial;//FlightGlobals.currentMainBody.pqsController.surfaceMaterial;//GrassMaterial.grassMaterial;
                    Debug.Log("NewQuad Subdiv material: " + newQuadMeshRenderer.sharedMaterial.name);
                    //newMaterials[0] = GetGrassMaterial();
                    //newMaterials[1] = GetGrassMaterial();
                    //newQuadMeshRenderer.materials = newMaterials;
                    newQuadMeshRenderer.enabled = true;

                    quadMeshRenderer.material = transparent;
                    quadMeshRenderer.material.SetTexture("_MainTex", Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(t => t.name == "Parallax/BlankAlpha"));
                    Debug.Log("NewQuad Subdiv material 2: " + newQuadMeshRenderer.sharedMaterial.name);
                    QuadMeshDictionary.subdividedQuadList.Add(newQuad.name, newQuad);

                }
                else if (distance >= distLimit && subdivided == true)
                {
                    string newQuadName = quad.name + "FAKE";
                    if (QuadMeshDictionary.subdividedQuadList.ContainsKey(newQuadName))
                    {
                        QuadMeshDictionary.subdividedQuadList[newQuadName].DestroyGameObject();    //Change to Destroy()
                        QuadMeshDictionary.subdividedQuadList.Remove(newQuadName);
                        quad.GetComponent<QuadMeshes>().subdivided = false;
                        quad.GetComponent<MeshRenderer>().sharedMaterial = trueMaterial;  //Don't make it transparent anymore
                        Destroy(collisionQuad);
                    }
                }
            }

        }
        public Material GetGrassMaterial()
        {
            return GrassMaterial.grassMaterial;
        }
    }
    public class PQSMod_Subdivide : PQSMod
    {


        public int subdivisionLevel = 1;
        public bool overrideDistLimit = false;
        public int customDistLimit = 1000;
        public static bool needed = false;
        Material fuckery = new Material(Shader.Find("Standard"));
        public override void OnVertexBuild(PQS.VertexBuildData data)
        {
            //Vector3 normal = Vector3.Normalize(LatLon.GetWorldSurfacePosition(FlightGlobals.currentMainBody.BodyFrame, FlightGlobals.currentMainBody.transform.position, FlightGlobals.currentMainBody.Radius, data.latitude, data.longitude, 0) - FlightGlobals.currentMainBody.transform.position);
            try
            {
                //data.vertHeight = (FlightGlobals.currentMainBody.Radius * 2) + 120 - data.vertHeight;
            }
            catch { }
        }
        public override void OnQuadBuilt(PQ quad)
        {
            if (FlightGlobals.currentMainBody == null)
            {
                return;
            }
            //try
            //{
            //    if (needed == true)
            //    {
            //        return;
            //    }
            //    if (quad.GetComponent<MeshCollider>() != null)
            //    {
            //        quad.GetComponent<MeshCollider>().enabled = false;
            //
            //    }
            //    if (quad.GetComponent<Collider>() != null)
            //    {
            //        quad.GetComponent<Collider>().enabled = false;
            //    }
            //    if (quad.GetComponent<MeshFilter>() != null)
            //    {
            //        //quad.GetComponent<MeshFilter>().transform.localScale *= -1;
            //        //quad.GetComponent<MeshFilter>().transform.Rotate(Vector3.Normalize(quad.transform.position - FlightGlobals.currentMainBody.transform.position), 180);
            //        //int[] temp = quad.GetComponent<MeshFilter>().sharedMesh.triangles;
            //        //Array.Reverse(quad.GetComponent<MeshFilter>().sharedMesh.triangles);
            //    }
            //
            //}
            //catch
            //{
            //
            //}
            //SUBDIVISION MOD
            if (quad is null || quad.mesh == null)
            {
                Debug.Log("Quad is null and has been caught. Distance to vessel is: ");
                try
                {
                    Debug.Log(quad.transform.position - FlightGlobals.ActiveVessel.transform.position);
                }
                catch
                {
                    Debug.Log("Unable to get distance to vessel");
                }
            }
            try
            {
                if (quad != null && quad.subdivision == FlightGlobals.currentMainBody.pqsController.maxLevel && HighLogic.LoadedScene == GameScenes.FLIGHT)
                {
                    quad.gameObject.AddComponent<QuadMeshes>();
                    quad.gameObject.GetComponent<QuadMeshes>().quad = quad;
                    quad.gameObject.GetComponent<QuadMeshes>().subdivisionLevel = (int)(subdivisionLevel * 1);
                    quad.gameObject.GetComponent<QuadMeshes>().overrideDistLimit = overrideDistLimit;
                    quad.gameObject.GetComponent<QuadMeshes>().customDistLimit = customDistLimit;
                }
            }
            catch (Exception e)
            {
                Debug.Log("[Parallax] Subdivision Error:\n" + e.ToString());
            }

            //ADAPTIVE PARALLAX

            try
            {
                double highPoint = 0;
                double lowPoint = 0;

                float lowStart = ParallaxBodies.parallaxBodies[FlightGlobals.currentMainBody.name]._LowStart;
                float lowEnd = ParallaxBodies.parallaxBodies[FlightGlobals.currentMainBody.name]._LowEnd;
                float highStart = ParallaxBodies.parallaxBodies[FlightGlobals.currentMainBody.name]._HighStart;
                float highEnd = ParallaxBodies.parallaxBodies[FlightGlobals.currentMainBody.name]._HighEnd;
                try
                {
                    highPoint = quad.meshVertMax;
                    lowPoint = quad.meshVertMin;
                }
                catch
                {
                    Debug.Log("Highpoint / Lowpoint not set!");
                }
                double quadAltitude = Vector3.Distance(quad.transform.position, FlightGlobals.currentMainBody.transform.position);
                quad.GetComponent<MeshRenderer>().sharedMaterial = ParallaxBodies.parallaxBodies[FlightGlobals.currentMainBody.name].full.parallaxMaterial;

                if (highPoint <= highStart)
                {
                    //LOW-MID
                    quad.GetComponent<MeshRenderer>().sharedMaterial = ParallaxBodies.parallaxBodies[FlightGlobals.currentMainBody.name].doubleLow.parallaxMaterial;
                }
                if (lowPoint >= lowEnd)
                {
                    //MID-HIGH
                    quad.GetComponent<MeshRenderer>().sharedMaterial = ParallaxBodies.parallaxBodies[FlightGlobals.currentMainBody.name].doubleHigh.parallaxMaterial;
                }


                if (highPoint < lowStart)
                {
                    //LOW
                    quad.GetComponent<MeshRenderer>().sharedMaterial = ParallaxBodies.parallaxBodies[FlightGlobals.currentMainBody.name].singleLow.parallaxMaterial;
                    //quad.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_SurfaceTexture", ParallaxBodies.parallaxBodies[FlightGlobals.currentMainBody.name]._LoadTexture(ParallaxBodies.parallaxBodies[FlightGlobals.currentMainBody.name]._SurfaceTexture));
                    //quad.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_BumpMap", ParallaxBodies.parallaxBodies[FlightGlobals.currentMainBody.name]._LoadTexture(ParallaxBodies.parallaxBodies[FlightGlobals.currentMainBody.name]._SurfaceTextureBumpMap));
                }
                if (highPoint < highStart && lowPoint > lowEnd)
                {
                    //MID
                    quad.GetComponent<MeshRenderer>().sharedMaterial = ParallaxBodies.parallaxBodies[FlightGlobals.currentMainBody.name].singleMid.parallaxMaterial;
                    //quad.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_SurfaceTexture", ParallaxBodies.parallaxBodies[FlightGlobals.currentMainBody.name]._LoadTexture(ParallaxBodies.parallaxBodies[FlightGlobals.currentMainBody.name]._SurfaceTextureMid));
                    //quad.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_BumpMap", ParallaxBodies.parallaxBodies[FlightGlobals.currentMainBody.name]._LoadTexture(ParallaxBodies.parallaxBodies[FlightGlobals.currentMainBody.name]._BumpMapMid));
                }
                if (lowPoint > highEnd)
                {
                    //HIGH
                    quad.GetComponent<MeshRenderer>().sharedMaterial = ParallaxBodies.parallaxBodies[FlightGlobals.currentMainBody.name].singleHigh.parallaxMaterial;
                    //quad.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_SurfaceTexture", ParallaxBodies.parallaxBodies[FlightGlobals.currentMainBody.name]._LoadTexture(ParallaxBodies.parallaxBodies[FlightGlobals.currentMainBody.name]._SurfaceTextureHigh));
                    //quad.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_BumpMap", ParallaxBodies.parallaxBodies[FlightGlobals.currentMainBody.name]._LoadTexture(ParallaxBodies.parallaxBodies[FlightGlobals.currentMainBody.name]._BumpMapHigh));
                }

                if (lowPoint < lowStart && highPoint > highStart)
                {
                    //LOW-MID-HIGH
                    quad.GetComponent<MeshRenderer>().sharedMaterial = ParallaxBodies.parallaxBodies[FlightGlobals.currentMainBody.name].full.parallaxMaterial;
                }
                //Steep calculation
                Vector3 normalVector = Vector3.Normalize(quad.transform.position - FlightGlobals.currentMainBody.transform.position);
                bool hasSteep = false;

                if (quadLocalMaxSlope < 0.975)   //0.05 = 1/20
                {
                    hasSteep = true;
                }
                if (hasSteep)
                {
                    if (highPoint < lowStart)
                    {
                        //LOW
                        quad.GetComponent<MeshRenderer>().sharedMaterial = ParallaxBodies.parallaxBodies[FlightGlobals.currentMainBody.name].singleSteepLow.parallaxMaterial;
                        //quad.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_SurfaceTexture", ParallaxBodies.parallaxBodies[FlightGlobals.currentMainBody.name]._LoadTexture(ParallaxBodies.parallaxBodies[FlightGlobals.currentMainBody.name]._SurfaceTexture));
                        //quad.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_BumpMap", ParallaxBodies.parallaxBodies[FlightGlobals.currentMainBody.name]._LoadTexture(ParallaxBodies.parallaxBodies[FlightGlobals.currentMainBody.name]._SurfaceTextureBumpMap));
                    }
                    if (highPoint < highStart &&
                        lowPoint > ParallaxBodies.parallaxBodies[FlightGlobals.currentMainBody.name]._LowEnd)
                    {
                        //MID
                        quad.GetComponent<MeshRenderer>().sharedMaterial = ParallaxBodies.parallaxBodies[FlightGlobals.currentMainBody.name].singleSteepMid.parallaxMaterial;
                        //quad.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_SurfaceTexture", ParallaxBodies.parallaxBodies[FlightGlobals.currentMainBody.name]._LoadTexture(ParallaxBodies.parallaxBodies[FlightGlobals.currentMainBody.name]._SurfaceTextureMid));
                        //quad.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_BumpMap", ParallaxBodies.parallaxBodies[FlightGlobals.currentMainBody.name]._LoadTexture(ParallaxBodies.parallaxBodies[FlightGlobals.currentMainBody.name]._BumpMapMid));
                    }
                    if (lowPoint > ParallaxBodies.parallaxBodies[FlightGlobals.currentMainBody.name]._HighEnd)
                    {
                        //HIGH
                        quad.GetComponent<MeshRenderer>().sharedMaterial = ParallaxBodies.parallaxBodies[FlightGlobals.currentMainBody.name].singleSteepHigh.parallaxMaterial;
                        //quad.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_SurfaceTexture", ParallaxBodies.parallaxBodies[FlightGlobals.currentMainBody.name]._LoadTexture(ParallaxBodies.parallaxBodies[FlightGlobals.currentMainBody.name]._SurfaceTextureHigh));
                        //quad.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_BumpMap", ParallaxBodies.parallaxBodies[FlightGlobals.currentMainBody.name]._LoadTexture(ParallaxBodies.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.BumpMapHigh));
                    }
                }

            }
            catch (Exception e)
            {
                Debug.Log("[Parallax] Subdivision Error (Adaptive Parallax):\n" + e.ToString());
            }

        }
        public override void OnQuadDestroy(PQ quad)
        {
            Debug.Log("Beginning quad destruction");
            if (quad.gameObject.GetComponent<QuadMeshes>() != null)
            {
                try
                {
                    //QuadMeshDictionary.subdividedQuadList[quad.gameObject.GetComponent<QuadMeshes>().newQuad.name].GetComponent<MeshRenderer>().enabled = false;
                    //QuadMeshDictionary.subdividedQuadList[quad.gameObject.GetComponent<QuadMeshes>().newQuad.name].DestroyGameObject();
                    QuadMeshDictionary.subdividedQuadList.Remove(quad.gameObject.GetComponent<QuadMeshes>().newQuad.name);
                }
                catch
                {
                }
                //var comp = QuadMeshDictionary.subdividedQuadList[quad.gameObject.GetComponent<QuadMeshes>().newQuad.name].GetComponent<AdvancedSubdivision.AdvancedSubdivision>();
                //if (comp != null)
                //{
                //    Destroy(comp.newQuad);
                //}

                Debug.Log("Destroying quadmesh component:");
                Destroy(quad.gameObject.GetComponent<QuadMeshes>());    //Quad is not maxLevel anymore, remove the damn thing

            }

        }
        public Vector3 maxVertexPosition;
        public Vector3 minVertexPosition;
        public double maxHeight = -10000000;
        public double minHeight = 10000000;
        public PQ currentBuildQuad;
        public double quadLocalMaxSlope = -1;
        public override void OnVertexBuildHeight(PQS.VertexBuildData data)
        {
            try
            {
                if (data.buildQuad == null)
                {
                    return;
                }
                if (currentBuildQuad != data.buildQuad)
                {
                    currentBuildQuad = data.buildQuad;
                }
                else
                {
                    if (data.vertHeight > maxHeight)
                    {
                        maxHeight = data.vertHeight;
                        maxVertexPosition = LatLon.GetWorldSurfacePosition(FlightGlobals.currentMainBody.BodyFrame, FlightGlobals.currentMainBody.position, FlightGlobals.currentMainBody.Radius, data.latitude, data.longitude, maxHeight);
                    }
                    if (data.vertHeight < minHeight)
                    {
                        minHeight = data.vertHeight;
                        minVertexPosition = LatLon.GetWorldSurfacePosition(FlightGlobals.currentMainBody.BodyFrame, FlightGlobals.currentMainBody.position, FlightGlobals.currentMainBody.Radius, data.latitude, data.longitude, maxHeight);
                    }
                    //float slope = abs(dot(normalize(o.world_vertex - _PlanetOrigin), normalize(o.normalDir)));
                    //slope = pow(slope, _SteepPower);
                    float slope = Math.Abs(Vector3.Dot(Vector3.Normalize(maxVertexPosition - minVertexPosition), Vector3.Normalize(data.buildQuad.transform.position - FlightGlobals.currentMainBody.transform.position)));

                    //Slope is now a value between 0 (Perpendicular to direction from terrain to planet centre) and 1 (Straight up fucking vertical)
                    slope = (float)Math.Pow(slope, ParallaxBodies.parallaxBodies[FlightGlobals.currentMainBody.name]._SteepPower);
                    //Slope is now an approximation to the slope calculated in the shader

                    quadLocalMaxSlope = slope;  //OnQuadBuilt happens at the end of each OnVertexBuildHeight
                }
            }
            catch
            {

            }

            //This method should run before OnQuadBuilt

        }
        public void ConvertLatLon(Vector2d latLon)
        {

        }
    }
    [RequireConfigType(ConfigType.Node)]
    public class Subdivide : ModLoader<PQSMod_Subdivide>
    {
        [ParserTarget("subdivisionLevel", Optional = false)]
        public NumericParser<int> subdivisionLevel
        {
            get { return Mod.subdivisionLevel; }
            set { Mod.subdivisionLevel = value; }
        }
        [ParserTarget("order", Optional = false)]
        public NumericParser<int> order
        {
            get { return Mod.order; }
            set { Mod.order = int.MaxValue; }
        }
        [ParserTarget("overrideDistLimit", Optional = true)]
        public NumericParser<bool> overrideDistLimit
        {
            get { return Mod.overrideDistLimit; }
            set { Mod.overrideDistLimit = value; }
        }
        [ParserTarget("customDistLimit", Optional = true)]
        public NumericParser<int> customDistLimit
        {
            get { return Mod.customDistLimit; }
            set { Mod.customDistLimit = value; }
        }

    }


    public class PQSMod_AlphaColorMap : PQSMod
    {
        public MapSO map;
        public override void OnSetup()
        {
            this.requirements = PQS.ModiferRequirements.MeshColorChannel;

        }
        public override void OnVertexBuild(PQS.VertexBuildData data)
        {
            Color mapCol = map.GetPixelColor(data.u, data.v);
            float alpha = mapCol.a;
            Color vertexColor = new Color(alpha, alpha, alpha, alpha);
            data.vertColor = vertexColor;           //Required for STOCK bodies

        }                                           //Module manager syntax is fucking difficult, man
    }
    [RequireConfigType(ConfigType.Node)]
    public class AlphaColorMap : ModLoader<PQSMod_AlphaColorMap>
    {
        [ParserTarget("map", Optional = false)]
        public MapSOParserRGBA<MapSO> Map
        {
            get { return Mod.map; }
            set { Mod.map = value; Debug.Log("PQSMod Map Set!"); }
        }
        [ParserTarget("order", Optional = false)]
        public NumericParser<int> order
        {
            get { return Mod.order; }
            set { Mod.order = int.MaxValue; }
        }
    }
    //[KSPAddon(KSPAddon.Startup.PSystemSpawn, false)]
    //public class GrassLoader : MonoBehaviour
    //{
    //    public void Start()
    //    {
    //        GrassMaterial.grassMaterial = new Material(ParallaxLoader.GetShader("Custom/Grass"));
    //        GrassMaterial.grassMaterial.SetColor("_TopColor", new Color(0.8396226f, 0.5663492f, 0.7778036f));
    //        GrassMaterial.grassMaterial.SetColor("_BottomColor", new Color(0.245283f, 0.03818085f, 0.182885f));
    //        GrassMaterial.grassMaterial.SetFloat("_BladeWidth", 10);
    //        GrassMaterial.grassMaterial.SetFloat("_BladeWidthRandom", 0f);
    //        GrassMaterial.grassMaterial.SetFloat("_BladeHeight", 100f);
    //        GrassMaterial.grassMaterial.SetFloat("_BladeHeightRandom", 0);
    //        GrassMaterial.grassMaterial.SetFloat("_BladeForwardAmount", 0);
    //        GrassMaterial.grassMaterial.SetFloat("_BladeCurvitureAmount", 1);
    //        GrassMaterial.grassMaterial.SetFloat("_BlendRotationRandom", 0);
    //        GrassMaterial.grassMaterial.SetFloat("_WindStrength", 1);
    //        GrassMaterial.grassMaterial.SetFloat("_TranslucentGain", 0.315f);
    //        GrassMaterial.grassMaterial.SetFloat("_TessellationEdgeLength", 6.6f);
    //    }
    //}
    
    [KSPAddon(KSPAddon.Startup.Instantly, false)]
    public class MaterialHolder : MonoBehaviour
    {
        public static Material standardUNKNOWN = new Material(Shader.Find("Standard"));

        public static Material standardLOW = new Material(Shader.Find("Standard"));
        public static Material standardMID = new Material(Shader.Find("Standard"));
        public static Material standardHIGH = new Material(Shader.Find("Standard"));

        public static Material standardLOWMID = new Material(Shader.Find("Standard"));
        public static Material standardMIDHIGH = new Material(Shader.Find("Standard"));
        public static Material standardLOWMIDHIGH = new Material(Shader.Find("Standard"));

        public static Material standardSTEEPLOW = new Material(Shader.Find("Standard"));
        public static Material standardSTEEPMID = new Material(Shader.Find("Standard"));
        public static Material standardSTEEPHIGH = new Material(Shader.Find("Standard"));
        public void Start()
        {
            standardLOW.SetColor("_Color", new Color(0.9f, 0.7f, 0.7f));
            standardMID.SetColor("_Color", new Color(0.7f, 0.9f, 0.7f));
            standardHIGH.SetColor("_Color", new Color(0.7f, 0.7f, 0.9f));

            standardSTEEPLOW.SetColor("_Color", new Color(0.75f, 0.5f, 0.5f));
            standardSTEEPMID.SetColor("_Color", new Color(0.5f, 0.75f, 0.5f));
            standardSTEEPHIGH.SetColor("_Color", new Color(0.5f, 0.5f, 0.75f));

            standardLOWMID.SetColor("_Color", new Color(0.8f, 0.8f, 0.4f));
            standardMIDHIGH.SetColor("_Color", new Color(0.4f, 0.8f, 0.8f));
            standardLOWMIDHIGH.SetColor("_Color", new Color(1f, 0.3f, 0.3f));

            standardUNKNOWN.SetColor("_Color", new Color(1, 1, 1));
        }
    }
    public class PQSMod_AdaptiveParallax : PQSMod
    {
        public override void OnQuadBuilt(PQ quad)
        {

        }
        //public override void OnQuadDestroy(PQ quad)
        //{
        //
        //}
        //public override void OnVertexBuildHeight(PQS.VertexBuildData data)
        //{
        //    if (data.buildQuad == null)
        //    {
        //        Debug.Log("This build quad is null");
        //        return;
        //    }
        //    if (data.buildQuad.subdivision <= 1)
        //    {
        //        Debug.Log("Returning as this quad subdivision is too low");
        //        return;
        //    }
        //    Debug.Log("Vertex Height: " + (data.vertHeight - FlightGlobals.currentMainBody.Radius));
        //    if (data.buildQuad.parent.gameObject == null)
        //    {
        //        Debug.Log("It doesn't fucking exist you mong");
        //    }
        //    Debug.Log("0.1");
        //    string addedComponent = "";
        //    Debug.Log("Current rad: " + FlightGlobals.currentMainBody.Radius);
        //    Debug.Log("Current lowfactor: " + lowStart);
        //    if (data.vertHeight - FlightGlobals.currentMainBody.Radius < lowStart)
        //    {
        //        Debug.Log("Adding low component");
        //        //This vertex is safely in the LOW area
        //        data.buildQuad.parent.gameObject.AddComponent<LowComponent>();
        //        addedComponent = "low";
        //        Debug.Log("The build quad does actually exist");
        //    }
        //    Debug.Log("1");
        //    if (data.vertHeight - FlightGlobals.currentMainBody.Radius > ParallaxBodies.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.LowEnd && data.vertHeight < highStart)
        //    {
        //        //This vertex is safely in the MID area
        //        data.buildQuad.parent.gameObject.AddComponent<MidComponent>();
        //        addedComponent = "mid";
        //        Debug.Log("The build quad does actually exist");
        //    }
        //    Debug.Log("2");
        //    if (data.vertHeight - FlightGlobals.currentMainBody.Radius > ParallaxBodies.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.HighEnd)
        //    {
        //        //This vertex is safely in the HIGH area
        //        data.buildQuad.parent.gameObject.AddComponent<HighComponent>();
        //        addedComponent = "high";
        //        Debug.Log("The build quad does actually exist");
        //    }
        //    Debug.Log("3");
        //    Debug.Log("Messing about with components and shiz now");
        //    if (data.buildQuad.parent.gameObject.GetComponent<LowComponent>() != null && data.buildQuad.parent.gameObject.GetComponent<MidComponent>() != null)
        //    {
        //        Debug.Log("This quad is in the LOW-MID region");
        //        addedComponent = "lowmid";
        //    }
        //    Debug.Log("4");
        //    if (data.buildQuad.parent.gameObject.GetComponent<MidComponent>() != null && data.buildQuad.parent.gameObject.GetComponent<HighComponent>() != null)
        //    {
        //        Debug.Log("This quad is in the MID-HIGH region");
        //        addedComponent = "midhigh";
        //    }
        //    Debug.Log("5");
        //    if (data.buildQuad.parent.gameObject.GetComponent<LowComponent>() != null && data.buildQuad.parent.gameObject.GetComponent<MidComponent>() != null && data.buildQuad.parent.gameObject.GetComponent<HighComponent>() != null)
        //    {
        //        Debug.Log("This quad is in the LOW-MID-HIGH region");
        //        addedComponent = "lowmidhigh";
        //    }
        //    Debug.Log("6");
        //    Debug.Log("Checking Components");
        //
        //    if (data.buildQuad.parent.gameObject.GetComponent<LowComponent>() != null && addedComponent != "low")
        //    {
        //        Destroy(data.buildQuad.parent.gameObject.GetComponent<LowComponent>());
        //    }
        //    if (data.buildQuad.parent.gameObject.GetComponent<MidComponent>() != null && addedComponent != "mid")
        //    {
        //        Destroy(data.buildQuad.parent.gameObject.GetComponent<MidComponent>());
        //    }
        //    if (data.buildQuad.parent.gameObject.GetComponent<HighComponent>() != null && addedComponent != "high")
        //    {
        //        Destroy(data.buildQuad.parent.gameObject.GetComponent<HighComponent>());
        //    }
        //
        //    if (addedComponent == "lowmid")
        //    {
        //        data.buildQuad.parent.gameObject.AddComponent<LowMidComponent>();
        //    }
        //    if (addedComponent == "midhigh")
        //    {
        //        data.buildQuad.parent.gameObject.AddComponent<MidHighComponent>();
        //    }
        //    if (addedComponent == "lowmidhigh")
        //    {
        //        data.buildQuad.parent.gameObject.AddComponent<LowMidHighComponent>();
        //    }
        //
        //    Debug.Log(" -    FINAL: This quad is in the " + addedComponent.ToUpper() + " region");
        //
        //    Color col = new Color(0, 0, 0);
        //    if (addedComponent == "low")
        //    {
        //        col = new Color(1, 0, 0);
        //    }
        //    if (addedComponent == "mid")
        //    {
        //        col = new Color(0, 1, 0);
        //    }
        //    if (addedComponent == "high")
        //    {
        //        col = new Color(0, 0, 1);
        //    }
        //    if (addedComponent == "lowmid")
        //    {
        //        col = new Color(1, 1, 0);
        //    }
        //    if (addedComponent == "midhigh")
        //    {
        //        col = new Color(0, 1, 1);
        //    }
        //    if (addedComponent == "lowmidhigh")
        //    {
        //        col = new Color(1, 1, 1);
        //    }
        //    data.vertColor = col;
        //}
    }
    [RequireConfigType(ConfigType.Node)]
    public class AdaptiveParallax : ModLoader<PQSMod_AdaptiveParallax>
    {
        [ParserTarget("order", Optional = false)]
        public NumericParser<int> order
        {
            get { return Mod.order; }
            set { Mod.order = int.MaxValue; }
        }
    }
    public class LowComponent : MonoBehaviour
    {
        public string name = "low";
        //Quad exists only in LOW
    }
    public class MidComponent : MonoBehaviour
    {
        public string name = "mid";
        //Quad exists only in MID
    }
    public class HighComponent : MonoBehaviour
    {
        public string name = "high";
        //Quad exists only in HIGH
    }

    public class LowMidComponent : MonoBehaviour
    {
        public string name = "lowmid";
        //Quad exists in LOW and MID
    }
    public class MidHighComponent : MonoBehaviour
    {
        public string name = "midhigh";
        //Quad exists in MID and HIGH
    }
    public class LowMidHighComponent : MonoBehaviour
    {
        public string name = "lowmidhigh";
        //Quad exists in LOW, MID and HIGH
    }

    [KSPAddon(KSPAddon.Startup.PSystemSpawn, false)]
    public class OptimizeMaxLevel : MonoBehaviour
    {
        public void Start() //
        {
            Debug.Log("Parallax Max Subdivision Updater:");
            Debug.Log(" - Exception: This is not an 'exception', but this is written as one because your settings.cfg file has been altered by Parallax");
            Debug.Log(" - This means that the planet terrain will remain more detailed even after uninstalling this mod.");
            Debug.Log(" - In order to restore these changes (which you can leave without any harm), simply delete your Settings.cfg file");
            for (int i = 0; i < PQSCache.PresetList.presets.Count; i++)
            {
                Debug.Log("PRESET: " + PQSCache.PresetList.presets[i].name);
                for (int b = 0; b < PQSCache.PresetList.presets[i].spherePresets.Count; b++)
                {
                    Debug.Log("     - SPHERE PRESET: " + PQSCache.PresetList.presets[i].spherePresets[b].name);
                    Debug.Log("     - minSubDiv: " + PQSCache.PresetList.presets[i].spherePresets[b].minSubdivision);
                    Debug.Log("     - maxSubDiv: " + PQSCache.PresetList.presets[i].spherePresets[b].maxSubdivision);
                    if (PQSCache.PresetList.presets[i].spherePresets[b].maxSubdivision <= 8)
                    {
                        PQSCache.PresetList.presets[i].spherePresets[b].maxSubdivision = 9;
                        Debug.Log("     - " + PQSCache.PresetList.presets[i].spherePresets[b].name + " max subdivision is too low! Set to 9");
                        if (PQSCache.PresetList.presets[i].spherePresets[b].name == "Gilly")
                        {
                            Debug.Log("Gilly detected, setting max subdivision to 7");
                            PQSCache.PresetList.presets[i].spherePresets[b].maxSubdivision = 7;
                        }
                        if (PQSCache.PresetList.presets[i].spherePresets[b].name == "Laythe")
                        {
                            PQSCache.PresetList.presets[i].spherePresets[b].maxSubdivision = PQSCache.PresetList.presets[i].spherePresets[b].maxSubdivision + 1;
                        }
                    }
                }
            }
            ChangeSpaceCenterColor();
            //foreach (CelestialBody body in FlightGlobals.Bodies)
            //{
            //    if (body.GetComponentsInChildren<PQS>()[0] != null)
            //    {
            //        Debug.Log(body.name + " is not null");
            //    }
            //    if (body.GetComponentsInChildren<PQS>()[0] != null && body.GetComponentsInChildren<PQS>()[0].maxLevel < 8)
            //    {
            //        body.pqsController.maxLevel = 10;
            //        Debug.Log("Max Level too low! Automatically increased " + body.name + "'s maxLevel to 10");
            //    }
            //}
        }
        public void ChangeSpaceCenterColor()
        {
            Debug.Log("Home planet detected as " + FlightGlobals.GetHomeBody().displayName);
            if (FlightGlobals.GetHomeBody().displayName == "Kerbin^N")
            {
                CelestialBody kerbin = FlightGlobals.GetHomeBody();
                PQSCity ksc = kerbin.pqsController.GetComponentsInChildren<PQSCity>(true).First(m => m.name == "KSC");
                ksc.gameObject.GetComponent<Renderer>().materials[0].color = new Color(24, 29, 19, 255);
            }
        }
    }
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class FixAll : MonoBehaviour
    {
        public void Update()
        {
            bool key = Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Alpha6);
            if (key)
            {
                foreach (Part part in FlightGlobals.ActiveVessel.parts)
                {
                    if (part.Modules[0] is ModuleWheelBase)
                    {
                        Debug.Log("Found a wheel");
                        part.PartRepair();
                    }
                }
            }
        }
    }
}