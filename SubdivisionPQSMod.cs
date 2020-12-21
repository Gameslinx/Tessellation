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
using ParallaxShader;


[assembly: KSPAssembly("ParallaxBoi", 1, 0)]
[assembly: KSPAssemblyDependency("Kopernicus", 1, 0)]
[assembly: KSPAssemblyDependency("Parallax", 1, 0)]
namespace PQSModExpansion
{

    public static class QuadMeshDictionary
    {
        public static Dictionary<string, GameObject> subdividedQuadList = new Dictionary<string, GameObject>();
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

        public void Start()
        {
            InvokeRepeating("CheckSubdivision", 1f, 1f);
        }
        public void CheckSubdivision()
        {
            if (quad != null && FlightGlobals.ActiveVessel != null)
            {

                distance = Vector3.Distance(FlightGlobals.ActiveVessel.transform.position, quad.transform.position);
                int distLimit = (int)(120 + (FlightGlobals.currentMainBody.Radius / 450000) * 800);
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
                    //newQuad.layer = 29; //Unused by KSP

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
                    MeshHelper.Subdivide(mesh, subdivisionLevel);
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
                    //newMaterials[0] = GetGrassMaterial();
                    //newMaterials[1] = GetGrassMaterial();
                    //newQuadMeshRenderer.materials = newMaterials;
                    newQuadMeshRenderer.enabled = true;
                    
                    quadMeshRenderer.material = transparent;
                    quadMeshRenderer.material.SetTexture("_MainTex", Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(t => t.name == "Parallax/BlankAlpha"));

                    QuadMeshDictionary.subdividedQuadList.Add(newQuad.name, newQuad);

                }
                else if (distance >= 1200 && subdivided == true)
                {
                    string newQuadName = quad.name + "FAKE";
                    if (QuadMeshDictionary.subdividedQuadList.ContainsKey(newQuadName))
                    {
                        QuadMeshDictionary.subdividedQuadList[newQuadName].DestroyGameObject();    //Change to Destroy()
                        QuadMeshDictionary.subdividedQuadList.Remove(newQuadName);
                        quad.GetComponent<QuadMeshes>().subdivided = false;
                        quad.GetComponent<MeshRenderer>().sharedMaterial = trueMaterial;  //Don't make it transparent anymore
                        Debug.Log("2");
                        Destroy(collisionQuad);
                        Debug.Log("2");
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
        Material fuckery = new Material(Shader.Find("Standard"));

        public override void OnQuadBuilt(PQ quad)
        {
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
                if (quad.subdivision == FlightGlobals.currentMainBody.pqsController.maxLevel && HighLogic.LoadedScene == GameScenes.FLIGHT)
                {
                    Debug.Log("Mask: " + LayerMask.GetMask());
                    quad.gameObject.AddComponent<QuadMeshes>();
                    quad.gameObject.GetComponent<QuadMeshes>().quad = quad;
                    quad.gameObject.GetComponent<QuadMeshes>().subdivisionLevel = (int)(subdivisionLevel * ParallaxSettings.tessMult);
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

                float lowStart = ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.LowStart;
                float lowEnd = ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.LowEnd;
                float highStart = ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.HighStart;
                float highEnd = ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.HighEnd;
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
                quad.GetComponent<MeshRenderer>().sharedMaterial = ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterial;

                if (highPoint <= highStart)
                {
                    //LOW-MID
                    quad.GetComponent<MeshRenderer>().sharedMaterial = ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialDOUBLELOW;
                }
                if (lowPoint >= lowEnd)
                {
                    //MID-HIGH
                    quad.GetComponent<MeshRenderer>().sharedMaterial = ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialDOUBLEHIGH;
                }


                if (highPoint < lowStart)
                {
                    //LOW
                    quad.GetComponent<MeshRenderer>().sharedMaterial = ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLELOW;
                    //quad.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_SurfaceTexture", ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.LoadTexture(ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.SurfaceTexture));
                    //quad.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_BumpMap", ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.LoadTexture(ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.SurfaceTextureBumpMap));
                }
                if (highPoint < highStart && lowPoint > lowEnd)
                {
                    //MID
                    quad.GetComponent<MeshRenderer>().sharedMaterial = ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLEMID;
                    //quad.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_SurfaceTexture", ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.LoadTexture(ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.SurfaceTextureMid));
                    //quad.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_BumpMap", ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.LoadTexture(ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.BumpMapMid));
                }
                if (lowPoint > highEnd)
                {
                    //HIGH
                    quad.GetComponent<MeshRenderer>().sharedMaterial = ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLEHIGH;
                    //quad.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_SurfaceTexture", ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.LoadTexture(ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.SurfaceTextureHigh));
                    //quad.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_BumpMap", ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.LoadTexture(ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.BumpMapHigh));
                }
                
                if (lowPoint < lowStart && highPoint > highStart)
                {
                    //LOW-MID-HIGH
                    quad.GetComponent<MeshRenderer>().sharedMaterial = ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterial;
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
                        quad.GetComponent<MeshRenderer>().sharedMaterial = ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPLOW;
                        //quad.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_SurfaceTexture", ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.LoadTexture(ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.SurfaceTexture));
                        //quad.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_BumpMap", ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.LoadTexture(ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.SurfaceTextureBumpMap));
                    }
                    if (highPoint < highStart &&
                        lowPoint > ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.LowEnd)
                    {
                        //MID
                        quad.GetComponent<MeshRenderer>().sharedMaterial = ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPMID;
                        //quad.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_SurfaceTexture", ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.LoadTexture(ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.SurfaceTextureMid));
                        //quad.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_BumpMap", ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.LoadTexture(ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.BumpMapMid));
                    }
                    if (lowPoint > ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.HighEnd)
                    {
                        //HIGH
                        quad.GetComponent<MeshRenderer>().sharedMaterial = ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPHIGH;
                        //quad.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_SurfaceTexture", ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.LoadTexture(ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.SurfaceTextureHigh));
                        //quad.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_BumpMap", ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.LoadTexture(ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.BumpMapHigh));
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
            if (quad.gameObject.GetComponent<QuadMeshes>() != null)
            {
                try
                {
                    QuadMeshDictionary.subdividedQuadList[quad.gameObject.GetComponent<QuadMeshes>().newQuad.name].GetComponent<MeshRenderer>().enabled = false;
                    QuadMeshDictionary.subdividedQuadList[quad.gameObject.GetComponent<QuadMeshes>().newQuad.name].DestroyGameObject();
                    QuadMeshDictionary.subdividedQuadList.Remove(quad.gameObject.GetComponent<QuadMeshes>().newQuad.name);
                }
                catch
                {
                }
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
                    slope = (float)Math.Pow(slope, ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.SteepPower);
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
            set { Mod.map = value; Debug.Log("PQSMod Map Set!");}
        }
        [ParserTarget("order", Optional = false)]
        public NumericParser<int> order
        {
            get { return Mod.order; }
            set { Mod.order = int.MaxValue; }
        }
    }
    [KSPAddon(KSPAddon.Startup.PSystemSpawn, false)]
    public class GrassLoader : MonoBehaviour
    {
        public void Start()
        {
            GrassMaterial.grassMaterial = new Material(ParallaxLoader.GetShader("Custom/Grass"));
            GrassMaterial.grassMaterial.SetColor("_TopColor", new Color(0.8396226f, 0.5663492f, 0.7778036f));
            GrassMaterial.grassMaterial.SetColor("_BottomColor", new Color(0.245283f, 0.03818085f, 0.182885f));
            GrassMaterial.grassMaterial.SetFloat("_BladeWidth", 10);
            GrassMaterial.grassMaterial.SetFloat("_BladeWidthRandom", 0f);
            GrassMaterial.grassMaterial.SetFloat("_BladeHeight", 100f);
            GrassMaterial.grassMaterial.SetFloat("_BladeHeightRandom", 0);
            GrassMaterial.grassMaterial.SetFloat("_BladeForwardAmount", 0);
            GrassMaterial.grassMaterial.SetFloat("_BladeCurvitureAmount", 1);
            GrassMaterial.grassMaterial.SetFloat("_BlendRotationRandom", 0);
            GrassMaterial.grassMaterial.SetFloat("_WindStrength", 1);
            GrassMaterial.grassMaterial.SetFloat("_TranslucentGain", 0.315f);
            GrassMaterial.grassMaterial.SetFloat("_TessellationEdgeLength", 6.6f);
        }
    }
    public static class MeshHelper
    {
        static List<Vector3> vertices;
        static List<Vector3> normals;
        static List<Color> colors;
        static List<Vector2> uv;
        static List<Vector2> uv2;
        static List<Vector2> uv3;
        static List<Vector4> tangents;

        static List<int> indices;
        static Dictionary<uint, int> newVectices;

        static void InitArrays(Mesh mesh)
        {
            vertices = new List<Vector3>(mesh.vertices);
            normals = new List<Vector3>(mesh.normals);
            tangents = new List<Vector4>(mesh.tangents);
            colors = new List<Color>(mesh.colors);
            uv = new List<Vector2>(mesh.uv);
            uv2 = new List<Vector2>(mesh.uv2);
            uv3 = new List<Vector2>(mesh.uv3);
            indices = new List<int>();
        }
        static void CleanUp()
        {
            vertices = null;
            normals = null;
            colors = null;
            uv = null;
            uv2 = null;
            uv3 = null;
            indices = null;
            tangents = null;
        }

        #region Subdivide4 (2x2)
        static int GetNewVertex4(int i1, int i2)
        {
            int newIndex = vertices.Count;
            uint t1 = ((uint)i1 << 16) | (uint)i2;
            uint t2 = ((uint)i2 << 16) | (uint)i1;
            if (newVectices.ContainsKey(t2))
                return newVectices[t2];
            if (newVectices.ContainsKey(t1))
                return newVectices[t1];

            newVectices.Add(t1, newIndex);

            vertices.Add((vertices[i1] + vertices[i2]) * 0.5f);
            if (normals.Count > 0)
                normals.Add((normals[i1] + normals[i2]).normalized);
            if (tangents.Count > 0)
                tangents.Add((tangents[i1] + tangents[i2]).normalized);
            if (colors.Count > 0)
                colors.Add((colors[i1] + colors[i2]) * 0.5f);
            if (uv.Count > 0)
                uv.Add((uv[i1] + uv[i2]) * 0.5f);
            if (uv2.Count > 0)
                uv2.Add((uv2[i1] + uv2[i2]) * 0.5f);
            if (uv3.Count > 0)
                uv3.Add((uv3[i1] + uv3[i2]) * 0.5f);

            return newIndex;
        }
        public static void RecalculateUVs(Mesh mesh)
        {

            //mesh.RecalculateBounds();
            //float width = mesh.bounds.size.x;
            //float length = mesh.bounds.size.z;
            //Debug.Log(width);
            //Debug.Log(length);
            //Vector3[] verts = mesh.vertices;
            //Vector2[] uvs = new Vector2[mesh.vertices.Length];
            ////Vector2 leftMost = mesh.
            //float lowX = 11111;
            //float highX = -11111;
            //float lowY = 11111;
            //float highY = -11111;
            //
            //for (int i = 0; i < verts.Length; i++)
            //{
            //    float actualX = verts[i].x + width / 2;
            //    float actualY = verts[i].z + length / 2;
            //    uvs[i] = new Vector2(1 - (actualX / width), 1 - (actualY / length));
            //    //Debug.Log(uvs[i]);
            //    if (verts[i].z < lowY)
            //    {
            //        lowY = verts[i].z;
            //    }
            //    if (verts[i].z > highY)
            //    {
            //        highY = verts[i].z;
            //    }
            //    if (verts[i].x < lowX)
            //    {
            //        lowX = verts[i].x;
            //    }
            //    if (verts[i].x > highX)
            //    {
            //        highX = verts[i].x;
            //    }
            //}
            //Debug.Log(lowX + ", " + highX + " | " + lowY + ", " + highY);
            mesh.uv = UVHolder.uv;
            //mesh.RecalculateNormals();
            //mesh.RecalculateTangents();
            //Debug.Log("This quad has " + mesh.vertexCount + " vertices");
            //for (int i = 0; i < mesh.vertexCount; i++)
            //{
            //    Debug.Log(mesh.vertices[i].ToString("F5") + "\t   |   " + mesh.uv[i].ToString("F5"));
            //}
        }


        /// <summary>
        /// Devides each triangles into 4. A quad(2 tris) will be splitted into 2x2 quads( 8 tris )
        /// </summary>
        /// <param name="mesh"></param>
        public static void Subdivide4(Mesh mesh)
        {
            newVectices = new Dictionary<uint, int>();

            InitArrays(mesh);

            int[] triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int i1 = triangles[i + 0];
                int i2 = triangles[i + 1];
                int i3 = triangles[i + 2];

                int a = GetNewVertex4(i1, i2);
                int b = GetNewVertex4(i2, i3);
                int c = GetNewVertex4(i3, i1);
                indices.Add(i1); indices.Add(a); indices.Add(c);
                indices.Add(i2); indices.Add(b); indices.Add(a);
                indices.Add(i3); indices.Add(c); indices.Add(b);
                indices.Add(a); indices.Add(b); indices.Add(c); // center triangle
            }
            mesh.Clear();
            mesh.vertices = vertices.ToArray();
            if (normals.Count > 0)
                mesh.normals = normals.ToArray();
            if (colors.Count > 0)
                mesh.colors = colors.ToArray();
            if (uv.Count > 0)
                mesh.uv = uv.ToArray();
            if (uv2.Count > 0)
                mesh.uv2 = uv2.ToArray();
            if (uv3.Count > 0)
                mesh.uv3 = uv3.ToArray();
            if (tangents.Count > 0)
                mesh.tangents = tangents.ToArray();

            mesh.triangles = indices.ToArray();

            CleanUp();
            return;
        }
        #endregion Subdivide4 (2x2)

        #region Subdivide9 (3x3)
        static int GetNewVertex9(int i1, int i2, int i3)
        {
            int newIndex = vertices.Count;

            // center points don't go into the edge list
            if (i3 == i1 || i3 == i2)
            {
                uint t1 = ((uint)i1 << 16) | (uint)i2;
                if (newVectices.ContainsKey(t1))
                    return newVectices[t1];
                newVectices.Add(t1, newIndex);
            }

            // calculate new vertex
            vertices.Add((vertices[i1] + vertices[i2] + vertices[i3]) / 3.0f);
            if (normals.Count > 0)
                normals.Add((normals[i1] + normals[i2] + normals[i3]).normalized);
            if (tangents.Count > 0)
                tangents.Add((tangents[i1] + tangents[i2] + tangents[i3]).normalized);
            if (colors.Count > 0)
                colors.Add((colors[i1] + colors[i2] + colors[i3]) / 3.0f);
            if (uv.Count > 0)
                uv.Add((uv[i1] + uv[i2] + uv[i3]) / 3.0f);
            if (uv2.Count > 0)
                uv2.Add((uv2[i1] + uv2[i2] + uv2[i3]) / 3.0f);
            if (uv3.Count > 0)
                uv3.Add((uv3[i1] + uv3[i2] + uv3[i3]) / 3.0f);
            return newIndex;
        }


        /// <summary>
        /// Devides each triangles into 9. A quad(2 tris) will be splitted into 3x3 quads( 18 tris )
        /// </summary>
        /// <param name="mesh"></param>
        public static void Subdivide9(Mesh mesh)
        {
            newVectices = new Dictionary<uint, int>();

            InitArrays(mesh);

            int[] triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int i1 = triangles[i + 0];
                int i2 = triangles[i + 1];
                int i3 = triangles[i + 2];

                int a1 = GetNewVertex9(i1, i2, i1);
                int a2 = GetNewVertex9(i2, i1, i2);
                int b1 = GetNewVertex9(i2, i3, i2);
                int b2 = GetNewVertex9(i3, i2, i3);
                int c1 = GetNewVertex9(i3, i1, i3);
                int c2 = GetNewVertex9(i1, i3, i1);

                int d = GetNewVertex9(i1, i2, i3);

                indices.Add(i1); indices.Add(a1); indices.Add(c2);
                indices.Add(i2); indices.Add(b1); indices.Add(a2);
                indices.Add(i3); indices.Add(c1); indices.Add(b2);
                indices.Add(d); indices.Add(a1); indices.Add(a2);
                indices.Add(d); indices.Add(b1); indices.Add(b2);
                indices.Add(d); indices.Add(c1); indices.Add(c2);
                indices.Add(d); indices.Add(c2); indices.Add(a1);
                indices.Add(d); indices.Add(a2); indices.Add(b1);
                indices.Add(d); indices.Add(b2); indices.Add(c1);
            }
            mesh.Clear();
            mesh.vertices = vertices.ToArray();
            if (normals.Count > 0)
                mesh.normals = normals.ToArray();
            if (colors.Count > 0)
                mesh.colors = colors.ToArray();
            if (uv.Count > 0)
                mesh.uv = uv.ToArray();
            if (uv2.Count > 0)
                mesh.uv2 = uv2.ToArray();
            if (uv3.Count > 0)
                mesh.uv3 = uv3.ToArray();
            if (tangents.Count > 0)
                mesh.tangents = tangents.ToArray();

            mesh.triangles = indices.ToArray();

            CleanUp();
        }
        #endregion Subdivide9 (3x3)


        /// <summary>
        /// This functions subdivides the mesh based on the level parameter
        /// Note that only the 4 and 9 subdivides are supported so only those divides
        /// are possible. [2,3,4,6,8,9,12,16,18,24,27,32,36,48,64, ...]
        /// The function tried to approximate the desired level 
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="level">Should be a number made up of (2^x * 3^y)
        /// [2,3,4,6,8,9,12,16,18,24,27,32,36,48,64, ...]
        /// </param>
        public static void Subdivide(Mesh mesh, int level)
        {
            RecalculateUVs(mesh);
            if (level < 2)
                return;
            while (level > 1)
            {
                // remove prime factor 3
                while (level % 3 == 0)
                {
                    Subdivide9(mesh);
                    level /= 3;
                }
                // remove prime factor 2
                while (level % 2 == 0)
                {
                    Subdivide4(mesh);
                    level /= 2;
                }
                // try to approximate. All other primes are increased by one
                // so they can be processed
                if (level > 3)
                    level++;
            }
            //


        }
    }
    [KSPAddon(KSPAddon.Startup.Instantly, false)]
    public class UVHolder : MonoBehaviour
    {
        public static Vector2[] uv = new Vector2[225];
        public void Start()
        {
            Debug.Log("[Parallax] Generating quad UVs: ");
            for (int i = 0; i < 225; i++)
            {
                int row = i / 15;
                int across = i % 15;
                uv[i] = new Vector2((float)across / 14, (float)row / 14);
                Debug.Log(" - Parallax Quad UV: " + uv[i].ToString("F3"));
            }
            Debug.Log("[Parallax] Finished generating quad UVs!");
        }
    }
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
        //    if (data.vertHeight - FlightGlobals.currentMainBody.Radius > ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.LowEnd && data.vertHeight < highStart)
        //    {
        //        //This vertex is safely in the MID area
        //        data.buildQuad.parent.gameObject.AddComponent<MidComponent>();
        //        addedComponent = "mid";
        //        Debug.Log("The build quad does actually exist");
        //    }
        //    Debug.Log("2");
        //    if (data.vertHeight - FlightGlobals.currentMainBody.Radius > ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.HighEnd)
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
                        PQSCache.PresetList.presets[i].spherePresets[b].maxSubdivision = 10;
                        Debug.Log("     - " + PQSCache.PresetList.presets[i].spherePresets[b].name + " max subdivision is too low! Set to 10");
                    }
                }
            }
            foreach (CelestialBody body in FlightGlobals.Bodies)
            {
                
                if (body.GetComponent<PQS>() != null && body.GetComponent<PQS>().maxLevel < 8 && body.Radius > 50000)
                {
                    body.pqsController.maxLevel = 10;
                    Debug.Log("Max Level too low! Automatically increased " + body.name + "'s maxLevel to 10");
                }
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



