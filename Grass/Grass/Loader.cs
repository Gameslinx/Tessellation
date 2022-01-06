using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ComputeLoader;

namespace ParallaxGrass
{
    public struct Properties
    {
        public Distribution scatterDistribution;
        public ScatterMaterial scatterMaterial;
        public Wind scatterWind;
    }
    public struct Distribution
    {
        public float _Range;                    //How far from the camera to render at the max graphics setting
        public float _PopulationMultiplier;     //How many scatters to render
        public float _SizeNoiseStrength;        //Strength of perlin noise - How varied the scatter size is
        public float _SizeNoiseScale;           //Size of perlin noise
        public Vector3 _SizeNoiseOffset;        //Offset the perlin noise
        public Vector3 _MinScale;               //Smallest scatter size
        public Vector3 _MaxScale;               //Largest scatter size
        public float _CutoffScale;              //Minimum scale at which, below that scale, the scatter is not placed
        public int updateRate;
    }
    public struct ScatterMaterial
    {
        public Color _MainColor;
        public Color _SubColor;
        public float _ColorNoiseStrength;
        public float _ColorNoiseScale;
    }
    public struct Wind
    {
        public Vector3 _WindSpeed;
        public float _WaveSpeed;
        public float _WaveAmp;
        public float _HeightCutoff;
        public float _HeightFactor;
    }
    public static class ScatterBodies
    {
        public static Dictionary<string, ScatterBody> scatterBodies = new Dictionary<string, ScatterBody>();
    }
    public class ScatterBody
    {
        public Dictionary<string, Scatter> scatters = new Dictionary<string, Scatter>();
        public string bodyName = "invalidname";
        public ScatterBody(string name)
        {
            bodyName = name;
        }
    }
    public class Scatter
    {
        public string scatterName = "invalidname";
        public Properties properties;
        public Scatter(string name)
        {
            scatterName = name;
        }
        public void ForceComputeUpdate()
        {
            ScreenMessages.PostScreenMessage("WARNING: Forcing a compute update is not recommended and should not be called in realtime!");
            ComputeComponent[] allComputeComponents = UnityEngine.Resources.FindObjectsOfTypeAll(typeof(ComputeComponent)) as ComputeComponent[];
            foreach (ComputeComponent comp in allComputeComponents)
            {
                if (comp.gameObject.activeSelf)
                {
                    Debug.Log("Found ComputeComponent: " + comp.name);
                    if (comp == null)
                    {
                        Debug.Log("Component is null??");
                    }
                    comp.properties = ScatterBodies.scatterBodies[FlightGlobals.currentMainBody.name].scatters["Grass"].properties;
                    comp.updateFPS = ScatterBodies.scatterBodies[FlightGlobals.currentMainBody.name].scatters["Grass"].properties.scatterDistribution.updateRate;
                    comp.EvaluatePositions();
                }
            }
        }
    }
    public static class ScatterLog
    {
        public static void Log(string message)
        {
            Debug.Log("[ParallaxScatter] " + message);
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
                ScatterBody body = new ScatterBody(bodyName);
                ScatterBodies.scatterBodies.Add(bodyName, body);
                ScatterLog.Log("Parsing " + bodyName);
                for (int b = 0; b < globalNodes[i].config.nodes.Count; b++)
                {
                    ConfigNode rootNode = globalNodes[i].config;
                    ConfigNode scatterNode = rootNode.nodes[b];
                    Debug.Log("Scatter node: " + scatterNode.name);
                    ConfigNode distributionNode = scatterNode.GetNode("Distribution");
                    ConfigNode materialNode = scatterNode.GetNode("Material");
                    ConfigNode windNode = scatterNode.GetNode("Wind");

                    //ConfigNode windNode = globalNodes[i].config.nodes[b].GetNode("Distribution");

                    ParseNewBody(scatterNode, distributionNode, materialNode, windNode, bodyName);
                }
            }
        }
        public void ParseNewBody(ConfigNode scatterNode, ConfigNode distributionNode, ConfigNode materialNode, ConfigNode windNode, string bodyName)
        {
            ScatterBody body = ScatterBodies.scatterBodies[bodyName];   //Bodies contain multiple scatters
            string scatterName = scatterNode.GetValue("name");
            Scatter scatter = new Scatter(scatterName);
            Properties props = new Properties();
            props.scatterDistribution = ParseDistribution(distributionNode);
            props.scatterMaterial = ParseMaterial(materialNode);
            props.scatterWind = ParseWind(windNode);
            scatter.properties = props;

            body.scatters.Add(scatterName, scatter);
        }
        public Distribution ParseDistribution(ConfigNode distributionNode)
        {
            Distribution distribution = new Distribution();

            distribution._Range = ParseFloat(ParseVar(distributionNode, "_Range"));
            distribution._PopulationMultiplier = ParseFloat(ParseVar(distributionNode, "_PopulationMultiplier"));
            distribution._SizeNoiseStrength = ParseFloat(ParseVar(distributionNode, "_SizeNoiseStrength"));
            distribution._SizeNoiseScale = ParseFloat(ParseVar(distributionNode, "_SizeNoiseScale"));
            distribution._CutoffScale = ParseFloat(ParseVar(distributionNode, "_CutoffScale"));

            distribution._SizeNoiseOffset = ParseVector(ParseVar(distributionNode, "_SizeNoiseOffset"));
            distribution._MinScale = ParseVector(ParseVar(distributionNode, "_MinScale"));
            distribution._MaxScale = ParseVector(ParseVar(distributionNode, "_MaxScale"));

            return distribution;
        }
        public ScatterMaterial ParseMaterial(ConfigNode materialNode)
        {
            ScatterMaterial material = new ScatterMaterial();

            material._MainColor = ParseColor(ParseVar(materialNode, "_MainColor"));
            material._SubColor = ParseColor(ParseVar(materialNode, "_SubColor"));

            material._ColorNoiseScale = ParseFloat(ParseVar(materialNode, "_ColorNoiseScale"));
            material._ColorNoiseStrength = ParseFloat(ParseVar(materialNode, "_ColorNoiseStrength"));

            return material;
        }
        public Wind ParseWind(ConfigNode windNode)
        {
            Wind material = new Wind();

            material._WindSpeed = ParseVector(ParseVar(windNode, "_WindSpeed"));
            material._WaveSpeed = ParseFloat(ParseVar(windNode, "_WaveSpeed"));
            material._WaveAmp = ParseFloat(ParseVar(windNode, "_WaveAmp"));

            material._HeightCutoff = ParseFloat(ParseVar(windNode, "_HeightCutoff"));
            material._HeightFactor = ParseFloat(ParseVar(windNode, "_HeightFactor"));

            return material;
        }
        public string ParseVar(ConfigNode scatter, string valueName)
        {
            string data = "invalid";
            bool succeeded = scatter.TryGetValue(valueName, ref data);
            if (!succeeded)
            {
                ScatterLog.Log("[Exception] Unable to get the value of " + valueName);
            }
            else
            {
                ScatterLog.Log("Parsed " + valueName + " as: " + data);
            }
            return data;
        }
        public Vector3 ParseVector(string data)
        {
            string cleanString = data.Replace(" ", string.Empty);
            string[] components = cleanString.Split(',');
            return new Vector3(float.Parse(components[0]), float.Parse(components[1]), float.Parse(components[2]));
        }
        public float ParseFloat(string data)
        {
            return float.Parse(data);
        }
        public Color ParseColor(string data)
        {
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
