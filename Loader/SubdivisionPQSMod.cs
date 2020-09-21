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
    public class QuadMeshes : MonoBehaviour
    {
        public bool subdivided = false;
        public PQ quad;
        public GameObject newQuad;
        public int subdivisionLevel = 1;
        Material transparent = new Material(Shader.Find("Unlit/Transparent"));
        private float distance = 0;
        
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
                Debug.Log(distLimit);
                if (distance < 800 && subdivided == false)
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

                    Mesh mesh = Instantiate(quadMeshFilter.sharedMesh);
                    MeshHelper.Subdivide(mesh, subdivisionLevel);

                    newQuad.AddComponent<MeshFilter>();
                    var newQuadMeshFilter = newQuad.GetComponent<MeshFilter>();

                    newQuadMeshFilter.sharedMesh = mesh;
                    newQuad.AddComponent<MeshRenderer>();
                    var newQuadMeshRenderer = newQuad.GetComponent<MeshRenderer>();

                    newQuadMeshRenderer.material = FlightGlobals.currentMainBody.pqsController.surfaceMaterial;
                    newQuadMeshRenderer.enabled = true;
                    quadMeshRenderer.material = transparent;
                    quadMeshRenderer.material.SetTexture("_MainTex", Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(t => t.name == "BeyondHome/Terrain/DDS/BlankAlpha"));

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
                        quad.GetComponent<MeshRenderer>().material = FlightGlobals.currentMainBody.pqsController.surfaceMaterial;  //Don't make it transparent anymore
                    }
                }
                Debug.Log("Distance (Vessel): " + distance);
            }
            
        }
    }
    public class PQSMod_Subdivide : PQSMod
    {
        

        public int subdivisionLevel = 1;
        Material fuckery = new Material(Shader.Find("Standard"));
        
        public override void OnQuadBuilt(PQ quad)
        {
            
            try
            {
                if (quad.subdivision == FlightGlobals.currentMainBody.pqsController.maxLevel && HighLogic.LoadedScene == GameScenes.FLIGHT)
                {
                    quad.gameObject.AddComponent<QuadMeshes>();
                    quad.gameObject.GetComponent<QuadMeshes>().quad = quad;
                    quad.gameObject.GetComponent<QuadMeshes>().subdivisionLevel = subdivisionLevel;
                }
            }
            catch (Exception e)
            {
                Debug.Log("Something went wrong with a quad! " + e.ToString());
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
                    Debug.Log("Unable to remove from dictionary");
                }
                Destroy(quad.gameObject.GetComponent<QuadMeshes>());    //Quad is not maxLevel anymore, remove the damn thing
                
            }
            
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


    }

    public static class MeshHelper			//Taken from http://wiki.unity3d.com/index.php/MeshHelper - Parallax license doesn't apply
    {
        static List<Vector3> vertices;
        static List<Vector3> normals;
        static List<Color> colors;
        static List<Vector2> uv;
        static List<Vector2> uv2;
        static List<Vector2> uv3;

        static List<int> indices;
        static Dictionary<uint, int> newVectices;

        static void InitArrays(Mesh mesh)
        {
            vertices = new List<Vector3>(mesh.vertices);
            normals = new List<Vector3>(mesh.normals);
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
        }
    }
}
