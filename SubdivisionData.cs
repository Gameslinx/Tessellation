using Parallax;
using ParallaxQualityLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ParallaxOptimized
{
    [KSPAddon(KSPAddon.Startup.PSystemSpawn, true)]
    public class ParallaxDebug : MonoBehaviour
    {
        public static Material low;
        public static Material mid;
        public static Material high;
        public static Material doublelow;
        public static Material doublehigh;
        public static Material lowsteep;
        public static Material midsteep;
        public static Material highsteep;
        public static Material full;
        public static Material transparent;
        public static Material wireframe;

        public static Material wireframeAlt;

        void Awake()
        {
            DontDestroyOnLoad(this);
        }
        void Start()
        {
            low = new Material(Shader.Find("Standard")); low.SetColor("_Color", Color.red);
            mid = new Material(Shader.Find("Standard")); mid.SetColor("_Color", Color.green);
            high = new Material(Shader.Find("Standard")); high.SetColor("_Color", Color.blue);

            doublelow = new Material(Shader.Find("Standard")); doublelow.SetColor("_Color", Color.cyan);
            doublehigh = new Material(Shader.Find("Standard")); doublehigh.SetColor("_Color", Color.magenta);

            lowsteep = new Material(Shader.Find("Standard")); lowsteep.SetColor("_Color", Color.red / 2f);
            midsteep = new Material(Shader.Find("Standard")); midsteep.SetColor("_Color", Color.green / 2f);
            highsteep = new Material(Shader.Find("Standard")); highsteep.SetColor("_Color", Color.blue / 2f);

            full = new Material(Shader.Find("Standard")); full.SetColor("_Color", Color.black);

            wireframe = new Material(ShaderHolder.GetShader("Custom/Wireframe"));
            wireframe.SetColor("_WireColor", new Color(0.4433962f, 0.4433962f, 0.4433962f));
            wireframe.SetColor("_Color", new Color(0.1215686f, 0.1215686f, 0.1215686f));

            wireframeAlt = new Material(ShaderHolder.GetShader("Custom/Wireframe"));
            wireframeAlt.SetColor("_WireColor", new Color(0.4433962f, 1f, 0.4433962f));
            wireframeAlt.SetColor("_Color", new Color(0.1215686f, 0.1215686f, 0.1215686f));

            transparent = new Material(Shader.Find("Unlit/Transparent"));

            transparent.SetTexture("_MainTex", Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(t => t.name == "Parallax/BlankAlpha"));
        }
    }
    public class SubdivisionData    //Only added to quads that could be subdivided (max level quads)
    {
        public PQ quad;
        public int subdivisionLevel;
        public bool subdividable = false;
        public GameObject newQuad;
        public bool initialized = false;
        public float quadWidth = 0;
        public float subdivisionSearchRadius = 0;
        public Mesh mesh;
        public Material quadMaterial;
        public bool materialCreated = false;
        public AdvancedSubdivision subdivisionComponent;
        public SubdivisionData(PQ quad, int subdivisionLevel, float subdivisionRadius, bool subdividable)
        {
            this.quad = quad;
            this.subdivisionLevel = subdivisionLevel;
            this.subdividable = subdividable;
            this.subdivisionSearchRadius = subdivisionRadius;
            quadMaterial = DetermineMaterial();

            if (subdividable)
            {
                quadWidth = (float)((2f * Mathf.PI * FlightGlobals.GetBodyByName(quad.sphereRoot.name).Radius / 4f) / (Mathf.Pow(2f, quad.sphereRoot.maxLevel)));
                mesh = GameObject.Instantiate(quad.mesh);
                QuadRangeCheck.OnQuadRangeCheck += RangeCheck;
                
                RangeCheck();
            }
            else
            {
                SwapMaterial(false);
            }
        }
        public void RangeCheck()    //Only start when in range
        {
            Vector3 origin = FlightGlobals.ActiveVessel == null ? Vector3.zero : FlightGlobals.ActiveVessel.transform.position;
            Vector3 localSpaceOrigin = quad.transform.InverseTransformPoint(origin);
            float distance = Vector3.Distance(origin, quad.gameObject.transform.position);
            if (distance < quadWidth)
            {
                if (!initialized)
                {
                    Start();
                    subdivisionComponent = new AdvancedSubdivision(quad, ref newQuad, ref mesh, quadWidth, ref quadMaterial, subdivisionLevel);
                    materialCreated = false;
                }
                subdivisionComponent.RangeCheck(ref localSpaceOrigin);
            }

            if (distance > quadWidth && initialized)
            {
                subdivisionComponent.Cleanup();
                subdivisionComponent = null;
                OutOfRange();
            }
            if (distance > quadWidth && !materialCreated)   //Handles out of range, but still maxlevel sudivision
            {
                SwapMaterial(false);
                materialCreated = true;
            }
            
        }
        public void Start()
        {
            newQuad = new GameObject(quad.name);

            newQuad.transform.position = quad.gameObject.transform.position;
            newQuad.transform.localScale = quad.gameObject.transform.localScale;
            newQuad.gameObject.transform.rotation = quad.gameObject.transform.rotation;
            newQuad.transform.parent = quad.gameObject.transform;
            newQuad.layer = quad.gameObject.layer;
            newQuad.tag = quad.gameObject.tag;
            
            MeshFilter quadMeshFilter = newQuad.AddComponent<MeshFilter>();
            MeshRenderer quadMeshRenderer = newQuad.AddComponent<MeshRenderer>();
            quadMeshFilter.sharedMesh = mesh;
            quadMeshRenderer.sharedMaterial = quadMaterial;

            newQuad.SetActive(true);

            SwapMaterial(true);

            initialized = true;
        }
        public void SwapMaterial(bool inSubdivisionRange)
        {
            if (inSubdivisionRange)
            {
                MeshRenderer oldMeshRenderer = quad.gameObject.GetComponent<MeshRenderer>();
                oldMeshRenderer.material = ParallaxDebug.transparent;
            }
            else
            {
                MeshRenderer oldMeshRenderer = quad.gameObject.GetComponent<MeshRenderer>();
                oldMeshRenderer.sharedMaterial = quadMaterial;
            }
        }
        public void Subdivide()
        {
            Int32[] indices = new Int32[mesh.triangles.Length];
            mesh.triangles.CopyTo(indices, 0);
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.triangles = indices;
            MeshHelper.Subdivide(mesh, subdivisionLevel);
        }
        public Material DetermineMaterial()
        {
            ParallaxBody body = ParallaxBodies.parallaxBodies[quad.sphereRoot.name];

            float lowStart = body._LowStart;
            float lowEnd = body._LowEnd;
            float highStart = body._HighStart;
            float highEnd = body._HighEnd;

            float highPoint = (float)quad.meshVertMax;
            float lowPoint = (float)quad.meshVertMin;

            Vector3[] normals = quad.mesh.normals;
            Vector3[] vertices = quad.mesh.vertices;
            Vector3 planetNormal = Vector3.zero;
            Vector3 worldNormal = Vector3.zero;
            Vector3 planetPos = FlightGlobals.GetBodyByName(quad.sphereRoot.name).gameObject.transform.position;
            float stepPow = body._SteepPower;
            bool hasSteep = false;
            for (int i = 0; i < normals.Length; i++)
            {
                worldNormal = Vector3.Normalize(quad.gameObject.transform.TransformDirection(normals[i]));
                planetNormal = Vector3.Normalize(quad.gameObject.transform.TransformPoint(vertices[i]) - planetPos);
                float steep = Mathf.Pow(Vector3.Dot(worldNormal, planetNormal), stepPow);
                steep = Mathf.Clamp01((steep - body._SteepMidpoint) * body._SteepContrast + body._SteepMidpoint);
                if (steep < 0.95) { hasSteep = true; }
            }

            if (lowPoint > highEnd)
            {
                //High
                if (hasSteep)
                {
                    return body.singleSteepHigh.parallaxMaterial;
                }
                return body.singleHigh.parallaxMaterial;
            }
            if (lowPoint > lowEnd && highPoint < highStart)
            {
                //Mid
                if (hasSteep)
                {
                    return body.singleSteepMid.parallaxMaterial;
                }
                return body.singleMid.parallaxMaterial;
            }
            if (highPoint < lowStart)
            {
                //Low
                if (hasSteep)
                {
                    return body.singleSteepLow.parallaxMaterial;
                }
                return body.singleLow.parallaxMaterial;
            }

            if ((lowPoint < lowStart && highPoint > lowEnd && highPoint < highStart) || (lowPoint < lowStart && highPoint > lowStart && highPoint < lowEnd) || (lowPoint > lowStart && lowPoint < lowEnd && highPoint > lowEnd && highPoint < highStart) || (lowPoint > lowStart && lowPoint < lowEnd && highPoint > lowStart && highPoint < lowEnd))
            {
                //Low double
                return body.doubleLow.parallaxMaterial;
            }
            if ((lowPoint < highStart && lowPoint > lowEnd && highPoint > highEnd) || (lowPoint < highStart && lowPoint > lowEnd && highPoint > highStart) || (lowPoint > highStart && lowPoint < highEnd && highPoint > highEnd) || (lowPoint > highStart && lowPoint < highEnd && highPoint > highStart && highPoint < highEnd))
            {
                //High double
                return body.doubleHigh.parallaxMaterial;
            }
            return body.full.parallaxMaterial;

        }
        public void OutOfRange()
        {
            UnityEngine.Object.Destroy(newQuad);
            MeshRenderer quadMeshRenderer = quad.gameObject.GetComponent<MeshRenderer>();
            quadMeshRenderer.sharedMaterial = quadMaterial;
            initialized = false;
        }
        public void Cleanup()
        {
            if (subdividable)
            {
                QuadRangeCheck.OnQuadRangeCheck -= RangeCheck;
                UnityEngine.Object.Destroy(newQuad);
                UnityEngine.Object.Destroy(mesh);
                initialized = false;
            }
            if (subdivisionComponent != null)
            {
                subdivisionComponent.Cleanup();
                subdivisionComponent = null;
            }
        }
    }
}
