using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UniLinq;
using UnityEngine;
using ParallaxGrass;
using UnityEngine.UI;
using ComputeLoader;

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
        private static Dictionary<string, string> Labels => Utils.LabelFromVar;
        public enum ChangeType
        {
            Distribution,
            Material,
            Subdivision,
            Base,
            None,
            Visibility
        }
        public void Start()
        {
            CelestialBody currentBody = FlightGlobals.currentMainBody;
            lastBodyName = currentBody.name;

            BodyScatters = ScatterBodies.scatterBodies.ContainsKey(currentBody.name) ? ScatterBodies.scatterBodies[currentBody.name].scatters : null;
            Debug.Log("Body scatters length: " + BodyScatters.Count);
        }

        public void Update()
        {
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
            
            window = GUILayout.Window(GetInstanceID(), window, DrawWindow, "Parallax Configurator");
        }
        public static bool displayCount = false;
        private static void DrawWindow(int windowID)
        {
            string currentKey = BodyScatters.Keys.ToArray()[currentScatterIndex];
            currentScatter = BodyScatters[currentKey];
            SubObject currentSub = null;
            if (currentScatter.subObjects != null && currentScatter.subObjectCount > 0)
            {
                currentSub = currentScatter.subObjects[currentSubIndex];
            }
            GUILayout.BeginVertical();

            GUIStyle alignment = GUI.skin.GetStyle("Label");
            alignment.alignment = TextAnchor.MiddleCenter;

            GUILayout.Label("Currently displaying scatter: " + currentScatter.scatterName, alignment, GUILayout.ExpandWidth(true));
            GUILayout.Label("Currently displaying sub: " + currentSub, alignment, GUILayout.ExpandWidth(true));
            GUILayout.BeginHorizontal();

            CreateAdvanceButton("Previous Scatter", ref currentScatterIndex, BodyScatters.Values.Count, 0.5f, false);
            CreateAdvanceButton("Next Scatter", ref currentScatterIndex, BodyScatters.Values.Count, 0.5f, true);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();

            CreateAdvanceButton("Previous Sub", ref currentSubIndex, currentScatter.subObjects.Length, 0.5f, false);
            CreateAdvanceButton("Next Sub", ref currentSubIndex, currentScatter.subObjects.Length, 0.5f, true);
            string showHide = "Hide this scatter";
           
            GUILayout.EndHorizontal();
            if (!currentScatter.isVisible)
            {
                showHide = "Show this scatter";
            }
            if (GUILayout.Button(showHide))
            {
                anyValueHasChanged = true;
                currentChangeType = ChangeType.Visibility;

                currentScatter.isVisible = !currentScatter.isVisible;
                
            }
            GetBaseScatterProps(currentScatter);
            GetScatterProperties(currentScatter, alignment);
            //if (currentSub != null)
            //{
            //    currentScatter.subObjects[currentSubIndex] = currentSub;
            //}


            GUILayout.EndVertical();
            GUI.DragWindow();
        }
        private static void GetBaseScatterProps(Scatter scatter)
        {
            scatter.scatterName = TextAreaLabelString("Name", scatter.scatterName, ChangeType.Base);
            scatter.updateFPS = TextAreaLabelFloat("Update Rate (FPS)", scatter.updateFPS, ChangeType.Base);
            scatter.model = TextAreaLabelModel("Model", scatter.model, ChangeType.Base);
        }
        static bool showDistribution = false;
        static bool showMaterial = false;
        static bool showSubdivision = false;
        static bool showSubObject = false;
        private static void GetScatterProperties(Scatter scatter, GUIStyle alignment)
        {
            if (GUILayout.Button("Display Distribution Settings"))
            {
                showDistribution = !showDistribution;
                if (!showDistribution)
                {
                    window.height = absoluteSize.height;
                }
            }
            if (showDistribution)
            {
                Distribution props = scatter.properties.scatterDistribution;
                //GUILayout.BeginVertical();
                GUILayout.Label("Scatter Distribution Settings", alignment, GUILayout.ExpandWidth(true));
                props._PopulationMultiplier = TextAreaLabelFloat("Population Multiplier", props._PopulationMultiplier, ChangeType.Distribution);
                props._Range = TextAreaLabelFloat("Max Range", props._Range, ChangeType.Distribution);
                props._SpawnChance = TextAreaLabelFloat("Spawn Chance", props._SpawnChance, ChangeType.Distribution);
                props._SizeNoiseStrength = TextAreaLabelFloat("Size Noise Strength", props._SizeNoiseStrength, ChangeType.Distribution);
                props._SizeNoiseScale = TextAreaLabelFloat("Size Noise Scale", props._SizeNoiseScale, ChangeType.Distribution);
                props._SizeNoiseOffset.x = TextAreaLabelFloat("Size Noise Offset", props._SizeNoiseOffset.x, ChangeType.Distribution);
                props._MinScale = TextAreaLabelVector("Min Scale", props._MinScale, ChangeType.Distribution);
                props._MaxScale = TextAreaLabelVector("Max Scale", props._MaxScale, ChangeType.Distribution);
                props._CutoffScale = TextAreaLabelFloat("Min Size Cutoff Scale", props._CutoffScale, ChangeType.Distribution);
                props._SteepPower = TextAreaLabelFloat("Steep Power", props._SteepPower, ChangeType.Distribution);
                props._SteepContrast = TextAreaLabelFloat("Steep Contrast", props._SteepContrast, ChangeType.Distribution);
                props._SteepMidpoint = TextAreaLabelFloat("Steep Midpoint", props._SteepMidpoint, ChangeType.Distribution);
                //GUILayout.EndVertical();
                scatter.properties.scatterDistribution = props;
            }
            if (GUILayout.Button("Display Material Settings"))
            {
                showMaterial = !showMaterial;
                if (!showMaterial)
                {
                    window.height = absoluteSize.height;
                }
            }
            if (showMaterial)
            {
                ScatterMaterial props = scatter.properties.scatterMaterial;
                //GUILayout.BeginVertical();
                GUILayout.Label("Scatter Material Settings", alignment, GUILayout.ExpandWidth(true));
                props = SetupMaterialUI(props);
                scatter.properties.scatterMaterial = props;
                //GUILayout.EndVertical();
            }
            if (GUILayout.Button("Display Subdivision Settings"))
            {
                showSubdivision = !showSubdivision;
                if (!showSubdivision)
                {
                    window.height = absoluteSize.height;
                }
            }
            if (showSubdivision)
            {
                SubdivisionProperties props = scatter.properties.subdivisionSettings;
                //GUILayout.BeginVertical();
                GUILayout.Label("Scatter Subdivision Settings", alignment, GUILayout.ExpandWidth(true));
                GUILayout.Label("<color=#FFAA00>SubdivisionMode cannot be changed in the UI!</color>", GUILayout.ExpandWidth(true));
                props.range = TextAreaLabelFloat("Subdivision Range", props.range, ChangeType.Subdivision);
                props.level = (int)TextAreaLabelFloat("Subdivision Level", props.level, ChangeType.Subdivision);

                scatter.properties.subdivisionSettings = props;
                //GUILayout.EndVertical();
            }

            if (GUILayout.Button("Display Sub Object Settings"))
            {
                showSubObject = !showSubObject;
                if (!showSubObject)
                {
                    window.height = absoluteSize.height;
                }
            }
            if (showSubObject && scatter.subObjectCount > 0)
            {
                SubObject currentSub = scatter.subObjects[currentSubIndex];
                SubObjectProperties props = currentSub.properties;
                GUILayout.Label("Scatter SubObject Settings: " + currentScatter.scatterName + " / " + currentSub.objectName, alignment, GUILayout.ExpandWidth(true));
                props.model = TextAreaLabelModel("Model", props.model, ChangeType.Distribution);
                GUILayout.Label("Distribution", GUILayout.ExpandWidth(true));
                props._Density = TextAreaLabelFloat("Spawn Chance", props._Density, ChangeType.Distribution);
                props._NoiseScale = TextAreaLabelFloat("Noise Scale", props._NoiseScale, ChangeType.Distribution);
                props._NoiseAmount = TextAreaLabelFloat("Noise Amount/Influence", props._NoiseAmount, ChangeType.Distribution);
                GUILayout.Label("Material", GUILayout.ExpandWidth(true));
                props.material = SetupMaterialUI(props.material);
                currentSub.properties = props;
                currentScatter.subObjects[currentSubIndex].properties = props;
                currentScatter.subObjects[currentSubIndex] = currentSub;
            }
            CreateSaveButton();
        }
        private static ScatterMaterial SetupMaterialUI(ScatterMaterial scatterMat)
        {
            for (int i = 0; i < scatterMat.Vectors.Keys.Count; i++)
            {
                string currentKey = scatterMat.Vectors.Keys.ToArray()[i];
                scatterMat.Vectors[currentKey] = TextAreaLabelVector(currentKey, scatterMat.Vectors[currentKey], ChangeType.Material);
            }
            for (int i = 0; i < scatterMat.Floats.Keys.Count; i++)
            {
                string currentKey = scatterMat.Floats.Keys.ToArray()[i];
                scatterMat.Floats[currentKey] = TextAreaLabelFloat(currentKey, scatterMat.Floats[currentKey], ChangeType.Material);
            }
            for (int i = 0; i < scatterMat.Colors.Keys.Count; i++)
            {
                string currentKey = scatterMat.Colors.Keys.ToArray()[i];
                scatterMat.Colors[currentKey] = TextAreaLabelColor(currentKey, scatterMat.Colors[currentKey], ChangeType.Material);
            }
            return scatterMat;
        }
        static void CreateSaveButton()
        {
            if (GUILayout.Button("Save " + FlightGlobals.currentMainBody.name + "'s Scatters To Config"))
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
            if (!add && GUILayout.Button(name, GUILayout.Width(absoluteSize.width * widthPerc)))
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
            if (add && GUILayout.Button(name, GUILayout.Width(absoluteSize.width * widthPerc)))
            {
                toAdvance++;
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
        private static Vector3 TextAreaLabelVector(string label, Vector3 value, ChangeType type)
        {
            Color colValue = new Color(value.x, value.y, value.z);
            Color col = TextAreaLabelColor(label, colValue, type);
            return new Vector3(col.r, col.g, col.b);
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
        }
        private static void SaveDefaultVars()
        {

            //Setup scatter stuff
        }

        private static void GetPropertyInfos()
        {
            
        }
        public void ForceMaterialUpdate(Scatter scatter)
        {
            ScatterLog.Log("Forcing a material update on " + scatter);
            StartCoroutine(scatter.ForceMaterialUpdate());
        }
        public void ForceFullUpdate(Scatter scatter)
        {
            ScatterLog.Log("Forcing a full update on " + scatter);
            StartCoroutine(scatter.ForceComputeUpdate());
            
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
        public static Material SetAllValues(Material material)
        {
            return Utils.SetShaderProperties(material, currentScatter.properties.scatterMaterial);
        }

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