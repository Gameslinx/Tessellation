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
        public static int planetAdvancedSubdivisionLevel = 32;
        public static int planetSubdivisionLevel = 1;
    }
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class AdvancedSubdivisionManager : MonoBehaviour
    {
        
        List<GameObject> lastQuadList;
        string nameCheck = "";
        bool force = false;
        public void Update()
        {
            if (GameSettings.TERRAIN_SHADER_QUALITY < 3)
            {
                return;
            }
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
            AdvancedSubdivisionGlobal.quadDirection = new string[quads.Count];
            for (int i = 0; i < quads.Count; i++)
            {
                int windingOrderCheck = quads[i].GetComponent<MeshFilter>().sharedMesh.triangles[2];
                int indexLength = (int)Mathf.Sqrt(quads[i].GetComponent<MeshFilter>().mesh.vertexCount);
                if (windingOrderCheck == indexLength)
                {
                    //Debug.Log("passing windingOrderCheck = indexLength");
                    if (QuadMeshDictionary.planetSubdivisionLevel % 2 != 0)
                    {
                        //Debug.Log("Clockwise 1");
                        AdvancedSubdivisionGlobal.quadDirection[i] = "clockwise";
                    }
                    else
                    {
                        //Debug.Log("Anticlockwise 1");
                        AdvancedSubdivisionGlobal.quadDirection[i] = "anticlockwise";
                    }
                       //We need to do the reverse here of what we do in the subdiv mod, idk why
                }
                else
                {
                    if (QuadMeshDictionary.planetSubdivisionLevel % 2 != 0)
                    {
                        //Debug.Log("Anticlockwise 2");
                        AdvancedSubdivisionGlobal.quadDirection[i] = "anticlockwise";
                    }
                    else
                    {
                        //Debug.Log("Clockwise 2");
                        AdvancedSubdivisionGlobal.quadDirection[i] = "clockwise";
                    }
                    
                }
                //Debug.Log("Finished determining winding order");
                if (i == 0)
                {
                    //Debug.Log("making primary");
                    MakePrimary(quads[i], i, quads.Count);
                }
                else
                {
                    //Debug.Log("making secondary");
                    MakeSecondary(quads[i], i, quads.Count);
                }
            }
            for (int i = 1; i < quads.Count; i++)
            {
                if (quads[i].GetComponent<SubQuad>() != null)
                {
                    quads[i].GetComponent<SubQuad>().ForceUpdate();
                    quads[i].GetComponent<SubQuad>().subdivisionLevel = QuadMeshDictionary.planetAdvancedSubdivisionLevel;
                }
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
                quad.GetComponent<AdvancedSubdivision.AdvancedSubdivision>().subdivisionLevel = QuadMeshDictionary.planetAdvancedSubdivisionLevel;
                return;
            }
            quad.AddComponent<AdvancedSubdivision.AdvancedSubdivision>();
            quad.GetComponent<AdvancedSubdivision.AdvancedSubdivision>().subdivisionLevel = QuadMeshDictionary.planetAdvancedSubdivisionLevel;
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
                //Debug.Log("Destroyed a tile on scene change");
            }
            QuadMeshDictionary.subdividedQuadList.Clear();
            //Debug.Log("Cleared the quad mesh dictionary");
        }
    }
    public static class GrassMaterial
    {
        public static Material grassMaterial;
    }
    public class QuadMeshes : MonoBehaviour
    {
        public bool subdivided = false;
        public int advancedSubdivisionLevel;
        public PQ quad;
        public GameObject newQuad;
        //public GameObject collisionQuad;
        public int subdivisionLevel = 1;
        Material transparent = new Material(Shader.Find("Unlit/Transparent"));
        private float distance = 0;
        public bool overrideDistLimit = false;
        public int customDistLimit = 1000;
        Material trueMaterial;
        public int timesUpdated = 0;
        Mesh quadMesh;
        GameObject[] lines;

        public void OnDestroy()
        {
            Destroy(newQuad);
            //Destroy(collisionQuad);
            //Debug.Log("[SubdivMod] Destroyed");
        }
        public void Start()
        {
            InvokeRepeating("CheckSubdivision", 1f, 1f);
            quadMesh = Instantiate(gameObject.GetComponent<MeshFilter>().sharedMesh);
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

                    trueMaterial = quad.GetComponent<MeshRenderer>().sharedMaterial;    //reference to sharedMaterial

                    Mesh mesh = Instantiate(quadMeshFilter.sharedMesh);
                    if (subdivisionLevel > 1)
                    {
                        int windingOrderCheck = mesh.triangles[2];
                        //Debug.Log("Winding order check = " + windingOrderCheck);
                        int indexLength = (int)Mathf.Sqrt(mesh.vertexCount);
                        //Debug.Log("IndexLength = " + indexLength);
                        string direction = "anticlockwise";
                        if (windingOrderCheck == indexLength)
                        {
                            //Debug.Log("passing windingOrderCheck = indexLength");
                            direction = "clockwise";
                        }
                        for (int i = 0; i < subdivisionLevel - 1; i++)
                        {
                            int subdivFactor = (int)Mathf.Pow(2, i);    //1, 2, 4, 8, 16
                            //Debug.Log("Index length is " + Mathf.Sqrt(mesh.vertexCount));
                            //Debug.Log("Grid x Grid is " + (Mathf.Sqrt(mesh.vertexCount) - 1) + " x " + (Mathf.Sqrt(mesh.vertexCount) - 1));
                            //Debug.Log("Passing in " + (14 * subdivFactor) + " x " + (14 * subdivFactor));
                            mesh = MeshHandler.Subdivide(mesh, 14 * subdivFactor, 14 * subdivFactor, direction); //Subdivide in multiples of 2
                        }
                        
                    }
                    
                    newQuad.AddComponent<MeshFilter>();
                    var newQuadMeshFilter = newQuad.GetComponent<MeshFilter>();
                    newQuadMeshFilter.sharedMesh = mesh;
                    newQuad.AddComponent<MeshRenderer>();
                    var newQuadMeshRenderer = newQuad.GetComponent<MeshRenderer>();
                    newQuadMeshRenderer.sharedMaterial = quad.GetComponent<MeshRenderer>().sharedMaterial;//FlightGlobals.currentMainBody.pqsController.surfaceMaterial;//GrassMaterial.grassMaterial;
                    //newMaterials[0] = GetGrassMaterial();
                    //newMaterials[1] = GetGrassMaterial();
                    //newQuadMeshRenderer.materials = newMaterials;
                    newQuadMeshRenderer.enabled = true;

                    quadMeshRenderer.material = transparent;
                    quadMeshRenderer.material.SetTexture("_MainTex", Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(t => t.name == "Parallax/BlankAlpha"));

                    QuadMeshDictionary.planetAdvancedSubdivisionLevel = advancedSubdivisionLevel;
                    QuadMeshDictionary.planetSubdivisionLevel = subdivisionLevel;
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
                        //Destroy(collisionQuad);
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

        public int advancedSubdivisionLevel = 32;
        public int subdivisionLevel = 1;
        public bool overrideDistLimit = false;
        public int customDistLimit = 1000;
        public static bool needed = false;
        public override void OnVertexBuild(PQS.VertexBuildData data)
        {

        }
        public override void OnQuadBuilt(PQ quad)
        {
            
            if (FlightGlobals.currentMainBody == null)
            {
                return;
            }
            try
            {

                if (GameSettings.TERRAIN_SHADER_QUALITY == 3 && quad != null && quad.subdivision == FlightGlobals.currentMainBody.pqsController.maxLevel && HighLogic.LoadedScene == GameScenes.FLIGHT)
                {
                    quad.gameObject.AddComponent<QuadMeshes>();
                    quad.gameObject.GetComponent<QuadMeshes>().quad = quad;
                    quad.gameObject.GetComponent<QuadMeshes>().subdivisionLevel = (int)(subdivisionLevel * 1);
                    quad.gameObject.GetComponent<QuadMeshes>().overrideDistLimit = overrideDistLimit;
                    quad.gameObject.GetComponent<QuadMeshes>().customDistLimit = customDistLimit;
                    quad.gameObject.GetComponent<QuadMeshes>().advancedSubdivisionLevel = advancedSubdivisionLevel;
                }
            }
            catch (Exception e)
            {
                //Debug.Log("[Parallax] Subdivision Error:\n" + e.ToString());
            }

            //ADAPTIVE PARALLAX
            //quad.GetComponent<MeshRenderer>().sharedMaterial = new Material(ShaderHolder.GetShader("Custom/Wireframe"));
            //return;
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
                    //Debug.Log("Highpoint / Lowpoint not set!");
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
                ParallaxBody body = ParallaxBodies.parallaxBodies[FlightGlobals.currentMainBody.name];
                Mesh mesh = Instantiate(quad.gameObject.GetComponent<MeshFilter>().sharedMesh);
                Vector3[] verts = mesh.vertices;
                Vector3[] normals = mesh.normals;
                int vertCount = mesh.vertexCount;
                float minSlope = 1;
                int indexLength = (int)(Mathf.Sqrt(mesh.vertexCount));
                for (int i = 0; i < vertCount; i++)
                {
                    Vector3 posInWorldSpace = quad.gameObject.transform.TransformPoint(verts[i]);
                    Vector3 meshNormal = Vector3.Normalize(quad.gameObject.transform.TransformVector(normals[i]));
                    Vector3 worldNormal = Vector3.Normalize(quad.gameObject.transform.TransformPoint(verts[i]) - FlightGlobals.currentMainBody.transform.position);

                    float slope = Mathf.Abs(Vector3.Dot(worldNormal, meshNormal));
                    slope = Mathf.Clamp01(Mathf.Pow(slope, body._SteepPower));
                    slope = Mathf.Clamp01((slope - body._SteepMidpoint) * body._SteepContrast + body._SteepMidpoint);
                    if (i < indexLength || i % indexLength == 0 || i >= indexLength * (indexLength - 1) || i % indexLength == indexLength - 1)
                    {
                        //top, left, bottom, or right of the quad - apply a bias since the true normal could be different!
                        slope -= 0.8f;
                    }
                    if (slope < minSlope)
                    {
                        minSlope = slope;
                    }
                    
                }
                if (minSlope < 0.05f)
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
                    //quad.GetComponent<MeshRenderer>().material.SetColor("_Color", new Color(0, 0, 0));
                    
                }

            }
            catch (Exception e)
            {
                Debug.Log("[Parallax] Subdivision Error (Adaptive Parallax):\n" + e.ToString());
            }

        }
        public override void OnQuadDestroy(PQ quad)
        {
            //Debug.Log("Beginning quad destruction");
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

                //Debug.Log("Destroying quadmesh component:");
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
        [ParserTarget("advancedSubdivisionLevel", Optional = true)]
        public NumericParser<int> advancedSubdivisionLevel
        {
            get { return Mod.advancedSubdivisionLevel; }
            set { Mod.advancedSubdivisionLevel = value; }
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
            standardLOW.SetColor("_Color", new Color(0.1f, 1f, 0.1f));
            standardMID.SetColor("_Color", new Color(0.1f, 1f, 0.1f));
            standardHIGH.SetColor("_Color", new Color(0.1f, 1f, 0.1f));

            standardSTEEPLOW.SetColor("_Color", new Color(1f, 0.94f, 0.1f));
            standardSTEEPMID.SetColor("_Color", new Color(1f, 0.94f, 0.1f));
            standardSTEEPHIGH.SetColor("_Color", new Color(1f, 0.94f, 0.1f));

            standardLOWMID.SetColor("_Color", new Color(1f, 0.4f, 0.1f));
            standardMIDHIGH.SetColor("_Color", new Color(1f, 0.4f, 0.1f));
            standardLOWMIDHIGH.SetColor("_Color", new Color(1f, 0.1f, 0.1f));


            standardLOW.SetFloat("_Gloss", 0);
            standardMID.SetFloat("_Gloss", 0);
            standardHIGH.SetFloat("_Gloss", 0);

            standardSTEEPLOW.SetFloat("_Gloss", 0);
            standardSTEEPMID.SetFloat("_Gloss", 0);
            standardSTEEPHIGH.SetFloat("_Gloss", 0);

            standardLOWMID.SetFloat("_Gloss", 0);
            standardMIDHIGH.SetFloat("_Gloss", 0);
            standardLOWMIDHIGH.SetFloat("_Gloss", 0);

            standardLOW.SetFloat("_Metallic", 0);
            standardMID.SetFloat("_Metallic", 0);
            standardHIGH.SetFloat("_Metallic", 0);

            standardSTEEPLOW.SetFloat("_Metallic", 0);
            standardSTEEPMID.SetFloat("_Metallic", 0);
            standardSTEEPHIGH.SetFloat("_Metallic", 0);

            standardLOWMID.SetFloat("_Metallic", 0);
            standardMIDHIGH.SetFloat("_Metallic", 0);
            standardLOWMIDHIGH.SetFloat("_Metallic", 0);


            standardUNKNOWN.SetColor("_Color", new Color(1, 1, 1));
        }
    }
  
 

    [KSPAddon(KSPAddon.Startup.PSystemSpawn, false)]
    public class OptimizeMaxLevel : MonoBehaviour
    {
        public void Start() //
        {
            //Debug.Log("Parallax Max Subdivision Updater:");
            //Debug.Log(" - Exception: This is not an 'exception', but this is written as one because your settings.cfg file has been altered by Parallax");
            //Debug.Log(" - This means that the planet terrain will remain more detailed even after uninstalling this mod.");
            //Debug.Log(" - In order to restore these changes (which you can leave without any harm), simply delete your Settings.cfg file");
            for (int i = 0; i < PQSCache.PresetList.presets.Count; i++)
            {
                //Debug.Log("PRESET: " + PQSCache.PresetList.presets[i].name);
                for (int b = 0; b < PQSCache.PresetList.presets[i].spherePresets.Count; b++)
                {
                    //Debug.Log("     - SPHERE PRESET: " + PQSCache.PresetList.presets[i].spherePresets[b].name);
                    //Debug.Log("     - minSubDiv: " + PQSCache.PresetList.presets[i].spherePresets[b].minSubdivision);
                    //Debug.Log("     - maxSubDiv: " + PQSCache.PresetList.presets[i].spherePresets[b].maxSubdivision);
                    if (PQSCache.PresetList.presets[i].spherePresets[b].maxSubdivision <= 8)
                    {
                        PQSCache.PresetList.presets[i].spherePresets[b].maxSubdivision = 9;
                        //Debug.Log("     - " + PQSCache.PresetList.presets[i].spherePresets[b].name + " max subdivision is too low! Set to 9");
                        if (PQSCache.PresetList.presets[i].spherePresets[b].name == "Gilly")
                        {
                            //Debug.Log("Gilly detected, setting max subdivision to 7");
                            PQSCache.PresetList.presets[i].spherePresets[b].maxSubdivision = 7;
                        }
                        if (PQSCache.PresetList.presets[i].spherePresets[b].name == "Laythe")
                        {
                            PQSCache.PresetList.presets[i].spherePresets[b].maxSubdivision = PQSCache.PresetList.presets[i].spherePresets[b].maxSubdivision + 1;
                        }
                    }
                }
            }
        }
        
    }
}