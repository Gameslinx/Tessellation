using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ComputeLoader;
using System.Collections;
using ScatterConfiguratorUtils;
using ParallaxGrass;
using LibNoise;

namespace Grass
{
    public struct Properties
    {
        public Distribution scatterDistribution;
        public ScatterMaterial scatterMaterial;
        public SubdivisionProperties subdivisionSettings;
        public int subObjectCount;
        public float memoryMultiplier; //Manual VRAM management until I figure out how to automate it
    }
    public struct DistributionNoise
    {
        public DistributionNoiseMode noiseMode;
        public float _Frequency;                //Size of noise
        public float _Lacunarity;
        public float _Persistence;
        public float _Octaves;
        public int _Seed;
        public NoiseQuality _NoiseQuality;
        public int _NoiseType;  //0 perlin, 1 rmf, 2 billow

        public float _SizeNoiseScale;
        public float _ColorNoiseScale;
        public float _SizeNoiseOffset;

        public int _MaxStacks;
        public float _StackSeparation;
    }
    public struct Distribution
    {
        public DistributionNoise noise;
        public LODs lods;
        public float _Range;                    //How far from the camera to render at the max graphics setting
        public float _RangePow;                 //Range fade power
        public float _PopulationMultiplier;     //How many scatters to render
        public float _SizeNoiseStrength;        //Strength of perlin noise - How varied the scatter size is
        public Vector3 _MinScale;               //Smallest scatter size
        public Vector3 _MaxScale;               //Largest scatter size
        public float _CutoffScale;              //Minimum scale at which, below that scale, the scatter is not placed
        public float _SteepPower;
        public float _SteepContrast;
        public float _SteepMidpoint;
        public float _SpawnChance;
        public float _MaxNormalDeviance;
        public float _MaxSubdivisionRange;
        public float _MinAltitude;
        public float _MaxAltitude;
        public float _Seed;
    }
    public struct LODs
    {
        public LOD[] lods;
        public int LODCount;
    }
    public struct LOD
    {
        public float range;
        public string modelName;
        public string mainTexName;
        public bool isBillboard;
    }
    public struct ScatterMaterial
    {
        public Dictionary<string, string> Textures;
        public Dictionary<string, float> Floats;
        public Dictionary<string, Vector3> Vectors;
        public Dictionary<string, Vector2> Scales;
        public Dictionary<string, Color> Colors;

        public Shader shader;
        public Color _MainColor;
        public Color _SubColor;
        public float _ColorNoiseStrength;
    }
    public struct SubdivisionProperties
    {
        public SubdivisionMode mode;
        public float range;
        public int level;
    }
    public enum DistributionNoiseMode
    {
        Persistent,
        NonPersistent,
        VerticalStack
    }
    public enum SubdivisionMode
    {
        NearestQuads,
        FixedRange
    }
    public struct SubObjectProperties
    {
        public ScatterMaterial material;
        public string model;
        public float _NoiseScale;
        public float _NoiseAmount;
        public float _Density;
    }
    public struct SubObjectMaterial
    {
        public Shader shader;
        public string _MainTex;
        public string _BumpMap;
        public float _Shininess;
        public Color _SpecColor;
    }
    public static class ScatterBodies
    {
        public static Dictionary<string, ScatterBody> scatterBodies = new Dictionary<string, ScatterBody>();
    }
    public class ScatterBody
    {
        public Dictionary<string, Scatter> scatters = new Dictionary<string, Scatter>();
        public string bodyName = "invalidname";
        public int minimumSubdivision = 6;
        public ScatterBody(string name, string minSub)
        {
            bodyName = name;
            bool converted = int.TryParse(minSub, out minimumSubdivision);
            if (!converted) { ScatterLog.SubLog("[Exception] Unable to get the value of minimumSubdivision"); minimumSubdivision = 6; }
        }
    }
    public class Scatter
    {
        public string scatterName = "invalidname";
        public string planetName = "invalidname";
        public string model;
        public float updateFPS = 1;
        public bool alignToTerrainNormal = false;
        public int subObjectCount = 0;
        public Properties properties;
        public UnityEngine.Rendering.ShadowCastingMode shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        public SubObject[] subObjects;
        public bool isVisible = true;
        public bool useSurfacePos = false;
        public float cullingRange = 0;
        public float cullingLimit = -15;
        
        public Scatter(string name)
        {
            scatterName = name;
        }
        public IEnumerator ForceComputeUpdate()
        {
            ScreenMessages.PostScreenMessage("WARNING: Forcing a compute update is not recommended and should not be called in realtime!");
            QuadComp[] allQuadComps = UnityEngine.Resources.FindObjectsOfTypeAll(typeof(QuadComp)) as QuadComp[];
            List<ComputeComponent> allQuads = new List<ComputeComponent>();
            foreach (var quadComp in allQuadComps) 
            {
                Debug.Log("Checking quadComp:");
                ComputeComponent cc = quadComp.comps.Where(c => c?.scatter?.scatterName == scatterName)?.FirstOrDefault(); 
                if (cc != null) 
                {
                    Debug.Log("Found CC");
                    allQuads.Add(cc); 
                } 
            }
            PQSMod_ScatterManager pqsMod = ActiveBuffers.mods.Find(x => x.scatterName == scatterName);
            pqsMod.scatter = this;
            foreach (var cc in allQuads) { cc.scatter = this; cc.GeneratePositions(); }

            //QuadComp[] allQuadComps = UnityEngine.Resources.FindObjectsOfTypeAll(typeof(QuadComp)) as QuadComp[];
            //Debug.Log("Updating " + allQuadComps.Length + " components...");
            //int counter = 0;
            //int scatterCount = ScatterBodies.scatterBodies[FlightGlobals.currentMainBody.name].scatters.Count;
            //string[] keys = ScatterBodies.scatterBodies[FlightGlobals.currentMainBody.name].scatters.Keys.ToArray();
            //int index = 0;
            //for (int i = 0; i < scatterCount; i++)
            //{
            //    string key = keys[i];
            //    if (scatterName == ScatterBodies.scatterBodies[FlightGlobals.currentMainBody.name].scatters[key].scatterName)
            //    {
            //        index = i;
            //    }
            //}
            //PQSMod_ScatterManager pqsMod = ActiveBuffers.mods.Find(x => x.scatterName == scatterName);
            //Debug.Log("Found pc: " + pqsMod.name);
            //ActiveBuffers.activeBuffers[scatterName].Dispose();
            //ActiveBuffers.mods.Remove(pqsMod);
            //pqsMod.bufferList.Dispose();
            //pqsMod.Start();
            //pqsMod.stop = true;
            //foreach (QuadComp quadComp in allQuadComps)
            //{
            //    counter++;
            //    if (quadComp.comps != null)
            //    {
            //        ComputeComponent cc = quadComp.comps[index];
            //        if (cc != null)
            //        {
            //            cc.scatter.properties = properties;
            //            cc.updateFPS = updateFPS;
            //            if (cc.mesh == null)
            //            {
            //                Debug.Log("Null mesh on ComputeComponent detected, instantiating quad mesh (again?)");
            //                if (quadComp.quad != null)
            //                {
            //                    cc.mesh = GameObject.Instantiate(quadComp.quad.GetComponent<MeshFilter>().mesh);
            //                }
            //                else { Debug.Log("Quad itself is null"); }
            //            }
            //            cc.Start();
            //        }
            //    }
            //    
            //}
            yield return null;
        }
        public IEnumerator SwitchQuadMaterial(Material mat, bool revert, Scatter scatter)
        {
            
            QuadComp[] allQMComps = UnityEngine.Resources.FindObjectsOfTypeAll(typeof(QuadComp)) as QuadComp[];
            int counter = 0;
            foreach (QuadComp comp in allQMComps)
            {
                counter++;
                if (comp.gameObject.activeSelf && !revert)
                {
                    Debug.Log("Instantiating");
                    PQ quad = comp.quad;
                    GameObject go = new GameObject();
                    go.name = quad.name + "-INST";
                    go.transform.position = quad.transform.position;
                    go.transform.rotation = quad.transform.rotation;
                    go.transform.localScale = quad.transform.localScale;
                    var newQMF = go.AddComponent<MeshFilter>();
                    newQMF.mesh = GameObject.Instantiate(quad.mesh);
                    float[] data = PQSMod_ScatterDistribute.scatterData.distributionData[scatter.scatterName].data[quad.name];
                    Color[] colors = new Color[data.Length];
                    Color32[] colors32 = new Color32[data.Length];
                    for (int i = 0; i < quad.mesh.colors.Length; i++)
                    {
                        if (data != null)
                        {
                            colors[i] = new Color(data[i], data[i], data[i]);
                            colors32[i] = new Color(data[i], data[i], data[i]);
                        }
                        else
                        {
                            Debug.Log("Null lol");
                        }
                    }
                    newQMF.mesh.colors = colors;
                    newQMF.mesh.colors32 = colors32;
                    go.GetComponent<MeshFilter>().mesh = newQMF.mesh;
                    var newQMR = go.AddComponent<MeshRenderer>();
            
                    newQMR.material = mat;
                    go.transform.position += Vector3.Normalize(FlightGlobals.ActiveVessel.transform.position - FlightGlobals.currentMainBody.transform.position);
                    go.SetActive(true);
                }
                if (revert)
                {
                    Debug.Log("Reverting");
                    PQ quad = comp.quad;
                    GameObject go = GameObject.Find(quad.name + "-INST");
                    if (go != null)
                    {
                        comp.DestroyGO(go);
                    }
                }
                if (counter % 20 == 0)
                {
                    yield return null;
                }
                
            }
        }
        public IEnumerator ForceMaterialUpdate(Scatter scatter)
        {
        
            PostCompute pc = (UnityEngine.Resources.FindObjectsOfTypeAll(typeof(PostCompute)) as PostCompute[]).Where(x => x.scatterName == scatterName)?.FirstOrDefault();
            if (pc != null)
            {
                pc.scatterProps = properties;
                pc.SetupAgain(scatter);
            }
            yield return null;

            //ScreenMessages.PostScreenMessage("WARNING: Forcing a compute update is not recommended and should not be called in realtime!");
            //PostCompute[] allPC = UnityEngine.Resources.FindObjectsOfTypeAll(typeof(PostCompute)) as PostCompute[];
            //Debug.Log("Updating " + allPC.Length + " components...");
            //int counter = 0;
            //int scatterCount = ScatterBodies.scatterBodies[FlightGlobals.currentMainBody.name].scatters.Count;
            //string[] keys = ScatterBodies.scatterBodies[FlightGlobals.currentMainBody.name].scatters.Keys.ToArray();
            //int index = 0;
            //for (int i = 0; i < scatterCount; i++)
            //{
            //    string key = keys[i];
            //    if (scatterName == ScatterBodies.scatterBodies[FlightGlobals.currentMainBody.name].scatters[key].scatterName)
            //    {
            //        index = i;
            //    }
            //}
            //
            //foreach (PostCompute quadComp in allPC)
            //{
            //    counter++;
            //    if (quadComp != null)
            //    {
            //        quadComp.scatterProps = properties;
            //        quadComp.SetupAgain(scatter);
            //    }
            //    if (counter % 20 == 0)
            //    {
            //        yield return null;
            //    }
            //}
        }
        public void ModifyScatterVisibility()
        {
            PostCompute[] allComputeComponents = UnityEngine.Resources.FindObjectsOfTypeAll(typeof(PostCompute)) as PostCompute[];
            foreach (PostCompute comp in allComputeComponents)
            {
                if (comp.scatterName != null && comp.scatterName == scatterName)
                {
                    Debug.Log("Found PC: " + comp.name);
                    if (comp == null)
                    {
                        Debug.Log("Component is null??");
                    }
                    comp.active = isVisible;
                }
            }
            ComputeComponent[] allComputeComponents2 = UnityEngine.Resources.FindObjectsOfTypeAll(typeof(ComputeComponent)) as ComputeComponent[];
            foreach (ComputeComponent comp in allComputeComponents2)
            {
                if (comp.scatter != null && comp.scatter.scatterName == scatterName)
                {
                    Debug.Log("Found CC: " + comp.name);
                    if (comp == null)
                    {
                        Debug.Log("Component is null??");
                    }
                    comp.doEvaluate = isVisible;
                }
            }
        }
        //public int GetGlobalVertexCount(Scatter currentScatter)
        //{
        //    if (currentScatter == null)
        //    {
        //        ScatterLog.Log("The next scatter is null!");
        //        return 0;
        //    }
        //    int[] objectCount = new int[] { 0, 0, 0, 0, 0, 0, 0 };
        //    int[] vertCount = new int[] { 0, 0, 0, 0, 0, 0, 0 };
        //    ScreenMessages.PostScreenMessage("WARNING: Counting all objects, this should not be called in realtime!");
        //    ComputeComponent[] allComputeComponents = UnityEngine.Resources.FindObjectsOfTypeAll(typeof(ComputeComponent)) as ComputeComponent[];
        //    foreach (ComputeComponent cc in allComputeComponents)
        //    {
        //        
        //        if (cc.gameObject.activeSelf && cc.scatter.scatterName == currentScatter.scatterName)
        //        {
        //            PostCompute pc = cc.pc;
        //            vertCount[0] = pc.vertexCount;
        //            vertCount[1] = pc.farVertexCount;
        //            vertCount[2] = pc.furtherVertexCount;
        //
        //            vertCount[3] = pc.subVertexCount1;
        //            vertCount[4] = pc.subVertexCount2;
        //            vertCount[5] = pc.subVertexCount3;
        //            vertCount[6] = pc.subVertexCount4;
        //
        //            //objectCount[0] += pc.countCheck;
        //            //objectCount[1] += pc.farCountCheck;
        //            //objectCount[2] += pc.furtherCountCheck;
        //            //
        //            //objectCount[3] += pc.subCount1;
        //            //objectCount[4] += pc.subCount2;
        //            //objectCount[5] += pc.subCount3;
        //            //objectCount[6] += pc.subCount4;
        //        }
        //    }
        //    ScatterLog.Log("Finished counting objects and vertices");
        //    ScatterLog.Log("Breakdown for Scatter '" + currentScatter.scatterName + "':");
        //    ScatterLog.Log("LOD0 Mesh vertex count: " + vertCount[0].ToString("N0"));
        //    ScatterLog.Log("LOD1 Mesh vertex count: " + vertCount[1].ToString("N0"));
        //    ScatterLog.Log("LOD2 Mesh vertex count: " + vertCount[2].ToString("N0"));
        //    Debug.Log("");
        //    ScatterLog.Log("SO 1 Mesh vertex count: " + vertCount[3].ToString("N0"));
        //    ScatterLog.Log("SO 2 Mesh vertex count: " + vertCount[4].ToString("N0"));
        //    ScatterLog.Log("SO 3 Mesh vertex count: " + vertCount[5].ToString("N0"));
        //    ScatterLog.Log("SO 4 Mesh vertex count: " + vertCount[6].ToString("N0"));
        //    ScatterLog.Log("----------------------------");
        //    ScatterLog.Log("LOD0 object count: " + objectCount[0].ToString("N0"));
        //    ScatterLog.Log("LOD1 object count: " + objectCount[1].ToString("N0"));
        //    ScatterLog.Log("LOD2 object count: " + objectCount[2].ToString("N0"));
        //    Debug.Log("");
        //    ScatterLog.Log("SO 1 object count: " + objectCount[3].ToString("N0"));
        //    ScatterLog.Log("SO 2 object count: " + objectCount[4].ToString("N0"));
        //    ScatterLog.Log("SO 3 object count: " + objectCount[5].ToString("N0"));
        //    ScatterLog.Log("SO 4 object count: " + objectCount[6].ToString("N0"));
        //    ScatterLog.Log("----------------------------");
        //    ScatterLog.Log("LOD0 global vertex count: " + (objectCount[0] * vertCount[0]).ToString("N0"));
        //    ScatterLog.Log("LOD1 global vertex count: " + (objectCount[1] * vertCount[1]).ToString("N0"));
        //    ScatterLog.Log("LOD2 global vertex count: " + (objectCount[2] * vertCount[2]).ToString("N0"));
        //    Debug.Log("");
        //    ScatterLog.Log("SO 1 global vertex count: " + (objectCount[3] * vertCount[3]).ToString("N0"));
        //    ScatterLog.Log("SO 2 global vertex count: " + (objectCount[4] * vertCount[4]).ToString("N0"));
        //    ScatterLog.Log("SO 3 global vertex count: " + (objectCount[5] * vertCount[5]).ToString("N0"));
        //    ScatterLog.Log("SO 4 global vertex count: " + (objectCount[6] * vertCount[6]).ToString("N0"));
        //    ScatterLog.Log("----------------------------");
        //    int totalVertCount = CountAll(objectCount, vertCount);
        //    ScatterLog.Log("Total amount of vertices for this scatter being rendered right now: " + totalVertCount.ToString("N0"));
        //    return totalVertCount;
        //}
        //public float GetPlanetVRAMUsage(Scatter currentScatter)
        //{
        //    if (currentScatter == null)
        //    {
        //        ScatterLog.Log("The next scatter is null!");
        //        return 0;
        //    }
        //    int[] objectCount = new int[] { 0, 0, 0, 0, 0, 0, 0 };
        //    int[] vertCount = new int[] { 0, 0, 0, 0, 0, 0, 0 };
        //    ScreenMessages.PostScreenMessage("WARNING: Getting total VRAM usage!");
        //    ComputeComponent[] allComputeComponents = UnityEngine.Resources.FindObjectsOfTypeAll(typeof(ComputeComponent)) as ComputeComponent[];
        //    float scatterRAMUsage = 0;
        //    foreach (ComputeComponent cc in allComputeComponents)
        //    {
        //        if (cc.vRAMinMb != 0 && cc.scatter.scatterName == currentScatter.scatterName)
        //        {
        //            scatterRAMUsage += cc.vRAMinMb;
        //        }
        //
        //    }
        //    ScatterLog.Log("Finished counting VRAM for " + currentScatter.scatterName + ": " + scatterRAMUsage + " mb");
        //    return scatterRAMUsage;
        //}
        public static int CountAll(int[] objs, int[] verts)
        {
            int total = 0;
            for (int i = 0; i < objs.Length; i++)
            {
                total += (objs[i] * verts[i]);
            }
            return total;
        }
        public static int GetMeshVertexCountSafe(Mesh mesh, int countCheck)
        {
            if (countCheck > 0 && mesh != null)
            {
                return mesh.vertexCount;
            }
            else
            {
                return 0;
            }
        }
    }
    public class SubObject
    {
        public string objectName;
        public Scatter scatter;
        public SubObjectProperties properties;
        public SubObject(Scatter parent, string name)
        {
            objectName = name;
            scatter = parent;
        }
    }
    public static class ScatterLog
    {
        public static void Log(string message)
        {
            Debug.Log("[ParallaxScatter] " + message);
        }
        public static void SubLog(string message)
        {
            Debug.Log("[ParallaxScatter] \t\t - " + message);
        }
    }
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class HardwareDetection : MonoBehaviour
    {
        void Start()
        {
            Debug.Log("[ParallaxScatter] Retrieving GPU capabilies");
            Debug.Log(" - " + Evaluate("Compute Shaders", SystemInfo.supportsComputeShaders));
            Debug.Log(" - " + Evaluate("Async Compute", SystemInfo.supportsAsyncCompute));
            Debug.Log(" - " + Evaluate("Async Readback", SystemInfo.supportsAsyncGPUReadback));
            Debug.Log(" - " + "Max compute work size: " + SystemInfo.maxComputeWorkGroupSize);
            Debug.Log(" - " + "Max compute work size (X): " + SystemInfo.maxComputeWorkGroupSizeX);
            Debug.Log(" - " + "Max compute work size (Y): " + SystemInfo.maxComputeWorkGroupSizeY);
            Debug.Log(" - " + "Max compute work size (Z): " + SystemInfo.maxComputeWorkGroupSizeZ);
        }
        string Evaluate(string name, bool supports)
        {
            if (supports)
            {
                return " supports " + name;
            }
            else
            {
                return " does not support " + name;
            }
        }
    }
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class ScatterLoader : MonoBehaviour
    {
        public UrlDir.UrlConfig[] globalNodes;
        public void Start()
        {
            globalNodes = GameDatabase.Instance.GetConfigs("ParallaxScatters");
            LoadScatterNodes();
        }
        public void LoadScatterNodes()
        {
            for (int i = 0; i < globalNodes.Length; i++)
            {
                string bodyName = globalNodes[i].config.GetValue("body");
                string minSubdiv = globalNodes[i].config.GetValue("minimumSubdivision");
                ScatterBody body = new ScatterBody(bodyName, minSubdiv);
                ScatterBodies.scatterBodies.Add(bodyName, body);
                ScatterLog.Log("Parsing body: " + bodyName);
                for (int b = 0; b < globalNodes[i].config.nodes.Count; b++)
                {
                    ConfigNode rootNode = globalNodes[i].config;
                    ConfigNode scatterNode = rootNode.nodes[b];
                    ConfigNode distributionNode = scatterNode.GetNode("Distribution");
                    ConfigNode materialNode = scatterNode.GetNode("Material");
                    ConfigNode windNode = scatterNode.GetNode("Wind");
                    ConfigNode subdivisionSettingsNode = scatterNode.GetNode("SubdivisionSettings");
                    ConfigNode subObjectNode = scatterNode.GetNode("SubObjects");
                    ConfigNode distributionNoiseNode = scatterNode.GetNode("DistributionNoise");

                    ParseNewBody(scatterNode, distributionNoiseNode, distributionNode, materialNode, windNode, subdivisionSettingsNode, subObjectNode, bodyName);
                }
                
            }
        }
        public void ParseNewBody(ConfigNode scatterNode, ConfigNode distributionNoiseNode, ConfigNode distributionNode, ConfigNode materialNode, ConfigNode windNode, ConfigNode subdivisionSettingsNode, ConfigNode subObjectNode, string bodyName)
        {
            ScatterBody body = ScatterBodies.scatterBodies[bodyName];   //Bodies contain multiple scatters
            string scatterName = scatterNode.GetValue("name");
            ScatterLog.Log("Parsing scatter: " + scatterName);
            Scatter scatter = new Scatter(scatterName);
            scatter.planetName = bodyName;
            Properties props = new Properties();
            scatter.model = scatterNode.GetValue("model");
            string forcedFull = "";
            bool forcedFullShadows = scatterNode.TryGetValue("shadowMode", ref forcedFull);
            if (forcedFullShadows && forcedFull == "forcedFull") { scatter.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
            string alignToNormal = "";
            bool requiresNormal = scatterNode.TryGetValue("alignToTerrainNormal", ref alignToNormal);
            if (requiresNormal) { scatter.alignToTerrainNormal = bool.Parse(alignToNormal); } else { scatter.alignToTerrainNormal = false;}
            bool useSurfacePos = false;
            useSurfacePos = scatterNode.TryGetValue("useSurfacePosition", ref useSurfacePos);
            if (useSurfacePos) { scatter.useSurfacePos = true; } else { scatter.useSurfacePos = false; }
            string cullRange = "";
            bool cullRangeCheck = scatterNode.TryGetValue("cullingRange", ref cullRange);
            if (cullRangeCheck) { scatter.cullingRange = float.Parse(cullRange); }
            string cullLimit = "";
            bool cullLimitCheck = scatterNode.TryGetValue("cullingLimit", ref cullLimit);
            if (cullLimitCheck) { scatter.cullingLimit = float.Parse(cullLimit); }
            if (useSurfacePos) { scatter.useSurfacePos = true; } else { scatter.useSurfacePos = false; }
            props.scatterDistribution = ParseDistribution(distributionNode);
            props.scatterDistribution.noise = ParseDistributionNoise(distributionNoiseNode);
            props.scatterMaterial = ParseMaterial(materialNode, false);
            props.subdivisionSettings = ParseSubdivisionSettings(subdivisionSettingsNode);
            props.memoryMultiplier = 100;
            scatter.properties = props;
            scatter.subObjects = ParseSubObjects(scatter, subObjectNode);
            scatter.updateFPS = ParseFloat(ParseVar(scatterNode, "updateFPS"));
            body.scatters.Add(scatterName, scatter);
        }
        public DistributionNoise ParseDistributionNoise(ConfigNode distributionNode)
        {
            DistributionNoise distribution = new DistributionNoise();
            string noiseMode = ParseVar(distributionNode, "mode");
            distribution._MaxStacks = 1;
            distribution._StackSeparation = 1;
            if (noiseMode.ToLower() == "nonpersistent") { distribution.noiseMode = DistributionNoiseMode.NonPersistent; }
            else if (noiseMode.ToLower() == "verticalstack") { distribution.noiseMode = DistributionNoiseMode.VerticalStack; }
            else { distribution.noiseMode = DistributionNoiseMode.Persistent; }
            if (distribution.noiseMode == DistributionNoiseMode.Persistent || distribution.noiseMode == DistributionNoiseMode.VerticalStack)
            {
                distribution._Frequency = ParseFloat(ParseVar(distributionNode, "_Frequency"));
                distribution._Lacunarity = ParseFloat(ParseVar(distributionNode, "_Lacunarity"));
                distribution._Persistence = ParseFloat(ParseVar(distributionNode, "_Persistence"));
                distribution._Octaves = ParseFloat(ParseVar(distributionNode, "_Octaves"));
                distribution._Seed = (int)ParseFloat(ParseVar(distributionNode, "_Seed"));
                string noiseType = ParseVar(distributionNode, "_NoiseType");
                switch (noiseType)
                {
                    default:
                        distribution._NoiseType = 0;
                        break;
                    case "RidgedMultifractal":
                        distribution._NoiseType = 1;
                        break;
                    case "Billow":
                        distribution._NoiseType = 2;
                        break;
                    case "1":
                        distribution._NoiseType = 1;
                        break;
                    case "2":
                        distribution._NoiseType = 2;
                        break;
                }
                string noiseQuality = ParseVar(distributionNode, "_NoiseQuality");
                switch (noiseQuality)
                {
                    default:
                        distribution._NoiseQuality = NoiseQuality.Standard;
                        break;
                    case "Low":
                        distribution._NoiseQuality = NoiseQuality.Low;
                        break;
                    case "High":
                        distribution._NoiseQuality = NoiseQuality.High;
                        break;
                }
                distribution._SizeNoiseScale = 0;
                distribution._ColorNoiseScale = 0;
                distribution._SizeNoiseOffset = 0;
                if (distribution.noiseMode == DistributionNoiseMode.VerticalStack)
                {
                    distribution._MaxStacks = (int)ParseFloat(ParseVar(distributionNode, "_MaxStacks"));
                    distribution._StackSeparation = (int)ParseFloat(ParseVar(distributionNode, "_StackSeparation"));
                }
            }
            else
            {
                distribution._SizeNoiseScale = ParseFloat(ParseVar(distributionNode, "_SizeNoiseScale"));
                distribution._ColorNoiseScale = ParseFloat(ParseVar(distributionNode, "_ColorNoiseScale"));
                distribution._SizeNoiseOffset = ParseFloat(ParseVar(distributionNode, "_SizeNoiseOffset"));
            }

            return distribution;
        }
        public Distribution ParseDistribution(ConfigNode distributionNode)
        {
            Distribution distribution = new Distribution();
            
            distribution._Range = ParseFloat(ParseVar(distributionNode, "_Range"));
            distribution._RangePow = ParseFloat(ParseVar(distributionNode, "_RangePow"));
            distribution._PopulationMultiplier = ParseFloat(ParseVar(distributionNode, "_PopulationMultiplier"));
            distribution._SizeNoiseStrength = ParseFloat(ParseVar(distributionNode, "_SizeNoiseStrength"));
            distribution._CutoffScale = ParseFloat(ParseVar(distributionNode, "_CutoffScale"));
            distribution._SteepPower = ParseFloat(ParseVar(distributionNode, "_SteepPower"));
            distribution._SteepContrast = ParseFloat(ParseVar(distributionNode, "_SteepContrast"));
            distribution._SteepMidpoint = ParseFloat(ParseVar(distributionNode, "_SteepMidpoint"));
            distribution._MaxNormalDeviance = ParseFloat(ParseVar(distributionNode, "_NormalDeviance"));
            distribution._MinScale = ParseVector(ParseVar(distributionNode, "_MinScale"));
            distribution._MaxScale = ParseVector(ParseVar(distributionNode, "_MaxScale"));
            distribution._MinAltitude = ParseFloat(ParseVar(distributionNode, "_MinAltitude"));
            distribution._MaxAltitude = ParseFloat(ParseVar(distributionNode, "_MaxAltitude"));
            distribution._SpawnChance = ParseFloat(ParseVar(distributionNode, "_SpawnChance"));
            distribution._Seed = ParseFloat(ParseVar(distributionNode, "_Seed"));

            ConfigNode lodNode = distributionNode.GetNode("LODs");
            distribution.lods = ParseLODs(lodNode);

            return distribution;
        }
        public LODs ParseLODs(ConfigNode lodNode)
        {
            LODs lods = new LODs();
            ConfigNode[] lodNodes = lodNode.GetNodes("LOD");
            lods.LODCount = lodNodes.Length;
            lods.lods = new LOD[lods.LODCount];
            for (int i = 0; i < lods.LODCount; i++)
            {
                LOD lod = new LOD();
                lod.range = ParseFloat(ParseVar(lodNodes[i], "range"));
                lod.modelName = ParseVar(lodNodes[i], "model");
                lod.mainTexName = ParseVar(lodNodes[i], "_MainTex");
                if (lodNodes[i].HasValue("billboard") && lodNodes[i].GetValue("billboard").ToLower() == "true") { lod.isBillboard = true; Debug.Log("has billboard"); }
                else { lod.isBillboard = false; }

                lods.lods[i] = lod;
                //Parse models on main menu after they have loaded
            }
            return lods;
        }
        public ScatterMaterial GetShaderVars(string shaderName, ScatterMaterial material, ConfigNode materialNode)
        {
            UrlDir.UrlConfig[] nodes = GameDatabase.Instance.GetConfigs("ScatterShader");
            for (int i = 0; i < nodes.Length; i++)
            {
                string configShaderName = nodes[i].config.GetValue("name");
                ScatterLog.Log("Loading shader from shader bank: " + configShaderName);
                if (configShaderName == shaderName)
                {
                    ConfigNode propertiesNode = nodes[i].config.GetNode("Properties");
                    ConfigNode texturesNode = propertiesNode.GetNode("Textures");
                    ConfigNode floatsNode = propertiesNode.GetNode("Floats");
                    ConfigNode vectorsNode = propertiesNode.GetNode("Vectors");
                    ConfigNode scalesNode = propertiesNode.GetNode("Scales");
                    ConfigNode colorsNode = propertiesNode.GetNode("Colors");
                    material = ParseNodeType(texturesNode, typeof(string), material);
                    material = ParseNodeType(floatsNode, typeof(float), material);
                    material = ParseNodeType(vectorsNode, typeof(Vector3), material);
                    material = ParseNodeType(scalesNode, typeof(Vector2), material);
                    material = ParseNodeType(colorsNode, typeof(Color), material);
                    material = SetShaderValues(materialNode, material);

                }
            }
            return material;
        }
        public ScatterMaterial ParseNodeType(ConfigNode node, Type type, ScatterMaterial material)
        {
            string[] values = node.GetValues("name");
            if (type == typeof(string))
            {
                material.Textures = new Dictionary<string, string>();
                for (int i = 0; i < values.Length; i++)
                {
                    ScatterLog.SubLog("Parsing " + type.Name + ": " + values[i] + " from the shader bank config");
                    material.Textures.Add(values[i], null);
                }
            }
            else if (type == typeof(float))
            {
                material.Floats = new Dictionary<string, float>();
                for (int i = 0; i < values.Length; i++)
                {
                    ScatterLog.SubLog("Parsing " + type.Name + ": " + values[i] + " from the shader bank config");
                    material.Floats.Add(values[i], 0);
                }
            }
            else if (type == typeof(Vector3))
            {
                material.Vectors = new Dictionary<string, Vector3>();
                for (int i = 0; i < values.Length; i++)
                {
                    ScatterLog.SubLog("Parsing " + type.Name + ": " + values[i] + " from the shader bank config");
                    material.Vectors.Add(values[i], Vector3.zero);
                }
            }
            else if (type == typeof(Vector2))
            {
                material.Scales = new Dictionary<string, Vector2>();
                for (int i = 0; i < values.Length; i++)
                {
                    ScatterLog.SubLog("Parsing " + type.Name + ": " + values[i] + " from the shader bank config");
                    material.Scales.Add(values[i], Vector2.zero);
                }
            }
            else if (type == typeof(Color))
            {
                material.Colors = new Dictionary<string, Color>();
                for (int i = 0; i < values.Length; i++)
                {
                    ScatterLog.SubLog("Parsing " + type.Name + ": " + values[i] + " from the shader bank config");
                    material.Colors.Add(values[i], Color.magenta);
                }
            }
            else
            {
                ScatterLog.SubLog("Unable to determine type");
            }
            return material;
        }
        public ScatterMaterial SetShaderValues(ConfigNode materialNode, ScatterMaterial material)
        {
            string[] textureKeys = material.Textures.Keys.ToArray();
            ScatterLog.Log("Setting shader values: ");
            for (int i = 0; i < material.Textures.Keys.Count; i++)
            {
                ScatterLog.SubLog("Parsing " + textureKeys[i] + " as " + materialNode.GetValue(textureKeys[i]));
                material.Textures[textureKeys[i]] = materialNode.GetValue(textureKeys[i]);
            }
            string[] floatKeys = material.Floats.Keys.ToArray();
            for (int i = 0; i < material.Floats.Keys.Count; i++)
            {
                ScatterLog.SubLog("Parsing " + floatKeys[i] + " as " + materialNode.GetValue(floatKeys[i]));
                material.Floats[floatKeys[i]] = float.Parse(materialNode.GetValue(floatKeys[i]));
            }
            string[] vectorKeys = material.Vectors.Keys.ToArray();
            for (int i = 0; i < material.Vectors.Keys.Count; i++)
            {
                string configValue = materialNode.GetValue(vectorKeys[i]);
                ScatterLog.SubLog("Parsing " + vectorKeys[i] + " as " + materialNode.GetValue(vectorKeys[i]));
                material.Vectors[vectorKeys[i]] = ParseVector(configValue);
            }
            string[] scaleKeys = material.Scales.Keys.ToArray();
            for (int i = 0; i < material.Scales.Keys.Count; i++)
            {
                string configValue = materialNode.GetValue(scaleKeys[i]);
                ScatterLog.SubLog("Parsing " + scaleKeys[i] + " as " + materialNode.GetValue(scaleKeys[i]));
                material.Scales[scaleKeys[i]] = ParseVector2D(configValue);
            }
            string[] colorKeys = material.Colors.Keys.ToArray();
            for (int i = 0; i < material.Colors.Keys.Count; i++)
            {
                string configValue = materialNode.GetValue(colorKeys[i]);
                ScatterLog.SubLog("Parsing " + colorKeys[i] + " as " + materialNode.GetValue(colorKeys[i]));
                material.Colors[colorKeys[i]] = ParseColor(configValue);
            }
            return material;
        }
        public ScatterMaterial ParseMaterial(ConfigNode materialNode, bool isSubObject)
        {
            ScatterMaterial material = new ScatterMaterial();

            material.shader = ScatterShaderHolder.GetShader(ParseVar(materialNode, "shader"));

            material = GetShaderVars(material.shader.name, material, materialNode);
            if (!isSubObject)
            {
                material._MainColor = ParseColor(ParseVar(materialNode, "_MainColor"));
                material._SubColor = ParseColor(ParseVar(materialNode, "_SubColor"));

                
                material._ColorNoiseStrength = ParseFloat(ParseVar(materialNode, "_ColorNoiseStrength"));
            }

            return material;
        }
        public SubdivisionProperties ParseSubdivisionSettings(ConfigNode subdivNode)
        {
            SubdivisionProperties props = new SubdivisionProperties();

            string mode = subdivNode.GetValue("subdivisionRangeMode");
            if (mode == "NearestQuads")
            {
                props.mode = SubdivisionMode.NearestQuads;
            }
            else if (mode == "FixedRange")
            {
                props.mode = SubdivisionMode.FixedRange;
            }
            else
            {
                props.mode = SubdivisionMode.FixedRange;
            }

            props.level = (int)ParseFloat(ParseVar(subdivNode, "subdivisionLevel"));
            props.range = ParseFloat(ParseVar(subdivNode, "subdivisionRange"));

            return props;
        }
        public SubObject[] ParseSubObjects(Scatter scatter, ConfigNode subObjectsNode)
        {
            if (subObjectsNode == null)
            {
                return new SubObject[0];
            }
            ConfigNode[] subObjects = subObjectsNode.GetNodes("Object");
            int count = subObjects.Length;
            SubObject[] objects = new SubObject[count];
            scatter.subObjectCount = count;
            for (int i = 0; i < count; i++)
            {
                string name = subObjects[i].GetValue("name");
                ScatterLog.Log("Parsing SubObject: " + name);
                SubObject subObject = new SubObject(scatter, name);
                subObject.properties = ParseSubObjectProperties(subObjects[i]);
                objects[i] = subObject;
            }
            return objects;
        }
        public SubObjectProperties ParseSubObjectProperties(ConfigNode subNode)
        {
            SubObjectProperties props = new SubObjectProperties();
            props.model = subNode.GetValue("model");
            props._NoiseScale = ParseFloat(ParseVar(subNode, "_NoiseScale"));
            props._NoiseAmount = ParseFloat(ParseVar(subNode, "_NoiseAmount"));
            props._Density = ParseFloat(ParseVar(subNode, "_Density"));
            props.material = ParseSubObjectMaterial(subNode.GetNode("Material"));
            return props;
        }
        public ScatterMaterial ParseSubObjectMaterial(ConfigNode subNode)
        {
            ScatterMaterial mat = ParseMaterial(subNode, true);
            
            
            return mat;
        }
        public string ParseVar(ConfigNode scatter, string valueName)
        {
            string data = "invalid";
            bool succeeded = scatter.TryGetValue(valueName, ref data);
            if (!succeeded)
            {
                ScatterLog.SubLog("[Exception] Unable to get the value of " + valueName);
                return null;
            }
            else
            {
                ScatterLog.SubLog("Parsed " + valueName + " as: " + data);
            }
            return data;
        }
        public Vector3 ParseVector(string data)
        {
            string cleanString = data.Replace(" ", string.Empty);
            string[] components = cleanString.Split(',');
            return new Vector3(float.Parse(components[0]), float.Parse(components[1]), float.Parse(components[2]));
        }
        public Vector2 ParseVector2D(string data)
        {
            string cleanString = data.Replace(" ", string.Empty);
            string[] components = cleanString.Split(',');
            return new Vector2(float.Parse(components[0]), float.Parse(components[1]));
        }
        public float ParseFloat(string data)
        {
            if (data == null)
            {
                ScatterLog.SubLog("Null value, returning 0");
                return 0;
            }
            return float.Parse(data);
        }
        public Color ParseColor(string data)
        {
            if (data == null)
            {
                ScatterLog.SubLog("Null value, returning 0");
            }
            string cleanString = data.Replace(" ", string.Empty);
            string[] components = cleanString.Split(',');
            return new Color(float.Parse(components[0]), float.Parse(components[1]), float.Parse(components[2]));
        }
    }
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class ScatterShaderHolder : MonoBehaviour
    {
        public static Dictionary<string, Shader> shaders = new Dictionary<string, Shader>();
        public static Dictionary<string, ComputeShader> computeShaders = new Dictionary<string, ComputeShader>();
        public void Awake()
        {
            string filePath = Path.Combine(KSPUtil.ApplicationRootPath + "GameData/" + "Parallax/Shaders/ScatterCompute");
            if (Application.platform == RuntimePlatform.LinuxPlayer || (Application.platform == RuntimePlatform.WindowsPlayer && SystemInfo.graphicsDeviceVersion.StartsWith("OpenGL")))
            {
                filePath = (filePath + "-linux.unity3d");
            }
            else if (Application.platform == RuntimePlatform.WindowsPlayer)
            {
                filePath = (filePath + "-windows.unity3d");
            }
            if (Application.platform == RuntimePlatform.OSXPlayer)
            {
                filePath = (filePath + "-macosx.unity3d");
            }
            var assetBundle = AssetBundle.LoadFromFile(filePath);
            Debug.Log("Loaded bundle");
            if (assetBundle == null)
            {
                Debug.Log("Failed to load bundle at");
                Debug.Log("Path: " + filePath);
            }
            else
            {
                ComputeShader[] theseComputeShaders = assetBundle.LoadAllAssets<ComputeShader>();
                Shader[] theseShaders = assetBundle.LoadAllAssets<Shader>();
                Debug.Log("Loaded all shaders");
                foreach (Shader thisShader in theseShaders)
                {
                    shaders.Add(thisShader.name, thisShader);
                    Debug.Log("Loaded shader: " + thisShader.name);
                }
                foreach (ComputeShader thisShader in theseComputeShaders)
                {
                    computeShaders.Add(thisShader.name, thisShader);
                    Debug.Log("Loaded compute shader: " + thisShader.name);
                }
            }
        }
        public static Shader GetShader(string name)
        {
            return shaders[name];
        }
        public static ComputeShader GetCompute(string name)
        {
            return computeShaders[name];
        }
    }

}
