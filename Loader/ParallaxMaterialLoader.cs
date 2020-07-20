using System.IO;
using System;
using UnityEngine;
using System.Linq;
using System.Security.Principal;
using System.Collections.Generic;

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
    public class position : MonoBehaviour
    {
        public void Update()
        {
            FlightGlobals.GetHomeBody().pqsController.surfaceMaterial.SetVector("_PlanetOrigin", (Vector3)FlightGlobals.GetHomeBody().transform.position);
            FlightGlobals.GetHomeBody().pqsController.highQualitySurfaceMaterial.SetVector("_PlanetOrigin", (Vector3)FlightGlobals.GetHomeBody().transform.position);
            FlightGlobals.GetHomeBody().pqsController.mediumQualitySurfaceMaterial.SetVector("_PlanetOrigin", (Vector3)FlightGlobals.GetHomeBody().transform.position);
            FlightGlobals.GetHomeBody().pqsController.lowQualitySurfaceMaterial.SetVector("_PlanetOrigin", (Vector3)FlightGlobals.GetHomeBody().transform.position);
            FlightGlobals.GetHomeBody().pqsController.ultraQualitySurfaceMaterial.SetVector("_PlanetOrigin", (Vector3)FlightGlobals.GetHomeBody().transform.position);
        }
    }
    [KSPAddon(KSPAddon.Startup.PSystemSpawn, false)]
    class ParallaxShaderLoader : MonoBehaviour
    {
        public static Dictionary<string, ParallaxBody> parallaxBodies;
    
        public void Awake()
        {
            parallaxBodies = new Dictionary<string, ParallaxBody>();
        }
        public void Start()
        {
            Log("Starting...");
            GetConfigNodes();
            ActivateConfigNodes();
        }
        public void GetConfigNodes()
        {
            UrlDir.UrlConfig[] nodeArray = GameDatabase.Instance.GetConfigs("Parallax");
            Log("Retrieving config nodes...");
            for (int i = 0; i < nodeArray.Length; i++)
            {
                Debug.Log("This parallax node has: " + nodeArray[i].config.nodes.Count + " nodes");
                for (int b = 0; b < nodeArray[i].config.nodes.Count; b++)
                {
                    Log("Debug: " + nodeArray[i].config.nodes[b].name);
                    ConfigNode parallax = nodeArray[i].config;
                    string bodyName = parallax.nodes[b].GetValue("name");
                    Log("////////////////////////////////////////////////");
                    Log("////////////////" + bodyName + "////////////////");
                    Log("////////////////////////////////////////////////\n");
                    ConfigNode parallaxBody = parallax.nodes[b].GetNode("Textures");
                    if (parallaxBody == null)
                    {
                        Log("\tParallax Body is null! Cancelling load");
                        return;
                    }
                    Log("\tRetrieved body node");
                    ParallaxBody thisBody = new ParallaxBody();
                    thisBody.Body = bodyName;
                    ParallaxBodyMaterial thisBodyMaterial = CreateParallaxBodyMaterial(parallaxBody);
                    Log("\tCreated parallax body material");
                    thisBody.ParallaxBodyMaterial = thisBodyMaterial;
                    Log("\tAssigned parallax body material");

                    try
                    {
                        parallaxBodies.Add(bodyName, thisBody); //Add to the list of parallax bodies
                        Log("\tAdded " + bodyName + "'s parallax config successfully");
                    }
                    catch (Exception e)
                    {
                        Log("\tDuplicate body detected!\n" + "\t\t" + e.ToString());
                        parallaxBodies[bodyName] = thisBody;
                        Log("\tOverwriting current body");
                    }
                }
                
            }
            
            
            
            Log("Activating config nodes...");
        }
        public void ActivateConfigNodes()
        {
            foreach (ParallaxBody body in parallaxBodies.Values)
            {
                if (body == null)
                {
                    Log("Body is null");
                }
                body.CreateMaterial();
                Log("Created material successfully for " + body.Body);
                if (body.Body == null)
                {
                    Debug.Log("Instance body is null");
                }
                body.Apply();
                Debug.Log("Applied config nodes");
                Debug.Log("There are " + parallaxBodies.Count + " Parallax bodies");
                Log("////////////////////////////////////////////////");
            }
        }
        public ParallaxBodyMaterial CreateParallaxBodyMaterial(ConfigNode parallaxBody)
        {
            Debug.Log(parallaxBody.name);
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
    
            return material;
    
        }
        public static void Log(string message)
        {
            Debug.Log("[Parallax]" + message);
        }
        
    }
    class ParallaxBody
    {
        private static string bodyName;
        private static ParallaxBodyMaterial parallaxBodyMaterial;
        private static Material parallaxMaterial;
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
            body.pqsController.ultraQualitySurfaceMaterial = parallaxMaterial;
        }
    }
    class ParallaxBodyMaterial
    {
        private Material parallaxMaterial;
    
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
    
        #region getsets
      
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
            parallaxMaterial.SetTexture("_ParallaxMap", LoadTexture(surfaceTextureParallaxMap));
            parallaxMaterial.SetTexture("_BumpMap", LoadTexture(surfaceTextureBumpMap));
            parallaxMaterial.SetTexture("_NoiseTex", LoadTexture(surfaceVarianceBlendMap));
            parallaxMaterial.SetTexture("_SurfaceVarianceTexture", LoadTexture(surfaceVarianceTexture));
            parallaxMaterial.SetTexture("_ParallaxMapMulti", LoadTexture(surfaceVarianceParallaxMap));
            ParallaxMaterial.SetTexture("_SurfaceVarianceBumpMap", LoadTexture(surfaceVarianceBumpMap));
            parallaxMaterial.SetTexture("_SteepTex", LoadTexture(steepTexture));
    
            parallaxMaterial.SetFloat("_SteepPower", steepPower);
            parallaxMaterial.SetTextureScale("_SurfaceTexture", CreateVector(surfaceTextureScale));
            parallaxMaterial.SetFloat("_SurfaceVarianceTextureScale", surfaceVarianceMapScale);
            parallaxMaterial.SetTextureScale("_NoiseTex", CreateVector(surfaceVarianceTextureScale));
            parallaxMaterial.SetFloat("_SurfaceVarianceTexturePow", surfaceVarianceTexturePower);
            parallaxMaterial.SetTextureScale("_NoiseTex", CreateVector(surfaceVarianceBlendMapScale));
            parallaxMaterial.SetTextureScale("_ParallaxMap", CreateVector(surfaceParallaxMapScale));    //Currently doesn't do anything in the shader
            parallaxMaterial.SetFloat("_Parallax", surfaceParallaxHeight);
            parallaxMaterial.SetTextureScale("_SteepTex", CreateVector(steepTextureScale));
            parallaxMaterial.SetFloat("_Metallic", smoothness);
    
            return parallaxMaterial;
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