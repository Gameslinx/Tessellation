using System.IO;
using System;
using UnityEngine;
using System.Linq;
using System.Security.Principal;
using System.Collections.Generic;
using Contracts.Predicates;
using Smooth.Collections;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.SceneManagement;
using PQSModExpansion;
using System.Collections;
using System.Security.Cryptography;
using UnityEngine.Rendering;
using System.Reflection;

[assembly: KSPAssembly("Parallax", 1, 0)]
[assembly: KSPAssemblyDependency("ParallaxBoi", 1, 0)]
namespace ParallaxShader
{
    [KSPAddon(KSPAddon.Startup.Instantly, false)]
    public class ParallaxLoader : MonoBehaviour
    {
        public static Dictionary<string, Shader> shaders = new Dictionary<string, Shader>();
        public void Awake()
        {
            string filePath = Path.Combine(KSPUtil.ApplicationRootPath + "GameData/" + "Parallax/Shaders/Parallax");
            if (Application.platform == RuntimePlatform.LinuxPlayer || (Application.platform == RuntimePlatform.WindowsPlayer && SystemInfo.graphicsDeviceVersion.StartsWith("OpenGL")))
            {
                filePath = (filePath + "-OpenGL-linux.unity3d");
            }
            else if (Application.platform == RuntimePlatform.WindowsPlayer)
            {
                filePath = (filePath + "-windows.unity3d");
            }
            if (Application.platform == RuntimePlatform.OSXPlayer)
            {
                filePath = (filePath + "-OpenGL-macosx.unity3d");
            }
            var assetBundle = AssetBundle.LoadFromFile(filePath);
            Debug.Log("Loaded bundle");
            if (assetBundle == null)
            {
                Debug.Log("Failed to load bundle");
                Debug.Log("Path: " + filePath);
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






            //filePath = Path.Combine(KSPUtil.ApplicationRootPath + "GameData/" + "Parallax/Shaders/Grass");
            //
            //if (Application.platform == RuntimePlatform.WindowsPlayer && SystemInfo.graphicsDeviceVersion.StartsWith("OpenGL"))
            //{
            //    filePath = (filePath + "-linux.unity3d");
            //}
            //else if (Application.platform == RuntimePlatform.WindowsPlayer)
            //{
            //    filePath = (filePath + "-windows.unity3d");
            //}
            //if (Application.platform == RuntimePlatform.OSXPlayer)
            //{
            //    filePath = (filePath + "-macosx.unity3d");
            //}
            //var assetBundle2 = AssetBundle.LoadFromFile(filePath);
            //Debug.Log("Loaded bundle");
            //if (assetBundle2 == null)
            //{
            //    Debug.Log("Failed to load bundle");
            //    Debug.Log(filePath);
            //}
            //else
            //{
            //    Shader[] theseShaders = assetBundle2.LoadAllAssets<Shader>();
            //    Debug.Log("Loaded all shaders");
            //    foreach (Shader thisShader in theseShaders)
            //    {
            //        shaders.Add(thisShader.name, thisShader);
            //        Debug.Log("Loaded shader: " + thisShader.name);
            //    }
            //
            //
            //
            //}
            //
            //filePath = Path.Combine(KSPUtil.ApplicationRootPath + "GameData/" + "Parallax/Shaders/ParallaxScaled");
            //
            //if (Application.platform == RuntimePlatform.WindowsPlayer && SystemInfo.graphicsDeviceVersion.StartsWith("OpenGL"))
            //{
            //    filePath = (filePath + "-linux.unity3d");
            //}
            //else if (Application.platform == RuntimePlatform.WindowsPlayer)
            //{
            //    filePath = (filePath + "-windows.unity3d");
            //}
            //if (Application.platform == RuntimePlatform.OSXPlayer)
            //{
            //    filePath = (filePath + "-macosx.unity3d");
            //}
            //var assetBundle3 = AssetBundle.LoadFromFile(filePath);
            //Debug.Log("Loaded bundle");
            //if (assetBundle3 == null)
            //{
            //    Debug.Log("Failed to load bundle");
            //    Debug.Log(filePath);
            //}
            //else
            //{
            //    Shader[] theseShaders = assetBundle3.LoadAllAssets<Shader>();
            //    Debug.Log("Loaded all shaders");
            //    foreach (Shader thisShader in theseShaders)
            //    {
            //        shaders.Add(thisShader.name, thisShader);
            //        Debug.Log("Loaded shader: " + thisShader.name);
            //    }
            //
            //
            //
            //}
        }
        public static Shader GetShader(string name)
        {
            return shaders[name];
        }
    }

    [KSPAddon(KSPAddon.Startup.Instantly, false)]
    public class ParallaxSettings : MonoBehaviour   //Stuff here is used after the parallax bodies have been assigned - these are global settings
    {
        //Internal / constant global values
        public static float tessellationEdgeLength = 25;
        public static float tessellationRange = 99;
        public static float tessellationMax = 64;
        public static int refreshRate = 30;
        public static ReflectionProbeRefreshMode refreshMode = ReflectionProbeRefreshMode.ViaScripting;
        public static ReflectionProbeTimeSlicingMode timeMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
        public static float reflectionResolution = 512;
        public static bool useReflections = true;
        public static bool tessellate = true;
        public static bool tessellateLighting = true;
        public static bool tessellateShadows = true;
        public static string trueLighting = "false";
        public static float tessMult = 1;
        public static bool collide = true;
        public static bool flatMinmus = false;
        public void Start()
        {
            AssignVariables();
            LogVariables();
        }
        public void AssignVariables()
        {
            UrlDir.UrlConfig[] nodeArray = GameDatabase.Instance.GetConfigs("ParallaxGlobalConfig");
            if (nodeArray == null)
            {
                Debug.Log("[ParallaxGlobalConfig] Exception: No global config detected! Using default values");
                return;
            }
            if (nodeArray.Length > 1)
            {
                Debug.Log("[ParallaxGlobalConfig] Exception: Multiple global configs detected!");
            }
            UrlDir.UrlConfig config = nodeArray[0];
            ConfigNode tessellationSettings = config.config.nodes.GetNode("TessellationSettings");
            ConfigNode reflectionSettings = config.config.nodes.GetNode("ReflectionSettings");
            ConfigNode lightingSettings = config.config.nodes.GetNode("LightingSettings");
            ConfigNode collisionSettings = config.config.nodes.GetNode("CollisionSettings");
            tessellationEdgeLength = float.Parse(tessellationSettings.GetValue("edgeLength"));
            tessellationRange = float.Parse(tessellationSettings.GetValue("range"));
            tessellationMax = float.Parse(tessellationSettings.GetValue("maxTessellation"));

            string refreshModeString = reflectionSettings.GetValue("refreshRate").ToLower();
            if (refreshModeString == "instantly")
            {
                refreshMode = ReflectionProbeRefreshMode.EveryFrame;
            }
            else if (int.Parse(refreshModeString) < Screen.currentResolution.refreshRate)    //Don't let it be updated any faster than screen refresh rate
            {
                refreshMode = ReflectionProbeRefreshMode.ViaScripting;
                refreshRate = int.Parse(refreshModeString);
            }
            string timeModeString = reflectionSettings.GetValue("timeSlicing").ToLower();
            if (timeModeString == "instantly")
            {
                timeMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
                refreshRate = Screen.currentResolution.refreshRate;
            }
            else if (timeModeString == "allfacesatonce")
            {
                timeMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            }
            else if (timeModeString == "individualfaces")
            {
                timeMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
            }
            else
            {
                Debug.Log("[ParallaxGlobalConfig] Exception: Unable to detect refresh setting: timeSlicing");
            }
            reflectionResolution = int.Parse(reflectionSettings.GetValue("resolution"));

            string reflectionsString = reflectionSettings.GetValue("reflections").ToLower();
            if (reflectionsString == "true")
            {
                useReflections = true;
            }
            else
            {
                useReflections = false;
            }

            string tessellateLightingString = lightingSettings.GetValue("tessellateLighting").ToLower();
            string tessellateShadowsString = lightingSettings.GetValue("tessellateShadows").ToLower();
            string tessellateString = lightingSettings.GetValue("tessellate").ToLower();

            if (tessellateLightingString == "true")
            {
                tessellateLighting = true;
            }
            else
            {
                tessellateShadows = false;
            }
            if (tessellateShadowsString == "true")
            {
                tessellateShadows = true;
            }
            else
            {
                tessellateLighting = false;
            }
            if (tessellateString == "true")
            {
                tessellate = true;
            }
            else
            {
                tessellate = false;
            }
            string trueLightingString = lightingSettings.GetValue("trueLighting").ToLower();
            if (trueLightingString == "true")
            {
                trueLighting = "true";
            }
            else if (trueLightingString == "false")
            {
                trueLighting = "false";
            }
            else //Use adaptive true lighting
            {
                trueLighting = "adaptive";
            }
            string tessQualityString = tessellationSettings.GetValue("tessellationQuality").ToLower();
            if (tessQualityString == "low")
            {
                tessMult = 0.75f;
            }
            if (tessQualityString == "normal")
            {
                tessMult = 1f;
            }
            if (tessQualityString == "high")
            {
                tessMult = 1.25f;
            }
            if (tessQualityString == "higher")
            {
                tessMult = 1.5f;
            }
            string collisionString = collisionSettings.GetValue("collide").ToLower();
            string flatMinmusString = collisionSettings.GetValue("minmusFlatsAreActuallyFlatBecauseYouDontKnowHowToBuildPlanesThatCantHandleProperTerrain").ToLower();
            if (collisionString == "false")
            {
                collide = false;
            }
            else
            {
                collide = true;
            }
            if (flatMinmusString == "false")
            {
                flatMinmus = false;
            }
            else
            {
                flatMinmus = true;
            }
        }
        public void LogVariables()
        {
            Log("Tessellation Edge Length = " + tessellationEdgeLength);
            Log("Tessellation Range = " + tessellationRange);
            Log("Maximum Tessellation = " + tessellationMax);
            Log("Reflection Resolution = " + reflectionResolution);
            Log("Reflection Refresh Rate = " + refreshRate);
            Log("Reflection Mode = " + refreshMode);
            Log("Reflection Time Slicing = " + timeMode);
            Log("Reflections = " + useReflections);
            Log("TessLighting = " + tessellateLighting);
            Log("TessShadows = " + tessellateShadows);
            Log("Tess = " + tessellate);
            Log("TessMult = " + tessMult);
        }
        public void Log(string message)
        {
            Debug.Log("[ParallaxGlobalConfig] " + message);
        }
    }


    public static class ParallaxReflectionProbes
    {
        public static GameObject probe;
        public static bool probeActive = false;
    }
    //public class CameraRotationDetection : MonoBehaviour      //Currently not implemented
    //{
    //    bool isRotating = false;
    //    bool isMouseDown = false;
    //    ReflectionProbe refProbe;
    //    Vector3 oldPosition;
    //    public void Update()
    //    {
    //
    //        //Vector3 newPosition = Camera.main.transform.position;
    //        //float distance = Vector3.Distance(oldPosition, newPosition);
    //        //
    //        //if (ParallaxReflectionProbes.probe != null && ParallaxReflectionProbes.probeActive == true)
    //        //{
    //        //    refProbe = ParallaxReflectionProbes.probe.GetComponent<ReflectionProbe>();
    //        //}
    //        //if (distance > 200000 && ParallaxReflectionProbes.probe != null && ParallaxReflectionProbes.probeActive == true)
    //        //{
    //        //    refProbe.resolution = 256;
    //        //    refProbe.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.NoTimeSlicing;
    //        //    refProbe.RenderProbe();
    //        //    var tex = ParallaxReflectionProbes.probe.GetComponent<ReflectionProbe>().texture;
    //        //    FlightGlobals.currentMainBody.pqsController.surfaceMaterial.SetTexture("_ReflectionMap", tex);
    //        //    foreach (KeyValuePair<string, GameObject> quad in QuadMeshDictionary.subdividedQuadList)
    //        //    {
    //        //        quad.Value.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_ReflectionMap", tex);
    //        //    }
    //        //    oldPosition = newPosition;
    //        //}
    //        //else if (distance <= 200000 && ParallaxReflectionProbes.probe != null)  //Always this one, for now
    //        //{
    //        //    //Cam not moving
    //        //    refProbe.resolution = 256;
    //        //    refProbe.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.NoTimeSlicing;
    //        //    refProbe.RenderProbe();
    //        //
    //        //    var tex = ParallaxReflectionProbes.probe.GetComponent<ReflectionProbe>().texture;
    //        //        FlightGlobals.currentMainBody.pqsController.surfaceMaterial.SetTexture("_ReflectionMap", tex);
    //        //        foreach (KeyValuePair<string, GameObject> quad in QuadMeshDictionary.subdividedQuadList)
    //        //        {
    //        //            quad.Value.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_ReflectionMap", tex);
    //        //        }
    //        //}
    //    }
    //}
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class Shader5 : MonoBehaviour
    {
        CelestialBody body;
        Material pqsMaterial;
        public void Start()
        {
            body = FlightGlobals.currentMainBody;
            //pqsMaterial = Instantiate(body.pqsController.surfaceMaterial);
            
            //foreach (ReflectionProbe probe in FindObjectsOfType(typeof(ReflectionProbe)))
            //{
            //    Debug.Log(probe.name + " is an active reflection probe");
            //}
        }
        public void Update()
        {

            //if (FlightGlobals.currentMainBody != body)
            //{
            //    body.pqsController.surfaceMaterial = pqsMaterial;
            //    Start();
            //}

            //float fadeStart = 60000;//FlightGlobals.currentMainBody.GetComponent<ScaledSpaceFader>().fadeStart;
            //float fadeEnd = 100000;//FlightGlobals.currentMainBody.GetComponent<ScaledSpaceFader>().fadeEnd;
            //float cameraAltitude = Vector3.Distance(Camera.main.transform.position, FlightGlobals.currentMainBody.transform.position) - (float)FlightGlobals.currentMainBody.Radius;
            //float fadeMult = Mathf.Clamp((cameraAltitude - fadeStart) / (fadeEnd - fadeStart), 0, 1);
            //
            //FlightGlobals.currentMainBody.pqsController.surfaceMaterial.Lerp(pqsMaterial, scaledMaterial, fadeMult);

            if (ParallaxShaderLoader.parallaxBodies.ContainsKey(FlightGlobals.currentMainBody.name) && ParallaxSettings.useReflections == true && HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                //Do camera height here
                GetHeightFromTerrain(Camera.main.transform);
                if (ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.UseReflections == true)
                {

                    if (ParallaxReflectionProbes.probe == null && ParallaxSettings.useReflections == true)
                    {
                        Debug.Log("Beginning reflection probe creation");
                        ParallaxReflectionProbes.probe = new GameObject();
                        ParallaxReflectionProbes.probe.AddComponent<ReflectionProbe>();

                        var probe = ParallaxReflectionProbes.probe.GetComponent<ReflectionProbe>();
                        probe.resolution = (int)ParallaxSettings.reflectionResolution;
                        probe.boxProjection = true;
                        //probe.clearFlags = ;// UnityEngine.Rendering.ReflectionProbeClearFlags.Skybox;
                        probe.shadowDistance = 100;
                        probe.importance = 1;
                        probe.intensity = 1;
                        probe.size = new Vector3(10000, 10000, 10000);
                        probe.hdr = true;
                        probe.nearClipPlane = 0.3f;
                        probe.farClipPlane = 5000f;
                        probe.refreshMode = ParallaxSettings.refreshMode;
                        probe.timeSlicingMode = ParallaxSettings.timeMode;
                        probe.enabled = true;
                        probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
                        probe.cullingMask = Camera.main.cullingMask;   //Atmosphere, scaled scenery, skysphere
                        Debug.Log("Reflection probe created!");

                        ParallaxReflectionProbes.probeActive = true;
                        StartCoroutine(UpdateProbe());
                        //cube = GameObject.CreatePrimitive(PrimitiveType.Sphere);


                    }
                    else
                    //Mirror the reflection probe in the terrain, find radar height of terrain by casting a ray
                    {

                        Vector3 cameraToPlanet = Vector3.Normalize(Camera.main.transform.position - FlightGlobals.currentMainBody.transform.position);  //Move camera along this vector
                        //float terrainHeightOfCamera = GetHeightFromTerrain(Camera.main.transform);
                        double terrainHeightOfCraft = FlightGlobals.ActiveVessel.radarAltitude;

                        Vector3 reflectionProbePositionChange = new Vector3((float)cameraToPlanet.x * (float)CameraRaycast.cameraAltitude * 2, (float)cameraToPlanet.y * (float)CameraRaycast.cameraAltitude * 2, (float)cameraToPlanet.z * (float)CameraRaycast.cameraAltitude * 2);
                        Vector3 reflectionProbePosition = Camera.main.transform.position - reflectionProbePositionChange;

                        ParallaxReflectionProbes.probe.GetComponent<ReflectionProbe>().transform.position = reflectionProbePosition;
                        Debug.Log("Camera alt: " + CameraRaycast.cameraAltitude + ", craft alt: " + terrainHeightOfCraft);
                    }
                }
                else
                {
                    if (ParallaxReflectionProbes.probe != null)
                    {
                        ParallaxReflectionProbes.probe.GetComponent<ReflectionProbe>().resolution = 1;
                        Destroy(ParallaxReflectionProbes.probe);
                        ParallaxReflectionProbes.probeActive = false;
                    }
                }
            }
            else
            {
                if (ParallaxReflectionProbes.probe != null)
                {
                    ParallaxReflectionProbes.probe.GetComponent<ReflectionProbe>().resolution = 1;
                    Destroy(ParallaxReflectionProbes.probe);
                    ParallaxReflectionProbes.probeActive = false;
                }
            }
        }
        public double GetPQSHeight(Vector3 cameraPos)
        {
            if (FlightGlobals.currentMainBody.pqsController != null)
            {
                Vector2d latLon = GetLatLon(cameraPos);
                return FlightGlobals.currentMainBody.pqsController.GetSurfaceHeight(FlightGlobals.currentMainBody.GetRelSurfaceNVector(latLon.x, latLon.y)) - FlightGlobals.currentMainBody.Radius;
            }
            return -1.0; //There's a hole in the fucking ground!!!
        }
        public Vector2d GetLatLon(Vector3 pos)
        {
            Vector2d latLon;    //latitude[0] longitude[1]
            latLon = FlightGlobals.currentMainBody.GetLatitudeAndLongitude(pos);
            return latLon;
        }
        public float GetHeightFromTerrain(Transform pos)    //Use fancy raycasting to achieve fancy things
        {
            float heightFromTerrain = 0;
            Vector3 vector = FlightGlobals.getUpAxis(FlightGlobals.currentMainBody, pos.position);
            float num = FlightGlobals.getAltitudeAtPos(pos.position, FlightGlobals.currentMainBody);
            if (num < 0f)
            {
                //Camera is underwater 
            }
            num += 600f;
            RaycastHit heightFromTerrainHit;
            if (Physics.Raycast(pos.position, -vector, out heightFromTerrainHit, num, 32768, QueryTriggerInteraction.Ignore))
            {
                heightFromTerrain = heightFromTerrainHit.distance;
                //this.objectUnderVessel = heightFromTerrainHit.collider.gameObject;
            }
            CameraRaycast.cameraAltitude = heightFromTerrain;
            return heightFromTerrain;
        }
        
        public IEnumerator UpdateProbe()    //Optimize the rendering of the reflection probe so that it doesn't kill the framerate
        {
            if (ParallaxReflectionProbes.probeActive == true)
            {
                ParallaxReflectionProbes.probe.GetComponent<ReflectionProbe>().RenderProbe(); //currently every frame
                var tex = ParallaxReflectionProbes.probe.GetComponent<ReflectionProbe>().texture;
                FlightGlobals.currentMainBody.pqsController.surfaceMaterial.SetTexture("_ReflectionMap", tex);
                foreach (KeyValuePair<string, GameObject> quad in QuadMeshDictionary.subdividedQuadList)
                {
                    quad.Value.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_ReflectionMap", tex);
                }
            }
            yield return new WaitForSeconds(1 / ParallaxSettings.refreshRate);   //30fps
            StartCoroutine(UpdateProbe());
        }
    }
    public class CameraRaycast
    {
        public static float cameraAltitude = 0;
    }
    [KSPAddon(KSPAddon.Startup.FlightAndKSC, false)]

    public class Position : MonoBehaviour
    {
        public CelestialBody lastBody;
        public PQSMod_CelestialBodyTransform fader;
        public static Vector3 floatUV = Vector3.zero;
        public void Start()
        {
            QualitySettings.shadowDistance = 10000;
            QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
            QualitySettings.shadowProjection = ShadowProjection.StableFit;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowCascade4Split = new Vector3(0.003f, 0.034f, 0.101f);

            Camera.main.nearClipPlane = 0.1f;
            Camera.current.nearClipPlane = 0.1f;
        }
        public void Update()
        {

            CelestialBody body = FlightGlobals.currentMainBody;
            if (ParallaxShaderLoader.parallaxBodies.ContainsKey(body.name))
            {
                body.pqsController.surfaceMaterial.SetVector("_PlanetOrigin", (Vector3)body.transform.position);
                body.pqsController.lowQualitySurfaceMaterial.SetVector("_PlanetOrigin", (Vector3)body.transform.position);
                body.pqsController.mediumQualitySurfaceMaterial.SetVector("_PlanetOrigin", (Vector3)body.transform.position);
                body.pqsController.highQualitySurfaceMaterial.SetVector("_PlanetOrigin", (Vector3)body.transform.position);
                body.pqsController.ultraQualitySurfaceMaterial.SetVector("_PlanetOrigin", (Vector3)body.transform.position);

                //body.pqsController.lowQualitySurfaceMaterial.SetVector("_LightPos", (Vector3)(FlightGlobals.Bodies[0].transform.position));
                //body.pqsController.mediumQualitySurfaceMaterial.SetVector("_LightPos", (Vector3)(FlightGlobals.Bodies[0].transform.position));
                //body.pqsController.surfaceMaterial.SetVector("_LightPos", (Vector3)(FlightGlobals.Bodies[0].transform.position));
                //body.pqsController.highQualitySurfaceMaterial.SetVector("_LightPos", (Vector3)(FlightGlobals.Bodies[0].transform.position));
                //body.pqsController.ultraQualitySurfaceMaterial.SetVector("_LightPos", (Vector3)(FlightGlobals.Bodies[0].transform.position));

                ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterial.SetVector("_PlanetOrigin", (Vector3)body.transform.position);
                ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterial.SetVector("_LightPos", (Vector3)body.transform.position);

                ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPLOW.SetVector("_PlanetOrigin", (Vector3)body.transform.position);
                ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPLOW.SetVector("_LightPos", (Vector3)(body.transform.position));
                ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPMID.SetVector("_PlanetOrigin", (Vector3)body.transform.position);
                ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPMID.SetVector("_LightPos", (Vector3)body.transform.position);
                ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPHIGH.SetVector("_PlanetOrigin", (Vector3)body.transform.position);
                ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPHIGH.SetVector("_LightPos", (Vector3)body.transform.position);

                ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLELOW.SetVector("_PlanetOrigin", (Vector3)body.transform.position);
                ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLELOW.SetVector("_LightPos", (Vector3)body.transform.position);
                ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLEMID.SetVector("_PlanetOrigin", (Vector3)body.transform.position);
                ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLEMID.SetVector("_LightPos", (Vector3)body.transform.position);
                ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLEHIGH.SetVector("_PlanetOrigin", (Vector3)body.transform.position);
                ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLEHIGH.SetVector("_LightPos", (Vector3)body.transform.position);

                ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialDOUBLELOW.SetVector("_PlanetOrigin", (Vector3)body.transform.position);
                ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialDOUBLELOW.SetVector("_LightPos", (Vector3)body.transform.position);
                ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialDOUBLEHIGH.SetVector("_PlanetOrigin", (Vector3)body.transform.position);
                ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialDOUBLEHIGH.SetVector("_LightPos", (Vector3)body.transform.position);


            }


            if (FlightGlobals.ActiveVessel != null && HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                if (ParallaxShaderLoader.parallaxBodies.ContainsKey(body.name))
                {

                    Vector3d accuratePlanetPosition = FlightGlobals.currentMainBody.position;   //Double precision planet origin
                    double surfaceTexture_ST = ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.SurfaceTextureScale;    //Scale of surface texture
                    Vector3d UV = accuratePlanetPosition * surfaceTexture_ST;
                    UV = new Vector3d(Clamp(UV.x), Clamp(UV.y), Clamp(UV.z));
                    floatUV = new Vector3((float)UV.x, (float)UV.y, (float)UV.z);
                    FlightGlobals.currentMainBody.pqsController.surfaceMaterial.SetVector("_SurfaceTextureUVs", floatUV);
                    FlightGlobals.currentMainBody.pqsController.highQualitySurfaceMaterial.SetVector("_SurfaceTextureUVs", floatUV);
                    FlightGlobals.currentMainBody.pqsController.mediumQualitySurfaceMaterial.SetVector("_SurfaceTextureUVs", floatUV);
                    FlightGlobals.currentMainBody.pqsController.lowQualitySurfaceMaterial.SetVector("_SurfaceTextureUVs", floatUV);
                    FlightGlobals.currentMainBody.pqsController.ultraQualitySurfaceMaterial.SetVector("_SurfaceTextureUVs", floatUV);
                    if (floatUV != null)
                    {
                        ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterial.SetVector("_SurfaceTextureUVs", floatUV);
                        ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPLOW.SetVector("_SurfaceTextureUVs", floatUV);
                        ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPMID.SetVector("_SurfaceTextureUVs", floatUV);
                        ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPHIGH.SetVector("_SurfaceTextureUVs", floatUV);
                        ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLELOW.SetVector("_SurfaceTextureUVs", floatUV);
                        ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLEMID.SetVector("_SurfaceTextureUVs", floatUV);
                        ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLEHIGH.SetVector("_SurfaceTextureUVs", floatUV);
                        ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialDOUBLELOW.SetVector("_SurfaceTextureUVs", floatUV);
                        ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialDOUBLEHIGH.SetVector("_SurfaceTextureUVs", floatUV);
                    }
                    

                    
                }
                


            }
            float _PlanetOpacity = FlightGlobals.currentMainBody.pqsController.surfaceMaterial.GetFloat("_PlanetOpacity");

            ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterial.SetFloat("_PlanetOpacity", _PlanetOpacity);
            ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPLOW.SetFloat("_PlanetOpacity", _PlanetOpacity);
            ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPMID.SetFloat("_PlanetOpacity", _PlanetOpacity);
            ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPHIGH.SetFloat("_PlanetOpacity", _PlanetOpacity);
            ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLELOW.SetFloat("_PlanetOpacity", _PlanetOpacity);
            ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLEMID.SetFloat("_PlanetOpacity", _PlanetOpacity);
            ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLEHIGH.SetFloat("_PlanetOpacity", _PlanetOpacity);
            ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialDOUBLELOW.SetFloat("_PlanetOpacity", _PlanetOpacity);
            ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialDOUBLEHIGH.SetFloat("_PlanetOpacity", _PlanetOpacity);

            lastBody = body;
            //FlightGlobals.GetHomeBody().pqsController.ultraQualitySurfaceMaterial.SetVector("_PlanetOrigin", (Vector3)FlightGlobals.GetHomeBody().transform.position);
        }
        public double Clamp(double input)
        {
            if (CameraRaycast.cameraAltitude < 250 && CameraRaycast.cameraAltitude != 0)
            {
                return input % 32;  //When close to the ground, 
            }
            if (CameraRaycast.cameraAltitude == 0)  //Outside ray dir
            {
                return input % 1024.0;
            }
            return input % 1024.0;
        }
    }
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class IngameEditor : MonoBehaviour
    {
        public void Update()
        {
            bool key = Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.Alpha1);
            bool key2 = Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.Alpha2);

            if (key)
            {
                ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterial.SetFloat("_displacement_offset", ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterial.GetFloat("_displacement_offset") + 0.05f);
                ScreenMessages.PostScreenMessage(ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterial.GetFloat("_displacement_offset").ToString(), 0.1f);
            }
            if (key2)
            {
                ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterial.SetFloat("_displacement_offset", ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterial.GetFloat("_displacement_offset") - 0.05f);
                ScreenMessages.PostScreenMessage(ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterial.GetFloat("_displacement_offset").ToString(), 0.1f);
            }

        }
    }
    
    [KSPAddon(KSPAddon.Startup.PSystemSpawn, false)]
    public class ParallaxShaderLoader : MonoBehaviour
    {
        public static Dictionary<string, ParallaxBody> parallaxBodies = new Dictionary<string, ParallaxBody>();
        public static float timeElapsed = 0;
        public void Start()
        {
            Log("Starting...");
            timeElapsed = Time.realtimeSinceStartup;
            GetConfigNodes();
            ActivateConfigNodes();
            timeElapsed = Time.realtimeSinceStartup - timeElapsed;
            Log("Parallax took " + timeElapsed + " milliseconds [" + timeElapsed / 1000 + " seconds] to load from start to finish.");


            
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
                    ConfigNode parallax = nodeArray[i].config;
                    string bodyName = parallax.nodes[b].GetValue("name");
                    Log("////////////////////////////////////////////////");
                    Log("////////////////" + bodyName + "////////////////");
                    Log("////////////////////////////////////////////////\n");
                    ConfigNode parallaxBody = parallax.nodes[b].GetNode("Textures");
                    if (parallaxBody == null)
                    {
                        Log(" - !!!Parallax Body is null! Cancelling load!!!"); //Essentially, you fucked up
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
                   //DebugAllProperties(parallaxBody, thisBody);
                    Log("////////////////////////////////////////////////\n");
                }

            }

            Log("Activating config nodes...");
        }
        public void ActivateConfigNodes()
        {

            foreach (KeyValuePair<string, ParallaxBody> body in parallaxBodies)
            {
                Log("////////////////////////////////////////////////");
                Log("////////////////" + body.Key + "////////////////");
                Log("////////////////////////////////////////////////\n");
                body.Value.CreateMaterial();
                
                Log(" - Created material successfully");
                body.Value.Apply();
                Debug.Log(" - Applied config nodes");

            }
        }
        public ParallaxBodyMaterial CreateParallaxBodyMaterial(ConfigNode parallaxBody, string bodyName)
        {
            ParallaxBodyMaterial material = new ParallaxBodyMaterial();
            material.SurfaceTexture = ParseString(parallaxBody, "surfaceTexture");
            material.SurfaceTextureParallaxMap = ParseString(parallaxBody, "surfaceTessellationMap");
            material.SurfaceTextureBumpMap = ParseString(parallaxBody, "surfaceTextureBumpMap");
            material.SteepTexture = ParseString(parallaxBody, "steepTexture");
            material.SurfaceTextureScale = ParseFloat(parallaxBody, "surfaceTextureScale");
            material.SurfaceParallaxHeight = ParseFloat(parallaxBody, "tessellationHeight");
            material.SteepTextureScale = ParseFloat(parallaxBody, "steepTextureScale");
            material.SteepPower = ParseFloat(parallaxBody, "steepPower");
            material.Smoothness = ParseFloat(parallaxBody, "smoothness");
            material.SurfaceTextureMid = ParseString(parallaxBody, "surfaceTextureMid");
            material.SurfaceTextureHigh = ParseString(parallaxBody, "surfaceTextureHigh");
            material.BumpMapMid = ParseString(parallaxBody, "surfaceBumpMapMid");
            material.BumpMapHigh = ParseString(parallaxBody, "surfaceBumpMapHigh");
            material.BumpMapSteep = ParseString(parallaxBody, "surfaceBumpMapSteep");
            material.LowStart = ParseFloat(parallaxBody, "lowStart");
            material.LowEnd = ParseFloat(parallaxBody, "lowEnd");
            material.HighStart = ParseFloat(parallaxBody, "highStart");
            material.HighEnd = ParseFloat(parallaxBody, "highEnd");
            material.PlanetName = bodyName;
            material.InfluenceMap = ParseString(parallaxBody, "influenceMap");
            material.FogTexture = ParseString(parallaxBody, "fogTexture");
            material.FogRange = ParseFloat(parallaxBody, "fogRange");
            material.UseReflections = ParseBool(parallaxBody, "hasWater");
            material.ReflectionMask = ParseVector(parallaxBody, "reflectionMask");
            material.DisplacementOffset = ParseFloat(parallaxBody, "displacementOffset");
            material.NormalSpecularInfluence = ParseFloat(parallaxBody, "normalSpecularInfluence");
            material.HasEmission = ParseBoolNumber(parallaxBody, "hasEmission");
            material.DetailTextureLow = ParseString(parallaxBody, "detailTexLow");
            material.DetailTextureMid = ParseString(parallaxBody, "detailTexMid");
            material.DetailTextureHigh = ParseString(parallaxBody, "detailTexHigh");
            material.DetailTextureSteepLow = ParseString(parallaxBody, "detailTexSteepLow");
            material.DetailTextureSteepMid = ParseString(parallaxBody, "detailTexSteepMid");
            material.DetailTextureSteepHigh = ParseString(parallaxBody, "detailTexSteepHigh");
            material.DetailNormalLow = ParseString(parallaxBody, "detailNormalLow");
            material.DetailNormalMid = ParseString(parallaxBody, "detailNormalMid");
            material.DetailNormalHigh = ParseString(parallaxBody, "detailNormalHigh");
            material.DetailNormalSteepLow = ParseString(parallaxBody, "detailNormalSteepLow");
            material.DetailNormalSteepMid = ParseString(parallaxBody, "detailNormalSteepMid");
            material.DetailNormalSteepHigh = ParseString(parallaxBody, "detailNormalSteepHigh");
            material.DetailScaleLow = ParseFloat(parallaxBody, "detailLowScale");
            material.DetailScaleMid = ParseFloat(parallaxBody, "detailMidScale");
            material.DetailScaleHigh = ParseFloat(parallaxBody, "detailHighScale");
            material.DetailRange = ParseFloat(parallaxBody, "detailRange");
            material.DetailPower = ParseFloat(parallaxBody, "detailPower");
            material.DetailOffset = ParseFloat(parallaxBody, "detailOffset");
            material.DetailScaleSteepLow = ParseFloat(parallaxBody, "detailScaleSteepLow");
            material.DetailScaleSteepMid = ParseFloat(parallaxBody, "detailScaleSteepMid");
            material.DetailScaleSteepHigh = ParseFloat(parallaxBody, "detailScaleSteepHigh");
            material.PhysicsTexDisplacement = ParseString(parallaxBody, "physicsTexDisplacement");
            material.ReversedNormal = ParseBoolNumber(parallaxBody, "useReversedNormals");
            material.DetailSteepPower = ParseFloat(parallaxBody, "detailSteepPower");
            string color = ParseString(parallaxBody, "tintColor"); //it pains me to write colour this way as a brit
            material.TintColor = new Color(float.Parse(color.Split(',')[0]), float.Parse(color.Split(',')[1]), float.Parse(color.Split(',')[2]));
            
            color = ParseString(parallaxBody, "emissionColor");
            if (color != null)
            {
                material.EmissionColor = new Color(float.Parse(color.Split(',')[0]), float.Parse(color.Split(',')[1]), float.Parse(color.Split(',')[2]));
            }

            return material;

        }
        public string ParseString(ConfigNode parallaxBody, string value)
        {
            string output = "";
            try
            {
                output = parallaxBody.GetValue(value);
                if (output == null)
                {
                    Log("Notice: " + value + " was not assigned - Returning 0,0,0");
                    return "0,0,0";
                }
            }
            catch
            {
                Debug.Log(parallaxBody.name + " / " + value + " was not assigned");
                output = "Unused_Texture";
            }
            return output;
        }
        public float ParseFloat(ConfigNode parallaxBody, string value)
        {
            string output = "";
            float realOutput = 0;
            try
            {
                output = parallaxBody.GetValue(value);
                if (output == null)
                {
                    Log("Notice: " + value + " was not assigned - Returning 0");
                    return 0;
                }
            }
            catch
            {
                Debug.Log(value + " was not assigned");
                output = "Unused_Texture";
                return realOutput;
            }
            try
            {
               
                realOutput = float.Parse(output);
            }
            catch
            {
                Debug.Log("Critical error: Input was a string, but it should have been a float: " + parallaxBody.name + " / " + value);
            }
            return realOutput;
        }
        public bool ParseBool(ConfigNode parallaxBody, string value)
        {
            string output = "";
            bool realOutput = false;
            try
            {
                output = parallaxBody.GetValue(value);
                if (output == null)
                {
                    Log("Notice: " + value + " was not assigned - Returning false");
                    return false;
                }
            }
            catch
            {
                Debug.Log(value + " was not assigned");
                output = "Unused_Texture";
            }
            if (output.ToString().ToLower() == "true")
            {
                realOutput = true;
            }
            else
            {
                realOutput = false;
            }
            return realOutput;
        }
        public int ParseBoolNumber(ConfigNode parallaxBody, string value)
        {
            string output = "";
            int realOutput = 0;
            try
            {
                output = parallaxBody.GetValue(value);
                if (output == null)
                {
                    Log("Notice: " + value + " was not assigned - Returning 0 (false)");
                    return 0;
                }
            }
            catch
            {
                Debug.Log(value + " was not assigned");
                output = "Unused_Texture";
            }
            if (output.ToString().ToLower() == "true")
            {
                realOutput = 1;
            }
            else
            {
                realOutput = 0;
            }
            return realOutput;
        }
        public Vector4 ParseVector(ConfigNode parallaxBody, string value)
        {
            string output = "";
            Vector4 realOutput = new Vector4(0, 0, 0, 0);
            try
            {
                output = parallaxBody.GetValue(value);
                if (output == null)
                {
                    Log("Notice: " + value + " was not assigned - Returning Vector3(0, 0, 0)");
                    return Vector3.zero;
                }
                else
                {
                    float[] values = new float[4];
                    string[] data = output.ToLower().Replace(" ", string.Empty).Split(',');
                    for (int i = 0; i < 4; i++)
                    {
                        values[i] = float.Parse(data[i]);
                    }
                    realOutput = new Vector4(values[0], values[1], values[2], values[3]);
                    return realOutput;
                }
            }
            catch
            {
                Debug.Log("Error parsing vector4: " + value);
                return realOutput;
            }
        }
        public static void Log(string message)
        {
            Debug.Log("[Parallax]" + message);
        }

    }
    public class PhysicsTexHolder
    {
        public static Texture2D physicsTexLow;
        public static Texture2D physicsTexMid;
        public static Texture2D physicsTexHigh;
        public static Texture2D physicsTexSteep;
        public static Texture2D displacementTex;
    }
    public class ParallaxBody
    {
        private string bodyName;
        private ParallaxBodyMaterial parallaxBodyMaterial;
        public Texture2DArray detailTexturesLower;   //Texture2D array passed to the shader as a sampler2darray declared by Unity - Bypass the sampler limit
        public Texture2DArray detailTexturesUpper;
        private Material parallaxMaterial;
        private Material parallaxMaterialSINGLELOW;
        private Material parallaxMaterialSINGLEMID;
        private Material parallaxMaterialSINGLEHIGH;

        private Material parallaxMaterialSINGLESTEEPLOW;
        private Material parallaxMaterialSINGLESTEEPMID;
        private Material parallaxMaterialSINGLESTEEPHIGH;

        private Material parallaxMaterialDOUBLELOW;
        private Material parallaxMaterialDOUBLEHIGH;
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
            Material[] materials = parallaxBodyMaterial.CreateMaterial();
            parallaxMaterial = materials[0];
            parallaxMaterialSINGLELOW = materials[1];
            parallaxMaterialSINGLEMID = materials[2];
            parallaxMaterialSINGLEHIGH = materials[3];

            parallaxMaterialSINGLESTEEPLOW = materials[4];
            parallaxMaterialSINGLESTEEPMID = materials[5];
            parallaxMaterialSINGLESTEEPHIGH = materials[6];

            parallaxMaterialDOUBLELOW = materials[7];
            parallaxMaterialDOUBLEHIGH = materials[8];
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
    public class ParallaxBodyMaterial : MonoBehaviour
    {
        private Material parallaxMaterial;
        private Material parallaxMaterialSINGLELOW;
        private Material parallaxMaterialSINGLEMID;
        private Material parallaxMaterialSINGLEHIGH;
        private Material parallaxMaterialSINGLESTEEPLOW;
        private Material parallaxMaterialSINGLESTEEPMID;
        private Material parallaxMaterialSINGLESTEEPHIGH;
        private Material parallaxMaterialDOUBLELOW;
        private Material parallaxMaterialDOUBLEHIGH;

        private string planetName;
        private string surfaceTexture;
        private string surfaceTextureParallaxMap;
        private string surfaceTextureBumpMap;
        private string steepTexture;

        private float steepPower;
        private float surfaceTextureScale;
        private float surfaceParallaxHeight;
        private float displacementOffset;
        private float steepTextureScale;
        private float smoothness;
        private Color tintColor;
        private float normalSpecularInfluence;

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
        private string influenceMap;
        private string fogTexture;
        private float fogRange;

        private string detailTextureLow;
        private string detailTextureMid;
        private string detailTextureHigh;
        private string detailNormalLow;
        private string detailNormalMid;
        private string detailNormalHigh;
        private float detailPower;
        private float detailOffset;
        private float detailRange;
        private float detailScaleLow;
        private float detailScaleMid;
        private float detailScaleHigh;
        private string detailTextureSteepLow;
        private string detailTextureSteepMid;
        private string detailTextureSteepHigh;
        private string detailNormalSteepLow;
        private string detailNormalSteepMid;
        private string detailNormalSteepHigh;
        private float detailScaleSteepLow;
        private float detailScaleSteepMid;
        private float detailScaleSteepHigh;
        private int detailResolution;
        private string physicsTexLow;
        private string physicsTexMid;
        private string physicsTexHigh;
        private string physicsTexSteep;
        private string physicsTexDisplacement;
        private int reversedNormal;
        private float detailSteepPower;

        private bool useReflections = false;
        private Vector4 reflectionMask = new Vector4(0, 0, 0, 0);

        private int hasEmission = 0;
        private Color emissionColor = new Color(0, 0, 0);

        #region getsets
        public float DetailSteepPower
        {
            get { return detailSteepPower; }
            set { detailSteepPower = value; }
        }
        public int ReversedNormal
        {
            get { return reversedNormal; }
            set { reversedNormal = value; }
        }
        public string PhysicsTexDisplacement
        {
            get { return physicsTexDisplacement; }
            set { physicsTexDisplacement = value; }
        }
        public float DetailScaleSteepLow
        {
            get { return detailScaleSteepLow; }
            set { detailScaleSteepLow = value; }
        }
        public float DetailScaleSteepMid
        {
            get { return detailScaleSteepMid; }
            set { detailScaleSteepMid = value; }
        }
        public float DetailScaleSteepHigh
        {
            get { return detailScaleSteepHigh; }
            set { detailScaleSteepHigh = value; }
        }
        public string DetailTextureSteepLow
        {
            get { return detailTextureSteepLow; }
            set { detailTextureSteepLow = value; }
        }
        public string DetailTextureSteepMid
        {
            get { return detailTextureSteepMid; }
            set { detailTextureSteepMid = value; }
        }
        public string DetailTextureSteepHigh
        {
            get { return detailTextureSteepHigh; }
            set { detailTextureSteepHigh = value; }
        }
        public string DetailNormalSteepLow
        {
            get { return detailNormalSteepLow; }
            set { detailNormalSteepLow = value; }
        }
        public string DetailNormalSteepMid
        {
            get { return detailNormalSteepMid; }
            set { detailNormalSteepMid = value; }
        }
        public string DetailNormalSteepHigh
        {
            get { return detailNormalSteepHigh; }
            set { detailNormalSteepHigh = value; }
        }
        public string DetailTextureLow
        {
            get { return detailTextureLow; }
            set { detailTextureLow = value; }
        }
        public string DetailTextureMid
        {
            get { return detailTextureMid; }
            set { detailTextureMid = value; }
        }
        public string DetailTextureHigh
        {
            get { return detailTextureHigh; }
            set { detailTextureHigh = value; }
        }
        public string DetailNormalLow
        {
            get { return detailNormalLow; }
            set { detailNormalLow = value; }
        }
        public string DetailNormalMid
        {
            get { return detailNormalMid; }
            set { detailNormalMid = value; }
        }
        public string DetailNormalHigh
        {
            get { return detailNormalHigh; }
            set { detailNormalHigh = value; }
        }
        public float DetailPower
        {
            get { return detailPower; }
            set { detailPower = value; }
        }
        public float DetailScaleLow
        {
            get { return detailScaleLow; }
            set { detailScaleLow = value; }
        }
        public float DetailScaleMid
        {
            get { return detailScaleMid; }
            set { detailScaleMid = value; }
        }
        public float DetailScaleHigh
        {
            get { return detailScaleHigh; }
            set { detailScaleHigh = value; }
        }
        public float DetailRange
        {
            get { return detailRange; }
            set { detailRange = value; }
        }
        public float DetailOffset
        {
            get { return detailOffset; }
            set { detailOffset = value; }
        }
        public Color EmissionColor
        {
            get { return emissionColor; }
            set { emissionColor = value; }
        }
        public int HasEmission
        {
            get { return hasEmission; }
            set { hasEmission = value; }
        }
        public float NormalSpecularInfluence
        {
            get { return normalSpecularInfluence; }
            set { normalSpecularInfluence = value; }
        }
        public float DisplacementOffset
        {
            get { return displacementOffset; }
            set { displacementOffset = value; }
        }
        public Vector4 ReflectionMask
        {
            get { return reflectionMask; }
            set { reflectionMask = value; }
        }
        public bool UseReflections
        {
            get { return useReflections; }
            set { useReflections = value; }
        }
        public float FogRange
        {
            get { return fogRange; }
            set { fogRange = value; }
        }
        public string FogTexture
        {
            get { return fogTexture; }
            set { fogTexture = value; }
        }
        public string InfluenceMap
        {
            get { return influenceMap; }
            set { influenceMap = value; }
        }
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
        public Material ParallaxMaterialSINGLELOW
        {
            get { return parallaxMaterialSINGLELOW; }
            set { parallaxMaterialSINGLELOW = value; }
        }
        public Material ParallaxMaterialSINGLEMID
        {
            get { return parallaxMaterialSINGLEMID; }
            set { parallaxMaterialSINGLEMID = value; }
        }
        public Material ParallaxMaterialSINGLEHIGH
        {
            get { return parallaxMaterialSINGLEHIGH; }
            set { parallaxMaterialSINGLEHIGH = value; }
        }
        public Material ParallaxMaterialSINGLESTEEPLOW
        {
            get { return parallaxMaterialSINGLESTEEPLOW; }
            set { parallaxMaterialSINGLESTEEPLOW = value; }
        }
        public Material ParallaxMaterialSINGLESTEEPMID
        {
            get { return parallaxMaterialSINGLESTEEPMID; }
            set { parallaxMaterialSINGLESTEEPMID = value; }
        }
        public Material ParallaxMaterialSINGLESTEEPHIGH
        {
            get { return parallaxMaterialSINGLESTEEPHIGH; }
            set { parallaxMaterialSINGLESTEEPHIGH = value; }
        }
        public Material ParallaxMaterialDOUBLELOW
        {
            get { return parallaxMaterialDOUBLELOW; }
            set { parallaxMaterialDOUBLELOW = value; }
        }
        public Material ParallaxMaterialDOUBLEHIGH
        {
            get { return parallaxMaterialDOUBLEHIGH; }
            set { parallaxMaterialDOUBLEHIGH = value; }
        }
        #endregion
        public void EnableKeywordFromInteger(Material material, int value, string keywordON, string keywordOFF)
        {
            if (value == 1)
            {
                material.EnableKeyword(keywordON);
                material.DisableKeyword(keywordOFF);
            }
            else
            {
                material.EnableKeyword(keywordOFF);
                material.DisableKeyword(keywordON);
            }
        }
        public Material[] CreateMaterial()    //Does the shit ingame and stuffs
        {
            Log("Beginning material creation");
            Material[] output = new Material[9];
            parallaxMaterial = new Material(ParallaxLoader.GetShader("Custom/ParallaxFULL"));
            parallaxMaterialSINGLELOW = new Material(ParallaxLoader.GetShader("Custom/ParallaxSINGLE"));
            parallaxMaterialSINGLEMID = new Material(ParallaxLoader.GetShader("Custom/ParallaxSINGLE"));
            parallaxMaterialSINGLEHIGH = new Material(ParallaxLoader.GetShader("Custom/ParallaxSINGLE"));
            parallaxMaterialSINGLESTEEPLOW = new Material(ParallaxLoader.GetShader("Custom/ParallaxSINGLESTEEP"));
            parallaxMaterialSINGLESTEEPMID = new Material(ParallaxLoader.GetShader("Custom/ParallaxSINGLESTEEP"));
            parallaxMaterialSINGLESTEEPHIGH = new Material(ParallaxLoader.GetShader("Custom/ParallaxSINGLESTEEP"));
            parallaxMaterialDOUBLELOW = new Material(ParallaxLoader.GetShader("Custom/ParallaxDOUBLELOW"));
            parallaxMaterialDOUBLEHIGH = new Material(ParallaxLoader.GetShader("Custom/ParallaxDOUBLEHIGH"));
            //if (HighLogic.LoadedScene == GameScenes.FLIGHT)
            //{
            //    parallaxMaterial = FlightGlobals.currentMainBody.pqsController.surfaceMaterial;
            //    parallaxMaterial.shader = ParallaxLoader.GetShader("Custom/ParallaxOcclusion");
            //} //Not needed any more


            //influenceMap = "BeyondHome/Terrain/DDS/BlankAlpha";

            //parallaxMaterial.SetTexture("_SurfaceTexture", LoadTexture(surfaceTexture));
            //parallaxMaterial.SetTexture("_DispTex", LoadTexture(surfaceTextureParallaxMap));
            //parallaxMaterial.SetTexture("_BumpMap", LoadTexture(surfaceTextureBumpMap));
            //parallaxMaterial.SetTexture("_SteepTex", LoadTexture(steepTexture));
            parallaxMaterial.SetFloat("_SteepPower", steepPower);
            parallaxMaterial.SetTextureScale("_SurfaceTexture", CreateVector(surfaceTextureScale));
            parallaxMaterial.SetFloat("_displacement_scale", surfaceParallaxHeight);
            parallaxMaterial.SetTextureScale("_SteepTex", CreateVector(steepTextureScale));
            parallaxMaterial.SetFloat("_Metallic", smoothness);
            //parallaxMaterial.SetTexture("_SurfaceTextureMid", LoadTexture(surfaceTextureMid));
            //parallaxMaterial.SetTexture("_SurfaceTextureHigh", LoadTexture(surfaceTextureHigh));
            //parallaxMaterial.SetTexture("_BumpMapMid", LoadTexture(bumpMapMid));
            //parallaxMaterial.SetTexture("_BumpMapHigh", LoadTexture(bumpMapHigh));
            //parallaxMaterial.SetTexture("_BumpMapSteep", LoadTexture(bumpMapSteep));
            parallaxMaterial.SetFloat("_LowStart", lowStart);
            parallaxMaterial.SetFloat("_LowEnd", lowEnd);
            parallaxMaterial.SetFloat("_HighStart", highStart);
            parallaxMaterial.SetFloat("_HighEnd", highEnd);
            parallaxMaterial.SetFloat("_PlanetRadius", (float)FlightGlobals.GetBodyByName(planetName).Radius);
            parallaxMaterial.SetColor("_MetallicTint", tintColor);
            //parallaxMaterial.SetTexture("_InfluenceMap", LoadTexture(influenceMap));
            parallaxMaterial.SetFloat("_TessellationRange", 99);
            //parallaxMaterial.SetTexture("_FogTexture", LoadTexture(fogTexture));
            parallaxMaterial.SetFloat("_FogRange", fogRange);
            parallaxMaterial.SetFloat("_displacement_offset", displacementOffset);
            parallaxMaterial.SetFloat("_NormalSpecularInfluence", normalSpecularInfluence);
            parallaxMaterial.SetFloat("_HasEmission", hasEmission);
            parallaxMaterial.SetColor("_EmissionColor", emissionColor);
            parallaxMaterial.SetFloat("_DetailSteepPow", detailSteepPower);
            if (useReflections)
            {
                parallaxMaterial.SetFloat("_UseReflections", 1);
                Log("_UseReflections", "true");
            }
            else
            {
                parallaxMaterial.SetFloat("_UseReflections", 0);
                Log("_UseReflections", "false");
            }

            parallaxMaterial.SetVector("_ReflectionMask", reflectionMask);
            parallaxMaterial.SetFloat("_TessellationEdgeLength", ParallaxSettings.tessellationEdgeLength);
            parallaxMaterial.SetFloat("_TessellationRange", ParallaxSettings.tessellationRange);
            parallaxMaterial.SetFloat("_TessellationMax", ParallaxSettings.tessellationMax);

            
            //PARALLAX SINGLE

            //parallaxMaterialSINGLELOW.SetTexture("_SurfaceTexture", LoadTexture(surfaceTexture));
            //parallaxMaterialSINGLELOW.SetTexture("_DispTex", LoadTexture(surfaceTextureParallaxMap));
            //parallaxMaterialSINGLELOW.SetTexture("_BumpMap", LoadTexture(surfaceTextureBumpMap));
            parallaxMaterialSINGLELOW.SetTextureScale("_SurfaceTexture", CreateVector(surfaceTextureScale));
            parallaxMaterialSINGLELOW.SetFloat("_displacement_scale", surfaceParallaxHeight);
            parallaxMaterialSINGLELOW.SetFloat("_Metallic", smoothness);
            parallaxMaterialSINGLELOW.SetFloat("_LowStart", lowStart);
            parallaxMaterialSINGLELOW.SetFloat("_LowEnd", lowEnd);
            parallaxMaterialSINGLELOW.SetFloat("_HighStart", highStart);
            parallaxMaterialSINGLELOW.SetFloat("_HighEnd", highEnd);
            parallaxMaterialSINGLELOW.SetFloat("_PlanetRadius", (float)FlightGlobals.GetBodyByName(planetName).Radius);
            parallaxMaterialSINGLELOW.SetColor("_MetallicTint", tintColor);
            parallaxMaterialSINGLELOW.SetFloat("_DetailSteepPow", detailSteepPower);
            //parallaxMaterialSINGLELOW.SetTexture("_InfluenceMap", LoadTexture(influenceMap));
            parallaxMaterialSINGLELOW.SetFloat("_TessellationRange", 99);
            //parallaxMaterialSINGLELOW.SetTexture("_FogTexture", LoadTexture(fogTexture));
            parallaxMaterialSINGLELOW.SetFloat("_FogRange", fogRange);
            parallaxMaterialSINGLELOW.SetFloat("_displacement_offset", displacementOffset);
            parallaxMaterialSINGLELOW.SetFloat("_NormalSpecularInfluence", normalSpecularInfluence);
            parallaxMaterialSINGLELOW.SetFloat("_HasEmission", hasEmission);
            parallaxMaterialSINGLELOW.SetColor("_EmissionColor", emissionColor);
            if (useReflections)
            {
                parallaxMaterialSINGLELOW.SetFloat("_UseReflections", 1);
                Log("_UseReflections", "true");
            }
            else
            {
                parallaxMaterialSINGLELOW.SetFloat("_UseReflections", 0);
                Log("_UseReflections", "false");
            }

            parallaxMaterialSINGLELOW.SetVector("_ReflectionMask", reflectionMask);
            parallaxMaterialSINGLELOW.SetFloat("_TessellationEdgeLength", ParallaxSettings.tessellationEdgeLength);
            parallaxMaterialSINGLELOW.SetFloat("_TessellationRange", ParallaxSettings.tessellationRange);
            parallaxMaterialSINGLELOW.SetFloat("_TessellationMax", ParallaxSettings.tessellationMax);

            parallaxMaterialSINGLELOW.SetFloat("_DetailPower", detailPower);
            parallaxMaterialSINGLELOW.SetFloat("_DetailOffset", detailOffset);
            parallaxMaterialSINGLELOW.SetFloat("_DetailRange", detailRange);
            parallaxMaterialSINGLELOW.SetTextureScale("_DetailTex", CreateVector(detailScaleLow));
            parallaxMaterialSINGLELOW.SetTextureScale("_DetailSteep", CreateVector(detailScaleSteepLow));
            //parallaxMaterialSINGLEMID.SetTexture("_SurfaceTexture", LoadTexture(surfaceTextureMid));
            //parallaxMaterialSINGLEMID.SetTexture("_DispTex", LoadTexture(surfaceTextureParallaxMap));
            //parallaxMaterialSINGLEMID.SetTexture("_BumpMap", LoadTexture(bumpMapMid));
            parallaxMaterialSINGLEMID.SetTextureScale("_SurfaceTexture", CreateVector(surfaceTextureScale));
            parallaxMaterialSINGLEMID.SetFloat("_displacement_scale", surfaceParallaxHeight);
            parallaxMaterialSINGLEMID.SetFloat("_Metallic", smoothness);
            parallaxMaterialSINGLEMID.SetFloat("_LowStart", lowStart);
            parallaxMaterialSINGLEMID.SetFloat("_LowEnd", lowEnd);
            parallaxMaterialSINGLEMID.SetFloat("_HighStart", highStart);
            parallaxMaterialSINGLEMID.SetFloat("_HighEnd", highEnd);
            parallaxMaterialSINGLEMID.SetFloat("_PlanetRadius", (float)FlightGlobals.GetBodyByName(planetName).Radius);
            parallaxMaterialSINGLEMID.SetColor("_MetallicTint", tintColor);
            //parallaxMaterialSINGLEMID.SetTexture("_InfluenceMap", LoadTexture(influenceMap));
            parallaxMaterialSINGLEMID.SetFloat("_TessellationRange", 99);
            //parallaxMaterialSINGLEMID.SetTexture("_FogTexture", LoadTexture(fogTexture));
            parallaxMaterialSINGLEMID.SetFloat("_FogRange", fogRange);
            parallaxMaterialSINGLEMID.SetFloat("_displacement_offset", displacementOffset);
            parallaxMaterialSINGLEMID.SetFloat("_NormalSpecularInfluence", normalSpecularInfluence);
            parallaxMaterialSINGLEMID.SetFloat("_HasEmission", hasEmission);
            parallaxMaterialSINGLEMID.SetColor("_EmissionColor", emissionColor);
            parallaxMaterialSINGLEMID.SetFloat("_DetailSteepPow", detailSteepPower);
            if (useReflections)
            {
                parallaxMaterialSINGLEMID.SetFloat("_UseReflections", 1);
                Log("_UseReflections", "true");
            }
            else
            {
                parallaxMaterialSINGLEMID.SetFloat("_UseReflections", 0);
                Log("_UseReflections", "false");
            }

            parallaxMaterialSINGLEMID.SetVector("_ReflectionMask", reflectionMask);
            parallaxMaterialSINGLEMID.SetFloat("_TessellationEdgeLength", ParallaxSettings.tessellationEdgeLength);
            parallaxMaterialSINGLEMID.SetFloat("_TessellationRange", ParallaxSettings.tessellationRange);
            parallaxMaterialSINGLEMID.SetFloat("_TessellationMax", ParallaxSettings.tessellationMax);

            parallaxMaterialSINGLEMID.SetFloat("_DetailPower", detailPower);
            parallaxMaterialSINGLEMID.SetFloat("_DetailOffset", detailOffset);
            parallaxMaterialSINGLEMID.SetFloat("_DetailRange", detailRange);
            parallaxMaterialSINGLEMID.SetTextureScale("_DetailTex", CreateVector(detailScaleMid));
            parallaxMaterialSINGLEMID.SetTextureScale("_DetailSteep", CreateVector(detailScaleSteepMid));
            //parallaxMaterialSINGLEHIGH.SetTexture("_SurfaceTexture", LoadTexture(surfaceTextureHigh));
            // parallaxMaterialSINGLEHIGH.SetTexture("_DispTex", LoadTexture(surfaceTextureParallaxMap));
            //parallaxMaterialSINGLEHIGH.SetTexture("_BumpMap", LoadTexture(bumpMapHigh));
            parallaxMaterialSINGLEHIGH.SetTextureScale("_SurfaceTexture", CreateVector(surfaceTextureScale));
            parallaxMaterialSINGLEHIGH.SetFloat("_displacement_scale", surfaceParallaxHeight);
            parallaxMaterialSINGLEHIGH.SetFloat("_Metallic", smoothness);
            parallaxMaterialSINGLEHIGH.SetFloat("_LowStart", lowStart);
            parallaxMaterialSINGLEHIGH.SetFloat("_LowEnd", lowEnd);
            parallaxMaterialSINGLEHIGH.SetFloat("_HighStart", highStart);
            parallaxMaterialSINGLEHIGH.SetFloat("_HighEnd", highEnd);
            parallaxMaterialSINGLEHIGH.SetFloat("_PlanetRadius", (float)FlightGlobals.GetBodyByName(planetName).Radius);
            parallaxMaterialSINGLEHIGH.SetColor("_MetallicTint", tintColor);
            //parallaxMaterialSINGLEHIGH.SetTexture("_InfluenceMap", LoadTexture(influenceMap));
            parallaxMaterialSINGLEHIGH.SetFloat("_TessellationRange", 99);
            //parallaxMaterialSINGLEHIGH.SetTexture("_FogTexture", LoadTexture(fogTexture));
            parallaxMaterialSINGLEHIGH.SetFloat("_FogRange", fogRange);
            parallaxMaterialSINGLEHIGH.SetFloat("_displacement_offset", displacementOffset);
            parallaxMaterialSINGLEHIGH.SetFloat("_NormalSpecularInfluence", normalSpecularInfluence);
            parallaxMaterialSINGLEHIGH.SetFloat("_HasEmission", hasEmission);
            parallaxMaterialSINGLEHIGH.SetColor("_EmissionColor", emissionColor);
            if (useReflections)
            {
                parallaxMaterialSINGLEHIGH.SetFloat("_UseReflections", 1);
                Log("_UseReflections", "true");
            }
            else
            {
                parallaxMaterialSINGLEHIGH.SetFloat("_UseReflections", 0);
                Log("_UseReflections", "false");
            }

            parallaxMaterialSINGLEHIGH.SetVector("_ReflectionMask", reflectionMask);
            parallaxMaterialSINGLEHIGH.SetFloat("_TessellationEdgeLength", ParallaxSettings.tessellationEdgeLength);
            parallaxMaterialSINGLEHIGH.SetFloat("_TessellationRange", ParallaxSettings.tessellationRange);
            parallaxMaterialSINGLEHIGH.SetFloat("_TessellationMax", ParallaxSettings.tessellationMax);
            parallaxMaterialSINGLEHIGH.SetFloat("_DetailSteepPow", detailSteepPower);
            parallaxMaterialSINGLEHIGH.SetFloat("_DetailPower", detailPower);
            parallaxMaterialSINGLEHIGH.SetFloat("_DetailOffset", detailOffset);
            parallaxMaterialSINGLEHIGH.SetFloat("_DetailRange", detailRange);
            parallaxMaterialSINGLEHIGH.SetTextureScale("_DetailTex", CreateVector(detailScaleHigh));
            parallaxMaterialSINGLEHIGH.SetTextureScale("_DetailSteep", CreateVector(detailScaleSteepHigh));
            //PARALLAX SINGLESTEEP

            //parallaxMaterialSINGLESTEEPLOW.SetTexture("_SurfaceTexture", LoadTexture(surfaceTexture));
            //parallaxMaterialSINGLESTEEPLOW.SetTexture("_DispTex", LoadTexture(surfaceTextureParallaxMap));
            //parallaxMaterialSINGLESTEEPLOW.SetTexture("_BumpMap", LoadTexture(surfaceTextureBumpMap));
            //parallaxMaterialSINGLESTEEPLOW.SetTexture("_SteepTex", LoadTexture(steepTexture));
            parallaxMaterialSINGLESTEEPLOW.SetFloat("_SteepPower", steepPower);
            parallaxMaterialSINGLESTEEPLOW.SetTextureScale("_SurfaceTexture", CreateVector(surfaceTextureScale));
            parallaxMaterialSINGLESTEEPLOW.SetFloat("_displacement_scale", surfaceParallaxHeight);
            parallaxMaterialSINGLESTEEPLOW.SetTextureScale("_SteepTex", CreateVector(steepTextureScale));
            parallaxMaterialSINGLESTEEPLOW.SetFloat("_Metallic", smoothness);
            //parallaxMaterialSINGLESTEEPLOW.SetTexture("_BumpMapSteep", LoadTexture(bumpMapSteep));
            parallaxMaterialSINGLESTEEPLOW.SetFloat("_LowStart", lowStart);
            parallaxMaterialSINGLESTEEPLOW.SetFloat("_LowEnd", lowEnd);
            parallaxMaterialSINGLESTEEPLOW.SetFloat("_HighStart", highStart);
            parallaxMaterialSINGLESTEEPLOW.SetFloat("_HighEnd", highEnd);
            parallaxMaterialSINGLESTEEPLOW.SetFloat("_PlanetRadius", (float)FlightGlobals.GetBodyByName(planetName).Radius);
            parallaxMaterialSINGLESTEEPLOW.SetColor("_MetallicTint", tintColor);
            //parallaxMaterialSINGLESTEEPLOW.SetTexture("_InfluenceMap", LoadTexture(influenceMap));
            parallaxMaterialSINGLESTEEPLOW.SetFloat("_TessellationRange", 99);
            //parallaxMaterialSINGLESTEEPLOW.SetTexture("_FogTexture", LoadTexture(fogTexture));
            parallaxMaterialSINGLESTEEPLOW.SetFloat("_FogRange", fogRange);
            parallaxMaterialSINGLESTEEPLOW.SetFloat("_displacement_offset", displacementOffset);
            ParallaxMaterialSINGLESTEEPLOW.SetFloat("_NormalSpecularInfluence", normalSpecularInfluence);
            parallaxMaterialSINGLESTEEPLOW.SetFloat("_HasEmission", hasEmission);
            parallaxMaterialSINGLESTEEPLOW.SetColor("_EmissionColor", emissionColor);
            if (useReflections)
            {
                parallaxMaterialSINGLESTEEPLOW.SetFloat("_UseReflections", 1);
                Log("_UseReflections", "true");
            }
            else
            {
                parallaxMaterialSINGLESTEEPLOW.SetFloat("_UseReflections", 0);
                Log("_UseReflections", "false");
            }
            parallaxMaterialSINGLESTEEPLOW.SetFloat("_DetailSteepPow", detailSteepPower);
            parallaxMaterialSINGLESTEEPLOW.SetVector("_ReflectionMask", reflectionMask);
            parallaxMaterialSINGLESTEEPLOW.SetFloat("_TessellationEdgeLength", ParallaxSettings.tessellationEdgeLength);
            parallaxMaterialSINGLESTEEPLOW.SetFloat("_TessellationRange", ParallaxSettings.tessellationRange);
            parallaxMaterialSINGLESTEEPLOW.SetFloat("_TessellationMax", ParallaxSettings.tessellationMax);
            parallaxMaterialSINGLESTEEPLOW.SetFloat("_DetailPower", detailPower);
            parallaxMaterialSINGLESTEEPLOW.SetFloat("_DetailOffset", detailOffset);
            parallaxMaterialSINGLESTEEPLOW.SetFloat("_DetailRange", detailRange);
            parallaxMaterialSINGLESTEEPLOW.SetTextureScale("_DetailTex", CreateVector(detailScaleLow));
            parallaxMaterialSINGLESTEEPLOW.SetTextureScale("_DetailSteep", CreateVector(detailScaleSteepLow));
            //parallaxMaterialSINGLESTEEPMID.SetTexture("_SurfaceTexture", LoadTexture(surfaceTextureMid));
            // parallaxMaterialSINGLESTEEPMID.SetTexture("_DispTex", LoadTexture(surfaceTextureParallaxMap));
            //parallaxMaterialSINGLESTEEPMID.SetTexture("_BumpMap", LoadTexture(bumpMapMid));
            //parallaxMaterialSINGLESTEEPMID.SetTexture("_SteepTex", LoadTexture(steepTexture));
            parallaxMaterialSINGLESTEEPMID.SetFloat("_SteepPower", steepPower);
            parallaxMaterialSINGLESTEEPMID.SetTextureScale("_SurfaceTexture", CreateVector(surfaceTextureScale));
            parallaxMaterialSINGLESTEEPMID.SetFloat("_displacement_scale", surfaceParallaxHeight);
            parallaxMaterialSINGLESTEEPMID.SetTextureScale("_SteepTex", CreateVector(steepTextureScale));
            parallaxMaterialSINGLESTEEPMID.SetFloat("_Metallic", smoothness);
            parallaxMaterialSINGLESTEEPMID.SetFloat("_DetailSteepPow", detailSteepPower);
            //parallaxMaterialSINGLESTEEPMID.SetTexture("_BumpMapSteep", LoadTexture(bumpMapSteep));
            parallaxMaterialSINGLESTEEPMID.SetFloat("_LowStart", lowStart);
            parallaxMaterialSINGLESTEEPMID.SetFloat("_LowEnd", lowEnd);
            parallaxMaterialSINGLESTEEPMID.SetFloat("_HighStart", highStart);
            parallaxMaterialSINGLESTEEPMID.SetFloat("_HighEnd", highEnd);
            parallaxMaterialSINGLESTEEPMID.SetFloat("_PlanetRadius", (float)FlightGlobals.GetBodyByName(planetName).Radius);
            parallaxMaterialSINGLESTEEPMID.SetColor("_MetallicTint", tintColor);
            //parallaxMaterialSINGLESTEEPMID.SetTexture("_InfluenceMap", LoadTexture(influenceMap));
            parallaxMaterialSINGLESTEEPMID.SetFloat("_TessellationRange", 99);
            //parallaxMaterialSINGLESTEEPMID.SetTexture("_FogTexture", LoadTexture(fogTexture));
            parallaxMaterialSINGLESTEEPMID.SetFloat("_FogRange", fogRange);
            parallaxMaterialSINGLESTEEPMID.SetFloat("_displacement_offset", displacementOffset);
            parallaxMaterialSINGLESTEEPMID.SetFloat("_NormalSpecularInfluence", normalSpecularInfluence);
            ParallaxMaterialSINGLESTEEPMID.SetFloat("_HasEmission", hasEmission);
            ParallaxMaterialSINGLESTEEPMID.SetColor("_EmissionColor", emissionColor);
            if (useReflections)
            {
                parallaxMaterialSINGLESTEEPMID.SetFloat("_UseReflections", 1);
                Log("_UseReflections", "true");
            }
            else
            {
                parallaxMaterialSINGLESTEEPMID.SetFloat("_UseReflections", 0);
                Log("_UseReflections", "false");
            }

            parallaxMaterialSINGLESTEEPMID.SetVector("_ReflectionMask", reflectionMask);
            parallaxMaterialSINGLESTEEPMID.SetFloat("_TessellationEdgeLength", ParallaxSettings.tessellationEdgeLength);
            parallaxMaterialSINGLESTEEPMID.SetFloat("_TessellationRange", ParallaxSettings.tessellationRange);
            parallaxMaterialSINGLESTEEPMID.SetFloat("_TessellationMax", ParallaxSettings.tessellationMax);

            parallaxMaterialSINGLESTEEPMID.SetFloat("_DetailPower", detailPower);
            parallaxMaterialSINGLESTEEPMID.SetFloat("_DetailOffset", detailOffset);
            parallaxMaterialSINGLESTEEPMID.SetFloat("_DetailRange", detailRange);
            parallaxMaterialSINGLESTEEPMID.SetTextureScale("_DetailTex", CreateVector(detailScaleMid));
            parallaxMaterialSINGLESTEEPMID.SetTextureScale("_DetailSteep", CreateVector(detailScaleSteepMid));
            //parallaxMaterialSINGLESTEEPHIGH.SetTexture("_SurfaceTexture", LoadTexture(surfaceTextureHigh));
            //parallaxMaterialSINGLESTEEPHIGH.SetTexture("_DispTex", LoadTexture(surfaceTextureParallaxMap));
            //parallaxMaterialSINGLESTEEPHIGH.SetTexture("_BumpMap", LoadTexture(bumpMapHigh));
            //parallaxMaterialSINGLESTEEPHIGH.SetTexture("_SteepTex", LoadTexture(steepTexture));
            parallaxMaterialSINGLESTEEPHIGH.SetFloat("_SteepPower", steepPower);
            parallaxMaterialSINGLESTEEPHIGH.SetTextureScale("_SurfaceTexture", CreateVector(surfaceTextureScale));
            parallaxMaterialSINGLESTEEPHIGH.SetFloat("_DetailSteepPow", detailSteepPower);
            parallaxMaterialSINGLESTEEPHIGH.SetFloat("_displacement_scale", surfaceParallaxHeight);
            parallaxMaterialSINGLESTEEPHIGH.SetTextureScale("_SteepTex", CreateVector(steepTextureScale));
            parallaxMaterialSINGLESTEEPHIGH.SetFloat("_Metallic", smoothness);
            //parallaxMaterialSINGLESTEEPHIGH.SetTexture("_BumpMapSteep", LoadTexture(bumpMapSteep));
            parallaxMaterialSINGLESTEEPHIGH.SetFloat("_LowStart", lowStart);
            parallaxMaterialSINGLESTEEPHIGH.SetFloat("_LowEnd", lowEnd);
            parallaxMaterialSINGLESTEEPHIGH.SetFloat("_HighStart", highStart);
            parallaxMaterialSINGLESTEEPHIGH.SetFloat("_HighEnd", highEnd);
            parallaxMaterialSINGLESTEEPHIGH.SetFloat("_PlanetRadius", (float)FlightGlobals.GetBodyByName(planetName).Radius);
            parallaxMaterialSINGLESTEEPHIGH.SetColor("_MetallicTint", tintColor);
            //parallaxMaterialSINGLESTEEPHIGH.SetTexture("_InfluenceMap", LoadTexture(influenceMap));
            parallaxMaterialSINGLESTEEPHIGH.SetFloat("_TessellationRange", 99);
           // parallaxMaterialSINGLESTEEPHIGH.SetTexture("_FogTexture", LoadTexture(fogTexture));
            parallaxMaterialSINGLESTEEPHIGH.SetFloat("_FogRange", fogRange);
            parallaxMaterialSINGLESTEEPHIGH.SetFloat("_displacement_offset", displacementOffset);
            parallaxMaterialSINGLESTEEPHIGH.SetFloat("_NormalSpecularInfluence", normalSpecularInfluence);
            ParallaxMaterialSINGLESTEEPHIGH.SetFloat("_HasEmission", hasEmission);
            ParallaxMaterialSINGLESTEEPHIGH.SetColor("_EmissionColor", emissionColor);
            if (useReflections)
            {
                parallaxMaterialSINGLESTEEPHIGH.SetFloat("_UseReflections", 1);
                Log("_UseReflections", "true");
            }
            else
            {
                parallaxMaterialSINGLESTEEPHIGH.SetFloat("_UseReflections", 0);
                Log("_UseReflections", "false");
            }

            parallaxMaterialSINGLESTEEPHIGH.SetVector("_ReflectionMask", reflectionMask);
            parallaxMaterialSINGLESTEEPHIGH.SetFloat("_TessellationEdgeLength", ParallaxSettings.tessellationEdgeLength);
            parallaxMaterialSINGLESTEEPHIGH.SetFloat("_TessellationRange", ParallaxSettings.tessellationRange);
            parallaxMaterialSINGLESTEEPHIGH.SetFloat("_TessellationMax", ParallaxSettings.tessellationMax);

            parallaxMaterialSINGLESTEEPHIGH.SetFloat("_DetailPower", detailPower);
            parallaxMaterialSINGLESTEEPHIGH.SetFloat("_DetailOffset", detailOffset);
            parallaxMaterialSINGLESTEEPHIGH.SetFloat("_DetailRange", detailRange);
            parallaxMaterialSINGLESTEEPHIGH.SetTextureScale("_DetailTex", CreateVector(detailScaleHigh));
            parallaxMaterialSINGLESTEEPHIGH.SetTextureScale("_DetailSteep", CreateVector(detailScaleSteepHigh));
            //PARALLAX DOUBLE

            //parallaxMaterialDOUBLELOW.SetTexture("_SurfaceTextureLower", LoadTexture(surfaceTexture));
            //parallaxMaterialDOUBLELOW.SetTexture("_DispTex", LoadTexture(surfaceTextureParallaxMap));
            //parallaxMaterialDOUBLELOW.SetTexture("_BumpMapLower", LoadTexture(surfaceTextureBumpMap));
            //parallaxMaterialDOUBLELOW.SetTexture("_SteepTex", LoadTexture(steepTexture));
            parallaxMaterialDOUBLELOW.SetFloat("_DetailSteepPow", detailSteepPower);
            parallaxMaterialDOUBLELOW.SetFloat("_SteepPower", steepPower);
            parallaxMaterialDOUBLELOW.SetTextureScale("_SurfaceTextureLower", CreateVector(surfaceTextureScale));
            parallaxMaterialDOUBLELOW.SetFloat("_displacement_scale", surfaceParallaxHeight);
            parallaxMaterialDOUBLELOW.SetTextureScale("_SteepTex", CreateVector(steepTextureScale));
            parallaxMaterialDOUBLELOW.SetFloat("_Metallic", smoothness);
            //parallaxMaterialDOUBLELOW.SetTexture("_SurfaceTextureHigher", LoadTexture(surfaceTextureMid));
            //parallaxMaterialDOUBLELOW.SetTexture("_BumpMapHigher", LoadTexture(BumpMapMid));
            //parallaxMaterialDOUBLELOW.SetTexture("_BumpMapSteep", LoadTexture(bumpMapSteep));
            parallaxMaterialDOUBLELOW.SetFloat("_LowStart", lowStart);
            parallaxMaterialDOUBLELOW.SetFloat("_LowEnd", lowEnd);
            parallaxMaterialDOUBLELOW.SetFloat("_HighStart", highStart);
            parallaxMaterialDOUBLELOW.SetFloat("_HighEnd", highEnd);
            parallaxMaterialDOUBLELOW.SetFloat("_PlanetRadius", (float)FlightGlobals.GetBodyByName(planetName).Radius);
            parallaxMaterialDOUBLELOW.SetColor("_MetallicTint", tintColor);
            //parallaxMaterialDOUBLELOW.SetTexture("_InfluenceMap", LoadTexture(influenceMap));
            parallaxMaterialDOUBLELOW.SetFloat("_TessellationRange", 99);
            //parallaxMaterialDOUBLELOW.SetTexture("_FogTexture", LoadTexture(fogTexture));
            parallaxMaterialDOUBLELOW.SetFloat("_FogRange", fogRange);
            parallaxMaterialDOUBLELOW.SetFloat("_displacement_offset", displacementOffset);
            ParallaxMaterialDOUBLELOW.SetFloat("_NormalSpecularInfluence", normalSpecularInfluence);
            parallaxMaterialDOUBLELOW.SetFloat("_HasEmission", hasEmission);
            parallaxMaterialDOUBLELOW.SetColor("_EmissionColor", emissionColor);
            if (useReflections)
            {
                parallaxMaterialDOUBLELOW.SetFloat("_UseReflections", 1);
                Log("_UseReflections", "true");
            }
            else
            {
                parallaxMaterialDOUBLELOW.SetFloat("_UseReflections", 0);
                Log("_UseReflections", "false");
            }

            parallaxMaterialDOUBLELOW.SetVector("_ReflectionMask", reflectionMask);
            parallaxMaterialDOUBLELOW.SetFloat("_TessellationEdgeLength", ParallaxSettings.tessellationEdgeLength);
            parallaxMaterialDOUBLELOW.SetFloat("_TessellationRange", ParallaxSettings.tessellationRange);
            parallaxMaterialDOUBLELOW.SetFloat("_TessellationMax", ParallaxSettings.tessellationMax);
            parallaxMaterialDOUBLELOW.SetFloat("_DetailPower", detailPower);
            parallaxMaterialDOUBLELOW.SetFloat("_DetailOffset", detailOffset);
            parallaxMaterialDOUBLELOW.SetFloat("_DetailRange", detailRange);
            parallaxMaterialDOUBLELOW.SetTextureScale("_DetailTexLower", CreateVector(detailScaleLow));
            parallaxMaterialDOUBLELOW.SetTextureScale("_DetailSteepLower", CreateVector(detailScaleSteepLow));
            parallaxMaterialDOUBLELOW.SetTextureScale("_DetailTexHigher", CreateVector(detailScaleMid));
            parallaxMaterialDOUBLELOW.SetTextureScale("_DetailSteepHigher", CreateVector(detailScaleSteepMid));
            //parallaxMaterialDOUBLEHIGH.SetTexture("_SurfaceTextureLower", LoadTexture(surfaceTextureMid));
            //parallaxMaterialDOUBLEHIGH.SetTexture("_DispTex", LoadTexture(surfaceTextureParallaxMap));
            //parallaxMaterialDOUBLEHIGH.SetTexture("_BumpMapLower", LoadTexture(bumpMapMid));
            //parallaxMaterialDOUBLEHIGH.SetTexture("_SteepTex", LoadTexture(steepTexture));
            parallaxMaterialDOUBLEHIGH.SetFloat("_DetailSteepPow", detailSteepPower);
            parallaxMaterialDOUBLEHIGH.SetFloat("_SteepPower", steepPower);
            parallaxMaterialDOUBLEHIGH.SetTextureScale("_SurfaceTextureLower", CreateVector(surfaceTextureScale));
            parallaxMaterialDOUBLEHIGH.SetFloat("_displacement_scale", surfaceParallaxHeight);
            parallaxMaterialDOUBLEHIGH.SetTextureScale("_SteepTex", CreateVector(steepTextureScale));
            parallaxMaterialDOUBLEHIGH.SetFloat("_Metallic", smoothness);
            //parallaxMaterialDOUBLEHIGH.SetTexture("_SurfaceTextureHigher", LoadTexture(surfaceTextureHigh));
            //parallaxMaterialDOUBLEHIGH.SetTexture("_BumpMapHigher", LoadTexture(bumpMapHigh));
            //parallaxMaterialDOUBLEHIGH.SetTexture("_BumpMapSteep", LoadTexture(bumpMapSteep));
            parallaxMaterialDOUBLEHIGH.SetFloat("_LowStart", lowStart);
            parallaxMaterialDOUBLEHIGH.SetFloat("_LowEnd", lowEnd);
            parallaxMaterialDOUBLEHIGH.SetFloat("_HighStart", highStart);
            parallaxMaterialDOUBLEHIGH.SetFloat("_HighEnd", highEnd);
            parallaxMaterialDOUBLEHIGH.SetFloat("_PlanetRadius", (float)FlightGlobals.GetBodyByName(planetName).Radius);
            parallaxMaterialDOUBLEHIGH.SetColor("_MetallicTint", tintColor);
            //parallaxMaterialDOUBLEHIGH.SetTexture("_InfluenceMap", LoadTexture(influenceMap));
            parallaxMaterialDOUBLEHIGH.SetFloat("_TessellationRange", 99);
            //parallaxMaterialDOUBLEHIGH.SetTexture("_FogTexture", LoadTexture(fogTexture));
            parallaxMaterialDOUBLEHIGH.SetFloat("_FogRange", fogRange);
            parallaxMaterialDOUBLEHIGH.SetFloat("_displacement_offset", displacementOffset);
            parallaxMaterialDOUBLEHIGH.SetFloat("_NormalSpecularInfluence", normalSpecularInfluence);
            parallaxMaterialDOUBLEHIGH.SetFloat("_HasEmission", hasEmission);
            parallaxMaterialDOUBLEHIGH.SetColor("_EmissionColor", emissionColor);
            if (useReflections)
            {
                parallaxMaterialDOUBLEHIGH.SetFloat("_UseReflections", 1);
                Log("_UseReflections", "true");
            }
            else
            {
                parallaxMaterialDOUBLEHIGH.SetFloat("_UseReflections", 0);
                Log("_UseReflections", "false");
            }

            parallaxMaterialDOUBLEHIGH.SetVector("_ReflectionMask", reflectionMask);
            parallaxMaterialDOUBLEHIGH.SetFloat("_TessellationEdgeLength", ParallaxSettings.tessellationEdgeLength);
            parallaxMaterialDOUBLEHIGH.SetFloat("_TessellationRange", ParallaxSettings.tessellationRange);
            parallaxMaterialDOUBLEHIGH.SetFloat("_TessellationMax", ParallaxSettings.tessellationMax);
            parallaxMaterialDOUBLEHIGH.SetFloat("_DetailPower", detailPower);
            parallaxMaterialDOUBLEHIGH.SetFloat("_DetailOffset", detailOffset);
            parallaxMaterialDOUBLEHIGH.SetFloat("_DetailRange", detailRange);
            parallaxMaterialDOUBLEHIGH.SetTextureScale("_DetailTexLower", CreateVector(detailScaleMid));
            parallaxMaterialDOUBLEHIGH.SetTextureScale("_DetailSteepLower", CreateVector(detailScaleSteepMid));
            parallaxMaterialDOUBLEHIGH.SetTextureScale("_DetailTexLower", CreateVector(detailScaleHigh));
            parallaxMaterialDOUBLEHIGH.SetTextureScale("_DetailSteepHigher", CreateVector(detailScaleSteepHigh));

            EnableKeywordFromInteger(parallaxMaterial, hasEmission, "EMISSION_ON", "EMISSION_OFF");
            EnableKeywordFromInteger(parallaxMaterialSINGLELOW, hasEmission, "EMISSION_ON", "EMISSION_OFF");
            EnableKeywordFromInteger(parallaxMaterialSINGLEMID, hasEmission, "EMISSION_ON", "EMISSION_OFF");
            EnableKeywordFromInteger(parallaxMaterialSINGLEHIGH, hasEmission, "EMISSION_ON", "EMISSION_OFF");
            EnableKeywordFromInteger(parallaxMaterialSINGLESTEEPLOW, hasEmission, "EMISSION_ON", "EMISSION_OFF");
            EnableKeywordFromInteger(parallaxMaterialSINGLESTEEPMID, hasEmission, "EMISSION_ON", "EMISSION_OFF");
            EnableKeywordFromInteger(parallaxMaterialSINGLESTEEPHIGH, hasEmission, "EMISSION_ON", "EMISSION_OFF");
            EnableKeywordFromInteger(parallaxMaterialDOUBLELOW, hasEmission, "EMISSION_ON", "EMISSION_OFF");
            EnableKeywordFromInteger(parallaxMaterialDOUBLEHIGH, hasEmission, "EMISSION_ON", "EMISSION_OFF");

            EnableKeywordFromInteger(parallaxMaterial, reversedNormal, "REVERSED_NORMAL_ON", "REVERSED_NORMAL_OFF");
            EnableKeywordFromInteger(parallaxMaterialSINGLELOW, reversedNormal, "REVERSED_NORMAL_ON", "REVERSED_NORMAL_OFF");
            EnableKeywordFromInteger(parallaxMaterialSINGLEMID, reversedNormal, "REVERSED_NORMAL_ON", "REVERSED_NORMAL_OFF");
            EnableKeywordFromInteger(parallaxMaterialSINGLEHIGH, reversedNormal, "REVERSED_NORMAL_ON", "REVERSED_NORMAL_OFF");
            EnableKeywordFromInteger(parallaxMaterialSINGLESTEEPLOW, reversedNormal, "REVERSED_NORMAL_ON", "REVERSED_NORMAL_OFF");
            EnableKeywordFromInteger(parallaxMaterialSINGLESTEEPMID, reversedNormal, "REVERSED_NORMAL_ON", "REVERSED_NORMAL_OFF");
            EnableKeywordFromInteger(parallaxMaterialSINGLESTEEPHIGH, reversedNormal, "REVERSED_NORMAL_ON", "REVERSED_NORMAL_OFF");
            EnableKeywordFromInteger(parallaxMaterialDOUBLELOW, reversedNormal, "REVERSED_NORMAL_ON", "REVERSED_NORMAL_OFF");
            EnableKeywordFromInteger(parallaxMaterialDOUBLEHIGH, reversedNormal, "REVERSED_NORMAL_ON", "REVERSED_NORMAL_OFF");


            ParseKeywords();    //Quality settings
            output[0] = parallaxMaterial;
            output[1] = parallaxMaterialSINGLELOW;
            output[2] = ParallaxMaterialSINGLEMID;
            output[3] = parallaxMaterialSINGLEHIGH;

            output[4] = parallaxMaterialSINGLESTEEPLOW;
            output[5] = parallaxMaterialSINGLESTEEPMID;
            output[6] = parallaxMaterialSINGLESTEEPHIGH;

            output[7] = parallaxMaterialDOUBLELOW;
            output[8] = parallaxMaterialDOUBLEHIGH;
            return output;
        }
        public void ParseKeywords()
        {
            if (ParallaxSettings.tessellate == true)
            {
                parallaxMaterial.EnableKeyword("TESS_ON");
                parallaxMaterial.DisableKeyword("TESS_OFF");

                parallaxMaterialSINGLESTEEPLOW.EnableKeyword("TESS_ON");
                parallaxMaterialSINGLESTEEPLOW.DisableKeyword("TESS_OFF");
                parallaxMaterialSINGLESTEEPMID.EnableKeyword("TESS_ON");
                parallaxMaterialSINGLESTEEPMID.DisableKeyword("TESS_OFF");
                parallaxMaterialSINGLESTEEPHIGH.EnableKeyword("TESS_ON");
                parallaxMaterialSINGLESTEEPHIGH.DisableKeyword("TESS_OFF");

                parallaxMaterialSINGLELOW.EnableKeyword("TESS_ON");
                parallaxMaterialSINGLELOW.DisableKeyword("TESS_OFF");
                parallaxMaterialSINGLEMID.EnableKeyword("TESS_ON");
                parallaxMaterialSINGLEMID.DisableKeyword("TESS_OFF");
                parallaxMaterialSINGLEHIGH.EnableKeyword("TESS_ON");
                parallaxMaterialSINGLEHIGH.DisableKeyword("TESS_OFF");

                parallaxMaterialDOUBLELOW.EnableKeyword("TESS_ON");
                parallaxMaterialDOUBLELOW.DisableKeyword("TESS_OFF");
                parallaxMaterialDOUBLEHIGH.EnableKeyword("TESS_ON");
                parallaxMaterialDOUBLEHIGH.DisableKeyword("TESS_OFF");
            }
            else
            {
                parallaxMaterial.EnableKeyword("TESS_OFF");
                parallaxMaterial.DisableKeyword("TESS_ON");

                parallaxMaterialSINGLESTEEPLOW.EnableKeyword("TESS_OFF");
                parallaxMaterialSINGLESTEEPLOW.DisableKeyword("TESS_ON");
                parallaxMaterialSINGLESTEEPMID.EnableKeyword("TESS_OFF");
                parallaxMaterialSINGLESTEEPMID.DisableKeyword("TESS_ON");
                parallaxMaterialSINGLESTEEPHIGH.EnableKeyword("TESS_OFF");
                parallaxMaterialSINGLESTEEPHIGH.DisableKeyword("TESS_ON");

                parallaxMaterialSINGLELOW.EnableKeyword("TESS_OFF");
                parallaxMaterialSINGLELOW.DisableKeyword("TESS_ON");
                parallaxMaterialSINGLEMID.EnableKeyword("TESS_OFF");
                parallaxMaterialSINGLEMID.DisableKeyword("TESS_ON");
                parallaxMaterialSINGLEHIGH.EnableKeyword("TESS_OFF");
                parallaxMaterialSINGLEHIGH.DisableKeyword("TESS_ON");


                parallaxMaterialDOUBLELOW.EnableKeyword("TESS_OFF");
                parallaxMaterialDOUBLELOW.DisableKeyword("TESS_ON");
                parallaxMaterialDOUBLEHIGH.EnableKeyword("TESS_OFF");
                parallaxMaterialDOUBLEHIGH.DisableKeyword("TESS_ON");
            }

            if (ParallaxSettings.tessellateLighting == true)
            {
                parallaxMaterial.EnableKeyword("HQ_LIGHTS_ON");
                parallaxMaterial.DisableKeyword("HQ_LIGHTS_OFF");

                parallaxMaterialSINGLESTEEPLOW.EnableKeyword("HQ_LIGHTS_ON");
                parallaxMaterialSINGLESTEEPLOW.DisableKeyword("HQ_LIGHTS_OFF");
                parallaxMaterialSINGLESTEEPMID.EnableKeyword("HQ_LIGHTS_ON");
                parallaxMaterialSINGLESTEEPMID.DisableKeyword("HQ_LIGHTS_OFF");
                parallaxMaterialSINGLESTEEPHIGH.EnableKeyword("HQ_LIGHTS_ON");
                parallaxMaterialSINGLESTEEPHIGH.DisableKeyword("HQ_LIGHTS_OFF");

                parallaxMaterialSINGLELOW.EnableKeyword("HQ_LIGHTS_ON");
                parallaxMaterialSINGLELOW.DisableKeyword("HQ_LIGHTS_OFF");
                parallaxMaterialSINGLEMID.EnableKeyword("HQ_LIGHTS_ON");
                parallaxMaterialSINGLEMID.DisableKeyword("HQ_LIGHTS_OFF");
                parallaxMaterialSINGLEHIGH.EnableKeyword("HQ_LIGHTS_ON");
                parallaxMaterialSINGLEHIGH.DisableKeyword("HQ_LIGHTS_OFF");

                parallaxMaterialDOUBLELOW.EnableKeyword("HQ_LIGHTS_ON");
                parallaxMaterialDOUBLELOW.DisableKeyword("HQ_LIGHTS_OFF");
                parallaxMaterialDOUBLEHIGH.EnableKeyword("HQ_LIGHTS_ON");
                parallaxMaterialDOUBLEHIGH.DisableKeyword("HQ_LIGHTS_OFF");
            }
            else
            {
                parallaxMaterial.EnableKeyword("HQ_LIGHTS_OFF");
                parallaxMaterial.DisableKeyword("HQ_LIGHTS_ON");

                parallaxMaterialSINGLESTEEPLOW.EnableKeyword("HQ_LIGHTS_OFF");
                parallaxMaterialSINGLESTEEPLOW.DisableKeyword("HQ_LIGHTS_ON");
                parallaxMaterialSINGLESTEEPMID.EnableKeyword("HQ_LIGHTS_OFF");
                parallaxMaterialSINGLESTEEPMID.DisableKeyword("HQ_LIGHTS_ON");
                parallaxMaterialSINGLESTEEPHIGH.EnableKeyword("HQ_LIGHTS_OFF");
                parallaxMaterialSINGLESTEEPHIGH.DisableKeyword("HQ_LIGHTS_ON");

                parallaxMaterialSINGLELOW.EnableKeyword("HQ_LIGHTS_OFF");
                parallaxMaterialSINGLELOW.DisableKeyword("HQ_LIGHTS_ON");
                parallaxMaterialSINGLEMID.EnableKeyword("HQ_LIGHTS_OFF");
                parallaxMaterialSINGLEMID.DisableKeyword("HQ_LIGHTS_ON");
                parallaxMaterialSINGLEHIGH.EnableKeyword("HQ_LIGHTS_OFF");
                parallaxMaterialSINGLEHIGH.DisableKeyword("HQ_LIGHTS_ON");


                parallaxMaterialDOUBLELOW.EnableKeyword("HQ_LIGHTS_OFF");
                parallaxMaterialDOUBLELOW.DisableKeyword("HQ_LIGHTS_ON");
                parallaxMaterialDOUBLEHIGH.EnableKeyword("HQ_LIGHTS_OFF");
                parallaxMaterialDOUBLEHIGH.DisableKeyword("HQ_LIGHTS_ON");
            }

            if (ParallaxSettings.tessellateShadows == true)
            {
                parallaxMaterial.EnableKeyword("HQ_SHADOWS_ON");
                parallaxMaterial.DisableKeyword("HQ_SHADOWS_OFF");

                parallaxMaterialSINGLESTEEPLOW.EnableKeyword("HQ_SHADOWS_ON");
                parallaxMaterialSINGLESTEEPLOW.DisableKeyword("HQ_SHADOWS_OFF");
                parallaxMaterialSINGLESTEEPMID.EnableKeyword("HQ_SHADOWS_ON");
                parallaxMaterialSINGLESTEEPMID.DisableKeyword("HQ_SHADOWS_OFF");
                parallaxMaterialSINGLESTEEPHIGH.EnableKeyword("HQ_SHADOWS_ON");
                parallaxMaterialSINGLESTEEPHIGH.DisableKeyword("HQ_SHADOWS_OFF");

                parallaxMaterialSINGLELOW.EnableKeyword("HQ_SHADOWS_ON");
                parallaxMaterialSINGLELOW.DisableKeyword("HQ_SHADOWS_OFF");
                parallaxMaterialSINGLEMID.EnableKeyword("HQ_SHADOWS_ON");
                parallaxMaterialSINGLEMID.DisableKeyword("HQ_SHADOWS_OFF");
                parallaxMaterialSINGLEHIGH.EnableKeyword("HQ_SHADOWS_ON");
                parallaxMaterialSINGLEHIGH.DisableKeyword("HQ_SHADOWS_OFF");

                parallaxMaterialDOUBLELOW.EnableKeyword("HQ_SHADOWS_ON");
                parallaxMaterialDOUBLELOW.DisableKeyword("HQ_SHADOWS_OFF");
                parallaxMaterialDOUBLEHIGH.EnableKeyword("HQ_SHADOWS_ON");
                parallaxMaterialDOUBLEHIGH.DisableKeyword("HQ_SHADOWS_OFF");
            }
            else
            {
                parallaxMaterial.EnableKeyword("HQ_SHADOWS_OFF");
                parallaxMaterial.DisableKeyword("HQ_SHADOWS_ON");

                parallaxMaterialSINGLESTEEPLOW.EnableKeyword("HQ_SHADOWS_OFF");
                parallaxMaterialSINGLESTEEPLOW.DisableKeyword("HQ_SHADOWS_ON");
                parallaxMaterialSINGLESTEEPMID.EnableKeyword("HQ_SHADOWS_OFF");
                parallaxMaterialSINGLESTEEPMID.DisableKeyword("HQ_SHADOWS_ON");
                parallaxMaterialSINGLESTEEPHIGH.EnableKeyword("HQ_SHADOWS_OFF");
                parallaxMaterialSINGLESTEEPHIGH.DisableKeyword("HQ_SHADOWS_ON");

                parallaxMaterialSINGLELOW.EnableKeyword("HQ_SHADOWS_OFF");
                parallaxMaterialSINGLELOW.DisableKeyword("HQ_SHADOWS_ON");
                parallaxMaterialSINGLEMID.EnableKeyword("HQ_SHADOWS_OFF");
                parallaxMaterialSINGLEMID.DisableKeyword("HQ_SHADOWS_ON");
                parallaxMaterialSINGLEHIGH.EnableKeyword("HQ_SHADOWS_OFF");
                parallaxMaterialSINGLEHIGH.DisableKeyword("HQ_SHADOWS_ON");


                parallaxMaterialDOUBLELOW.EnableKeyword("HQ_SHADOWS_OFF");
                parallaxMaterialDOUBLELOW.DisableKeyword("HQ_SHADOWS_ON");
                parallaxMaterialDOUBLEHIGH.EnableKeyword("HQ_SHADOWS_OFF");
                parallaxMaterialDOUBLEHIGH.DisableKeyword("HQ_SHADOWS_ON");
            }
            if (ParallaxSettings.trueLighting == "true")
            {
                parallaxMaterial.EnableKeyword("ATTENUATIONOVERRIDE_ON");
                parallaxMaterial.DisableKeyword("ATTENUATIONOVERRIDE_OFF");

                parallaxMaterialSINGLESTEEPLOW.EnableKeyword("ATTENUATIONOVERRIDE_ON");
                parallaxMaterialSINGLESTEEPLOW.DisableKeyword("ATTENUATIONOVERRIDE_OFF");
                parallaxMaterialSINGLESTEEPMID.EnableKeyword("ATTENUATIONOVERRIDE_ON");
                parallaxMaterialSINGLESTEEPMID.DisableKeyword("ATTENUATIONOVERRIDE_OFF");
                parallaxMaterialSINGLESTEEPHIGH.EnableKeyword("ATTENUATIONOVERRIDE_ON");
                parallaxMaterialSINGLESTEEPHIGH.DisableKeyword("ATTENUATIONOVERRIDE_OFF");

                parallaxMaterialSINGLELOW.EnableKeyword("ATTENUATIONOVERRIDE_ON");
                parallaxMaterialSINGLELOW.DisableKeyword("ATTENUATIONOVERRIDE_OFF");
                parallaxMaterialSINGLEMID.EnableKeyword("ATTENUATIONOVERRIDE_ON");
                parallaxMaterialSINGLEMID.DisableKeyword("ATTENUATIONOVERRIDE_OFF");
                parallaxMaterialSINGLEHIGH.EnableKeyword("ATTENUATIONOVERRIDE_ON");
                parallaxMaterialSINGLEHIGH.DisableKeyword("ATTENUATIONOVERRIDE_OFF");

                parallaxMaterialDOUBLELOW.EnableKeyword("ATTENUATIONOVERRIDE_ON");
                parallaxMaterialDOUBLELOW.DisableKeyword("ATTENUATIONOVERRIDE_OFF");
                parallaxMaterialDOUBLEHIGH.EnableKeyword("ATTENUATIONOVERRIDE_ON");
                parallaxMaterialDOUBLEHIGH.DisableKeyword("ATTENUATIONOVERRIDE_OFF");
            }
            else
            {
                //Off or Adaptive will disable true lighting by default
                if (FlightGlobals.GetBodyByName(planetName).atmosphere == true && ParallaxSettings.trueLighting == "adaptive")
                {
                    Debug.Log("[Parallax] Adaptive Lighting is enabled - this planet has an atmosphere");
                    parallaxMaterial.EnableKeyword("ATTENUATIONOVERRIDE_OFF");
                    parallaxMaterial.DisableKeyword("ATTENUATIONOVERRIDE_ON");

                    parallaxMaterialSINGLESTEEPLOW.EnableKeyword("ATTENUATIONOVERRIDE_OFF");
                    parallaxMaterialSINGLESTEEPLOW.DisableKeyword("ATTENUATIONOVERRIDE_ON");
                    parallaxMaterialSINGLESTEEPMID.EnableKeyword("ATTENUATIONOVERRIDE_OFF");
                    parallaxMaterialSINGLESTEEPMID.DisableKeyword("ATTENUATIONOVERRIDE_ON");
                    parallaxMaterialSINGLESTEEPHIGH.EnableKeyword("ATTENUATIONOVERRIDE_OFF");
                    parallaxMaterialSINGLESTEEPHIGH.DisableKeyword("ATTENUATIONOVERRIDE_ON");

                    parallaxMaterialSINGLELOW.EnableKeyword("ATTENUATIONOVERRIDE_OFF");
                    parallaxMaterialSINGLELOW.DisableKeyword("ATTENUATIONOVERRIDE_ON");
                    parallaxMaterialSINGLEMID.EnableKeyword("ATTENUATIONOVERRIDE_OFF");
                    parallaxMaterialSINGLEMID.DisableKeyword("ATTENUATIONOVERRIDE_ON");
                    parallaxMaterialSINGLEHIGH.EnableKeyword("ATTENUATIONOVERRIDE_OFF");
                    parallaxMaterialSINGLEHIGH.DisableKeyword("ATTENUATIONOVERRIDE_ON");


                    parallaxMaterialDOUBLELOW.EnableKeyword("ATTENUATIONOVERRIDE_OFF");
                    parallaxMaterialDOUBLELOW.DisableKeyword("ATTENUATIONOVERRIDE_ON");
                    parallaxMaterialDOUBLEHIGH.EnableKeyword("ATTENUATIONOVERRIDE_OFF");
                    parallaxMaterialDOUBLEHIGH.DisableKeyword("ATTENUATIONOVERRIDE_ON");
                }
                else
                {
                    


                    parallaxMaterial.EnableKeyword("ATTENUATIONOVERRIDE_ON");
                    parallaxMaterial.DisableKeyword("ATTENUATIONOVERRIDE_OFF");

                    parallaxMaterialSINGLESTEEPLOW.EnableKeyword("ATTENUATIONOVERRIDE_ON");
                    parallaxMaterialSINGLESTEEPLOW.DisableKeyword("ATTENUATIONOVERRIDE_OFF");
                    parallaxMaterialSINGLESTEEPMID.EnableKeyword("ATTENUATIONOVERRIDE_ON");
                    parallaxMaterialSINGLESTEEPMID.DisableKeyword("ATTENUATIONOVERRIDE_OFF");
                    parallaxMaterialSINGLESTEEPHIGH.EnableKeyword("ATTENUATIONOVERRIDE_ON");
                    parallaxMaterialSINGLESTEEPHIGH.DisableKeyword("ATTENUATIONOVERRIDE_OFF");

                    parallaxMaterialSINGLELOW.EnableKeyword("ATTENUATIONOVERRIDE_ON");
                    parallaxMaterialSINGLELOW.DisableKeyword("ATTENUATIONOVERRIDE_OFF");
                    parallaxMaterialSINGLEMID.EnableKeyword("ATTENUATIONOVERRIDE_ON");
                    parallaxMaterialSINGLEMID.DisableKeyword("ATTENUATIONOVERRIDE_OFF");
                    parallaxMaterialSINGLEHIGH.EnableKeyword("ATTENUATIONOVERRIDE_ON");
                    parallaxMaterialSINGLEHIGH.DisableKeyword("ATTENUATIONOVERRIDE_OFF");

                    parallaxMaterialDOUBLELOW.EnableKeyword("ATTENUATIONOVERRIDE_ON");
                    parallaxMaterialDOUBLELOW.DisableKeyword("ATTENUATIONOVERRIDE_OFF");
                    parallaxMaterialDOUBLEHIGH.EnableKeyword("ATTENUATIONOVERRIDE_ON");
                    parallaxMaterialDOUBLEHIGH.DisableKeyword("ATTENUATIONOVERRIDE_OFF");
                }
            }
        }

        private void Log(string name, string value)
        {
            Debug.Log("\t - " + name + " is " + value);
        }
        private void Log(string name, float value)
        {
            Debug.Log("\t - " + name + " is " + value);
        }
        private void Log(string message)
        {
            Debug.Log("\t" + message);
        }
        public Texture LoadTexture(string name)
        {
            try
            {
                return Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(t => t.name == name);
            }
            catch
            {
                Debug.Log("The texture, '" + name + "', could not be found");
                return Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(t => t.name == "TessellationBlank");
            }
        }
        private Vector2 CreateVector(float size)
        {
            return new Vector2(size, size);
        }
    }
    [KSPAddon(KSPAddon.Startup.FlightAndKSC, false)]
    public class ParallaxOnDemandLoader : MonoBehaviour
    {
        bool thisBodyIsLoaded = false;
        CelestialBody lastKnownBody;
        CelestialBody currentBody;
        public static Dictionary<string, Texture2D> activeTextures = new Dictionary<string, Texture2D>();
        public static Dictionary<string, string> activeTexturePaths = new Dictionary<string, string>();
        public static bool finishedMainLoad = false;
        float timeElapsed = 0;
        public void Start()
        {
            Log("Starting Parallax On-Demand loader");
            Sun.Instance.sunLight.shadowStrength = 1;
        }
        public void LateUpdate()
        {
            if (ParallaxShaderLoader.parallaxBodies.ContainsKey(FlightGlobals.currentMainBody.name))
            {
                Sun.Instance.sunLight.shadowStrength = 1;
            }
            
        }
        public void Update()
        {
            //foreach (Light l in GameObject.FindObjectsOfType<Light>())
            //{
            //    Debug.Log(l.name + ": " + l.shadowStrength);
            //}
            
            timeElapsed = Time.realtimeSinceStartup;
            currentBody = FlightGlobals.currentMainBody;
            bool key = Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.Alpha2);
            if (currentBody != lastKnownBody)           //Vessel is around a new planet, so its textures must be loaded
            {
                finishedMainLoad = false;
                Debug.Log("Set finishedMainLoad to false");
                bool bodyExists = ParallaxShaderLoader.parallaxBodies.ContainsKey(currentBody.name);
                if (bodyExists)
                {
                    
                    Log("SOI changed, beginning transfer of surface textures for " + currentBody.name);
                    Log("Unloading " + activeTextures.Count + " textures");
                    UnloadTextures();
                    LoadTextures();
                }
                timeElapsed = Time.realtimeSinceStartup - timeElapsed;
                Debug.Log("Loading " + activeTextures.Count + " textures from disk took " + timeElapsed + "ms");
                lastKnownBody = currentBody;
            }


        }
        private void Log(string message)
        {
            Debug.Log("[Parallax] " + message);
        }
        public void LoadTextures()
        {
            ParallaxBody thisBody = ParallaxShaderLoader.parallaxBodies[currentBody.name];
            ValidateEverything(thisBody);
            AttemptTextureLoad(currentBody.name);
            Debug.Log("Finished main load, passing value to Parallax Collisions");
            finishedMainLoad = true;
        }
        public void UnloadTextures()
        {
            foreach (KeyValuePair<string, Texture2D> texture in activeTextures)
            {
                Destroy(texture.Value); //Destroy the texture before clearing from the dictionary
            }
            activeTextures.Clear();
            activeTexturePaths.Clear();
            
        }
        public void ValidateEverything(ParallaxBody body)
        {
            ValidatePath(body.ParallaxBodyMaterial.SurfaceTexture, "_SurfaceTexture");
            ValidatePath(body.ParallaxBodyMaterial.SurfaceTextureMid, "_SurfaceTextureMid");
            ValidatePath(body.ParallaxBodyMaterial.SurfaceTextureHigh, "_SurfaceTextureHigh");
            ValidatePath(body.ParallaxBodyMaterial.SteepTexture, "_SteepTex");

            ValidatePath(body.ParallaxBodyMaterial.SurfaceTextureBumpMap, "_BumpMap");
            ValidatePath(body.ParallaxBodyMaterial.BumpMapMid, "_BumpMapMid");
            ValidatePath(body.ParallaxBodyMaterial.BumpMapHigh, "_BumpMapHigh");
            ValidatePath(body.ParallaxBodyMaterial.BumpMapSteep, "_BumpMapSteep");

            ValidatePath(body.ParallaxBodyMaterial.SurfaceTextureParallaxMap, "_DispTex");
            ValidatePath(body.ParallaxBodyMaterial.InfluenceMap, "_InfluenceMap");

            ValidatePath(body.ParallaxBodyMaterial.FogTexture, "_FogTexture");

            ValidatePath(body.ParallaxBodyMaterial.DetailTextureLow, "_DetailTexLow");
            ValidatePath(body.ParallaxBodyMaterial.DetailTextureMid, "_DetailTexMid");
            ValidatePath(body.ParallaxBodyMaterial.DetailTextureHigh, "_DetailTexHigh");
            ValidatePath(body.ParallaxBodyMaterial.DetailTextureSteepLow, "_DetailSteepLow");
            ValidatePath(body.ParallaxBodyMaterial.DetailTextureSteepMid, "_DetailSteepMid");
            ValidatePath(body.ParallaxBodyMaterial.DetailTextureSteepHigh, "_DetailSteepHigh");
            ValidatePath(body.ParallaxBodyMaterial.DetailNormalLow, "_DetailNormalLow");
            ValidatePath(body.ParallaxBodyMaterial.DetailNormalMid, "_DetailNormalMid");
            ValidatePath(body.ParallaxBodyMaterial.DetailNormalHigh, "_DetailNormalHigh");
            ValidatePath(body.ParallaxBodyMaterial.DetailNormalSteepLow, "_DetailSteepNormalLow");
            ValidatePath(body.ParallaxBodyMaterial.DetailNormalSteepMid, "_DetailSteepNormalMid");
            ValidatePath(body.ParallaxBodyMaterial.DetailNormalSteepHigh, "_DetailSteepNormalHigh");

            ValidatePath(body.ParallaxBodyMaterial.PhysicsTexDisplacement, "_PhysicsTexDisplacement");
        }
        public void ValidatePath(string path, string name)
        {
            string actualPath = "";
            
            actualPath = Application.dataPath;
            int lastDash = actualPath.LastIndexOf('/');
            actualPath = actualPath.Remove(lastDash + 1) + "GameData/" + path;
            //string actualPath = Path.Combine(KSPUtil.ApplicationRootPath + "GameData/" + "Parallax/Shaders/Parallax");
            Log("Validating " + actualPath);
            bool fileExists = File.Exists(actualPath);
            if (fileExists)
            {
                Log(" - Texture exists");
                activeTexturePaths.Add(name, actualPath);
            }
            else
            {
                Log(" - Texture doesn't exist, skipping: " + name + " with filepath: " + path);
            }
            //if (File.Exists(Application.dataPath))
        }
        public void AttemptTextureLoad(string planetName)
        {
            Debug.Log("1");
            ParallaxBody thisBody = ParallaxShaderLoader.parallaxBodies[planetName]; ;
            if (thisBody == null)
            {
                Debug.Log("Body is null");
                return;
            }
            Debug.Log("1");
            //ParallaxShaderLoader.parallaxBodies[planetName].detailTexturesLower = new Texture2DArray(thisBody.ParallaxBodyMaterial.DetailResolution, thisBody.ParallaxBodyMaterial.DetailResolution, 8, TextureFormat.RGBA32, true);
            //ParallaxShaderLoader.parallaxBodies[planetName].detailTexturesUpper = new Texture2DArray(thisBody.ParallaxBodyMaterial.DetailResolution, thisBody.ParallaxBodyMaterial.DetailResolution, 8, TextureFormat.RGBA32, true);
            //Holds 8 detail textures

            foreach (KeyValuePair<string, string> path in activeTexturePaths)
            {
                activeTextures.Add(path.Key, Texture2D.blackTexture);
                Texture2D textureRef = activeTextures[path.Key];
                Debug.Log("2");
                byte[] bytes = System.IO.File.ReadAllBytes(path.Value);
                Debug.Log("2");
                if (path.Key == "_PhysicsTexDisplacement")
                {
                    textureRef = LoadDDSTexture(bytes, path.Key + " | " + path.Value);
                    Debug.Log("2.1");
                    PhysicsTexHolder.displacementTex = textureRef;
                    activeTextures[path.Key] = textureRef;
                    Debug.Log("Loaded all physics textures");
                }
                else
                {
                    Debug.Log("2.2");
                    textureRef = LoadDDSTexture(bytes, path.Key + " | " + path.Value);
                    Debug.Log("2.2");
                    activeTextures[path.Key] = textureRef;
                    Debug.Log("2.2");
                    //FlightGlobals.currentMainBody.pqsController.surfaceMaterial.SetTexture(path.Key, textureRef);
                    thisBody.ParallaxBodyMaterial.ParallaxMaterial.SetTexture(path.Key, textureRef);
                    thisBody.ParallaxBodyMaterial.ParallaxMaterialDOUBLEHIGH.SetTexture(path.Key, textureRef);
                    thisBody.ParallaxBodyMaterial.ParallaxMaterialDOUBLELOW.SetTexture(path.Key, textureRef);
                    thisBody.ParallaxBodyMaterial.ParallaxMaterialSINGLELOW.SetTexture(path.Key, textureRef);
                    thisBody.ParallaxBodyMaterial.ParallaxMaterialSINGLEMID.SetTexture(path.Key, textureRef);
                    thisBody.ParallaxBodyMaterial.ParallaxMaterialSINGLEHIGH.SetTexture(path.Key, textureRef);
                    thisBody.ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPLOW.SetTexture(path.Key, textureRef);
                    thisBody.ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPMID.SetTexture(path.Key, textureRef);
                    thisBody.ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPHIGH.SetTexture(path.Key, textureRef);
                    Debug.Log("2.2");
                    if (path.Key == "_SurfaceTexture")
                    {
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialSINGLELOW.SetTexture("_SurfaceTexture", textureRef);
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPLOW.SetTexture("_SurfaceTexture", textureRef);
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialDOUBLELOW.SetTexture("_SurfaceTextureLower", textureRef);
                    }
                    if (path.Key == "_SurfaceTextureMid")
                    {
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialSINGLEMID.SetTexture("_SurfaceTexture", textureRef);
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPMID.SetTexture("_SurfaceTexture", textureRef);
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialDOUBLEHIGH.SetTexture("_SurfaceTextureLower", textureRef);
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialDOUBLELOW.SetTexture("_SurfaceTextureHigher", textureRef);
                    }
                    if (path.Key == "_SurfaceTextureHigh")
                    {
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialSINGLEHIGH.SetTexture("_SurfaceTexture", textureRef);
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPHIGH.SetTexture("_SurfaceTexture", textureRef);
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialDOUBLEHIGH.SetTexture("_SurfaceTextureHigher", textureRef);
                    }
                    if (path.Key == "_BumpMap")
                    {
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialSINGLELOW.SetTexture("_BumpMap", textureRef);
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPLOW.SetTexture("_BumpMap", textureRef);
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialDOUBLELOW.SetTexture("_BumpMapLower", textureRef);
                    }
                    if (path.Key == "_BumpMapMid")
                    {
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialSINGLEMID.SetTexture("_BumpMap", textureRef);
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPMID.SetTexture("_BumpMap", textureRef);
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialDOUBLEHIGH.SetTexture("_BumpMapLower", textureRef);
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialDOUBLELOW.SetTexture("_BumpMapHigher", textureRef);
                    }
                    if (path.Key == "_BumpMapHigh")
                    {
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialSINGLEHIGH.SetTexture("_BumpMap", textureRef);
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPHIGH.SetTexture("_BumpMap", textureRef);
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialDOUBLEHIGH.SetTexture("_BumpMapHigher", textureRef);
                    }


                    if (path.Key == "_DetailTexLow")
                    {
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialSINGLELOW.SetTexture("_DetailTex", textureRef);
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPLOW.SetTexture("_DetailTex", textureRef);
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialDOUBLELOW.SetTexture("_DetailTexLower", textureRef);
                        //AttemptDetailSetLower(thisBody, textureRef, 0);

                    }
                    if (path.Key == "_DetailTexMid")
                    {
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialSINGLEMID.SetTexture("_DetailTex", textureRef);
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPMID.SetTexture("_DetailTex", textureRef);
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialDOUBLEHIGH.SetTexture("_DetailTexLower", textureRef);
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialDOUBLELOW.SetTexture("_DetailTexHigher", textureRef);
                        //AttemptDetailSetLower(thisBody, textureRef, 2);
                        //AttemptDetailSetUpper(thisBody, textureRef, 0);
                    }
                    if (path.Key == "_DetailTexHigh")
                    {
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialSINGLEHIGH.SetTexture("_DetailTex", textureRef);
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPHIGH.SetTexture("_DetailTex", textureRef);
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialDOUBLEHIGH.SetTexture("_DetailTexHigher", textureRef);
                        //AttemptDetailSetUpper(thisBody, textureRef, 2);
                    }
                    if (path.Key == "_DetailNormalLow")
                    {
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialSINGLELOW.SetTexture("_DetailNormal", textureRef);
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPLOW.SetTexture("_DetailNormal", textureRef);
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialDOUBLELOW.SetTexture("_DetailTexLowerNormal", textureRef);
                        //AttemptDetailSetLower(thisBody, textureRef, 1);
                    }
                    if (path.Key == "_DetailNormalMid")
                    {
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialSINGLEMID.SetTexture("_DetailNormal", textureRef);
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPMID.SetTexture("_DetailNormal", textureRef);
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialDOUBLEHIGH.SetTexture("_DetailTexLowerNormal", textureRef);
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialDOUBLELOW.SetTexture("_DetailTexHigherNormal", textureRef);
                        //AttemptDetailSetLower(thisBody, textureRef, 3);
                        //AttemptDetailSetUpper(thisBody, textureRef, 1);
                    }
                    if (path.Key == "_DetailNormalHigh")
                    {
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialSINGLEHIGH.SetTexture("_DetailNormal", textureRef);
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPHIGH.SetTexture("_DetailNormal", textureRef);
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialDOUBLEHIGH.SetTexture("_DetailTexHigherNormal", textureRef);
                        //AttemptDetailSetUpper(thisBody, textureRef, 3);
                    }
                    if (path.Key == "_DetailSteepLow")
                    {
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPLOW.SetTexture("_DetailSteep", textureRef);
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialSINGLELOW.SetTexture("_DetailSteep", textureRef);
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialDOUBLELOW.SetTexture("_DetailSteepLower", textureRef);
                        //AttemptDetailSetLower(thisBody, textureRef, 4);
                    }
                    if (path.Key == "_DetailSteepNormalLow")
                    {
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPLOW.SetTexture("_DetailSteepNormal", textureRef);
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialSINGLELOW.SetTexture("_DetailSteepNormal", textureRef);
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialDOUBLELOW.SetTexture("_DetailSteepLowerNormal", textureRef);
                    }
                    if (path.Key == "_DetailSteepMid")
                    {
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPMID.SetTexture("_DetailSteep", textureRef);
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialSINGLEMID.SetTexture("_DetailSteep", textureRef);
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialDOUBLELOW.SetTexture("_DetailSteepHigher", textureRef);
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialDOUBLEHIGH.SetTexture("_DetailSteepLower", textureRef);
                    }
                    if (path.Key == "_DetailSteepNormalMid")
                    {
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPMID.SetTexture("_DetailSteepNormal", textureRef);
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialSINGLEMID.SetTexture("_DetailSteepNormal", textureRef);
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialDOUBLELOW.SetTexture("_DetailSteepHigherNormal", textureRef);
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialDOUBLEHIGH.SetTexture("_DetailSteepLowerNormal", textureRef);
                    }
                    if (path.Key == "_DetailSteepHigh")
                    {
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPHIGH.SetTexture("_DetailSteep", textureRef);
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialSINGLEHIGH.SetTexture("_DetailSteep", textureRef);
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialDOUBLEHIGH.SetTexture("_DetailSteepHigher", textureRef);
                    }
                    if (path.Key == "_DetailSteepNormalHigh")
                    {
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPHIGH.SetTexture("_DetailSteepNormal", textureRef);
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialSINGLEHIGH.SetTexture("_DetailSteepNormal", textureRef);
                        thisBody.ParallaxBodyMaterial.ParallaxMaterialDOUBLEHIGH.SetTexture("_DetailSteepHigherNormal", textureRef);
                    }
                    Debug.Log("2.2");
                    Debug.Log("Set texture: " + path.Key + " // " + textureRef.name);
                    Debug.Log("2.2");
                    //.pqsController.surfaceMaterial.SetTexture(path.Key, textureRef);
                }
            }
                
            //ParallaxShaderLoader.parallaxBodies[planetName].ParallaxBodyMaterial.SetTexturesOnDemand();
            //
            //FlightGlobals.currentMainBody.pqsController.surfaceMaterial = ParallaxShaderLoader.parallaxBodies[planetName].ParallaxBodyMaterial.ParallaxMaterial;
            //FlightGlobals.currentMainBody.pqsController.highQualitySurfaceMaterial = ParallaxShaderLoader.parallaxBodies[planetName].ParallaxBodyMaterial.ParallaxMaterial;
            //FlightGlobals.currentMainBody.pqsController.mediumQualitySurfaceMaterial = ParallaxShaderLoader.parallaxBodies[planetName].ParallaxBodyMaterial.ParallaxMaterial;
            //FlightGlobals.currentMainBody.pqsController.lowQualitySurfaceMaterial = ParallaxShaderLoader.parallaxBodies[planetName].ParallaxBodyMaterial.ParallaxMaterial;
            Debug.Log("Completed load on demand");
        }
        public void AttemptDetailSetLower(ParallaxBody body, Texture2D map, int level)
        {
            body.detailTexturesLower.SetPixels32(map.GetPixels32(), level);
        }
        public void AttemptDetailSetUpper(ParallaxBody body, Texture2D map, int level)
        {
            body.detailTexturesUpper.SetPixels32(map.GetPixels32(), level);
        }
        public Texture2D LoadDDSTexture(byte[] data, string name)
        {
            byte ddsSizeCheck = data[4];
            if (ddsSizeCheck != 124)
            {
                Log("This DDS texture is invalid - Unable to read the size check value from the header.");
            }


            int height = data[13] * 256 + data[12];
            int width = data[17] * 256 + data[16];
            Log("Texture width = " + width);
            Log("Texture height = " + height);


            int DDS_HEADER_SIZE = 128;
            byte[] dxtBytes = new byte[data.Length - DDS_HEADER_SIZE];
            Buffer.BlockCopy(data, DDS_HEADER_SIZE, dxtBytes, 0, data.Length - DDS_HEADER_SIZE);
            int mipMapCount = (data[28]) | (data[29] << 8) | (data[30] << 16) | (data[31] << 24);
            Debug.Log("Mipmap count = " + mipMapCount);

            TextureFormat format = TextureFormat.DXT1;
            if (data[84] == 'D')
            {
                Debug.Log("Texture is a DXT");

                if (data[87] == 49) //Also char '1'
                {
                    Debug.Log("Texture is DXT1");
                    format = TextureFormat.DXT1;
                }
                else if (data[87] == 53)    //Also char '5'
                {
                    Debug.Log("Texture is a DXT5");
                    format = TextureFormat.DXT5;
                }
                else
                {
                    Debug.Log("Texture is not a DXT 1 or DXT5");
                }
            }
            Texture2D texture;
            if (mipMapCount == 1)
            {
                texture = new Texture2D(width, height, format, false);
            }
            else
            {
                texture = new Texture2D(width, height, format, true);
            }
            try
            {
                texture.LoadRawTextureData(dxtBytes);
            }
            catch
            {
                Log("CRITICAL ERROR: Parallax has halted the OnDemand loading process because texture.LoadRawTextureData(dxtBytes) would have resulted in overread");
                Log("Please check the format for this texture and refer to the wiki if you're unsure:");
                Log("Exception: " + name);
            }
            texture.Apply();

            return (texture);
        }
        public Texture2D LoadPNGTexture(string url)
        {
            Texture2D tex;
            tex = new Texture2D(2, 2);
            tex.LoadRawTextureData(File.ReadAllBytes(url));
            tex.Apply();
           
            Debug.Log("Loaded physics image: " + tex.width + " x " + tex.height);
            return tex;
        }
    }
    [KSPAddon(KSPAddon.Startup.Instantly, false)]
    public class FastLoad : MonoBehaviour
    {
        public void Start()
        {
            QualitySettings.vSyncCount = 0; //For some reason VSYNC is forced on during loading. We don't like that. That makes things load slowly.

        }
    }

    //[KSPAddon(KSPAddon.Startup.PSystemSpawn, false)]
    //public class ScaledParallaxLoader : MonoBehaviour
    //{
    //    private GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Sphere);
    //    public void Start()
    //    {
    //        ScaledParallaxBodies.scaledParallaxBodies = new Dictionary<string, ScaledParallaxBody>();
    //        GetConfigNodes();
    //        ActivateConfigNodes();
    //        Debug.Log("Finished scaled parallax");
    //        
    //        
    //        //var planetMeshFilter = fakePlanet.GetComponent<MeshFilter>();//FlightGlobals.GetBodyByName("Gilly").scaledBody.GetComponent<MeshFilter>();
    //        //var planetMeshRenderer = fakePlanet.GetComponent<MeshRenderer>();//FlightGlobals.GetBodyByName("Gilly").scaledBody.GetComponent<MeshRenderer>();
    //        //Destroy(fakePlanet.GetComponent<MeshCollider>());
    //        //Material a = new Material(ParallaxLoader.GetShader("Custom/ParallaxScaled"));
    //        //a.SetTexture("_ColorMap", Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(t => t.name == "Parallax_StockTextures/_Scaled/Duna_Color"));
    //        //a.SetTexture("_NormalMap", Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(t => t.name == "Parallax_StockTextures/_Scaled/Duna_Normal"));
    //        //a.SetTexture("_HeightMap", Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(t => t.name == "Parallax_StockTextures/_Scaled/Duna_Height"));
    //        //a.SetTextureScale("_ColorMap", new Vector2(1, 1));
    //        //a.SetFloat("_PlanetRadius", 1);
    //        //a.SetFloat("_displacement_scale", 0.07f);
    //        //a.SetFloat("_TessellationRange", 10000f);
    //        //a.SetFloat("_TessellationMax", 64);
    //        //a.SetFloat("_SteepPower", 0.001f);
    //        //a.SetFloat("_Metallic", 0.01f);
    //        //a.SetFloat("_TessellationEdgeLength", 8f);
    //        //a.SetFloat("_PlanetOpacity", 1);
    //        //a.SetFloat("_FresnelExponent", 23.6f);
    //        //a.SetFloat("_TransitionWidth", 0.5f);
    //        //a.SetTexture("_FogTexture", Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(t => t.name == "Parallax_StockTextures/_Scaled/Duna_Fog"));
    //        //fakePlanet.SetActive(true);
    //        //planetMeshRenderer.enabled = true;
    //        //planetMeshRenderer.material = a;
    //        //planetMeshRenderer.sharedMaterial = a;
    //        //fakePlanet.layer = 10;
    //        //fakePlanet.transform.parent = FlightGlobals.GetHomeBody().scaledBody.transform;
    //        //fakePlanet.transform.localPosition = new Vector3(0, 0, 0);
    //        //
    //        //fakePlanet.transform.localScale = new Vector3(3000, 3000, 3000);
    //        //Debug.Log("Material set");
    //    }
    //    public void GetConfigNodes()
    //    {
    //        UrlDir.UrlConfig[] nodeArray = GameDatabase.Instance.GetConfigs("ScaledParallax");
    //        for (int i = 0; i < nodeArray.Length; i++)
    //        {
    //            for (int b = 0; b < nodeArray[i].config.nodes.Count; b++)
    //            {
    //                ConfigNode parallax = nodeArray[i].config;
    //                string bodyName = parallax.nodes[b].GetValue("name");
    //                Log("////////////////////////////////////////////////");
    //                Log("////////////////" + bodyName + "////////////////");
    //                Log("////////////////////////////////////////////////\n");
    //                GameObject fakePlanet = GameDatabase.Instance.GetModel("Parallax_StockTextures/_Scaled/KSPPlanet");
    //                fakePlanet.AddComponent<MeshRenderer>();
    //                fakePlanet.SetActive(true);
    //                if (fakePlanet == null)
    //                {
    //                    Debug.Log("It's null you idot");
    //                }
    //                Destroy(fakePlanet.GetComponent<MeshCollider>());
    //                //fakePlanet.GetComponent<MeshFilter>().sharedMesh = GameDatabase.Instance.GetModel("Parallax_StockTextures/_Scaled/KSPPlanet").GetComponent<MeshFilter>().mesh;
    //                ConfigNode parallaxBody = parallax.nodes[b].GetNode("Textures");
    //                if (parallaxBody == null)
    //                {
    //                    Log(" - !!!Parallax Body is null! Cancelling load!!!"); //Essentially, you fucked up
    //                    return;
    //                }
    //                Log(" - Retrieved body node");
    //                ScaledParallaxBody thisBody = new ScaledParallaxBody();
    //                fakePlanet.transform.parent = FlightGlobals.GetBodyByName(bodyName).scaledBody.transform;
    //                fakePlanet.transform.localPosition = new Vector3(0, 0, 0);
    //                fakePlanet.transform.Rotate(0, 90, 0);
    //                float scaledSpaceFactor = (float)FlightGlobals.GetBodyByName(bodyName).Radius / 1000;
    //                fakePlanet.transform.localScale = new Vector3((float)(FlightGlobals.GetBodyByName(bodyName).Radius / scaledSpaceFactor), (float)(FlightGlobals.GetBodyByName(bodyName).Radius / scaledSpaceFactor), (float)(FlightGlobals.GetBodyByName(bodyName).Radius / scaledSpaceFactor)) ;
    //                
    //                fakePlanet.layer = 10;  //So it's visible in scaled space, the jammy bastard
    //                
    //                thisBody.scaledBody = fakePlanet;
    //                thisBody.bodyName = bodyName;
    //                thisBody.scaledMaterial = CreateScaledBodyMaterial(parallaxBody, bodyName);
    //                FlightGlobals.GetBodyByName(bodyName).scaledBody.GetComponent<MeshFilter>().sharedMesh = Instantiate(primitive.GetComponent<MeshFilter>().mesh);
    //
    //                Log(" - Assigned parallax body material");
    //
    //                try
    //                {
    //                    ScaledParallaxBodies.scaledParallaxBodies.Add(bodyName, thisBody); //Add to the list of parallax bodies
    //                    Log(" - Added " + bodyName + "'s parallax config successfully");
    //                    Log(ScaledParallaxBodies.scaledParallaxBodies[bodyName].bodyName);
    //                }
    //                catch (Exception e)
    //                {
    //                    Log(" - Duplicate body detected!\n" + " - " + e.ToString());
    //                    ScaledParallaxBodies.scaledParallaxBodies[bodyName] = thisBody;
    //                    Log(" - Overwriting current body");
    //                }
    //                Log("////////////////////////////////////////////////\n");
    //            }
    //
    //        }
    //    }
    //    public ScaledParallaxBodyMaterial CreateScaledBodyMaterial(ConfigNode scaledBody, string name)
    //    {
    //        ScaledParallaxBodyMaterial material = new ScaledParallaxBodyMaterial();
    //
    //        material.bodyName = name;
    //        material.colorMap = ParseString(scaledBody, "colorMap");
    //        material.normalMap = ParseString(scaledBody, "normalMap");
    //        material.heightMap = ParseString(scaledBody, "heightMap");
    //        material.fogRamp = ParseString(scaledBody, "fogRamp");
    //        material.steepTex = ParseString(scaledBody, "steepTex");
    //        material.steepNormal = ParseString(scaledBody, "steepNormal");
    //
    //        material.tessellationEdgeLength = 5; //Constants for now
    //        material.tessellationRange = 10000; //Hella risky, yo
    //        material.tessellationMax = 64;
    //
    //        material.displacementOffset = 0f;
    //        material.displacementScale = ParseFloat(scaledBody, "displacementScale");
    //        material.metallic = ParseFloat(scaledBody, "metallic");
    //        material.normalSpecularInfluence = ParseFloat(scaledBody, "normalSpecularInfluence");
    //        bool hasEmission = ParseBool(scaledBody, "hasEmission");
    //        if (hasEmission == true)
    //        {
    //            material.hasEmission = 1;
    //        }
    //        else
    //        {
    //            material.hasEmission = 0;
    //        }
    //        
    //
    //        material.fresnelExponent = ParseFloat(scaledBody, "fresnelExponent");
    //        material.fresnelWidth = ParseFloat(scaledBody, "fresnelWidth");
    //
    //        string color = ParseString(scaledBody, "metallicTint"); //it pains me to write colour this way as a brit
    //        material.metallicTint = new Color(float.Parse(color.Split(',')[0]), float.Parse(color.Split(',')[1]), float.Parse(color.Split(',')[2]));
    //        //Might as well reuse the same variable
    //        color = ParseString(scaledBody, "emissionColor");
    //        if (color != null)
    //        {
    //            material.emissionColor = new Color(float.Parse(color.Split(',')[0]), float.Parse(color.Split(',')[1]), float.Parse(color.Split(',')[2]));
    //        }
    //        return material;
    //    }
    //    
    //    public void ActivateConfigNodes()
    //    {
    //
    //        foreach (KeyValuePair<string, ScaledParallaxBody> body in ScaledParallaxBodies.scaledParallaxBodies)
    //        {
    //            Log("////////////////////////////////////////////////");
    //            Log("////////////////" + body.Key + "////////////////");
    //            Log("////////////////////////////////////////////////\n");
    //            body.Value.scaledMaterial.GenerateMaterial(body.Value.scaledMaterial);
    //            Log(" - Created material successfully");
    //
    //
    //        }
    //    }
    //    public string ParseString(ConfigNode node, string input)
    //    {
    //        string output = node.GetValue(input);
    //        if (output == null)
    //        {
    //            Log("NullReferenceException: " + node.name + " was not defined in the config and has been automatically set to NULL");
    //            return "NULL";
    //        }
    //        else { return output; }
    //    }
    //    public float ParseFloat(ConfigNode node, string input)
    //    {
    //        string output = node.GetValue(input);
    //        float realOutput = 0;
    //        if (output == null)
    //        {
    //            Log("NullReferenceException: " + node.name + " was not defined in the config and has been automatically set to 0");
    //            return 0;
    //        }
    //        else
    //        {
    //            try
    //            {
    //                realOutput = float.Parse(output);
    //            }
    //            catch
    //            {
    //                Log("InvalidTypeException: " + node.name + " was defined, could not be converted to a float, and has been automatically set to 0");
    //                return 0;
    //            }
    //        }
    //        return realOutput;
    //    }
    //    public bool ParseBool(ConfigNode node, string input)
    //    {
    //        string output = node.GetValue(input).ToLower();
    //        bool realOutput = false;
    //        if (output == null)
    //        {
    //            Log("NullReferenceException: " + node.name + " was not defined in the config and has been automatically set to FALSE");
    //            return false;
    //        }
    //        else
    //        {
    //            try
    //            {
    //                realOutput = bool.Parse(output);
    //            }
    //            catch
    //            {
    //                Log("InvalidTypeException: " + node.name + " was defined, could not be converted to a float, and has been automatically set to FALSE");
    //                return false;
    //            }
    //        }
    //        return realOutput;
    //    }
    //    public void Log(string message)
    //    {
    //        Debug.Log("[ParallaxScaled] " + message);
    //    }
    //    
    //}
    //public class ScaledParallaxBody
    //{
    //    public string bodyName;
    //    public ScaledParallaxBodyMaterial scaledMaterial;
    //    public GameObject scaledBody;
    //}
    //public class ScaledParallaxBodyMaterial
    //{
    //    public Material material;
    //    public string bodyName;
    //
    //    public string colorMap; //Textures
    //    public string normalMap;
    //    public string heightMap;
    //    public string fogRamp; 
    //    public string steepTex; //Really couldn't be bothered to use get-sets here. Might remedy in the future, idk
    //    public string steepNormal;
    //    
    //    public float tessellationMax;   //Quality settings
    //    public float tessellationEdgeLength;
    //    public float tessellationRange;
    //    public float displacementScale;
    //    public float displacementOffset = -0.5f;
    //
    //    public float metallic;  //PBR settings
    //    public Color metallicTint;
    //    public Color emissionColor;
    //    public float hasEmission;
    //    public float normalSpecularInfluence;
    //        
    //    public float fresnelExponent;   //Atmosphere settings
    //    public float fresnelWidth;
    //    
    //    public ScaledParallaxBody GetScaledParallaxBody(string bodyName)
    //    {
    //        return ScaledParallaxBodies.scaledParallaxBodies[bodyName];
    //    }
    //    public void GenerateMaterial(ScaledParallaxBodyMaterial scaledBody)
    //    {
    //        Material scaledMaterial = new Material(ParallaxLoader.GetShader("Custom/ParallaxScaled"));
    //        scaledMaterial.SetTexture("_ColorMap", LoadTexture(scaledBody.colorMap));
    //        scaledMaterial.SetTexture("_NormalMap", LoadTexture(scaledBody.normalMap));
    //        scaledMaterial.SetTexture("_HeightMap", LoadTexture(scaledBody.heightMap));
    //        scaledMaterial.SetTexture("_FogTexture", LoadTexture(scaledBody.fogRamp));
    //        scaledMaterial.SetTexture("_SteepMap", LoadTexture(scaledBody.steepTex));
    //        scaledMaterial.SetTexture("_SteepNormal", LoadTexture(scaledBody.steepNormal));
    //
    //        scaledMaterial.SetFloat("_TessellationEdgeLength", scaledBody.tessellationEdgeLength);
    //        scaledMaterial.SetFloat("_TessellationMax", scaledBody.tessellationMax);
    //        scaledMaterial.SetFloat("_TessellationRange", scaledBody.tessellationRange);
    //        scaledMaterial.SetFloat("_displacement_offset", scaledBody.displacementOffset);
    //        scaledMaterial.SetFloat("_displacement_scale", scaledBody.displacementScale);
    //        scaledMaterial.SetFloat("_Metallic", scaledBody.metallic);
    //        scaledMaterial.SetColor("_MetallicTint", scaledBody.metallicTint);
    //        scaledMaterial.SetFloat("_TessellationEdgeLength", scaledBody.tessellationEdgeLength);
    //        scaledMaterial.SetFloat("_HasEmission", scaledBody.hasEmission);
    //        scaledMaterial.SetFloat("_FresnelExponent", scaledBody.fresnelExponent);
    //        scaledMaterial.SetFloat("_FresnelWidth", scaledBody.fresnelWidth);
    //        scaledMaterial.SetColor("_EmissionColor", scaledBody.emissionColor);
    //        scaledMaterial.SetFloat("_PlanetRadius", 1);
    //        scaledMaterial.SetFloat("_SteepPower", 0.0001f);
    //
    //        scaledBody.material = scaledMaterial;
    //        scaledBody.GetScaledParallaxBody(scaledBody.bodyName).scaledBody.GetComponent<MeshRenderer>().material = scaledBody.material;
    //        scaledBody.GetScaledParallaxBody(scaledBody.bodyName).scaledBody.GetComponent<MeshRenderer>().sharedMaterial = scaledBody.material;
    //    }
    //    public Texture LoadTexture(string name)
    //    {
    //        try
    //        {
    //            return Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(t => t.name == name);
    //        }
    //        catch
    //        {
    //            Debug.Log("The texture, '" + name + "', could not be found");
    //            return Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(t => t.name == "TessellationBlank");
    //        }
    //    }
    //}
    //public class ScaledParallaxBodies
    //{
    //    public static Dictionary<string, ScaledParallaxBody> scaledParallaxBodies;
    //}
    //[KSPAddon(KSPAddon.Startup.TrackingStation, false)]
    //public class EnableTrackingShadows : MonoBehaviour
    //{
    //    public void Start()
    //    {
    //        //QualitySettings.shadowDistance = 100000;
    //        //QualitySettings.shadowCascades = 4;
    //        //QualitySettings.shadowProjection = ShadowProjection.StableFit;
    //        //QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
    //        //QualitySettings.shadows = ShadowQuality.All;
    //        //Light light = GameObject.Find("Scaledspace SunLight").GetComponent<Light>();
    //        //if (light == null)
    //        //{
    //        //    Debug.Log("Light doesn't exist");
    //        //}
    //        //light.shadows = LightShadows.Soft;
    //        //light.shadowStrength = 1;
    //        //light.shadowBias = 0.05f;
    //        //light.shadowNormalBias = 0.4f;
    //        //light.shadowNearPlane = 0.2f;
    //        //light.lightShadowCasterMode = LightShadowCasterMode.Everything;
    //        //Debug.Log("Enabled shadows, probably. Idk");
    //
    //    }
    //}
    //[KSPAddon(KSPAddon.Startup.Flight, false)]
    //public class HyperEditGUI : MonoBehaviour
    //{
    //    public void Update()
    //    {
    //        double liquidFuel = 0;
    //        double maxLF = 0;
    //        foreach (Part part in FlightGlobals.ActiveVessel.parts)
    //        {
    //            if (part.partInfo.category == PartCategories.FuelTank)
    //            {
    //                liquidFuel += part.Resources[0].amount;
    //                maxLF += part.Resources[0].maxAmount;
    //            }
    //        }
    //        double percentage = (liquidFuel / maxLF);
    //        //ScaledParallaxBodies.scaledParallaxBodies[FlightGlobals.currentMainBody.name].scaledMaterial.material.SetFloat("_displacement_scale", (float)percentage);
    //        ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialDOUBLEHIGH.SetFloat("_DetailOffset", (float)percentage);
    //        ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialDOUBLELOW.SetFloat("_DetailOffset", (float)percentage);
    //        ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLEHIGH.SetFloat("_DetailOffset", (float)percentage);
    //        ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLEMID.SetFloat("_DetailOffset", (float)percentage);
    //
    //        ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLELOW.SetFloat("_DetailOffset", (float)percentage);
    //        ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPHIGH.SetFloat("_DetailOffset", (float)percentage);
    //        ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPMID.SetFloat("_DetailOffset", (float)percentage);
    //        ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPLOW.SetFloat("_DetailOffset", (float)percentage);
    //        ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterial.SetFloat("_DetailOffset", (float)percentage);
    //    }
    //}
}
