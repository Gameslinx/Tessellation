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
        public static int currentIndex = 0;
        private static Rect window = new Rect(100, 100, 450, 200);
        private static bool showTextures = false;
        private static bool advanceIndex;
        private static bool advanceSub;
        public static Scatter currentScatter;
        private static bool showPqs = false;
        private static string lastBodyName;
        private static bool anyValueHasChanged;
        private static bool firstRun = true;
        public static int currentSubCountIndex;

        public static Dictionary<string, ScatterBody> ScatterBodiesOriginal;

        private static readonly Dictionary<string, PropertyInfo> VarProperties = new Dictionary<string, PropertyInfo>();
        private static readonly Dictionary<string, PropertyInfo> TextureProperties = new Dictionary<string, PropertyInfo>();
        public static bool ShowUI { get; set; }

        private static Dictionary<string, string> Labels => Utils.LabelFromVar;

        public void Start()
        {
            CelestialBody currentBody = FlightGlobals.currentMainBody;
            lastBodyName = currentBody.name;

            BodyScatters = ScatterBodies.scatterBodies.ContainsKey(currentBody.name) ? ScatterBodies.scatterBodies[currentBody.name].scatters : null;
            Debug.Log("Body scatters length: " + BodyScatters.Count);
            if (firstRun)
            {
                firstRun = false;
                SaveDefaultVars();
                GetPropertyInfos();
            }
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
                UpdateShaderValues();
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
            GUILayout.BeginVertical();

            if (GUILayout.Button("Save current config to ParallaxConfigurator folder"))
            {

            }
            if (GUILayout.Button("Load config from file"))
            {

            }

            if (advanceIndex != GUILayout.Toggle(advanceIndex, "Next Scatter")) //Toggles
            {
                advanceIndex = !advanceIndex;
                window = new Rect(window.position.x, window.position.y, 450, 200);
            }
            if (advanceIndex)
            {
                currentIndex++;
                if (currentIndex >= BodyScatters.Keys.Count)
                {
                    currentIndex = 0;
                }
                currentSubCountIndex = 0;
                advanceIndex = !advanceIndex;
                Debug.Log("Index advanced: " + currentIndex);
            }


            


            Debug.Log("Current Index is " + currentIndex);
            string thisIndex = ScatterBodies.scatterBodies[FlightGlobals.currentMainBody.name].scatters.Keys.ToArray()[currentIndex];
            Debug.Log("Searching for scatter: " + thisIndex);
            currentScatter = ScatterBodies.scatterBodies[FlightGlobals.currentMainBody.name].scatters[thisIndex];

            if (advanceSub != GUILayout.Toggle(advanceSub, "Next Sub")) //Toggles
            {
                advanceSub = !advanceSub;
                window = new Rect(window.position.x, window.position.y, 450, 200);
            }
            if (advanceSub)
            {
                currentSubCountIndex++;
                if (currentSubCountIndex >= currentScatter.subObjectCount)
                {
                    currentSubCountIndex = 0;
                }
                advanceSub = !advanceSub;
                Debug.Log("Index advanced: " + currentSubCountIndex);
            }


            if (displayCount != GUILayout.Toggle(displayCount, "Log scatter vertex count")) //Toggles
            {
                displayCount = !displayCount;
                window = new Rect(window.position.x, window.position.y, 450, 200);
            }
            if (displayCount)
            {
                displayCount = !displayCount;
                int totalCount = 0;
                foreach (Scatter scatter in BodyScatters.Values)
                {
                    int thisScatterCount = scatter.GetGlobalVertexCount(scatter);
                    totalCount += thisScatterCount;
                }
                ScatterLog.Log("All scatters combined contribute to a total of " + totalCount.ToString("N0") + " vertices being rendered right now");
            }


            Debug.Log("Current scatter name is " + currentScatter.scatterName);
            currentScatter.properties.scatterDistribution._Range = TextAreaLabelFloat(Labels[nameof(currentScatter.properties.scatterDistribution._Range)], currentScatter.properties.scatterDistribution._Range);
            currentScatter.properties.scatterDistribution._PopulationMultiplier = TextAreaLabelFloat(Labels[nameof(currentScatter.properties.scatterDistribution._PopulationMultiplier)], currentScatter.properties.scatterDistribution._PopulationMultiplier);
            currentScatter.properties.scatterDistribution._SizeNoiseStrength = TextAreaLabelFloat(Labels[nameof(currentScatter.properties.scatterDistribution._SizeNoiseStrength)], currentScatter.properties.scatterDistribution._SizeNoiseStrength);
            currentScatter.properties.scatterDistribution._SizeNoiseScale = TextAreaLabelFloat(Labels[nameof(currentScatter.properties.scatterDistribution._SizeNoiseScale)], currentScatter.properties.scatterDistribution._SizeNoiseScale);
            //
            //currentScatter.properties.scatterDistribution._SizeNoiseOffset = TextAreaLabelFloat(Labels[nameof(BodyScatter.properties.scatterDistribution._SizeNoiseOffset.x)], BodyScatter.properties.scatterDistribution._SizeNoiseOffset.x);
            //
            currentScatter.properties.scatterDistribution._MinScale = TextAreaLabelVector(Labels[nameof(currentScatter.properties.scatterDistribution._MinScale)], currentScatter.properties.scatterDistribution._MinScale);
            currentScatter.properties.scatterDistribution._MaxScale = TextAreaLabelVector(Labels[nameof(currentScatter.properties.scatterDistribution._MaxScale)], currentScatter.properties.scatterDistribution._MaxScale);
            currentScatter.properties.scatterDistribution._CutoffScale = TextAreaLabelFloat(Labels[nameof(currentScatter.properties.scatterDistribution._CutoffScale)], currentScatter.properties.scatterDistribution._CutoffScale);
            //currentScatter.properties.scatterDistribution._LODRange = TextAreaLabelFloat(Labels[nameof(currentScatter.properties.scatterDistribution._LODRange)], currentScatter.properties.scatterDistribution._LODRange);
            currentScatter.properties.scatterMaterial._MainColor = TextAreaLabelColor(Labels[nameof(currentScatter.properties.scatterMaterial._MainColor)], currentScatter.properties.scatterMaterial._MainColor);
            currentScatter.properties.scatterMaterial._SubColor = TextAreaLabelColor(Labels[nameof(currentScatter.properties.scatterMaterial._SubColor)], currentScatter.properties.scatterMaterial._SubColor);
            currentScatter.properties.scatterMaterial._ColorNoiseScale = TextAreaLabelFloat(Labels[nameof(currentScatter.properties.scatterMaterial._ColorNoiseScale)], currentScatter.properties.scatterMaterial._ColorNoiseScale);
            currentScatter.properties.scatterMaterial._ColorNoiseStrength = TextAreaLabelFloat(Labels[nameof(currentScatter.properties.scatterMaterial._ColorNoiseStrength)], currentScatter.properties.scatterMaterial._ColorNoiseStrength);
            currentScatter.properties.scatterDistribution.updateRate = (int)TextAreaLabelFloat(Labels[nameof(currentScatter.properties.scatterDistribution.updateRate)], currentScatter.properties.scatterDistribution.updateRate);

            currentScatter.properties.scatterMaterial = SetupMaterialUI(currentScatter.properties.scatterMaterial);
            if (currentScatter.subObjectCount > 0)




            {
                currentScatter.subObjects[currentSubCountIndex].properties._Density = TextAreaLabelFloat("SubObject density", currentScatter.subObjects[currentSubCountIndex].properties._Density);
                currentScatter.subObjects[currentSubCountIndex].properties._NoiseAmount = TextAreaLabelFloat("SubObject NoiseAmount", currentScatter.subObjects[currentSubCountIndex].properties._NoiseAmount);
                currentScatter.subObjects[currentSubCountIndex].properties._NoiseScale = TextAreaLabelFloat("SubObject NoiseScale", currentScatter.subObjects[currentSubCountIndex].properties._NoiseScale);
                currentScatter.subObjects[currentSubCountIndex].properties.material = SetupMaterialUI(currentScatter.subObjects[currentSubCountIndex].properties.material);
            }
                


            GUILayout.EndVertical();

            if (showTextures != GUILayout.Toggle(showTextures, "Show Textures"))
            {
                showTextures = !showTextures;
                window = new Rect(window.position.x, window.position.y, 450, 200);
            }

            if (showTextures)
            {
                GUILayout.BeginVertical();



                GUILayout.EndVertical();
            }
            GUI.DragWindow();
        }
        private static ScatterMaterial SetupMaterialUI(ScatterMaterial scatterMat)
        {
            for (int i = 0; i < scatterMat.Vectors.Keys.Count; i++)
            {
                string currentKey = scatterMat.Vectors.Keys.ToArray()[i];
                scatterMat.Vectors[currentKey] = TextAreaLabelVector(currentKey, scatterMat.Vectors[currentKey]);
            }
            for (int i = 0; i < scatterMat.Floats.Keys.Count; i++)
            {
                string currentKey = scatterMat.Floats.Keys.ToArray()[i];
                scatterMat.Floats[currentKey] = TextAreaLabelFloat(currentKey, scatterMat.Floats[currentKey]);
            }
            for (int i = 0; i < scatterMat.Colors.Keys.Count; i++)
            {
                string currentKey = scatterMat.Colors.Keys.ToArray()[i];
                scatterMat.Colors[currentKey] = TextAreaLabelColor(currentKey, scatterMat.Colors[currentKey]);
            }
            return scatterMat;
        }
        private static string TextAreaLabelTexture(string label, string value)
        {
            GUILayout.BeginHorizontal();
            string newValue = InputFields.TexField(label, value);
            GUILayout.EndHorizontal();

            if (newValue != value)
                anyValueHasChanged = true;

            return newValue;
        }
        private static float TextAreaLabelFloat(string label, float value)
        {
            GUILayout.BeginHorizontal();
            float newValue = InputFields.FloatField(label, value);
            GUILayout.EndHorizontal();

            if (Math.Abs(newValue - value) > 0.001)
                anyValueHasChanged = true;

            return newValue;
        }
        private static Vector3 TextAreaLabelVector(string label, Vector3 value)
        {
            Color colValue = new Color(value.x, value.y, value.z);
            Color col = TextAreaLabelColor(label, colValue);
            return new Vector3(col.r, col.g, col.b);
        }
        private static int TextAreaLabelInt(string label, int value, int minValue, int maxValue)
        {
            GUILayout.BeginHorizontal();
            int newValue = InputFields.IntField(label, value, minValue, maxValue);
            GUILayout.EndHorizontal();

            return newValue;
        }

        private static Color TextAreaLabelColor(string label, Color value)
        {
            GUILayout.BeginHorizontal();
            Color newValue = InputFields.ColorField(label, value);
            GUILayout.EndHorizontal();

            if (newValue != value)
                anyValueHasChanged = true;

            return newValue;
        }

        private static void SaveDefaultVars()
        {

            //Setup scatter stuff
        }

        private static void GetPropertyInfos()
        {
            
        }

        public void UpdateShaderValues()
        {


            Scatter scatter = currentScatter;
            Debug.Log("Forcing compute update");
            StartCoroutine(scatter.ForceComputeUpdate(currentScatter));
            Debug.Log("Done!");
            
            PostCompute[] allPostComputeComponents = UnityEngine.Resources.FindObjectsOfTypeAll(typeof(PostCompute)) as PostCompute[];
            foreach (PostCompute comp in allPostComputeComponents)
            {
                if (comp.gameObject.activeSelf)
                {
                    Debug.Log("Found ComputeComponent: " + comp.name);
                    if (comp == null)
                    {
                        Debug.Log("Component is null??");
                    }
                    comp.material = SetAllValues(comp.material);
                }
            }
        }
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