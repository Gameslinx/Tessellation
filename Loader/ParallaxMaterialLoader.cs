using System.IO;
using System;
using UnityEngine;
using System.Linq;
using System.Security.Principal;
using System.Collections.Generic;
using Contracts.Predicates;
using Smooth.Collections;

namespace ParallaxShader
{
    [KSPAddon(KSPAddon.Startup.Instantly, false)]
    public class ParallaxLoader : MonoBehaviour
    {
        public static Dictionary<string, Shader> shaders = new Dictionary<string, Shader>();
        public void Awake()
        {
            //Locate and load the shaders. Then stored in Kopernicus shader dictionary
            //ShaderLoader.LoadAssetBundle("Terrain/Shaders", "ParallaxOcclusion");
            //var assetBundle = AssetBundle.LoadFromFile(
            string filePath = Path.Combine(KSPUtil.ApplicationRootPath + "GameData/" + "BeyondHome/Shaders/ParallaxOcclusion");

            if (Application.platform == RuntimePlatform.WindowsPlayer)
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
                Debug.Log("Failed to load bundle");
                Debug.Log(filePath);
            }
            else
            {
                Shader[] theseShaders = assetBundle.LoadAllAssets<Shader>();
                Debug.Log("Loaded all shaders");
                foreach (Shader thisShader in theseShaders)
                {
                    shaders.Add(thisShader.name, thisShader);
                    Debug.Log("Loaded shader: " + thisShader.name);
                }



            }
        }
        public static Shader GetShader(string name)
        {
            return shaders[name];
        }
    }

    [KSPAddon(KSPAddon.Startup.FlightAndKSC, false)]
    [DefaultExecutionOrder(900)]
    public class Position : MonoBehaviour
    {
        public void Start()
        {
            foreach (CelestialBody body in FlightGlobals.Bodies)
            {
                Debug.Log("PQS for: " + body.name);
                foreach (PQSMod mod in body.GetComponentsInChildren<PQSMod>())
                {
                    Debug.Log("PQS: " + mod.ToString());
                    Debug.Log("Name: " + mod.name + "\n");
                }
            }
        }
        public void LateUpdate()
        {
            CelestialBody body = FlightGlobals.currentMainBody;
            body.pqsController.surfaceMaterial.SetVector("_PlanetOrigin", (Vector3)body.transform.localPosition);
            body.pqsController.highQualitySurfaceMaterial.SetVector("_PlanetOrigin", (Vector3)body.transform.localPosition);
            body.pqsController.mediumQualitySurfaceMaterial.SetVector("_PlanetOrigin", (Vector3)body.transform.localPosition);
            body.pqsController.lowQualitySurfaceMaterial.SetVector("_PlanetOrigin", (Vector3)body.transform.localPosition);

            body.pqsController.lowQualitySurfaceMaterial.SetVector("_LightPos", (Vector3)(FlightGlobals.Bodies[0].transform.position));
            body.pqsController.highQualitySurfaceMaterial.SetVector("_LightPos", (Vector3)(FlightGlobals.Bodies[0].transform.position));
            body.pqsController.mediumQualitySurfaceMaterial.SetVector("_LightPos", (Vector3)(FlightGlobals.Bodies[0].transform.position));
            body.pqsController.surfaceMaterial.SetVector("_LightPos", (Vector3)(FlightGlobals.Bodies[0].transform.position));



            if (FlightGlobals.ActiveVessel != null)
            {
                Debug.Log("Planet pos: " + body.transform.localPosition);
                Debug.Log("Vessel pos: " + FlightGlobals.ActiveVessel.transform.position);
                if (HighLogic.LoadedScene == GameScenes.FLIGHT)
                {
                    Vector3d vec = FlightGlobals.GetHomeBody().position;
                    Vector3 noPres = new Vector3((float)(vec.x), (float)(vec.y), (float)(vec.z));
                    Debug.Log(noPres.ToString("f10"));
                    body.pqsController.surfaceMaterial.SetVector("_VesselOrigin", new Vector3((float)noPres.x, (float)noPres.y, (float)noPres.z));
                }
                

            }


            //FlightGlobals.GetHomeBody().pqsController.ultraQualitySurfaceMaterial.SetVector("_PlanetOrigin", (Vector3)FlightGlobals.GetHomeBody().transform.position);
        }
        public double Clamp(double value)
        {
            double position = value / 2048;
            string posString = "0." + position.ToString().Split('.')[1];
            position = double.Parse(posString);
            value = position * 2048;
            value = value / 2048;
            return value;
        }
    }
    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    class TerrainQualitySetter : MonoBehaviour
    {
        public void Start()
        {
            GameSettings.TERRAIN_SHADER_QUALITY--;
        }
    }
    [KSPAddon(KSPAddon.Startup.PSystemSpawn, false)]
    class ParallaxShaderLoader : MonoBehaviour
    {
        public static Dictionary<string, ParallaxBody> parallaxBodies = new Dictionary<string, ParallaxBody>();

        public void Start()
        {
            Log("Starting...");
            GetConfigNodes();
            ActivateConfigNodes();
            GameSettings.TERRAIN_SHADER_QUALITY = 3;
        }
        //public void GetConfigNodes()
        //{
        //    for (int i = 0; i < 1; i++)
        //    {
        //        Debug.Log("This parallax node has: " + 5 + " nodes");
        //        for (int b = 0; b < 5; b++)
        //        {
        //            ParallaxBody a = new ParallaxBody();
        //            string name = b.ToString();
        //            a.Body = name;
        //            parallaxBodies.Add(name, a);
        //
        //        }
        //    }
        //    foreach (KeyValuePair<string, ParallaxBody> body in parallaxBodies)
        //    {
        //        Debug.Log("Value: " + body.Value.Body);
        //        Debug.Log("Key: " + body.Key);
        //    }
        //}
        public void GetConfigNodes()
        {
            UrlDir.UrlConfig[] nodeArray = GameDatabase.Instance.GetConfigs("Parallax");
            Log("Retrieving config nodes...");
            for (int i = 0; i < nodeArray.Length; i++)
            {
                Debug.Log("This parallax node has: " + nodeArray[i].config.nodes.Count + " nodes");
                for (int b = 0; b < nodeArray[i].config.nodes.Count; b++)
                {
                    Debug.Log("b is " + b);
                    Log("Debug: " + nodeArray[i].config.nodes[b].name);
                    ConfigNode parallax = nodeArray[i].config;
                    string bodyName = parallax.nodes[b].GetValue("name");
                    Log("////////////////////////////////////////////////");
                    Log("////////////////" + bodyName + "////////////////");
                    Log("////////////////////////////////////////////////\n");
                    ConfigNode parallaxBody = parallax.nodes[b].GetNode("Textures");
                    if (parallaxBody == null)
                    {
                        Log(" - Parallax Body is null! Cancelling load");
                        return;
                    }
                    Log(" - Retrieved body node");
                    ParallaxBody thisBody = new ParallaxBody();
                    thisBody.Body = bodyName;
                    thisBody.ParallaxBodyMaterial = CreateParallaxBodyMaterial(parallaxBody, bodyName);
        
                    Log(" - Assigned parallax body material");
        
                    try
                    {
                        parallaxBodies.Add(bodyName, thisBody); //Add to the list of parallax bodies
                        Log(" - Added " + bodyName + "'s parallax config successfully");
                        Log(parallaxBodies[bodyName].Body);
                    }
                    catch (Exception e)
                    {
                        Log(" - Duplicate body detected!\n" + " - " + e.ToString());
                        parallaxBodies[bodyName] = thisBody;
                        Log(" - Overwriting current body");
                    }
                    Log("////////////////////////////////////////////////\n");
                }
                
            }
        
            foreach (KeyValuePair<string, ParallaxBody> body in parallaxBodies)
            {
                Debug.Log(body.Value.Body);
                Debug.Log(body.Key);
            }
        
            Log("Activating config nodes...");
        }
        public void ActivateConfigNodes()
        {

            foreach (KeyValuePair<string, ParallaxBody> body in parallaxBodies)
            {
                Log(body.Value.Body + " hey");
                if (body.Value == null)
                {
                    Log("Body is null");
                }
                body.Value.CreateMaterial();
                Log("Created material successfully for " + body.Value.Body);
                if (body.Value.Body == null)
                {
                    Debug.Log("Instance body is null");
                }
                body.Value.Apply();
                Debug.Log("Applied config nodes");
                Debug.Log("There are " + parallaxBodies.Count + " Parallax bodies");

            }
        }
        public ParallaxBodyMaterial CreateParallaxBodyMaterial(ConfigNode parallaxBody, string bodyName)
        {
            ParallaxBodyMaterial material = new ParallaxBodyMaterial();
            material.SurfaceTexture = parallaxBody.GetValue("surfaceTexture");
            material.SurfaceTextureParallaxMap = parallaxBody.GetValue("surfaceTextureParallaxMap");
            material.SurfaceTextureBumpMap = parallaxBody.GetValue("surfaceTextureBumpMap");
            material.SurfaceVarianceBlendMap = parallaxBody.GetValue("surfaceVarianceBlendMap");
            material.SurfaceVarianceTexture = parallaxBody.GetValue("surfaceVarianceTexture");
            material.SurfaceVarianceParallaxMap = parallaxBody.GetValue("surfaceVarianceParallaxMap");
            material.SurfaceVarianceBumpMap = parallaxBody.GetValue("surfaceVarianceBumpMap");
            material.SteepTexture = parallaxBody.GetValue("steepTexture");
            material.SurfaceTextureScale = float.Parse(parallaxBody.GetValue("surfaceTextureScale"));
            material.SurfaceVarianceMapScale = float.Parse(parallaxBody.GetValue("surfaceVarianceMapScale"));
            material.SurfaceVarianceTextureScale = float.Parse(parallaxBody.GetValue("surfaceVarianceTextureScale"));
            material.SurfaceVarianceTexturePower = float.Parse(parallaxBody.GetValue("surfaceVarianceTexturePower"));
            material.SurfaceVarianceBlendMapScale = float.Parse(parallaxBody.GetValue("surfaceVarianceBlendMapScale"));
            material.SurfaceParallaxMapScale = float.Parse(parallaxBody.GetValue("surfaceParallaxMapScale"));
            material.SurfaceParallaxHeight = float.Parse(parallaxBody.GetValue("surfaceParallaxHeight"));
            material.SteepTextureScale = float.Parse(parallaxBody.GetValue("steepTextureScale"));
            material.SteepPower = float.Parse(parallaxBody.GetValue("steepPower"));
            material.Smoothness = float.Parse(parallaxBody.GetValue("smoothness"));
            material.SurfaceTextureMid = parallaxBody.GetValue("surfaceTextureMid");
            material.SurfaceTextureHigh = parallaxBody.GetValue("surfaceTextureHigh");
            material.BumpMapMid = parallaxBody.GetValue("surfaceBumpMapMid");
            material.BumpMapHigh = parallaxBody.GetValue("surfaceBumpMapHigh");
            material.BumpMapSteep = parallaxBody.GetValue("surfaceBumpMapSteep");
            material.LowStart = float.Parse(parallaxBody.GetValue("lowStart"));
            material.LowEnd = float.Parse(parallaxBody.GetValue("lowEnd"));
            material.HighStart = float.Parse(parallaxBody.GetValue("highStart"));
            material.HighEnd = float.Parse(parallaxBody.GetValue("highEnd"));
            material.PlanetName = bodyName;

            string color = parallaxBody.GetValue("tintColor"); //it pains me to write colour this way as a brit
            material.TintColor = new Color(float.Parse(color.Split(',')[0]), float.Parse(color.Split(',')[1]), float.Parse(color.Split(',')[2]));
            Debug.Log(material.TintColor + " COLOR1");

            return material;
    
        }
        public static void Log(string message)
        {
            Debug.Log("[Parallax]" + message);
        }
        
    }
    class ParallaxBody
    {
        private string bodyName;
        private ParallaxBodyMaterial parallaxBodyMaterial;
        private Material parallaxMaterial;
        public string Body
        {
            get { return bodyName; }
            set { bodyName = value; }
        }
        public ParallaxBodyMaterial ParallaxBodyMaterial
        {
            get { return parallaxBodyMaterial; }
            set { parallaxBodyMaterial = value; }
        }
        public void CreateMaterial()
        {
            parallaxMaterial = parallaxBodyMaterial.CreateMaterial();
        }
        public void Apply()
        {
            CelestialBody body = FlightGlobals.GetBodyByName(bodyName);
            if (body == null)
            {
                Debug.Log("Unable to get body by name: " + bodyName);
            }
            body.pqsController.surfaceMaterial = parallaxMaterial;
            body.pqsController.highQualitySurfaceMaterial = parallaxMaterial;
            body.pqsController.mediumQualitySurfaceMaterial = parallaxMaterial;
            body.pqsController.lowQualitySurfaceMaterial = parallaxMaterial;
            //body.pqsController.ultraQualitySurfaceMaterial = parallaxMaterial;
        }
    }
    class ParallaxBodyMaterial
    {
        private Material parallaxMaterial;

        private string planetName;
        private string surfaceTexture;
        private string surfaceTextureParallaxMap;
        private string surfaceTextureBumpMap;
        private string surfaceVarianceBlendMap;
        private string surfaceVarianceTexture;
        private string surfaceVarianceParallaxMap;
        private string surfaceVarianceBumpMap;
        private string steepTexture;
    
        private float steepPower;
        private float surfaceTextureScale;
        private float surfaceVarianceMapScale;
        private float surfaceVarianceTextureScale;
        private float surfaceVarianceTexturePower;
        private float surfaceVarianceBlendMapScale;
        private float surfaceParallaxMapScale;
        private float surfaceParallaxHeight;
        private float steepTextureScale;
        private float smoothness;
        private Color tintColor;

        private string surfaceTextureMid;
        private string surfaceTextureHigh;
        private string bumpMapMid;
        private string bumpMapHigh;
        private string bumpMapSteep;

        private float lowStart;
        private float lowEnd;
        private float highStart;
        private float highEnd;

        private float planetRadius;
    
        #region getsets

        public Color TintColor
        {
            get { return tintColor; }
            set { tintColor = value; }
        }
        public string PlanetName
        {
            get { return planetName; }
            set { planetName = value; }
        }
            
        public float PlanetRadius
        {
            get { return planetRadius; }
            set { planetRadius = value; }
        }
        public string SurfaceTextureMid
        {
            get { return surfaceTextureMid; }
            set { surfaceTextureMid = value; }
        }
        public string SurfaceTextureHigh
        {
            get { return surfaceTextureHigh; }
            set { surfaceTextureHigh = value; }
        }
        public string BumpMapMid
        {
            get { return bumpMapMid; }
            set { bumpMapMid = value; }
        }
        public string BumpMapHigh
        {
            get { return bumpMapHigh; }
            set { bumpMapHigh = value; }
        }
        public string BumpMapSteep
        {
            get { return bumpMapSteep; }
            set { bumpMapSteep = value; }
        }

        public float LowStart
        {
            get { return lowStart; }
            set { lowStart = value; }
        }
        public float LowEnd
        {
            get { return lowEnd; }
            set { lowEnd = value; }
        }
        public float HighStart
        {
            get { return highStart; }
            set { highStart = value; }
        }
        public float HighEnd
        {
            get { return highEnd; }
            set { highEnd = value; }
        }
        public string SurfaceTexture
        {
            get { return surfaceTexture; }
            set { surfaceTexture = value; }
        }
        public string SurfaceTextureParallaxMap
        {
            get { return surfaceTextureParallaxMap; }
            set { surfaceTextureParallaxMap = value; }
        }
        public string SurfaceTextureBumpMap
        {
            get { return surfaceTextureBumpMap; }
            set { surfaceTextureBumpMap = value; }
        }
        public string SurfaceVarianceBlendMap
        {
            get { return surfaceVarianceBlendMap; }
            set { surfaceVarianceBlendMap = value; }
        }
        public string SurfaceVarianceTexture
        {
            get { return surfaceVarianceTexture; }
            set { surfaceVarianceTexture = value; }
        }
        public string SurfaceVarianceParallaxMap
        {
            get { return surfaceVarianceParallaxMap; }
            set { surfaceVarianceParallaxMap = value; }
        }
        public string SurfaceVarianceBumpMap
        {
            get { return surfaceVarianceBumpMap; }
            set { surfaceVarianceBumpMap = value; }
        }
        public string SteepTexture
        {
            get { return steepTexture; }
            set { steepTexture = value; }
        }
        public float SteepPower
        {
            get { return steepPower; }
            set { steepPower = value; }
        }
        public float SurfaceTextureScale
        {
            get { return surfaceTextureScale; }
            set { surfaceTextureScale = value; }
        }
        public float SurfaceVarianceMapScale
        {
            get { return surfaceVarianceMapScale; }
            set { surfaceVarianceMapScale = value; }
        }
        public float SurfaceVarianceTextureScale
        {
            get { return surfaceVarianceTextureScale; }
            set { surfaceVarianceTextureScale = value; }
        }
        public float SurfaceVarianceTexturePower
        {
            get { return surfaceVarianceTexturePower; }
            set { surfaceVarianceTexturePower = value; }
        }
        public float SurfaceVarianceBlendMapScale
        {
            get { return surfaceVarianceBlendMapScale; }
            set { surfaceVarianceBlendMapScale = value; }
        }
        public float SurfaceParallaxMapScale
        {
            get { return surfaceParallaxMapScale; }
            set { surfaceParallaxMapScale = value; }
        }
        public float SurfaceParallaxHeight
        {
            get { return surfaceParallaxHeight; }
            set { surfaceParallaxHeight = value; }
        }
        public float SteepTextureScale
        {
            get { return steepTextureScale; }
            set { steepTextureScale = value; }
        }
        public float Smoothness
        {
            get { return smoothness; }
            set { smoothness = value; }
        }
        public Material ParallaxMaterial
        {
            get { return parallaxMaterial; }
            set { parallaxMaterial = value; }
        }
        #endregion
        public Material CreateMaterial()    //Does the shit ingame and stuffs
        {
            parallaxMaterial = new Material(ParallaxLoader.GetShader("Custom/ParallaxOcclusion"));
            if (HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                parallaxMaterial = FlightGlobals.GetHomeBody().pqsController.surfaceMaterial; //change this for all bodies or whatever idc it'll probably be forgotten about
                parallaxMaterial.shader = ParallaxLoader.GetShader("Custom/ParallaxOcclusion");
            }
            
    
            parallaxMaterial.SetTexture("_SurfaceTexture", LoadTexture(surfaceTexture));
            Log("_SurfaceTexture", surfaceTexture);
            parallaxMaterial.SetTexture("_ParallaxMap", LoadTexture(surfaceTextureParallaxMap));
            Log("_ParallaxMap", surfaceTextureParallaxMap);
            parallaxMaterial.SetTexture("_BumpMap", LoadTexture(surfaceTextureBumpMap));
            Log("_BumpMap", surfaceTextureBumpMap);
            parallaxMaterial.SetTexture("_NoiseTex", LoadTexture(surfaceVarianceBlendMap));
            Log("_NoiseTex", surfaceVarianceBlendMap);
            parallaxMaterial.SetTexture("_SurfaceVarianceTexture", LoadTexture(surfaceVarianceTexture));
            Log("_SurfaceVarianceTexture", surfaceVarianceTexture);
            parallaxMaterial.SetTexture("_ParallaxMapMulti", LoadTexture(surfaceVarianceParallaxMap));
            Log("_ParallaxMapMulti", surfaceVarianceParallaxMap);
            ParallaxMaterial.SetTexture("_SurfaceVarianceBumpMap", LoadTexture(surfaceVarianceBumpMap));
            Log("_SurfaceVarianceBumpMap", surfaceVarianceBumpMap);
            parallaxMaterial.SetTexture("_SteepTex", LoadTexture(steepTexture));
            Log("_SteepTex", steepTexture);
    
            parallaxMaterial.SetFloat("_SteepPower", steepPower);
            Log("_SteepPower", steepPower);
            parallaxMaterial.SetTextureScale("_SurfaceTexture", CreateVector(surfaceTextureScale));
            Log("_SurfaceTexture", surfaceTextureScale);
            parallaxMaterial.SetFloat("_SurfaceVarianceTextureScale", surfaceVarianceMapScale);
            Log("_SurfaceVarianceTextureScale", surfaceVarianceMapScale);
            parallaxMaterial.SetTextureScale("_NoiseTex", CreateVector(surfaceVarianceTextureScale));
            Log("_NoiseTex", surfaceVarianceTextureScale);
            parallaxMaterial.SetFloat("_SurfaceVarianceTexturePow", surfaceVarianceTexturePower);
            Log("_SurfaceVarianceTexturePow", surfaceVarianceTexturePower);
            parallaxMaterial.SetTextureScale("_NoiseTex", CreateVector(surfaceVarianceBlendMapScale));
            Log("_NoiseTex", surfaceVarianceBlendMapScale);
            parallaxMaterial.SetTextureScale("_ParallaxMap", CreateVector(surfaceParallaxMapScale));    //Currently doesn't do anything in the shader
            Log("_ParallaxMap", surfaceParallaxMapScale);
            parallaxMaterial.SetFloat("_Parallax", surfaceParallaxHeight);
            Log("_Parallax", surfaceParallaxHeight);
            parallaxMaterial.SetTextureScale("_SteepTex", CreateVector(steepTextureScale));
            Log("_SteepTex", steepTextureScale);
            parallaxMaterial.SetFloat("_Metallic", smoothness);
            Log("_Metallic", smoothness);

            parallaxMaterial.SetTexture("_SurfaceTextureMid", LoadTexture(surfaceTextureMid));
            parallaxMaterial.SetTexture("_SurfaceTextureHigh", LoadTexture(surfaceTextureHigh));
            parallaxMaterial.SetTexture("_BumpMapMid", LoadTexture(bumpMapMid));
            parallaxMaterial.SetTexture("_BumpMapHigh", LoadTexture(bumpMapHigh));
            parallaxMaterial.SetTexture("_BumpMapSteep", LoadTexture(bumpMapSteep));
            parallaxMaterial.SetFloat("_LowStart", lowStart);
            parallaxMaterial.SetFloat("_LowEnd", lowEnd);
            parallaxMaterial.SetFloat("_HighStart", highStart);
            parallaxMaterial.SetFloat("_HighEnd", highEnd);
            parallaxMaterial.SetFloat("_PlanetRadius", (float)FlightGlobals.GetBodyByName(planetName).Radius);
            parallaxMaterial.SetColor("_MetallicTint", tintColor);

            return parallaxMaterial;
        }
        private void Log(string name, string value)
        {
            Debug.Log(name + " is " + value);
        }
        private void Log(string name, float value)
        {
            Debug.Log(name + " is " + value);
        }
        private Texture LoadTexture(string name)
        {
            return Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(t => t.name == name);
        }
        private Vector2 CreateVector(float size)
        {
            return new Vector2(size, size);
        }
    }
}
