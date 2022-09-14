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
        public float maxCount;
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

        public string useNoiseProfile;

        public float _PlacementAltitude;        //For fixedalt scatters this is the altitude stuff spawns at
    }
    public struct Distribution
    {
        public DistributionNoise noise;
        public LODs lods;
        public BiomeBlacklist blacklist;
        public float _Range;                    //How far from the camera to render at the max graphics setting
        public float _SqrRange;                 //Use for fast range calculation in distance checks CPU side
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
        public float _MinAltitude;
        public float _MaxAltitude;
        public float _Seed;
        public float _AltitudeFadeRange;        //Fade out a scatter over a vertical distance according to size noise. Reduces harshness of a sudden cutoff
        public float _RotationMult;             //Max amount of rotation applied to an object, from 0 to 1
    }
    public struct LODs
    {
        public LOD[] lods;
        public int LODCount;
    }
    public struct BiomeBlacklist
    {
        public string[] biomes;
        public Dictionary<string, string> fastBiomes;   //When wanting to request a biome name but unsure if it is contained, use the much faster dictionary
    }
    public struct LOD
    {
        public float range;
        public string modelName;
        public string mainTexName;
        public string normalName;
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
        public int minLevel;
    }
    public enum DistributionNoiseMode
    {
        Persistent,
        NonPersistent,
        VerticalStack,
        FixedAltitude
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
        public bool isVisible = true;
        public bool useSurfacePos = false;
        public float cullingRange = 0;
        public float cullingLimit = -15;
        public bool shared = false;
        public string sharedParent;
        public int maxObjects = 10000;  //Large memory impact. 10k is a middle ground number
        public bool collideable = false;
        public bool alwaysCollideable = false;
        public string collisionMesh;
        
        public Scatter(string name)
        {
            scatterName = name;
        }
        public IEnumerator ForceComputeUpdate()
        {
            ScatterComponent pqsMod = ScatterManagerPlus.scatterComponents[planetName].Find(x => x.scatter == this);
            pqsMod.scatterQueue.Clear();
            ScreenMessages.PostScreenMessage("WARNING: Forcing a compute update is not recommended and should not be called in realtime!");
            foreach (KeyValuePair<PQ, QuadData> data in PQSMod_ParallaxScatter.quadList)
            {
                foreach (KeyValuePair<Scatter, ScatterCompute> scatter in data.Value.comps)
                {
                    if (scatter.Value.scatter.scatterName == scatterName)
                    {
                        scatter.Value.updateTime = false;
                        scatter.Value.Start();
                        scatter.Value.updateTime = true;
                    }
                }
            }
            pqsMod.scatter = this;
            yield return null;
        }
        public IEnumerator SwitchQuadMaterial(Material mat, bool revert, Scatter scatter)
        {

            QuadData[] allData = PQSMod_ParallaxScatter.quadList.Values.ToArray();
            int counter = 0;
            foreach (QuadData comp in allData)
            {
                counter++;
                if (!revert)
                {
                    if (!comp.comps.ContainsKey(scatter))
                    {
                        continue;
                    }
                    PQ quad = comp.quad;
                    GameObject go = new GameObject();
                    go.name = quad.name + "-INST";
                    go.transform.position = quad.transform.position;
                    go.transform.rotation = quad.transform.rotation;
                    go.transform.localScale = quad.transform.localScale;
                    var newQMF = go.AddComponent<MeshFilter>();
                    newQMF.mesh = GameObject.Instantiate(quad.mesh);
                    float[] data = PQSMod_ScatterDistribute.scatterData.distributionData[scatter.scatterName].data[quad].data;
                    Color[] colors = new Color[data.Length];
                    Color32[] colors32 = new Color32[data.Length];
                    Color[] meshColors = quad.mesh.colors;
                    for (int c = 0; c < meshColors.Length; c++)
                    {
                        if (data != null)
                        {
                            colors[c] = new Color(data[c], data[c], data[c]);
                            colors32[c] = new Color(data[c], data[c], data[c]);
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
                    go.transform.position += Vector3.Normalize(GlobalPoint.originPoint - FlightGlobals.currentMainBody.transform.position) * 0.1f;
                    go.SetActive(true);
                }
                if (revert)
                {
                    Debug.Log("Reverting");
                    PQ quad = comp.quad;
                    GameObject go = GameObject.Find(quad.name + "-INST");
                    if (go != null)
                    {
                        GameObject.Destroy(go);
                    }
                }
                if (counter % 50 == 0)
                {
                    yield return null;
                }

            }
        }
        public IEnumerator ForceMaterialUpdate(Scatter scatter)
        {
            PostCompute[] pcs = UnityEngine.Resources.FindObjectsOfTypeAll(typeof(PostCompute)) as PostCompute[];
            Debug.Log("Found " + pcs.Length + " PostCompute components");
            foreach (PostCompute pc in pcs)
            {
                if (pc.scatterName != null && pc.scatterName == scatter.scatterName)
                {
                    if (pc != null)
                    {
                        pc.scatterProps = properties;
                        pc.SetupAgain(scatter);
                    }
                }
            }
            
            yield return null;
        }
        public void ModifyScatterVisibility()
        {
            List<ScatterComponent> scs = ScatterManagerPlus.scatterComponents[FlightGlobals.currentMainBody.name];
            foreach (ScatterComponent sc in scs)
            {
                if (sc.scatter.scatterName == scatterName)
                {
                    PostCompute pc = sc.pc;
                    pc.active = !pc.active;
                }
                
            }
        }
        public int GetGlobalVertexCount(Scatter currentScatter)
        {
            return 0;
        }
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
    public class ScatterGlobalSettings
    {
        public static bool enableScatters = true;

        public static float densityMult = 1;
        public static float rangeMult = 1;
        public static bool frustumCull = true;
        public static float updateMult = 1;
        public static float lodRangeMult = 1;

        public static float scatterTextureMult = 1.0f;
        public static int maxTextureRes = 8192;

        public static bool enableCollisions = false;
        public static bool onlyQueryControllable = true;

        public static bool castShadows = true;
    }
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class ScatterLoader : MonoBehaviour
    {
        public UrlDir.UrlConfig[] globalNodes;
        public UrlDir.UrlConfig settingsNode;
        public static ScatterLoader Instance;
        float subdivisionRangeRestraint = 0;    //Collideables MUST share the same range
        void Awake()
        {
            GameObject.DontDestroyOnLoad(this);
            Instance = this;
        }
        public void Start()
        {
            globalNodes = GameDatabase.Instance.GetConfigs("ParallaxScatters");
            settingsNode = GameDatabase.Instance.GetConfigs("ParallaxGlobalConfig").First();
            LoadGlobalSettings();
            if (!ScatterGlobalSettings.enableScatters) { return; }
            LoadScatterNodes();
        }
        public void LoadGlobalSettings()
        {
            ConfigNode scatterNode = settingsNode.config.GetNode("ScatterSettings");
            ScatterGlobalSettings.enableScatters = bool.Parse(scatterNode.GetValue("enableScatters"));

            ScatterGlobalSettings.densityMult = ParseFloat(ParseVar(scatterNode, "densityMultiplier", "1"));
            ScatterGlobalSettings.rangeMult = ParseFloat(ParseVar(scatterNode, "rangeMultiplier", "1"));
            ScatterGlobalSettings.updateMult = ParseFloat(ParseVar(scatterNode, "computeShaderUpdateMultiplier", "1"));
            ScatterGlobalSettings.lodRangeMult = ParseFloat(ParseVar(scatterNode, "lodRangeMultiplier", "1"));
            ScatterGlobalSettings.frustumCull = bool.Parse(ParseVar(scatterNode, "frustumCulling", "true"));

            ConfigNode textureNode = settingsNode.config.GetNode("TextureSettings");
            ScatterGlobalSettings.scatterTextureMult = ParseFloat(ParseVar(textureNode, "scatterTextureQualityMultiplier", "1"));
            ScatterGlobalSettings.maxTextureRes = (int)ParseFloat(ParseVar(textureNode, "maxTextureResolution", "8192"));

            ConfigNode collisionsNode = settingsNode.config.GetNode("CollisionSettings");
            ScatterGlobalSettings.enableCollisions = bool.Parse(ParseVar(collisionsNode, "enableCollisions", "false"));

            ScatterGlobalSettings.castShadows = bool.Parse(ParseVar(scatterNode, "castShadows", "true"));

            InstallNotifs.PostSettings();
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
                subdivisionRangeRestraint = 0;
                for (int b = 0; b < globalNodes[i].config.nodes.Count; b++)
                {
                    ConfigNode rootNode = globalNodes[i].config;
                    ConfigNode scatterNode = rootNode.nodes[b];
                    string parentName = scatterNode.GetValue("parent");
                    if (parentName != null)
                    {
                        ConfigNode materialNode = scatterNode.GetNode("Material");
                        ConfigNode distributionNode = scatterNode.GetNode("Distribution");
                        ParseSharedScatter(parentName, scatterNode, distributionNode, materialNode, bodyName);    //Shares its distribution data with another scatter
                    }
                    else
                    {
                        ConfigNode distributionNode = scatterNode.GetNode("Distribution");
                        ConfigNode materialNode = scatterNode.GetNode("Material");
                        ConfigNode subdivisionSettingsNode = scatterNode.GetNode("SubdivisionSettings");
                        ConfigNode subObjectNode = scatterNode.GetNode("SubObjects");
                        ConfigNode distributionNoiseNode = scatterNode.GetNode("DistributionNoise");

                        ParseNewScatter(scatterNode, distributionNoiseNode, distributionNode, materialNode, subdivisionSettingsNode, subObjectNode, bodyName);
                    }
                }
                
            }
        }
        public void LoadNewScatter(string type)
        {
            if (!ScatterBodies.scatterBodies.ContainsKey(FlightGlobals.currentMainBody.name))
            {
                ScatterBodies.scatterBodies.Add(FlightGlobals.currentMainBody.name, new ScatterBody(FlightGlobals.currentMainBody.name, FlightGlobals.currentMainBody.pqsController.maxLevel.ToString()));
            }

            UrlDir.UrlConfig[] uiNode = GameDatabase.Instance.GetConfigs("ParallaxUIDefault");
            for (int b = 0; b < uiNode[0].config.nodes.Count; b++)
            {
                ConfigNode rootNode = uiNode[0].config;
                ConfigNode scatterNode = rootNode.nodes[b];
                string name = scatterNode.GetValue("name");
                Debug.Log("Parsing: " + name);
                if (name == type)
                {
                    ConfigNode distributionNode = scatterNode.GetNode("Distribution");
                    ConfigNode materialNode = scatterNode.GetNode("Material");
                    ConfigNode subdivisionSettingsNode = scatterNode.GetNode("SubdivisionSettings");
                    ConfigNode subObjectNode = scatterNode.GetNode("SubObjects");
                    ConfigNode distributionNoiseNode = scatterNode.GetNode("DistributionNoise");

                    ParseNewScatter(scatterNode, distributionNoiseNode, distributionNode, materialNode, subdivisionSettingsNode, subObjectNode, FlightGlobals.currentMainBody.name);
                }
            }
        }
        public void ParseNewScatter(ConfigNode scatterNode, ConfigNode distributionNoiseNode, ConfigNode distributionNode, ConfigNode materialNode, ConfigNode subdivisionSettingsNode, ConfigNode subObjectNode, string bodyName)
        {
            
            ScatterBody body = ScatterBodies.scatterBodies[bodyName];   //Bodies contain multiple scatters
            string scatterName = bodyName + "-" + scatterNode.GetValue("name");

            string repeatedName = scatterName;
            int repeatedCount = 1;
            while (body.scatters.ContainsKey(repeatedName))  //Just for the UI adding a new scatter to avoid adding duplicate
            {
                repeatedName = scatterName + repeatedCount.ToString();
                repeatedCount++;
            }
            scatterName = repeatedName;
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
            string maxObjects = "";
            bool maxObjectsCheck = scatterNode.TryGetValue("maxObjects", ref maxObjects);
            if (maxObjectsCheck) { scatter.maxObjects = int.Parse(maxObjects); }
            string hasCollider = "";
            bool colliderCheck = ScatterGlobalSettings.enableCollisions ? scatterNode.TryGetValue("collideable", ref hasCollider) : false;
            if (colliderCheck) { scatter.collideable = bool.Parse(hasCollider); }
            string hasAlwaysCollideable = "";
            bool alwaysCollideableCheck = ScatterGlobalSettings.enableCollisions ? scatterNode.TryGetValue("alwaysCollideable", ref hasAlwaysCollideable) : false;
            if (alwaysCollideableCheck) { scatter.alwaysCollideable = bool.Parse(hasAlwaysCollideable); }
            string hasCollisionMesh = "";
            bool collisionMeshCheck = scatterNode.TryGetValue("collisionMesh", ref hasCollisionMesh);
            if (collisionMeshCheck) { scatter.collisionMesh = hasCollisionMesh; }

            props.scatterDistribution = ParseDistribution(distributionNode);
            props.scatterDistribution.noise = ParseDistributionNoise(distributionNoiseNode, bodyName);
            props.scatterMaterial = ParseMaterial(materialNode, false);
            props.subdivisionSettings = ParseSubdivisionSettings(subdivisionSettingsNode, body);
            props.memoryMultiplier = 100;
            scatter.properties = props;
            scatter.updateFPS = ParseFloat(ParseVar(scatterNode, "updateFPS", "30"));
            body.scatters.Add(scatterName, scatter);

            if (subdivisionRangeRestraint == 0 && scatter.collideable) { subdivisionRangeRestraint = props.subdivisionSettings.range; }


        }
        public void ParseSharedScatter(string parentName, ConfigNode scatterNode, ConfigNode distributionNode, ConfigNode materialNode, string bodyName)
        {
            ScatterBody body = ScatterBodies.scatterBodies[bodyName];   //Bodies contain multiple scatters
            string scatterName = bodyName + "-" + scatterNode.GetValue("name");
            ScatterLog.Log("Parsing shared scatter: " + scatterName);
            Scatter scatter = new Scatter(scatterName);
            scatter.shared = true;
            scatter.sharedParent = parentName;
            scatter.planetName = bodyName;
            Properties props = new Properties();
            scatter.model = scatterNode.GetValue("model");

            string forcedFull = "";
            bool forcedFullShadows = scatterNode.TryGetValue("shadowMode", ref forcedFull);
            if (forcedFullShadows && forcedFull == "forcedFull") { scatter.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }

            props.scatterMaterial = ParseMaterial(materialNode, false);
            props.memoryMultiplier = 100;
            Distribution distribution = new Distribution();
            ConfigNode lodNode = distributionNode.GetNode("LODs");
            distribution.lods = ParseLODs(lodNode);
            distribution._Range = body.scatters[bodyName + "-" + parentName].properties.scatterDistribution._Range;
            distribution._SqrRange = distribution._Range * distribution._Range;
            props.scatterDistribution = distribution;
            scatter.properties = props;
            body.scatters.Add(scatterName, scatter);
        }
        public DistributionNoise ParseDistributionNoise(ConfigNode distributionNode, string bodyName)
        {
            DistributionNoise distribution = new DistributionNoise();

            string noiseMode = ParseVar(distributionNode, "mode", "Persistent");
            if (noiseMode.ToLower() == "nonpersistent") { distribution.noiseMode = DistributionNoiseMode.NonPersistent; }
            else if (noiseMode.ToLower() == "verticalstack") { distribution.noiseMode = DistributionNoiseMode.VerticalStack; }
            else if (noiseMode.ToLower() == "fixedaltitude") { distribution.noiseMode = DistributionNoiseMode.FixedAltitude; }
            else { distribution.noiseMode = DistributionNoiseMode.Persistent; }

            distribution.useNoiseProfile = (ParseVar(distributionNode, "useNoiseProfile", null));
            if (distribution.useNoiseProfile != null) { distribution.useNoiseProfile = bodyName + "-" + distribution.useNoiseProfile; }
            if (distribution.useNoiseProfile != null) { ScatterLog.SubLog("Using noise profile: " + distribution.useNoiseProfile); }
            if (distribution.useNoiseProfile != null && (distribution.noiseMode == DistributionNoiseMode.NonPersistent)) { ScatterLog.SubLog("[Exception] Attempting to use a noise profile for a non-persistent scatter. This only works if you want to share the same noise as another persistent scatter!"); }
            if (distribution.useNoiseProfile != null) { distribution._Frequency = 1; distribution._Lacunarity = 1; distribution._Persistence = 1; distribution._Octaves = 1; distribution._Seed = 1; distribution._NoiseType = 1; distribution._NoiseQuality = LibNoise.NoiseQuality.Low; distribution._MaxStacks = 1; distribution._StackSeparation = 1;  
                                                        return distribution; }

            
            distribution._MaxStacks = 1;
            distribution._StackSeparation = 1;

            if (distribution.noiseMode == DistributionNoiseMode.Persistent || distribution.noiseMode == DistributionNoiseMode.VerticalStack || distribution.noiseMode == DistributionNoiseMode.FixedAltitude)
            {
                distribution._Frequency = ParseFloat(ParseVar(distributionNode, "_Frequency", "100"));
                distribution._Lacunarity = ParseFloat(ParseVar(distributionNode, "_Lacunarity", "4"));
                distribution._Persistence = ParseFloat(ParseVar(distributionNode, "_Persistence", "0.5"));
                distribution._Octaves = ParseFloat(ParseVar(distributionNode, "_Octaves", "4"));
                distribution._Seed = (int)ParseFloat(ParseVar(distributionNode, "_Seed", "69420")); //Very mature
                

                string noiseType = ParseVar(distributionNode, "_NoiseType", "1");
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
                string noiseQuality = ParseVar(distributionNode, "_NoiseQuality", "Low");
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
                    distribution._MaxStacks = (int)ParseFloat(ParseVar(distributionNode, "_MaxStacks", "1"));
                    distribution._StackSeparation = (int)ParseFloat(ParseVar(distributionNode, "_StackSeparation", "10"));
                }
                if (distribution.noiseMode == DistributionNoiseMode.FixedAltitude)
                {
                    distribution._PlacementAltitude = (int)ParseFloat(ParseVar(distributionNode, "_PlacementAltitude", "0"));
                }
            }
            else
            {
                distribution._SizeNoiseScale = ParseFloat(ParseVar(distributionNode, "_SizeNoiseScale", "4"));
                distribution._ColorNoiseScale = ParseFloat(ParseVar(distributionNode, "_ColorNoiseScale", "4"));
                distribution._SizeNoiseOffset = ParseFloat(ParseVar(distributionNode, "_SizeNoiseOffset", "0"));
            }

            return distribution;
        }
        public Distribution ParseDistribution(ConfigNode distributionNode)
        {
            Distribution distribution = new Distribution();
            
            distribution._Range = ParseFloat(ParseVar(distributionNode, "_Range", "1000")) * ScatterGlobalSettings.rangeMult;
            distribution._SqrRange = distribution._Range * distribution._Range;
            distribution._RangePow = ParseFloat(ParseVar(distributionNode, "_RangePow", "1000"));
            distribution._PopulationMultiplier = ParseFloat(ParseVar(distributionNode, "_PopulationMultiplier", "1")) * ScatterGlobalSettings.densityMult;
            distribution._SizeNoiseStrength = ParseFloat(ParseVar(distributionNode, "_SizeNoiseStrength", "1"));
            distribution._CutoffScale = ParseFloat(ParseVar(distributionNode, "_CutoffScale", "0"));
            distribution._SteepPower = ParseFloat(ParseVar(distributionNode, "_SteepPower", "1"));
            distribution._SteepContrast = ParseFloat(ParseVar(distributionNode, "_SteepContrast", "1"));
            distribution._SteepMidpoint = ParseFloat(ParseVar(distributionNode, "_SteepMidpoint", "0.5"));
            distribution._MaxNormalDeviance = ParseFloat(ParseVar(distributionNode, "_NormalDeviance", "1"));
            distribution._MinScale = ParseVector(ParseVar(distributionNode, "_MinScale", "1,1,1"));
            distribution._MaxScale = ParseVector(ParseVar(distributionNode, "_MaxScale", "1,1,1"));
            distribution._MinAltitude = ParseFloat(ParseVar(distributionNode, "_MinAltitude", "0"));
            distribution._MaxAltitude = ParseFloat(ParseVar(distributionNode, "_MaxAltitude", "1000000"));
            distribution._SpawnChance = ParseFloat(ParseVar(distributionNode, "_SpawnChance", "1"));
            distribution._Seed = ParseFloat(ParseVar(distributionNode, "_Seed", "69"));
            distribution._AltitudeFadeRange = ParseFloat(ParseVar(distributionNode, "_AltitudeFadeRange", "5"));

            string rotCheck = "1.0";
            distributionNode.TryGetValue("_RotationMultiplier", ref rotCheck);
            distribution._RotationMult = float.Parse(rotCheck);

            if ((int)(distribution._PopulationMultiplier) == 0) { distribution._PopulationMultiplier = 1; }
            ConfigNode lodNode = distributionNode.GetNode("LODs");
            ConfigNode biomeBlacklist = null;  //optional
            bool hasBlacklist = distributionNode.TryGetNode("BiomeBlacklist", ref biomeBlacklist);
            distribution.blacklist = ParseBlacklist(biomeBlacklist, hasBlacklist);
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
                
                lod.modelName = ParseVar(lodNodes[i], "model", "[Exception] No model defined in the scatter config");
                lod.mainTexName = ParseVar(lodNodes[i], "_MainTex", "parent");
                string normalName = "parent";
                bool hasNormal = lodNodes[i].TryGetValue("_BumpMap", ref normalName);
                if (!hasNormal) { normalName = "parent"; }
                lod.normalName = normalName;
                if (lodNodes[i].HasValue("billboard") && lodNodes[i].GetValue("billboard").ToLower() == "true") { lod.isBillboard = true; Debug.Log("has billboard"); }
                else { lod.isBillboard = false; }

                if (lodNodes[i].HasValue("range")) { lod.range = ParseFloat(ParseVar(lodNodes[i], "range", "5")) * ScatterGlobalSettings.lodRangeMult; }

                lods.lods[i] = lod;
                //Parse models on main menu after they have loaded
            }
            return lods;
        }
        public BiomeBlacklist ParseBlacklist(ConfigNode node, bool hasBlacklist)
        {
            if (!hasBlacklist)
            {
                BiomeBlacklist emptyList = new BiomeBlacklist();
                emptyList.fastBiomes = new Dictionary<string, string>();
                emptyList.biomes = new string[0];
                return emptyList;
            }
            BiomeBlacklist blacklist = new BiomeBlacklist();
            string[] values = node.GetValues("name");
            blacklist.biomes = values;
            blacklist.fastBiomes = new Dictionary<string, string>();
            for (int i = 0; i < values.Length; i++)
            {
                blacklist.fastBiomes.Add(values[i], values[i]);
            }
            return blacklist;
        }
        public ScatterMaterial GetShaderVars(string shaderName, ScatterMaterial material, ConfigNode materialNode)
        {
            UrlDir.UrlConfig[] nodes = GameDatabase.Instance.GetConfigs("ScatterShader");
            for (int i = 0; i < nodes.Length; i++)
            {
                string configShaderName = nodes[i].config.GetValue("name");
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
                    material.Textures.Add(values[i], null);
                }
            }
            else if (type == typeof(float))
            {
                material.Floats = new Dictionary<string, float>();
                for (int i = 0; i < values.Length; i++)
                {
                    material.Floats.Add(values[i], 0);
                }
            }
            else if (type == typeof(Vector3))
            {
                material.Vectors = new Dictionary<string, Vector3>();
                for (int i = 0; i < values.Length; i++)
                {
                    material.Vectors.Add(values[i], Vector3.zero);
                }
            }
            else if (type == typeof(Vector2))
            {
                material.Scales = new Dictionary<string, Vector2>();
                for (int i = 0; i < values.Length; i++)
                {
                    material.Scales.Add(values[i], Vector2.zero);
                }
            }
            else if (type == typeof(Color))
            {
                material.Colors = new Dictionary<string, Color>();
                for (int i = 0; i < values.Length; i++)
                {
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

            material.shader = ScatterShaderHolder.GetShader(ParseVar(materialNode, "shader", "Custom/ParallaxInstanced"));

            material = GetShaderVars(material.shader.name, material, materialNode);
            if (!isSubObject)
            {
                material._MainColor = ParseColor(ParseVar(materialNode, "_MainColor", "1,1,1,1"));
                material._SubColor = ParseColor(ParseVar(materialNode, "_SubColor", "1,1,1,1"));

                
                material._ColorNoiseStrength = ParseFloat(ParseVar(materialNode, "_ColorNoiseStrength", "1"));
            }

            return material;
        }
        public SubdivisionProperties ParseSubdivisionSettings(ConfigNode subdivNode, ScatterBody body)
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

            props.level = (int)ParseFloat(ParseVar(subdivNode, "subdivisionLevel", "1"));
            props.range = ParseFloat(ParseVar(subdivNode, "subdivisionRange", "1000"));
            
            string minLevel = "";
            bool hasMinLevel = subdivNode.TryGetValue("minimumSubdivision", ref minLevel);
            props.minLevel = hasMinLevel ? (int)ParseFloat(minLevel) : body.minimumSubdivision;

            return props;
        }
       
        public string ParseVar(ConfigNode scatter, string valueName, string fallback)
        {
            string data = null;
            bool succeeded = scatter.TryGetValue(valueName, ref data);
            if (!succeeded)
            {
                ScatterLog.SubLog("[Warning] Unable to get the value of " + valueName + ", it has been set to " + fallback);
                return fallback;
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
