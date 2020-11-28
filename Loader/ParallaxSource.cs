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
            //Locate and load the shaders. Then stored in Kopernicus shader dictionary
            //ShaderLoader.LoadAssetBundle("Terrain/Shaders", "ParallaxOcclusion");
            //var assetBundle = AssetBundle.LoadFromFile(
            string filePath = Path.Combine(KSPUtil.ApplicationRootPath + "GameData/" + "Parallax/Shaders/Parallax");
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
                    Debug.Log($"Loaded shader: {thisShader.name} {thisShader.isSupported}");
                }



            }






            filePath = Path.Combine(KSPUtil.ApplicationRootPath + "GameData/" + "Parallax/Shaders/Grass");

            if (Application.platform == RuntimePlatform.WindowsPlayer && SystemInfo.graphicsDeviceVersion.StartsWith("OpenGL"))
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
            var assetBundle2 = AssetBundle.LoadFromFile(filePath);
            Debug.Log("Loaded bundle");
            if (assetBundle2 == null)
            {
                Debug.Log("Failed to load bundle");
                Debug.Log(filePath);
            }
            else
            {
                Shader[] theseShaders = assetBundle2.LoadAllAssets<Shader>();
                Debug.Log("Loaded all shaders");
                foreach (Shader thisShader in theseShaders)
                {
                    shaders.Add(thisShader.name, thisShader);
                    Debug.Log("Loaded shader: " + thisShader.name);
                }



            }

            filePath = Path.Combine(KSPUtil.ApplicationRootPath + "GameData/" + "Parallax/Shaders/ParallaxScaled");

            if (Application.platform == RuntimePlatform.WindowsPlayer && SystemInfo.graphicsDeviceVersion.StartsWith("OpenGL"))
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
            var assetBundle3 = AssetBundle.LoadFromFile(filePath);
            Debug.Log("Loaded bundle");
            if (assetBundle3 == null)
            {
                Debug.Log("Failed to load bundle");
                Debug.Log(filePath);
            }
            else
            {
                Shader[] theseShaders = assetBundle3.LoadAllAssets<Shader>();
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
            pqsMaterial = Instantiate(body.pqsController.surfaceMaterial);

            foreach (ReflectionProbe probe in FindObjectsOfType(typeof(ReflectionProbe)))
            {
                Debug.Log(probe.name + " is an active reflection probe");
            }
        }
        public void Update()
        {

            if (FlightGlobals.currentMainBody != body)
            {
                body.pqsController.surfaceMaterial = pqsMaterial;
                Start();
            }

            //float fadeStart = 60000;//FlightGlobals.currentMainBody.GetComponent<ScaledSpaceFader>().fadeStart;
            //float fadeEnd = 100000;//FlightGlobals.currentMainBody.GetComponent<ScaledSpaceFader>().fadeEnd;
            //float cameraAltitude = Vector3.Distance(Camera.main.transform.position, FlightGlobals.currentMainBody.transform.position) - (float)FlightGlobals.currentMainBody.Radius;
            //float fadeMult = Mathf.Clamp((cameraAltitude - fadeStart) / (fadeEnd - fadeStart), 0, 1);
            //
            //FlightGlobals.currentMainBody.pqsController.surfaceMaterial.Lerp(pqsMaterial, scaledMaterial, fadeMult);

            if (ParallaxShaderLoader.parallaxBodies.ContainsKey(FlightGlobals.currentMainBody.name))
            {
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
                        float terrainHeightOfCamera = GetHeightFromTerrain(Camera.main.transform);
                        double terrainHeightOfCraft = FlightGlobals.ActiveVessel.radarAltitude;

                        Vector3 reflectionProbePositionChange = new Vector3((float)cameraToPlanet.x * (float)terrainHeightOfCamera * 2, (float)cameraToPlanet.y * (float)terrainHeightOfCamera * 2, (float)cameraToPlanet.z * (float)terrainHeightOfCamera * 2);
                        Vector3 reflectionProbePosition = Camera.main.transform.position - reflectionProbePositionChange;

                        ParallaxReflectionProbes.probe.GetComponent<ReflectionProbe>().transform.position = reflectionProbePosition;
                        Debug.Log("Camera alt: " + terrainHeightOfCamera + ", craft alt: " + terrainHeightOfCraft);
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
            return heightFromTerrain;
        }
        public IEnumerator UpdateProbe()    //Optimize the rendering of the reflection probe so that it doesn't kill the framerate
        {
            Debug.Log("Update fired");
            if (ParallaxReflectionProbes.probeActive == true)
            {
                Debug.Log("Probe is active");
                ParallaxReflectionProbes.probe.GetComponent<ReflectionProbe>().RenderProbe(); //currently every frame
                var tex = ParallaxReflectionProbes.probe.GetComponent<ReflectionProbe>().texture;
                FlightGlobals.currentMainBody.pqsController.surfaceMaterial.SetTexture("_ReflectionMap", tex);
                foreach (KeyValuePair<string, GameObject> quad in QuadMeshDictionary.subdividedQuadList)
                {
                    quad.Value.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_ReflectionMap", tex);
                }
                Debug.Log("Updated reflection map");
            }
            yield return new WaitForSeconds(1 / ParallaxSettings.refreshRate);   //30fps
            StartCoroutine(UpdateProbe());
        }
    }
    [KSPAddon(KSPAddon.Startup.FlightAndKSC, false)]
    public class Position : MonoBehaviour
    {
        public CelestialBody lastBody;
        public PQSMod_CelestialBodyTransform fader;
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

                body.pqsController.lowQualitySurfaceMaterial.SetVector("_LightPos", (Vector3)(FlightGlobals.Bodies[0].transform.position));
                body.pqsController.mediumQualitySurfaceMaterial.SetVector("_LightPos", (Vector3)(FlightGlobals.Bodies[0].transform.position));
                body.pqsController.surfaceMaterial.SetVector("_LightPos", (Vector3)(FlightGlobals.Bodies[0].transform.position));
                body.pqsController.highQualitySurfaceMaterial.SetVector("_LightPos", (Vector3)(FlightGlobals.Bodies[0].transform.position));
                body.pqsController.ultraQualitySurfaceMaterial.SetVector("_LightPos", (Vector3)(FlightGlobals.Bodies[0].transform.position));

                ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterial.SetVector("_PlanetOrigin", (Vector3)body.transform.position);
                ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterial.SetVector("_LightPos", (Vector3)(FlightGlobals.Bodies[0].transform.position));

                ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPLOW.SetVector("_PlanetOrigin", (Vector3)body.transform.position);
                ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPLOW.SetVector("_LightPos", (Vector3)(FlightGlobals.Bodies[0].transform.position));
                ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPMID.SetVector("_PlanetOrigin", (Vector3)body.transform.position);
                ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPMID.SetVector("_LightPos", (Vector3)(FlightGlobals.Bodies[0].transform.position));
                ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPHIGH.SetVector("_PlanetOrigin", (Vector3)body.transform.position);
                ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPHIGH.SetVector("_LightPos", (Vector3)(FlightGlobals.Bodies[0].transform.position));

                ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLELOW.SetVector("_PlanetOrigin", (Vector3)body.transform.position);
                ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLELOW.SetVector("_LightPos", (Vector3)(FlightGlobals.Bodies[0].transform.position));
                ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLEMID.SetVector("_PlanetOrigin", (Vector3)body.transform.position);
                ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLEMID.SetVector("_LightPos", (Vector3)(FlightGlobals.Bodies[0].transform.position));
                ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLEHIGH.SetVector("_PlanetOrigin", (Vector3)body.transform.position);
                ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialSINGLEHIGH.SetVector("_LightPos", (Vector3)(FlightGlobals.Bodies[0].transform.position));

                ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialDOUBLELOW.SetVector("_PlanetOrigin", (Vector3)body.transform.position);
                ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialDOUBLELOW.SetVector("_LightPos", (Vector3)(FlightGlobals.Bodies[0].transform.position));
                ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialDOUBLEHIGH.SetVector("_PlanetOrigin", (Vector3)body.transform.position);
                ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.ParallaxMaterialDOUBLEHIGH.SetVector("_LightPos", (Vector3)(FlightGlobals.Bodies[0].transform.position));


            }

            if (lastBody != body)
            {
                foreach (PQSMod mod in body.GetComponentsInChildren<PQSMod>())
                {
                    if (mod is PQSMod_CelestialBodyTransform)
                    {
                        fader = (PQSMod_CelestialBodyTransform)mod;
                    }
                }

                float fadeStart = fader.planetFade.fadeStart;
                float fadeEnd = fader.planetFade.fadeEnd;

                body.pqsController.surfaceMaterial.SetFloat("_FadeStart", fadeStart);
                body.pqsController.lowQualitySurfaceMaterial.SetFloat("_FadeStart", fadeStart);
                body.pqsController.mediumQualitySurfaceMaterial.SetFloat("_FadeStart", fadeStart);
                body.pqsController.highQualitySurfaceMaterial.SetFloat("_FadeStart", fadeStart);
                body.pqsController.ultraQualitySurfaceMaterial.SetFloat("_FadeStart", fadeStart);

                body.pqsController.surfaceMaterial.SetFloat("_FadeEnd", fadeEnd);
                body.pqsController.lowQualitySurfaceMaterial.SetFloat("_FadeEnd", fadeEnd);
                body.pqsController.mediumQualitySurfaceMaterial.SetFloat("_FadeEnd", fadeEnd);
                body.pqsController.highQualitySurfaceMaterial.SetFloat("_FadeEnd", fadeEnd);
                body.pqsController.ultraQualitySurfaceMaterial.SetFloat("_FadeEnd", fadeEnd);
            }

            if (FlightGlobals.ActiveVessel != null)
            {
                if (HighLogic.LoadedScene == GameScenes.FLIGHT && ParallaxShaderLoader.parallaxBodies.ContainsKey(body.name))
                {

                    Vector3d accuratePlanetPosition = FlightGlobals.currentMainBody.position;   //Double precision planet origin
                    double surfaceTexture_ST = ParallaxShaderLoader.parallaxBodies[FlightGlobals.currentMainBody.name].ParallaxBodyMaterial.SurfaceTextureScale;    //Scale of surface texture
                    Vector3d UV = accuratePlanetPosition * surfaceTexture_ST;
                    UV = new Vector3d(Clamp(UV.x), Clamp(UV.y), Clamp(UV.z));
                    Vector3 floatUV = new Vector3((float)UV.x, (float)UV.y, (float)UV.z);
                    //FlightGlobals.currentMainBody.pqsController.surfaceMaterial.SetVector("_SurfaceTextureUVs", floatUV);
                    //FlightGlobals.currentMainBody.pqsController.highQualitySurfaceMaterial.SetVector("_SurfaceTextureUVs", floatUV);
                    //FlightGlobals.currentMainBody.pqsController.mediumQualitySurfaceMaterial.SetVector("_SurfaceTextureUVs", floatUV);
                    //FlightGlobals.currentMainBody.pqsController.lowQualitySurfaceMaterial.SetVector("_SurfaceTextureUVs", floatUV);
                    //FlightGlobals.currentMainBody.pqsController.ultraQualitySurfaceMaterial.SetVector("_SurfaceTextureUVs", floatUV);

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

            //var planetMeshFilter = FlightGlobals.GetBodyByName("Kerbin").scaledBody.GetComponent<MeshFilter>();
            //planetMeshFilter.mesh = Resources.FindObjectsOfTypeAll<Mesh>().FirstOrDefault(t => t.name == "ParallaxStockTextures/_Scaled/KSPPlanetHuge");
            //if (planetMeshFilter.mesh != null)
            //{
            //    Debug.Log("Mesh not null");
            //}
            //var planetMeshRenderer = FlightGlobals.GetBodyByName("Kerbin").scaledBody.GetComponent<MeshRenderer>();
            //
            //Material a = new Material(ParallaxLoader.GetShader("Custom/ParallaxScaled"));
            //a.SetTexture("_ColorMap", Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(t => t.name == "ParallaxStockTextures/_Scaled/Kerbin_Color"));
            //a.SetTexture("_NormalMap", Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(t => t.name == "ParallaxStockTextures/_Scaled/Kerbin_Normal"));
            //a.SetTexture("_HeightMap", Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(t => t.name == "ParallaxStockTextures/_Scaled/Kerbin_Height"));
            //a.SetTextureScale("_ColorMap", new Vector2(1, 1));
            //a.SetFloat("_PlanetRadius", (float)(FlightGlobals.GetBodyByName("Kerbin").Radius / 600));
            //a.SetFloat("_displacement_scale", 0f);
            //a.SetFloat("_Metallic", 0.01f);
            //a.SetFloat("_TessellationEdgeLength", 1f);
            //a.SetFloat("_PlanetOpacity", 1);
            //a.SetFloat("_FresnelExponent", 23.6f);
            //a.SetFloat("_TransitionWidth", 0.5f);
            //a.SetTexture("_FogTexture", Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(t => t.name == "ParallaxStockTextures/_Scaled/Kerbin_Fog"));
            //
            //planetMeshRenderer.material = a;
            //Debug.Log("Material set");
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
                    Debug.Log("Error parsing bool: " + value);
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
                    Debug.Log("Error parsing bool: " + value);
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
                    Debug.Log("Error parsing vector4: " + value);
                    return realOutput;
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
    public class ParallaxBody
    {
        private string bodyName;
        private ParallaxBodyMaterial parallaxBodyMaterial;
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

        private bool useReflections = false;
        private Vector4 reflectionMask = new Vector4(0, 0, 0, 0);

        private int hasEmission = 0;
        private Color emissionColor = new Color(0, 0, 0);

        #region getsets
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

            parallaxMaterialSINGLESTEEPLOW.SetVector("_ReflectionMask", reflectionMask);
            parallaxMaterialSINGLESTEEPLOW.SetFloat("_TessellationEdgeLength", ParallaxSettings.tessellationEdgeLength);
            parallaxMaterialSINGLESTEEPLOW.SetFloat("_TessellationRange", ParallaxSettings.tessellationRange);
            parallaxMaterialSINGLESTEEPLOW.SetFloat("_TessellationMax", ParallaxSettings.tessellationMax);


            //parallaxMaterialSINGLESTEEPMID.SetTexture("_SurfaceTexture", LoadTexture(surfaceTextureMid));
           // parallaxMaterialSINGLESTEEPMID.SetTexture("_DispTex", LoadTexture(surfaceTextureParallaxMap));
            //parallaxMaterialSINGLESTEEPMID.SetTexture("_BumpMap", LoadTexture(bumpMapMid));
            //parallaxMaterialSINGLESTEEPMID.SetTexture("_SteepTex", LoadTexture(steepTexture));
            parallaxMaterialSINGLESTEEPMID.SetFloat("_SteepPower", steepPower);
            parallaxMaterialSINGLESTEEPMID.SetTextureScale("_SurfaceTexture", CreateVector(surfaceTextureScale));
            parallaxMaterialSINGLESTEEPMID.SetFloat("_displacement_scale", surfaceParallaxHeight);
            parallaxMaterialSINGLESTEEPMID.SetTextureScale("_SteepTex", CreateVector(steepTextureScale));
            parallaxMaterialSINGLESTEEPMID.SetFloat("_Metallic", smoothness);
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

            //parallaxMaterialSINGLESTEEPHIGH.SetTexture("_SurfaceTexture", LoadTexture(surfaceTextureHigh));
            //parallaxMaterialSINGLESTEEPHIGH.SetTexture("_DispTex", LoadTexture(surfaceTextureParallaxMap));
            //parallaxMaterialSINGLESTEEPHIGH.SetTexture("_BumpMap", LoadTexture(bumpMapHigh));
            //parallaxMaterialSINGLESTEEPHIGH.SetTexture("_SteepTex", LoadTexture(steepTexture));
            parallaxMaterialSINGLESTEEPHIGH.SetFloat("_SteepPower", steepPower);
            parallaxMaterialSINGLESTEEPHIGH.SetTextureScale("_SurfaceTexture", CreateVector(surfaceTextureScale));
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


            //PARALLAX DOUBLE

            //parallaxMaterialDOUBLELOW.SetTexture("_SurfaceTextureLower", LoadTexture(surfaceTexture));
            //parallaxMaterialDOUBLELOW.SetTexture("_DispTex", LoadTexture(surfaceTextureParallaxMap));
            //parallaxMaterialDOUBLELOW.SetTexture("_BumpMapLower", LoadTexture(surfaceTextureBumpMap));
            //parallaxMaterialDOUBLELOW.SetTexture("_SteepTex", LoadTexture(steepTexture));
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


            //parallaxMaterialDOUBLEHIGH.SetTexture("_SurfaceTextureLower", LoadTexture(surfaceTextureMid));
            //parallaxMaterialDOUBLEHIGH.SetTexture("_DispTex", LoadTexture(surfaceTextureParallaxMap));
            //parallaxMaterialDOUBLEHIGH.SetTexture("_BumpMapLower", LoadTexture(bumpMapMid));
            //parallaxMaterialDOUBLEHIGH.SetTexture("_SteepTex", LoadTexture(steepTexture));
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

            EnableKeywordFromInteger(parallaxMaterial, hasEmission, "EMISSION_ON", "EMISSION_OFF");
            EnableKeywordFromInteger(parallaxMaterialSINGLELOW, hasEmission, "EMISSION_ON", "EMISSION_OFF");
            EnableKeywordFromInteger(parallaxMaterialSINGLEMID, hasEmission, "EMISSION_ON", "EMISSION_OFF");
            EnableKeywordFromInteger(parallaxMaterialSINGLEHIGH, hasEmission, "EMISSION_ON", "EMISSION_OFF");
            EnableKeywordFromInteger(parallaxMaterialSINGLESTEEPLOW, hasEmission, "EMISSION_ON", "EMISSION_OFF");
            EnableKeywordFromInteger(parallaxMaterialSINGLESTEEPMID, hasEmission, "EMISSION_ON", "EMISSION_OFF");
            EnableKeywordFromInteger(parallaxMaterialSINGLESTEEPHIGH, hasEmission, "EMISSION_ON", "EMISSION_OFF");
            EnableKeywordFromInteger(parallaxMaterialDOUBLELOW, hasEmission, "EMISSION_ON", "EMISSION_OFF");
            EnableKeywordFromInteger(parallaxMaterialDOUBLEHIGH, hasEmission, "EMISSION_ON", "EMISSION_OFF");


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
        }
        public void SetTexturesOnDemand()
        {
            Material newparallaxMaterial = Instantiate(parallaxMaterial);
            foreach (KeyValuePair<string, Texture2D> texture in ParallaxOnDemandLoader.activeTextures)
            {
                parallaxMaterial.SetTexture(texture.Key, texture.Value);
                Debug.Log(texture.Value.width);
                Log("[OnDemand] Set texture: " + texture.Key);
            }
            parallaxMaterial = newparallaxMaterial;
            FlightGlobals.currentMainBody.pqsController.surfaceMaterial = parallaxMaterial;
            FlightGlobals.currentMainBody.pqsController.highQualitySurfaceMaterial = parallaxMaterial;
            FlightGlobals.currentMainBody.pqsController.mediumQualitySurfaceMaterial = parallaxMaterial;
            FlightGlobals.currentMainBody.pqsController.lowQualitySurfaceMaterial = parallaxMaterial;
            FlightGlobals.currentMainBody.pqsController.ultraQualitySurfaceMaterial = parallaxMaterial;
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
    class ParallaxOnDemandLoader : MonoBehaviour
    {
        bool thisBodyIsLoaded = false;
        CelestialBody lastKnownBody;
        CelestialBody currentBody;
        public static Dictionary<string, Texture2D> activeTextures = new Dictionary<string, Texture2D>();
        public static Dictionary<string, string> activeTexturePaths = new Dictionary<string, string>();
        float timeElapsed = 0;
        public void Start()
        {
            Log("Starting Parallax On-Demand loader");
        }
        public void Update()
        {
            timeElapsed = Time.realtimeSinceStartup;
            currentBody = FlightGlobals.currentMainBody;
            bool key = Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.Alpha2);
            if (currentBody != lastKnownBody)           //Vessel is around a new planet, so its textures must be loaded
            {
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
        }
        public void ValidatePath(string path, string name)
        {
            string actualPath = Application.dataPath.Remove(Application.dataPath.Length - 12, 12) + "GameData/" + path;
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
            foreach (KeyValuePair<string, string> path in activeTexturePaths)
            {
                activeTextures.Add(path.Key, Texture2D.blackTexture);
                Texture2D textureRef = activeTextures[path.Key];
                byte[] bytes = System.IO.File.ReadAllBytes(path.Value);

                textureRef = LoadDDSTexture(bytes);

                activeTextures[path.Key] = textureRef;

                //FlightGlobals.currentMainBody.pqsController.surfaceMaterial.SetTexture(path.Key, textureRef);
                ParallaxShaderLoader.parallaxBodies[planetName].ParallaxBodyMaterial.ParallaxMaterial.SetTexture(path.Key, textureRef);
                ParallaxShaderLoader.parallaxBodies[planetName].ParallaxBodyMaterial.ParallaxMaterialDOUBLEHIGH.SetTexture(path.Key, textureRef);
                ParallaxShaderLoader.parallaxBodies[planetName].ParallaxBodyMaterial.ParallaxMaterialDOUBLELOW.SetTexture(path.Key, textureRef);
                ParallaxShaderLoader.parallaxBodies[planetName].ParallaxBodyMaterial.ParallaxMaterialSINGLELOW.SetTexture(path.Key, textureRef);
                ParallaxShaderLoader.parallaxBodies[planetName].ParallaxBodyMaterial.ParallaxMaterialSINGLEMID.SetTexture(path.Key, textureRef);
                ParallaxShaderLoader.parallaxBodies[planetName].ParallaxBodyMaterial.ParallaxMaterialSINGLEHIGH.SetTexture(path.Key, textureRef);
                ParallaxShaderLoader.parallaxBodies[planetName].ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPLOW.SetTexture(path.Key, textureRef);
                ParallaxShaderLoader.parallaxBodies[planetName].ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPMID.SetTexture(path.Key, textureRef);
                ParallaxShaderLoader.parallaxBodies[planetName].ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPHIGH.SetTexture(path.Key, textureRef);

                if (path.Key == "_SurfaceTexture")
                {
                    ParallaxShaderLoader.parallaxBodies[planetName].ParallaxBodyMaterial.ParallaxMaterialSINGLELOW.SetTexture("_SurfaceTexture", textureRef);
                    ParallaxShaderLoader.parallaxBodies[planetName].ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPLOW.SetTexture("_SurfaceTexture", textureRef);
                    ParallaxShaderLoader.parallaxBodies[planetName].ParallaxBodyMaterial.ParallaxMaterialDOUBLELOW.SetTexture("_SurfaceTextureLower", textureRef);
                }
                if (path.Key == "_SurfaceTextureMid")
                {
                    ParallaxShaderLoader.parallaxBodies[planetName].ParallaxBodyMaterial.ParallaxMaterialSINGLEMID.SetTexture("_SurfaceTexture", textureRef);
                    ParallaxShaderLoader.parallaxBodies[planetName].ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPMID.SetTexture("_SurfaceTexture", textureRef);
                    ParallaxShaderLoader.parallaxBodies[planetName].ParallaxBodyMaterial.ParallaxMaterialDOUBLEHIGH.SetTexture("_SurfaceTextureLower", textureRef);
                    ParallaxShaderLoader.parallaxBodies[planetName].ParallaxBodyMaterial.ParallaxMaterialDOUBLELOW.SetTexture("_SurfaceTextureHigher", textureRef);
                }
                if (path.Key == "_SurfaceTextureHigh")
                {
                    ParallaxShaderLoader.parallaxBodies[planetName].ParallaxBodyMaterial.ParallaxMaterialSINGLEHIGH.SetTexture("_SurfaceTexture", textureRef);
                    ParallaxShaderLoader.parallaxBodies[planetName].ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPHIGH.SetTexture("_SurfaceTexture", textureRef);
                    ParallaxShaderLoader.parallaxBodies[planetName].ParallaxBodyMaterial.ParallaxMaterialDOUBLEHIGH.SetTexture("_SurfaceTextureHigher", textureRef);
                }
                if (path.Key == "_BumpMap")
                {
                    ParallaxShaderLoader.parallaxBodies[planetName].ParallaxBodyMaterial.ParallaxMaterialSINGLELOW.SetTexture("_BumpMap", textureRef);
                    ParallaxShaderLoader.parallaxBodies[planetName].ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPLOW.SetTexture("_BumpMap", textureRef);
                    ParallaxShaderLoader.parallaxBodies[planetName].ParallaxBodyMaterial.ParallaxMaterialDOUBLELOW.SetTexture("_BumpMapLower", textureRef);
                }
                if (path.Key == "_BumpMapMid")
                {
                    ParallaxShaderLoader.parallaxBodies[planetName].ParallaxBodyMaterial.ParallaxMaterialSINGLEMID.SetTexture("_BumpMap", textureRef);
                    ParallaxShaderLoader.parallaxBodies[planetName].ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPMID.SetTexture("_BumpMap", textureRef);
                    ParallaxShaderLoader.parallaxBodies[planetName].ParallaxBodyMaterial.ParallaxMaterialDOUBLEHIGH.SetTexture("_BumpMapLower", textureRef);
                    ParallaxShaderLoader.parallaxBodies[planetName].ParallaxBodyMaterial.ParallaxMaterialDOUBLELOW.SetTexture("_BumpMapHigher", textureRef);
                }
                if (path.Key == "_BumpMapHigh")
                {
                    ParallaxShaderLoader.parallaxBodies[planetName].ParallaxBodyMaterial.ParallaxMaterialSINGLEHIGH.SetTexture("_BumpMap", textureRef);
                    ParallaxShaderLoader.parallaxBodies[planetName].ParallaxBodyMaterial.ParallaxMaterialSINGLESTEEPHIGH.SetTexture("_BumpMap", textureRef);
                    ParallaxShaderLoader.parallaxBodies[planetName].ParallaxBodyMaterial.ParallaxMaterialDOUBLEHIGH.SetTexture("_BumpMapHigher", textureRef);
                }
                Debug.Log("Set texture: " + path.Key + " // " + textureRef.name);
                //.pqsController.surfaceMaterial.SetTexture(path.Key, textureRef);
            }
            //ParallaxShaderLoader.parallaxBodies[planetName].ParallaxBodyMaterial.SetTexturesOnDemand();
            //
            //FlightGlobals.currentMainBody.pqsController.surfaceMaterial = ParallaxShaderLoader.parallaxBodies[planetName].ParallaxBodyMaterial.ParallaxMaterial;
            //FlightGlobals.currentMainBody.pqsController.highQualitySurfaceMaterial = ParallaxShaderLoader.parallaxBodies[planetName].ParallaxBodyMaterial.ParallaxMaterial;
            //FlightGlobals.currentMainBody.pqsController.mediumQualitySurfaceMaterial = ParallaxShaderLoader.parallaxBodies[planetName].ParallaxBodyMaterial.ParallaxMaterial;
            //FlightGlobals.currentMainBody.pqsController.lowQualitySurfaceMaterial = ParallaxShaderLoader.parallaxBodies[planetName].ParallaxBodyMaterial.ParallaxMaterial;
            Debug.Log("Completed load on demand");
        }
        public Texture2D LoadDDSTexture(byte[] data)
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
            if (mipMapCount == 0)
            {
                texture = new Texture2D(width, height, format, false);
            }
            else
            {
                texture = new Texture2D(width, height, format, true);
            }

            texture.LoadRawTextureData(dxtBytes);
            texture.Apply();

            return (texture);
        }
    }
    [KSPAddon(KSPAddon.Startup.Instantly, false)]
    public class StopKopernicusShaderEnforce : MonoBehaviour
    {
        public void Start()
        {
            GameSettings.TERRAIN_SHADER_QUALITY = 2;
            GameSettings.ApplySettings();
            GameSettings.SaveSettings();

        }
    }
}