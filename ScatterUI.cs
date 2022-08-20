using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UniLinq;
using UnityEngine;
using ParallaxGrass;
using UnityEngine.UI;
using ComputeLoader;
using Grass;

namespace ScatterConfiguratorUtils
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class ParallaxConfiguratorMain : MonoBehaviour
    {
        public static Dictionary<string, Scatter> BodyScatters;
        public static int currentScatterIndex = 0;
        private static Rect window = new Rect(100, 100, 450, 200);
        private static Rect absoluteSize = new Rect(200, 200, 450, 50);

        public static Scatter currentScatter;
        private static string lastBodyName;
        private static bool anyValueHasChanged;
        public static int currentSubIndex = 0;

        public static Dictionary<string, ScatterBody> ScatterBodiesOriginal;

        private static readonly Dictionary<string, PropertyInfo> VarProperties = new Dictionary<string, PropertyInfo>();
        private static readonly Dictionary<string, PropertyInfo> TextureProperties = new Dictionary<string, PropertyInfo>();
        public static bool ShowUI { get; set; }
        public static ChangeType currentChangeType;
        public static bool revertTerrainMaterial = false;
        public static bool revertDistributionMaterial = false;
        private static Dictionary<string, string> Labels => Utils.LabelFromVar;
        private static bool displayDeviance;
        private static bool displayDistribution;
        private static bool showNoiseDistribution;
        private static bool showLODs;

        private static bool justResized = false;
        public enum ChangeType
        {
            Distribution,
            Rebuild,
            Material,
            Subdivision,
            Base,
            None,
            Visibility,
            TerrainMaterial,
            DistributionMaterial,
            Memory,
            Evaluate,
            Manager,
            Model,
            Texture
        }
        public void Start()
        {
            if (!FlightGlobals.ready) { return; }
            BodyScatters = ScatterBodies.scatterBodies.ContainsKey(FlightGlobals.currentMainBody?.name) ? ScatterBodies.scatterBodies[FlightGlobals.currentMainBody?.name].scatters : null;
        }

        public void Update()
        {
            if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.L))
            {
                ScatterLog.Log("Switching to 1 camera");
                Utils.oneCamera = !Utils.oneCamera;
            }
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.P))
            {
                if (BodyScatters != null)
                    ShowUI = !ShowUI;
                else
                {
                    PopupDialog.SpawnPopupDialog(
                        new Vector2(0.5f, 0.5f),
                        new Vector2(0.5f, 0.5f),
                        "ParallaxConfiguratorInvalidBody",
                        "Error",
                        $"Body {FlightGlobals.currentMainBody.name} is not parallax configured",
                        "Close",
                        false,
                        HighLogic.UISkin
                        );
                }
            }
            if (!FlightGlobals.ready) { return; }
            if (lastBodyName != FlightGlobals.currentMainBody?.name)
                this.Start();

            if (anyValueHasChanged)
            {
                anyValueHasChanged = false;
                WhatChanged(currentChangeType, currentScatter);
                currentChangeType = ChangeType.None;
            }
        }

        public void OnGUI()
        {
            if (!ShowUI)
                return;
            
            window = GUILayout.Window(GetInstanceID(), window, DrawWindow, "Parallax Configurator", HighLogic.Skin.window);
        }
        public static bool displayCount = false;
        private static bool showCreateScatter = false;
        private static void DrawWindow(int windowID)
        {
            string currentKey = BodyScatters.Keys.ToArray()[currentScatterIndex];
            currentScatter = BodyScatters[currentKey];
            GUILayout.BeginVertical();

            GUIStyle alignment = HighLogic.Skin.label;//GUI.skin.GetStyle("Label");
            alignment.alignment = TextAnchor.MiddleCenter;

            GUILayout.Label("Currently displaying scatter: " + currentScatter.scatterName, alignment, GUILayout.ExpandWidth(true));
            GUILayout.BeginHorizontal();

            CreateAdvanceButton("Previous Scatter", ref currentScatterIndex, BodyScatters.Values.Count, 0.333f, false);
            if (GUILayout.Button("Create Scatter", HighLogic.Skin.button, GUILayout.Width(absoluteSize.width * 0.333f)))
            {
                showCreateScatter = !showCreateScatter;
                justResized = true;
            }
            CreateAdvanceButton("Next Scatter", ref currentScatterIndex, BodyScatters.Values.Count, 0.333f, true);
            GUILayout.EndHorizontal();
            if (showCreateScatter)
            {
                
                JustResized();
                GUILayout.BeginVertical();
                GUILayout.Label("Create Scatter", alignment, GUILayout.ExpandWidth(true));
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Create Non Persistent Scatter", HighLogic.Skin.button, GUILayout.Width(absoluteSize.width * 0.333f)))
                {
                    CreateNonPersistentScatter();
                }
                if (GUILayout.Button("Clone This Scatter", HighLogic.Skin.button, GUILayout.Width(absoluteSize.width * 0.333f)))
                {
                    CloneThisScatter();
                }
                if (GUILayout.Button("Create Persistent Scatter", HighLogic.Skin.button, GUILayout.Width(absoluteSize.width * 0.333f)))
                {
                    CreatePersistentScatter();
                }
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }

            GUILayout.BeginHorizontal();

            string showHide = "Hide this scatter";
           
            GUILayout.EndHorizontal();
            if (!currentScatter.isVisible)
            {
                showHide = "Show this scatter";
            }
            if (GUILayout.Button(showHide, HighLogic.Skin.button))
            {
                anyValueHasChanged = true;
                currentChangeType = ChangeType.Visibility;

                currentScatter.isVisible = !currentScatter.isVisible;
                
            }
            GetBaseScatterProps(currentScatter);
            GetScatterProperties(currentScatter, alignment);


            GUILayout.EndVertical();
            GUI.DragWindow();
        }
        static bool alignToTerrainNormal = false;
        static bool forceFullShadows = false;
        private static void GetBaseScatterProps(Scatter scatter)
        {
            scatter.updateFPS = TextAreaLabelFloat("Update Rate (FPS)", scatter.updateFPS, ChangeType.Base);
            scatter.model = TextAreaLabelString("Model", scatter.model, ChangeType.Model);
            scatter.maxObjects = (int)TextAreaLabelSlider("Max Objects", scatter.maxObjects, 0, 100000, ChangeType.Manager);
            scatter.cullingRange = (int)TextAreaLabelFloat("Culling Range", scatter.cullingRange, ChangeType.Evaluate);
            scatter.cullingLimit = (int)TextAreaLabelFloat("Culling Limit", scatter.cullingLimit, ChangeType.Evaluate);
            alignToTerrainNormal = scatter.alignToTerrainNormal;
            alignToTerrainNormal = GUILayout.Toggle(alignToTerrainNormal, "Align To Terrain Normal");
            if (scatter.alignToTerrainNormal != alignToTerrainNormal)
            {
                scatter.alignToTerrainNormal = alignToTerrainNormal;
                currentChangeType = ChangeType.Distribution;
                anyValueHasChanged = true;
            }
            bool scatterIsForcingShadows = false;
            if (scatter.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.On) { forceFullShadows = true; scatterIsForcingShadows = true; }
            
            //forceFullShadows = scatter.forceFullShadows;
            forceFullShadows = GUILayout.Toggle(forceFullShadows, "Force Full Shadows");
            if (scatterIsForcingShadows != forceFullShadows)
            {
                Debug.Log(forceFullShadows);
                scatterIsForcingShadows = forceFullShadows;
                currentChangeType = ChangeType.Distribution;
                anyValueHasChanged = true;
                if (scatterIsForcingShadows)
                {
                    scatter.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                }
                else
                {
                    scatter.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
        }
        private static void JustResized()
        {
            if (justResized) { window.height = absoluteSize.height; justResized = false; }
        }
        static bool showDistribution = false;
        static bool showMaterial = false;
        static bool showSubdivision = false;
        private static void GetScatterProperties(Scatter scatter, GUIStyle alignment)
        {
            Properties mainProps = scatter.properties;
            Distribution distProps = scatter.properties.scatterDistribution;
            
            scatter.properties = mainProps;
            ScatterComponent pqsMod = ScatterManagerPlus.scatterComponents[scatter.planetName].Find(x => x.scatter.scatterName == scatter.scatterName);
            //currentChangeType = ChangeType.Memory;
            //anyValueHasChanged = true;
            //bool displayDeviance = false;
            if (GUILayout.Button("Display Distribution Settings", HighLogic.Skin.button))
            {
                showDistribution = !showDistribution;
                justResized = true;
                if (!showDistribution)
                {
                    window.height = absoluteSize.height;
                }
            }
            if (showDistribution)
            {
                JustResized();
                showMaterial = false;
                showSubdivision = false;
                showNoiseDistribution = false;

                if (scatter.properties.scatterDistribution.noise.noiseMode == DistributionNoiseMode.Persistent && GUILayout.Button("Show Distribution Noise", HighLogic.Skin.button))
                {
                    displayDistribution = !displayDistribution;
                    if (!displayDistribution)
                    {
                        revertDistributionMaterial = true;
                        anyValueHasChanged = true;
                        currentChangeType = ChangeType.DistributionMaterial;
                        Debug.Log("Revert time");
                    }
                    if (displayDistribution)
                    {
                        //Update all quads with this material
                        revertDistributionMaterial = false;
                        anyValueHasChanged = true;
                        currentChangeType = ChangeType.DistributionMaterial;
                    }
                }
                if (scatter.properties.scatterDistribution.noise.noiseMode == DistributionNoiseMode.Persistent && GUILayout.Button("Show Normal Deviancy", HighLogic.Skin.button))
                {
                    displayDeviance = !displayDeviance;
                    if (!displayDeviance)
                    {
                        revertTerrainMaterial = true;
                        anyValueHasChanged = true;
                        currentChangeType = ChangeType.TerrainMaterial;
                        Debug.Log("Revert time");
                    }
                    if (displayDeviance)
                    {
                        //Update all quads with this material
                        revertTerrainMaterial = false;
                        anyValueHasChanged = true;
                        currentChangeType = ChangeType.TerrainMaterial;
                    }
                }
                
                //GUILayout.BeginVertical();
                GUILayout.Label("Scatter Distribution Settings", alignment, GUILayout.ExpandWidth(true));
                distProps._Seed = TextAreaLabelFloat("Location Seed", distProps._Seed, ChangeType.Distribution);
                distProps._PopulationMultiplier = TextAreaLabelSlider("Population Multiplier", distProps._PopulationMultiplier, 1, 100, ChangeType.Distribution);//TextAreaLabelFloat("Population Multiplier", distProps._PopulationMultiplier, ChangeType.Distribution);
                distProps._Range = TextAreaLabelFloat("Max Range", distProps._Range, ChangeType.Distribution);
                distProps._SpawnChance = TextAreaLabelFloat("Spawn Chance", distProps._SpawnChance, ChangeType.Distribution);
                distProps._SizeNoiseStrength = TextAreaLabelFloat("Size Noise Strength", distProps._SizeNoiseStrength, ChangeType.Distribution);
                distProps._MinScale = TextAreaLabelVector("Min Scale", distProps._MinScale, ChangeType.Distribution);
                distProps._MaxScale = TextAreaLabelVector("Max Scale", distProps._MaxScale, ChangeType.Distribution);
                distProps._CutoffScale = TextAreaLabelFloat("Min Size Cutoff Scale", distProps._CutoffScale, ChangeType.Distribution);
                distProps._SteepPower = TextAreaLabelFloat("Steep Power", distProps._SteepPower, ChangeType.Distribution);
                distProps._SteepContrast = TextAreaLabelFloat("Steep Contrast", distProps._SteepContrast, ChangeType.Distribution);
                distProps._SteepMidpoint = TextAreaLabelFloat("Steep Midpoint", distProps._SteepMidpoint, ChangeType.Distribution);
                distProps._MaxNormalDeviance = TextAreaLabelFloat("Max Normal Deviance", distProps._MaxNormalDeviance, ChangeType.Distribution);
                distProps._MinAltitude = TextAreaLabelFloat("Min Altitude", distProps._MinAltitude, ChangeType.Distribution);
                distProps._MaxAltitude = TextAreaLabelFloat("Max Altitude", distProps._MaxAltitude, ChangeType.Distribution);
                distProps._AltitudeFadeRange = TextAreaLabelSlider("Altitude Fade Range", distProps._AltitudeFadeRange, 0.001f, 20, ChangeType.Distribution);
                
                //GUILayout.EndVertical();
                scatter.properties.scatterDistribution = distProps;
                if (GUILayout.Button("Display LOD Settings", HighLogic.Skin.button))
                {
                    showLODs = !showLODs;
                    if (!showLODs)
                    {
                        window.height = absoluteSize.height;
                    }
                }
                if (showLODs)
                {
                    GUILayout.Label("Scatter LOD Settings", alignment, GUILayout.ExpandWidth(true));
                    LODs lodProps = scatter.properties.scatterDistribution.lods;
                    for (int i = 0; i < lodProps.lods.Length; i++)
                    {
                       
                        GUILayout.Label("LOD " + (i + 1).ToString(), alignment, GUILayout.ExpandWidth(true));
                        Grass.LOD lod = lodProps.lods[i];
                        lod.range = TextAreaLabelSlider("Range", lod.range, 0, scatter.properties.scatterDistribution._Range, ChangeType.Distribution);
                        lod.modelName = TextAreaLabelString("LOD " + (i + 1) + " Model Name", lod.modelName, ChangeType.Model);
                        lod.mainTexName = TextAreaLabelString("LOD " + (i + 1) + " Main Texture Name", lod.mainTexName, ChangeType.Texture);
                        lod.normalName = TextAreaLabelString("LOD " + (i + 1) + " Normal Map Name", lod.normalName, ChangeType.Texture);

                        bool isBillboard = lod.isBillboard;
                        if(isBillboard != GUILayout.Toggle(isBillboard, "Billboard"))
                        {
                            lod.isBillboard = !isBillboard;
                            currentChangeType = ChangeType.Material;
                            anyValueHasChanged = true;
                        }
                        lodProps.lods[i] = lod;
                    }
                    scatter.properties.scatterDistribution.lods = lodProps;
                }
            }
            if (GUILayout.Button("Display Noise Settings", HighLogic.Skin.button))
            {
                justResized = true;
                showNoiseDistribution = !showNoiseDistribution;
                if (!showNoiseDistribution)
                {
                    window.height = absoluteSize.height;
                }
            }
            if (showNoiseDistribution)
            {
                JustResized();
                showMaterial = false;
                showSubdivision = false;
                showDistribution = false;

                DistributionNoise props = scatter.properties.scatterDistribution.noise;
                if (props.noiseMode == DistributionNoiseMode.Persistent || props.noiseMode == DistributionNoiseMode.VerticalStack)
                {
                    props._Frequency = TextAreaLabelFloat("Frequency", props._Frequency, ChangeType.Rebuild);
                    props._Persistence = TextAreaLabelFloat("Persistence", props._Persistence, ChangeType.Rebuild);
                    props._Lacunarity = TextAreaLabelFloat("Lacunarity", props._Lacunarity, ChangeType.Rebuild);
                    props._Octaves = TextAreaLabelFloat("Octaves", props._Octaves, ChangeType.Rebuild);
                    props._Seed = (int)TextAreaLabelFloat("Seed", props._Seed, ChangeType.Rebuild);
                    props._NoiseType = (int)TextAreaLabelFloat("NoiseType", props._NoiseType, ChangeType.Rebuild);
                    if (props.noiseMode == DistributionNoiseMode.VerticalStack)
                    {
                        props._MaxStacks = (int)TextAreaLabelFloat("Max Stacks", props._MaxStacks, ChangeType.Distribution);
                        props._StackSeparation = TextAreaLabelFloat("Stack Separation", props._StackSeparation, ChangeType.Distribution);
                    }
                }
                else if (props.noiseMode == DistributionNoiseMode.NonPersistent)
                {
                    props._SizeNoiseScale = TextAreaLabelFloat("Size Noise Scale", props._SizeNoiseScale, ChangeType.Distribution);
                    props._ColorNoiseScale = TextAreaLabelFloat("Color Noise Scale", props._ColorNoiseScale, ChangeType.Distribution);
                    props._SizeNoiseOffset = TextAreaLabelFloat("Size Noise Offset", props._SizeNoiseOffset, ChangeType.Distribution);
                }
                scatter.properties.scatterDistribution.noise = props;
            }
            if (GUILayout.Button("Display Material Settings", HighLogic.Skin.button))
            {
                justResized = true;
                showMaterial = !showMaterial;
                if (!showMaterial)
                {
                    window.height = absoluteSize.height;
                }
            }
            if (showMaterial)
            {
                JustResized();
                showSubdivision = false;
                showNoiseDistribution = false;
                showDistribution = false;

                ScatterMaterial props = scatter.properties.scatterMaterial;
                //GUILayout.BeginVertical();
                GUILayout.Label("Scatter Material Settings", alignment, GUILayout.ExpandWidth(true));
                props = SetupMaterialUI(props);
                scatter.properties.scatterMaterial = props;
                //GUILayout.EndVertical();
            }
            if (GUILayout.Button("Display Subdivision Settings", HighLogic.Skin.button))
            {
                justResized = true;
                showSubdivision = !showSubdivision;
                if (!showSubdivision)
                {
                    window.height = absoluteSize.height;
                }
            }
            if (showSubdivision)
            {
                JustResized();
                showMaterial = false;
                showNoiseDistribution = false;
                showDistribution = false;

                SubdivisionProperties props = scatter.properties.subdivisionSettings;
                //GUILayout.BeginVertical();
                GUILayout.Label("Scatter Subdivision Settings", alignment, GUILayout.ExpandWidth(true));
                GUILayout.Label("<color=#FFAA00>SubdivisionMode cannot be changed in the UI!</color>", GUILayout.ExpandWidth(true));
                props.range = TextAreaLabelSlider("Subdivision Range", props.range, 0, scatter.properties.scatterDistribution._Range * 10, ChangeType.Subdivision);
                props.level = (int)TextAreaLabelFloat("Subdivision Level", props.level, ChangeType.Subdivision);

                scatter.properties.subdivisionSettings = props;
                foreach (QuadData qd in PQSMod_ParallaxScatter.quadList.Values)
                {
                    foreach (Scatter sc in qd.scatters)
                    {
                        if (sc.scatterName == scatter.scatterName)
                        {
                            sc.properties.subdivisionSettings.range = props.range;
                        }
                    }
                }
                //GUILayout.EndVertical();
            }
            CreateSaveButton();
        }
        private static ScatterMaterial SetupMaterialUI(ScatterMaterial scatterMat)
        {
            for (int i = 0; i < scatterMat.Textures.Keys.Count; i++)
            {
                string currentKey = scatterMat.Textures.Keys.ToArray()[i];
                scatterMat.Textures[currentKey] = TextAreaLabelString(currentKey, scatterMat.Textures[currentKey], ChangeType.Texture);
            }
            for (int i = 0; i < scatterMat.Vectors.Keys.Count; i++)
            {
                string currentKey = scatterMat.Vectors.Keys.ToArray()[i];
                scatterMat.Vectors[currentKey] = TextAreaLabelVector(currentKey, scatterMat.Vectors[currentKey], ChangeType.Material);
            }
            for (int i = 0; i < scatterMat.Scales.Keys.Count; i++)
            {
                string currentKey = scatterMat.Scales.Keys.ToArray()[i];
                scatterMat.Scales[currentKey] = TextAreaLabelVector2D(currentKey, scatterMat.Scales[currentKey], ChangeType.Material);
            }
            for (int i = 0; i < scatterMat.Floats.Keys.Count; i++)
            {
                string currentKey = scatterMat.Floats.Keys.ToArray()[i];
                scatterMat.Floats[currentKey] = TextAreaLabelFloat(currentKey, scatterMat.Floats[currentKey], ChangeType.Material);
            }
            for (int i = 0; i < scatterMat.Colors.Keys.Count; i++)
            {
                string currentKey = scatterMat.Colors.Keys.ToArray()[i];
                Color col = scatterMat.Colors[currentKey];
                Vector3 vec = TextAreaLabelVector(currentKey, new Vector3(col.r, col.g, col.b), ChangeType.Material);
                scatterMat.Colors[currentKey] = new Color(vec.x, vec.y, vec.z);
            }
            return scatterMat;
        }
        static void CreateSaveButton()
        {
            if (GUILayout.Button("Save " + (FlightGlobals.currentMainBody != null ? FlightGlobals.currentMainBody.name : "(No Body)")+ "'s Scatters To Config", HighLogic.Skin.button))
            {
                ConfigUtil.SaveAllToConfig(FlightGlobals.currentMainBody.name);
            }
            
        }
        static void CreateAdvanceButton(string name, ref int toAdvance, int length, float widthPerc, bool add)
        {
            if (length == 0)
            {
                name = "";
            }
            if (!add && GUILayout.Button(name, HighLogic.Skin.button, GUILayout.Width(absoluteSize.width * widthPerc)))
            {
                toAdvance--;
                if (toAdvance < 0)
                {
                    toAdvance = length - 1;
                }
                if (name.Contains("Scatter"))
                {
                    currentSubIndex = 0;
                }
            }
            if (add && GUILayout.Button(name, HighLogic.Skin.button, GUILayout.Width(absoluteSize.width * widthPerc)))
            {
                toAdvance++;
                alignToTerrainNormal = false;
                forceFullShadows = false;
                if (toAdvance > length - 1)
                {
                    toAdvance = 0;
                }
                if (name.Contains("Scatter"))
                {
                    currentSubIndex = 0;
                }
            }
        }
        static void CreateNonPersistentScatter()
        {
            ScatterLoader.Instance.LoadNewScatter("NewNonPersistentScatter");
            LoadOnDemand.Load(FlightGlobals.currentMainBody.name);
            ScatterManagerPlus.Instance.Restart();
            PQ[] keys = PQSMod_ParallaxScatter.quadList.Keys.ToArray();
            for (int i = 0; i < keys.Length; i++)
            {
                PQ key = keys[i];
                PQSMod_ParallaxScatter.quadList[key].Cleanup();
                PQSMod_ParallaxScatter.quadList[key] = new QuadData(key);
            }
        }
        static void CloneThisScatter()
        {
            UrlDir.UrlConfig[] configs = GameDatabase.Instance.GetConfigs("ParallaxScatters");
            for (int i = 0; i < configs.Length; i++)
            {
                UrlDir.UrlConfig config = configs[i];
                if (config.config.GetValue("body") == currentScatter.planetName)
                {
                    Debug.Log("Found body: " + currentScatter.planetName);
                    ConfigNode[] nodes = config.config.GetNodes("Scatter");
                    for (int b = 0; b < nodes.Length; b++)
                    {
                        if (config.config.GetValue("body") + "-" + nodes[b].GetValue("name") == currentScatter.scatterName)
                        {
                            Debug.Log("Found scatter: " + currentScatter.scatterName);
                            ScatterLoader.Instance.ParseNewScatter(nodes[b], nodes[b].GetNode("DistributionNoise"), nodes[b].GetNode("Distribution"), nodes[b].GetNode("Material"), nodes[b].GetNode("SubdivisionSettings"), null, currentScatter.planetName);
                        }
                    }
                }
                
            }

            LoadOnDemand.Load(FlightGlobals.currentMainBody.name);
            ScatterManagerPlus.Instance.Restart();
            FlightGlobals.currentMainBody.pqsController.RebuildSphere();
        }
        static void CreatePersistentScatter()
        {
            ScatterLoader.Instance.LoadNewScatter("NewPersistentScatter");
            LoadOnDemand.Load(FlightGlobals.currentMainBody.name);
            ScatterManagerPlus.Instance.Restart();
            FlightGlobals.currentMainBody.pqsController.RebuildSphere();
        }
        private static string TextAreaLabelTexture(string label, string value, ChangeType type)
        {
            GUILayout.BeginHorizontal();
            string newValue = InputFields.TexField(label, value);
            GUILayout.EndHorizontal();

            if (newValue != value)
            {
                anyValueHasChanged = true;
                currentChangeType = type;
            }
                

            return newValue;
        }
        private static float TextAreaLabelSlider(string label, float value, float min, float max, ChangeType type)
        {
            //GUILayout.BeginHorizontal();
            float newValue = InputFields.SliderField(label, value, min, max);
            //GUILayout.EndHorizontal();
            if (newValue != value)
            {
                anyValueHasChanged = true;
                currentChangeType = type;
            }

            return newValue;
        }
        private static string TextAreaLabelModel(string label, string value, ChangeType type)
        {
            GUILayout.BeginHorizontal();
            string newValue = InputFields.TexField(label, value);
            GUILayout.EndHorizontal();

            if (newValue != value)
            {
                anyValueHasChanged = true;
                currentChangeType = type;
            }

            return newValue;
        }
        private static string TextAreaLabelString(string label, string value, ChangeType type)
        {
            GUILayout.BeginHorizontal();
            string newValue = InputFields.TexField(label, value);
            GUILayout.EndHorizontal();
            if (newValue != value)
            {
                bool fileExists = File.Exists(KSPUtil.ApplicationRootPath + "GameData/" + newValue + (type == ChangeType.Model ? ".mu" : ""));
                if (fileExists)
                {
                    ScatterLog.Log("[UI] Found file: " + newValue);
                    anyValueHasChanged = true;
                    currentChangeType = type;
                    if (type == ChangeType.Texture)
                    {
                        foreach (ScatterComponent sc in ScatterManagerPlus.scatterComponents[FlightGlobals.currentMainBody.name])
                        {
                            if (sc.scatter.scatterName == currentScatter.scatterName && !label.Contains("LOD"))
                            {
                                Texture2D tex = LoadOnDemand.LoadTexture(newValue);
                                sc.pc.material.SetTexture(label, tex);
                                
                                Debug.Log("Set texture: " + newValue);
                            }
                            if (sc.scatter.scatterName == currentScatter.scatterName && label.Contains("LOD"))  //Have to set the texture on the material manually. It's funky, just calling SetupAgain would work with LODs but not the main tex, despite loading the correct texture
                            {
                                Texture2D newTex = LoadOnDemand.LoadTexture(newValue);
                                if (label.Contains("LOD 1 Main"))   //This is so scuffed
                                {
                                    sc.pc.materialFar.SetTexture("_MainTex", newTex);
                                }
                                if (label.Contains("LOD 1 Normal"))
                                {
                                    sc.pc.materialFar.SetTexture("_BumpMap", newTex);
                                }
                                if (label.Contains("LOD 2 Main"))
                                {
                                    sc.pc.materialFurther.SetTexture("_MainTex", newTex);
                                }
                                if (label.Contains("LOD 2 Normal"))
                                {
                                    sc.pc.materialFurther.SetTexture("_BumpMap", newTex);
                                }
                            }
                        }
                    }
                    if (type == ChangeType.Model)
                    {
                        foreach (ScatterComponent sc in ScatterManagerPlus.scatterComponents[FlightGlobals.currentMainBody.name])
                        {
                            if (sc.scatter.scatterName == currentScatter.scatterName)
                            {
                                if (label.Contains("Model") && !label.Contains("LOD"))
                                {
                                    Debug.Log("Finding: " + newValue);
                                    GameObject go = GameDatabase.Instance.GetModel(newValue);
                                    if (go == null) { ScreenMessages.PostScreenMessage("This model is not loaded"); return value; }
                                    Mesh mesh = Instantiate(go.GetComponent<MeshFilter>().mesh);
                                    sc.pc.mesh = mesh;
                                    sc.pc.vertexCount = mesh.vertexCount;
                                    sc.pc.ReInitializeBuffers();
                                }
                                if (label.Contains("LOD 1 Model"))
                                {
                                    GameObject go = GameDatabase.Instance.GetModel(newValue);
                                    Mesh mesh = Instantiate(go.GetComponent<MeshFilter>().mesh);
                                    sc.pc.farMesh = mesh;
                                    sc.pc.farVertexCount = mesh.vertexCount;
                                    sc.pc.ReInitializeBuffers();
                                }
                                if (label.Contains("LOD 2 Model"))
                                {
                                    GameObject go = GameDatabase.Instance.GetModel(newValue);
                                    Mesh mesh = Instantiate(go.GetComponent<MeshFilter>().mesh);
                                    sc.pc.furtherMesh = mesh;
                                    sc.pc.furtherVertexCount = mesh.vertexCount;
                                    sc.pc.ReInitializeBuffers();
                                }
                            }
                        }
                    }
                }
            }

            return newValue;
        }
        private static Vector3 TextAreaLabelVector(string label, Vector3 value, ChangeType type)
        {
            Vector3 newValue = InputFields.VectorField(label, value);
            if (newValue != value)
            {
                anyValueHasChanged = true;
                currentChangeType = type;
            }
            return newValue;
        }
        private static float TextAreaLabelFloat(string label, float value, ChangeType type)
        {
            GUILayout.BeginHorizontal();
            float newValue = InputFields.FloatField(label, value);
            GUILayout.EndHorizontal();

            if (Math.Abs(newValue - value) > 0.001)
            {
                anyValueHasChanged = true;
                currentChangeType = type;
            }

            return newValue;
        }
        //private static Vector3 TextAreaLabelVector(string label, Vector3 value, ChangeType type)
        //{
        //    Color colValue = new Color(value.x, value.y, value.z);
        //    Color col = TextAreaLabelColor(label, colValue, type);
        //    return new Vector3(col.r, col.g, col.b);
        //}

        private static Vector2 TextAreaLabelVector2D(string label, Vector2 value, ChangeType type)
        {
            Vector3 val = InputFields.VectorField(label, new Vector3(value.x, value.y, 0));
            Vector2 newValue = new Vector2(val.x, val.y);
            if (newValue != value)
            {
                anyValueHasChanged = true;
                currentChangeType = type;
            }
            return newValue;
        }
        private static int TextAreaLabelInt(string label, int value, int minValue, int maxValue)
        {
            GUILayout.BeginHorizontal();
            int newValue = InputFields.IntField(label, value, minValue, maxValue);
            GUILayout.EndHorizontal();

            return newValue;
        }

        private static Color TextAreaLabelColor(string label, Color value, ChangeType type)
        {
            GUILayout.BeginHorizontal();
            Color newValue = InputFields.ColorField(label, value);
            GUILayout.EndHorizontal();

            if (newValue != value)
            {
                anyValueHasChanged = true;
                currentChangeType = type;
            }

            return newValue;
        }
        private void WhatChanged(ChangeType type, Scatter currentScatter)
        {
            if (type == ChangeType.Base)
            {
                ForceFullUpdate(currentScatter);
            }
            if (type == ChangeType.Distribution)
            {
                ForceFullUpdate(currentScatter);

            }
            if (type == ChangeType.Rebuild)
            {
                FlightGlobals.currentMainBody.pqsController.RebuildSphere();
                ForceFullUpdate(currentScatter);

            }
            if (type == ChangeType.Material)
            {
                ForceMaterialUpdate(currentScatter);
            }
            if (type == ChangeType.Subdivision)
            {
                //Probably best to add a button to rebuild tbh
            }
            if (type == ChangeType.Visibility)
            {
                UpdateVisibility(currentScatter);
            }
            if (type == ChangeType.TerrainMaterial)
            {
                ForceTerrainMaterialUpdate(currentScatter, revertTerrainMaterial);
            }
            if (type == ChangeType.DistributionMaterial)
            {
                ForceDistributionMaterialUpdate(currentScatter, revertDistributionMaterial);
            }
            if (type == ChangeType.Evaluate)
            {
                ForceFullUpdate(currentScatter);
            }
            if (type == ChangeType.Manager)
            {
                List<ScatterComponent> scatterComponents = ScatterManagerPlus.scatterComponents[currentScatter.planetName];
                foreach (ScatterComponent scatterComponent in scatterComponents)
                {
                    if (scatterComponent.scatter.scatterName == currentScatter.scatterName)
                    {
                        scatterComponent.OnDisable();
                        scatterComponent.scatter = currentScatter;
                        scatterComponent.OnEnable();
                    }
                }
            }
            if (type == ChangeType.Texture)
            {
                //LoadOnDemand.Load(FlightGlobals.currentMainBody.name);
                //List<ScatterComponent> scatterComponents = ScatterManagerPlus.scatterComponents[currentScatter.planetName];
                //foreach (ScatterComponent scatterComponent in scatterComponents)
                //{
                //    if (scatterComponent.scatter.scatterName == currentScatter.scatterName)
                //    {
                //        //scatterComponent.pc.SetupAgain(currentScatter);
                //        Destroy(scatterComponent.pc);
                //        scatterComponent.pc = ScatterManagerPlus.gameObjects[currentScatter.planetName].AddComponent<PostCompute>();
                //        scatterComponent.pc.scatterName = currentScatter.scatterName;
                //        scatterComponent.pc.planetName = currentScatter.planetName;
                //        scatterComponent.pc.scatterProps = currentScatter.properties;
                //        scatterComponent.pc.Setup(Buffers.activeBuffers[scatterComponent.scatter.scatterName].buffer, Buffers.activeBuffers[scatterComponent.scatter.scatterName].farBuffer, Buffers.activeBuffers[scatterComponent.scatter.scatterName].furtherBuffer, currentScatter);
                //    }
                //}
            }
            if (type == ChangeType.Model)
            {
                ScreenMessages.PostScreenMessage("Note: Models can only be loaded if the file existed BEFORE the game was started");
                List<ScatterComponent> scatterComponents = ScatterManagerPlus.scatterComponents[currentScatter.planetName];
                foreach (ScatterComponent scatterComponent in scatterComponents)
                {
                    if (scatterComponent.scatter.scatterName == currentScatter.scatterName)
                    {
                        scatterComponent.pc.SetupAgain(currentScatter);
                    }
                }
            }
        }
        public void ForceMaterialUpdate(Scatter scatter)
        {
            ScatterLog.Log("Forcing a material update on " + scatter);
            StartCoroutine(scatter.ForceMaterialUpdate(scatter));
        }
        public void ForceFullUpdate(Scatter scatter)
        {
            ScatterLog.Log("Forcing a full update on " + scatter);
            ScatterComponent pqsMod = ScatterManagerPlus.scatterComponents[scatter.planetName].Find(x => x.scatter.scatterName == scatter.scatterName);
            ScatterLog.Log("Attempting to stop OnUpdate coroutine for " + scatter.scatterName);
            ScatterLog.Log("Stopped");
            StartCoroutine(scatter.ForceComputeUpdate());
            
        }
        public void ForceTerrainMaterialUpdate(Scatter scatter, bool revert)
        {
            Material deviancyMat = new Material(ScatterShaderHolder.GetShader("Custom/NormalDeviancyViewer"));
            deviancyMat.SetFloat("_NormalDeviancy", currentScatter.properties.scatterDistribution._MaxNormalDeviance);
            StartCoroutine(scatter.SwitchQuadMaterial(deviancyMat, revert, scatter));
        }
        public void ForceDistributionMaterialUpdate(Scatter scatter, bool revert)
        {
            Material distributionViewer = new Material(ScatterShaderHolder.GetShader("Custom/VertexColor"));
            StartCoroutine(scatter.SwitchQuadMaterial(distributionViewer, revert, scatter));
        }
        public void UpdateVisibility(Scatter scatter)
        {
            ScatterLog.Log("Updating visibility on " + scatter);
            currentScatter.ModifyScatterVisibility();

        }
        //public void UpdateShaderValues()
        //{
        //
        //
        //    Scatter scatter = currentScatter;
        //    Debug.Log("Forcing compute update");
        //    StartCoroutine(scatter.ForceComputeUpdate());
        //    Debug.Log("Done!");
        //    
        //    PostCompute[] allPostComputeComponents = UnityEngine.Resources.FindObjectsOfTypeAll(typeof(PostCompute)) as PostCompute[];
        //    foreach (PostCompute comp in allPostComputeComponents)
        //    {
        //        if (comp.gameObject.activeSelf)
        //        {
        //            Debug.Log("Found ComputeComponent: " + comp.name);
        //            if (comp == null)
        //            {
        //                Debug.Log("Component is null??");
        //            }
        //            comp.material = SetAllValues(comp.material);
        //        }
        //    }
        //}


        private static void SaveConfigs(string fileName)
        {

        }

        

        private static void LoadConfigs(string fileName)
        {
            string url = "ParallaxConfigurator/" + fileName;
            string path = KSPUtil.ApplicationRootPath + "GameData/" + url;

            if (!File.Exists(path))
            {
                PopupDialog.SpawnPopupDialog(
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    "ParallaxConfiguratorFileMissing",
                    "Warning",
                    $"File \"{url}\" doesn't exist!",
                    "Close",
                    false,
                    HighLogic.UISkin
                    );
            }
            else
            {
                ReadFileAndLoadValues(path);
            }
        }

        private static void ReadFileAndLoadValues(string path)
        {
            int configs = 0;
            bool loaded = false;

            string[] lines = File.ReadAllLines(path);
            int bracketCounter = 0;

            // count existing configs
            foreach (var line in lines)
            {
                if (line.StartsWith("{")) // config started
                {
                    bracketCounter++;
                }
                else if (bracketCounter > 0 && line.StartsWith("}")) // config ended
                {
                    bracketCounter--;

                    // prevent rogue brackets
                    if (bracketCounter == 0)
                        configs++;
                }
            }

            if (configs == 1)
            {
                loaded = ReadAndLoadConfigFromFile(lines, 0);
            }
            else if (configs > 1)
            {
                SpawnConfigSelectionPopupDialog(lines, configs - 1);
                // check is done later
                loaded = true;
            }

            if (configs == 0 || !loaded)
            {
                ScreenMessages.PostScreenMessage("Invalid file", 4, ScreenMessageStyle.UPPER_RIGHT, false);
            }
        }

        private static void SpawnConfigSelectionPopupDialog(string[] lines, int maxIndex)
        {
            var dialog = new List<DialogGUIBase>();
            var buttons = new List<DialogGUIHorizontalLayout>();

            // add button for each config found
            for (int i = maxIndex; i >= 0; i--)
            {
                var button = new DialogGUIButton<int>(GetDateFromConfigIndex(lines, i, maxIndex), (int n) => ReadAndLoadConfigFromFile(lines, n), i, true);
                var h = new DialogGUIHorizontalLayout(true, false, 4, new RectOffset(), TextAnchor.MiddleCenter, button);

                buttons.Add(h);
            }

            // create scroll list from buttons
            var scrollList = new DialogGUIBase[buttons.Count + 1];
            scrollList[0] = new DialogGUIContentSizer(ContentSizeFitter.FitMode.Unconstrained, ContentSizeFitter.FitMode.PreferredSize, true);
            for (int i = 0; i < buttons.Count; i++)
            {
                scrollList[i + 1] = buttons[i];
            }

            // add scroll list to dialog
            dialog.Add(new DialogGUIScrollList(Vector2.one, false, true,
                new DialogGUIVerticalLayout(10, 100, 4, new RectOffset(6, 24, 10, 10), TextAnchor.MiddleLeft, scrollList)
                ));

            // add spacing and cancel button
            dialog.Add(new DialogGUISpace(4));
            dialog.Add(new DialogGUIHorizontalLayout(
                    new DialogGUIFlexibleSpace(),
                    new DialogGUIButton("Cancel", delegate { }),
                    new DialogGUIFlexibleSpace()
                    )
                );

            PopupDialog.SpawnPopupDialog(
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new MultiOptionDialog(
                    "ParallaxConfiguratorMultipleConfigs",
                    "Choose a config:",
                    $"Detected {maxIndex + 1} configs",
                    HighLogic.UISkin,
                    new Rect(0.5f, 0.5f, 300f, 200f),
                    dialog.ToArray()
                    ),
                false,
                HighLogic.UISkin
                );
        }

        private static string GetDateFromConfigIndex(string[] lines, int index, int maxValue)
        {
            string date = null;

            string[] dateLines = lines.Where(l => l.StartsWith("// CONFIGS FROM ")).ToArray();

            if (index < dateLines.Length)
            {
                date = dateLines[index]?.Remove(0, 16);

                if (index == maxValue)
                    date += " (latest)";
            }

            return date ?? "invalid";
        }

        private static bool ReadAndLoadConfigFromFile(string[] lines, int index)
        {
            int counter = -1;
            bool valid = false;
            Debug.Log($"[ParallaxConfigurator] Parsing config #{index}");

            foreach (string line in lines)
            {
                if (line.StartsWith("{"))
                    counter++;

                // ignore line if it is not in the right config or if it is a comment
                if (counter != index || line.StartsWith("//"))
                    continue;

                // if config is over, break
                if (line.StartsWith("}"))
                {
                    valid = true;
                    break;
                }

                // remove partial comments
                int commentIndex;
                if ((commentIndex = line.IndexOf("//", StringComparison.Ordinal)) != -1)
                    line.Remove(commentIndex);

                string[] array = line.Split('=');
                if (array.Length != 2)
                    continue;

                SetVariableValueFromName(array[0].Trim(), array[1].Trim());
            }

            if (!valid)
            {
                ScreenMessages.PostScreenMessage("Unable to read file", 4, ScreenMessageStyle.UPPER_RIGHT, false);
                Debug.Log($"[ParallaxConfigurator] unable to read config #{index}");
                return false;
            }

            //this.UpdateShaderValues();
            return true;
        }

        private static void SetVariableValueFromName(string varName, string value)
        {
            
        }
    }
}