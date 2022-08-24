using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using ParallaxGrass;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;
using Grass;
using LibNoise;

namespace ScatterConfiguratorUtils
{
    public class Utils
    {
        public static bool oneCamera = false;
        public static Vector3 initialPlanetRelative = Vector3.zero;
        public static void DestroyComputeBufferSafe(ref ComputeBuffer buffer)
        {
            if (buffer != null)
            {
                buffer.Dispose();
                buffer = null;
            }
        }
        public static ComputeBuffer SetupComputeBufferSafe(int count, int stride, ComputeBufferType type)
        {
            if (count == 0 || stride == 0)
            {
                return null;
            }
            if (type == ComputeBufferType.Append)
            {
                return new ComputeBuffer(count, stride, ComputeBufferType.Append);
            }
            else if (type == ComputeBufferType.IndirectArguments)
            {
                return new ComputeBuffer(count, stride, ComputeBufferType.IndirectArguments);
            }
            else if (type == ComputeBufferType.Structured)
            {
                return new ComputeBuffer(count, stride);
            }
            else
            {
                ScatterLog.Log("Exception trying to create a buffer with a type that SetupComputeBufferSafe() does not account for: " + type);
                return new ComputeBuffer(count, stride);
            }
        }
        public static void SafetyCheckRelease(ComputeBuffer buffer, string nameThisBufferSomethingUseful)
        {
            if (buffer != null)
            {
                buffer.Release();
            }
            if (buffer == null)
            {
                //ScatterLog.Log("Exception performing release safety check on " + nameThisBufferSomethingUseful + " because it is null!");
            }
        }
        public static void SafetyCheckDispose(ref ComputeBuffer buffer, string nameThisBufferSomethingUseful)
        {
            if (buffer != null)
            {
                buffer.Dispose();
                buffer = null;
            }
            if (buffer == null)
            {
                //ScatterLog.Log("Exception performing dispose safety check on " + nameThisBufferSomethingUseful + " because it is null!");
            }
        }
        public static QuadDistributionData GetDistributionData(Scatter thisScatter, PQ quad)
        {
            DistributionNoise noise = thisScatter.properties.scatterDistribution.noise;
    
            if (noise.useNoiseProfile != null)
            {
                return PQSMod_ScatterDistribute.scatterData.distributionData[noise.useNoiseProfile].data[quad];
            }
            else
            {
                return PQSMod_ScatterDistribute.scatterData.distributionData[thisScatter.scatterName].data[quad];
            }
        }
        public static void SetDistributionVars(ref ComputeShader distribute, Scatter scatter, Transform transform, int quadSubdivisionDifference, int triCount, string sphereName, int kernel)
        {
            
            CelestialBody body = FlightGlobals.GetBodyByName(sphereName);
            distribute.SetInt("_PopulationMultiplier", (int)scatter.properties.scatterDistribution._PopulationMultiplier * quadSubdivisionDifference); //quadsubdiv diff
            distribute.SetMatrix("_ObjectToWorld", transform.localToWorldMatrix);
            distribute.SetVector("_PlanetOrigin", body.transform.position);
            distribute.SetVector("_PlanetNormal", Vector3.Normalize(GlobalPoint.originPoint - body.transform.position));
            distribute.SetVector("_ShaderOffset", -((Vector3)FloatingOrigin.TerrainShaderOffset));

            if (FlightGlobals.ActiveVessel != null && !FlightGlobals.ready) { distribute.SetVector("_ShaderOffset", Vector3.zero); }    //During scene change

            distribute.SetInt("_MaxCount", (triCount / 3) * (int)scatter.properties.scatterDistribution._PopulationMultiplier * quadSubdivisionDifference);
            distribute.SetVector("minScale", scatter.properties.scatterDistribution._MinScale);
            distribute.SetVector("maxScale", scatter.properties.scatterDistribution._MaxScale);
            distribute.SetFloat("minAltitude", scatter.properties.scatterDistribution._MinAltitude);
            distribute.SetFloat("maxAltitude", scatter.properties.scatterDistribution._MaxAltitude);
            distribute.SetFloat("grassSizeNoiseScale", scatter.properties.scatterDistribution.noise._SizeNoiseScale);
            distribute.SetFloat("grassSizeNoiseOffset", scatter.properties.scatterDistribution.noise._SizeNoiseOffset);
            distribute.SetVector("grassColorMain", scatter.properties.scatterMaterial._MainColor);//scatter.properties.scatterMaterial._MainColor);
            distribute.SetVector("grassColorSub", scatter.properties.scatterMaterial._SubColor);
            distribute.SetFloat("grassColorNoiseStrength", scatter.properties.scatterMaterial._ColorNoiseStrength);
            distribute.SetFloat("grassColorNoiseScale", scatter.properties.scatterDistribution.noise._ColorNoiseScale);
            distribute.SetFloat("seed", scatter.properties.scatterDistribution._Seed);
            if (scatter.properties.scatterDistribution.noise.noiseMode == DistributionNoiseMode.VerticalStack)
            {
                distribute.SetFloat("_StackSeparation", scatter.properties.scatterDistribution.noise._StackSeparation);
                distribute.SetInt("_VerticalMult", scatter.properties.scatterDistribution.noise._MaxStacks);
            }
            distribute.SetFloat("grassCutoffScale", scatter.properties.scatterDistribution._CutoffScale);
            distribute.SetFloat("grassSizeNoiseStrength", scatter.properties.scatterDistribution._SizeNoiseStrength);
            distribute.SetFloat("_SteepPower", scatter.properties.scatterDistribution._SteepPower);
            distribute.SetFloat("_SteepContrast", scatter.properties.scatterDistribution._SteepContrast);
            distribute.SetFloat("_SteepMidpoint", scatter.properties.scatterDistribution._SteepMidpoint);
            distribute.SetFloat("rotationMult", 1);
            distribute.SetFloat("_MaxNormalDeviance", scatter.properties.scatterDistribution._MaxNormalDeviance);
            distribute.SetFloat("_PlanetRadius", (float)body.Radius);
            distribute.SetVector("_PlanetRelative", Utils.initialPlanetRelative);
            distribute.SetMatrix("_WorldToPlanet", body.gameObject.transform.worldToLocalMatrix);
            distribute.SetFloat("spawnChance", scatter.properties.scatterDistribution._SpawnChance);
            if (scatter.properties.scatterDistribution.noise.noiseMode == DistributionNoiseMode.NonPersistent)
            {
                distribute.SetFloat("altFadeRange", scatter.properties.scatterDistribution._AltitudeFadeRange);
            }

            UnityEngine.Vector2d latlon = LatLon.GetLatitudeAndLongitude(body.BodyFrame, body.transform.position, transform.position);
            double lat = System.Math.Abs(latlon.x) % 45.0 - 22.5;
            double lon = System.Math.Abs(latlon.y) % 45.0 - 22.5;   //From -22.5 to 22.5 where 0 we want the highest density and -22.5 we want 1/3 density
            lat /= 22.5;
            lon /= 22.5;    //Now from -1 to 1, with 0 being where we want most density and -1 where we want 1/3

            lat = System.Math.Abs(lat);
            lon = System.Math.Abs(lon);    //Now from 0 to 1. 1 when at a corner

            double factor = (lat + lon) / 2;
            float multiplier = Mathf.Clamp01(Mathf.Lerp(1.0f, 0.333333f, Mathf.Pow((float)factor, 2)));
            float spawnChance = multiplier * scatter.properties.scatterDistribution._SpawnChance;
            distribute.SetFloat("_SpawnChance", spawnChance);
            //if (scatter.properties.scatterDistribution._PopulationMultiplier > 2 && multiplier < 0.65f)
            //{
            //    distribute.SetInt("_PopulationMultiplier", (int)(Mathf.Round((scatter.properties.scatterDistribution._PopulationMultiplier * quadSubdivisionDifference) * multiplier)));
            //    distribute.SetInt("_MaxCount", (triCount / 3) * (int)((float)scatter.properties.scatterDistribution._PopulationMultiplier * (float)quadSubdivisionDifference * multiplier));
            //}
            //if (scatter.properties.scatterDistribution._PopulationMultiplier < 3 && multiplier < 0.65f)
            //{
            //    distribute.SetFloat("spawnChance", scatter.properties.scatterDistribution._SpawnChance * multiplier);
            //    distribute.SetInt("_MaxCount", (triCount / 3) * (int)((float)scatter.properties.scatterDistribution._PopulationMultiplier * (float)quadSubdivisionDifference * multiplier));
            //}

            distribute.SetVector("_PlanetRelative", Utils.initialPlanetRelative);
            if (scatter.alignToTerrainNormal) { distribute.SetInt("_AlignToNormal", 1); } else { distribute.SetInt("_AlignToNormal", 0); }
        }
        public static void SetEvaluationVars(ref ComputeShader evaluate, Scatter scatter, Transform transform, int objectCount)
        {
            evaluate.SetFloat("range", scatter.properties.scatterDistribution._Range);
            evaluate.SetVector("_CameraPos", ActiveBuffers.cameraPos);// GlobalPoint.originPoint);//Camera.allCameras.FirstOrDefault(_cam => _cam.name == "Camera 00").gameObject.transform.position - gameObject.transform.position);
            evaluate.SetVector("_CraftPos", GlobalPoint.originPoint);
            if (scatter.useSurfacePos) { evaluate.SetVector("_CameraPos", ActiveBuffers.surfacePos); }

            evaluate.SetFloat("_LODPerc", scatter.properties.scatterDistribution.lods.lods[0].range / scatter.properties.scatterDistribution._Range);    //At what range does the LOD change to the low one?
            evaluate.SetFloat("_LOD2Perc", scatter.properties.scatterDistribution.lods.lods[1].range / scatter.properties.scatterDistribution._Range);

            evaluate.SetVector("_ShaderOffset", -((Vector3)FloatingOrigin.TerrainShaderOffset));
            evaluate.SetVector("_ThisPos", transform.position);
            evaluate.SetInt("_MaxCount", objectCount);

            evaluate.SetFloat("_CurrentTime", Time.timeSinceLevelLoad);
            evaluate.SetFloat("_FadePow", scatter.properties.scatterDistribution._RangePow);
            evaluate.SetFloats("_CameraFrustumPlanes", ActiveBuffers.planeNormals);             //Frustum culling
            evaluate.SetFloat("_CullLimit", scatter.cullingLimit);
            float cullingRangePerc = scatter.cullingRange / scatter.properties.scatterDistribution._Range;

            evaluate.SetFloat("_CullStartRange", cullingRangePerc);
            if (!ScatterGlobalSettings.frustumCull)
            {
                evaluate.SetFloat("_CullStartRange", 1);
            }
        }
        public static uint[] GenerateArgs(Mesh mesh)
        {
            
            uint[] args = new uint[5];
            if (mesh != null)
            {
                args[0] = (uint)mesh.GetIndexCount(0);
                args[1] = (uint)1;  //This is copied into using a compute buffer to avoid CPU readback
                args[2] = (uint)mesh.GetIndexStart(0);
                args[3] = (uint)mesh.GetBaseVertex(0);
            }
            return args;
        }
        public static void ForceGPUFinish(ComputeBuffer buffer, Type type, int count)
        {
            if (type == typeof(Vector3))
            {
                Vector3[] force = new Vector3[count];
                buffer.GetData(force);
                ScatterLog.Log("Forced GPU finish for " + count + " objects");
                force = null;
            }
        }
        public static void SetShaderProperties(ref Material mat, ref ScatterMaterial scatterMaterial, string scatterName)
        {
            Dictionary<string, string> textures = scatterMaterial.Textures;
            Dictionary<string, float> floats = scatterMaterial.Floats;
            Dictionary<string, Vector3> vectors = scatterMaterial.Vectors;
            Dictionary<string, Vector2> scales = scatterMaterial.Scales;
            Dictionary<string, Color> colors = scatterMaterial.Colors;
            string[] texKeys = textures.Keys.ToArray();
            string[] floatKeys = floats.Keys.ToArray();
            string[] vectorKeys = vectors.Keys.ToArray();
            string[] scaleKeys = scales.Keys.ToArray();
            string[] colorKeys = colors.Keys.ToArray();
            for (int i = 0; i < texKeys.Length; i++)
            {
                Debug.Log("Setting texture: " + texKeys[i] + " to " + scatterName + "-" + textures[texKeys[i]]);
                mat.SetTexture(texKeys[i], LoadOnDemand.activeTextures[scatterName + "-" + texKeys[i]]);
            }
            for (int i = 0; i < floatKeys.Length; i++)
            {
                mat.SetFloat(floatKeys[i], floats[floatKeys[i]]);
            }
            for (int i = 0; i < vectorKeys.Length; i++)
            {
                mat.SetVector(vectorKeys[i], vectors[vectorKeys[i]]);
            }
            for (int i = 0; i < scaleKeys.Length; i++)
            {
                mat.SetTextureScale(scaleKeys[i].Replace("Scale", string.Empty), scales[scaleKeys[i]]);    //_MainTex|Scale|
            }
            for (int i = 0; i < colorKeys.Length; i++)
            {
                mat.SetColor(colorKeys[i], colors[colorKeys[i]]);
            }
            mat.SetFloat("_InitialTime", Time.realtimeSinceStartup);
        }
        public static readonly Dictionary<string, string>
            VarFromLabels = new Dictionary<string, string>
            {
                {nameof(scatterBody.properties.scatterDistribution._Range),  nameof(scatterBody.properties.scatterDistribution._Range)},
                {nameof(scatterBody.properties.scatterDistribution._PopulationMultiplier),  nameof(scatterBody.properties.scatterDistribution._PopulationMultiplier)},
                {nameof(scatterBody.properties.scatterDistribution._SizeNoiseStrength),  nameof(scatterBody.properties.scatterDistribution._SizeNoiseStrength)},
                {nameof(scatterBody.properties.scatterDistribution.noise._Frequency),  nameof(scatterBody.properties.scatterDistribution.noise._Frequency)},
                {nameof(scatterBody.properties.scatterDistribution.noise._Persistence),  nameof(scatterBody.properties.scatterDistribution.noise._Persistence)},
                {nameof(scatterBody.properties.scatterDistribution.noise._Lacunarity),  nameof(scatterBody.properties.scatterDistribution.noise._Lacunarity)},

                {nameof(scatterBody.properties.scatterDistribution.noise._Octaves),  nameof(scatterBody.properties.scatterDistribution.noise._Octaves)},
                {nameof(scatterBody.properties.scatterDistribution.noise._Seed),  nameof(scatterBody.properties.scatterDistribution.noise._Seed)},
                {nameof(scatterBody.properties.scatterDistribution.noise._NoiseType),  nameof(scatterBody.properties.scatterDistribution.noise._NoiseType)},

                {nameof(scatterBody.properties.scatterDistribution._MaxScale),  nameof(scatterBody.properties.scatterDistribution._MaxScale)},
                {nameof(scatterBody.properties.scatterDistribution._MinScale),  nameof(scatterBody.properties.scatterDistribution._MinScale)},
                {nameof(scatterBody.properties.scatterDistribution._CutoffScale),  nameof(scatterBody.properties.scatterDistribution._CutoffScale)},

                {nameof(scatterBody.properties.scatterMaterial._MainColor),  nameof(scatterBody.properties.scatterMaterial._MainColor)},
                {nameof(scatterBody.properties.scatterMaterial._SubColor),  nameof(scatterBody.properties.scatterMaterial._SubColor)},
                {nameof(scatterBody.properties.scatterMaterial._ColorNoiseStrength),  nameof(scatterBody.properties.scatterMaterial._ColorNoiseStrength)},

                 //{nameof(scatterBody.properties.scatterDistribution._LODRange),  nameof(scatterBody.properties.scatterDistribution._LODRange)}
            };

        public static readonly Dictionary<string, string>
            LabelFromVar = new Dictionary<string, string>
            {
                {nameof(scatterBody.properties.scatterDistribution._Range),  nameof(scatterBody.properties.scatterDistribution._Range)},
                {nameof(scatterBody.properties.scatterDistribution._PopulationMultiplier),  nameof(scatterBody.properties.scatterDistribution._PopulationMultiplier)},
                {nameof(scatterBody.properties.scatterDistribution._SizeNoiseStrength),  nameof(scatterBody.properties.scatterDistribution._SizeNoiseStrength)},
                {nameof(scatterBody.properties.scatterDistribution.noise._Frequency),  nameof(scatterBody.properties.scatterDistribution.noise._Frequency)},
                {nameof(scatterBody.properties.scatterDistribution.noise._Persistence),  nameof(scatterBody.properties.scatterDistribution.noise._Persistence)},
                {nameof(scatterBody.properties.scatterDistribution.noise._Lacunarity),  nameof(scatterBody.properties.scatterDistribution.noise._Lacunarity)},

                {nameof(scatterBody.properties.scatterDistribution.noise._Octaves),  nameof(scatterBody.properties.scatterDistribution.noise._Octaves)},
                {nameof(scatterBody.properties.scatterDistribution.noise._Seed),  nameof(scatterBody.properties.scatterDistribution.noise._Seed)},
                {nameof(scatterBody.properties.scatterDistribution.noise._NoiseType),  nameof(scatterBody.properties.scatterDistribution.noise._NoiseType)},
                {nameof(scatterBody.properties.scatterDistribution._MaxScale),  nameof(scatterBody.properties.scatterDistribution._MaxScale)},
                {nameof(scatterBody.properties.scatterDistribution._MinScale),  nameof(scatterBody.properties.scatterDistribution._MinScale)},
                {nameof(scatterBody.properties.scatterDistribution._CutoffScale),  nameof(scatterBody.properties.scatterDistribution._CutoffScale)},
                {nameof(scatterBody.properties.scatterMaterial._MainColor),  nameof(scatterBody.properties.scatterMaterial._MainColor)},
                {nameof(scatterBody.properties.scatterMaterial._SubColor),  nameof(scatterBody.properties.scatterMaterial._SubColor)},
                {nameof(scatterBody.properties.scatterMaterial._ColorNoiseStrength),  nameof(scatterBody.properties.scatterMaterial._ColorNoiseStrength)},


                //{nameof(scatterBody.properties.scatterDistribution._LODRange),  nameof(scatterBody.properties.scatterDistribution._LODRange)}
            };
        private static Scatter scatterBody => ParallaxConfiguratorMain.currentScatter;

        public static object GetVariableOriginalValue(string varName)
        {
            Debug.Log($"[ParallaxConfigurator] Resetting to original value");
            return 1;
        }
    }
    public class InputFields
    {
        private static int activeFieldID = -1;

        private static float activeFloatFieldLastValue = 0;
        private static string activeFloatFieldString = "";

        private static Color activeColorFieldLastValue = new Color();
        private static string activeColorFieldString = "";

        private static string activeTexFieldLastValue = "";
        private static string activeTexFieldString = "";

        private static float activeSliderFieldLastValue = 0;
        private static string activeSliderFieldString = "";

        private static float activeVectorXStringLV = 0;
        private static float activeVectorYStringLV = 0;
        private static float activeVectorZStringLV = 0;

        private static readonly GUIStyle TexStyle = new GUIStyle(GUI.skin.textArea) { wordWrap = true };
        private static readonly GUIStyle ResetButtonStyle = HighLogic.Skin.button;
        private static readonly GUIStyle FloatStyle = HighLogic.Skin.textField;
        private static readonly GUIStyle ColorStyle = HighLogic.Skin.textField;

        /// <summary>
        /// Float input field for in-game cfg editing. Behaves exactly like UnityEditor.EditorGUILayout.FloatField
        /// </summary>
        /// 
        public static Vector3 VectorField(Vector3 value)
        {
            // Get rect and control for this float field for identification
            Rect pos = GUILayoutUtility.GetRect(new GUIContent(value.x.ToString(CultureInfo.InvariantCulture)),
                GUI.skin.label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(50));
            pos.x -= 110;
            Rect pos2 = pos;
            pos2.x += 55;
            Rect pos3 = pos2;
            pos3.x += 55;


            string str = value.x.ToString(CultureInfo.InvariantCulture);
            string str2 = value.y.ToString(CultureInfo.InvariantCulture);
            string str3 = value.z.ToString(CultureInfo.InvariantCulture);

            // pass it in the text field
            string strValue = GUI.TextField(pos, str, FloatStyle);
            string strValue2 = GUI.TextField(pos2, str2, FloatStyle);
            string strValue3 = GUI.TextField(pos3, str3, FloatStyle);
            // Update stored value if this one is recorded


            // Try Parse if value got changed. If the string could not be parsed, ignore it and keep last value
            bool parsed = true;
            if (strValue + ", " + strValue2 + ", " + strValue3 != value.x.ToString(CultureInfo.InvariantCulture))
            {
                parsed = float.TryParse(strValue, out float newValue);
                if (parsed)
                    value.x = activeVectorXStringLV = newValue;
                parsed = float.TryParse(strValue2, out float newValue2);
                if (parsed)
                    value.y = activeVectorYStringLV = newValue2;
                parsed = float.TryParse(strValue3, out float newValue3);
                if (parsed)
                    value.z = activeVectorZStringLV = newValue3;
            }

            activeFieldID = -1;

            return value;
        }
        public static float FloatField(float value)
        {
            // Get rect and control for this float field for identification
            Rect pos = GUILayoutUtility.GetRect(new GUIContent(value.ToString(CultureInfo.InvariantCulture)),
                GUI.skin.label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(50));
            int floatFieldID = GUIUtility.GetControlID("FloatField".GetHashCode(), FocusType.Keyboard, pos) + 1;
            if (floatFieldID == 0)
                return value;

            // has the value been recorded?
            bool recorded = activeFieldID == floatFieldID;
            // is the field being edited?
            bool active = floatFieldID == GUIUtility.keyboardControl;

            if (active && recorded && activeFloatFieldLastValue != value)
            {
                // Value has been modified externally
                activeFloatFieldLastValue = value;
                activeFloatFieldString = value.ToString(CultureInfo.InvariantCulture);
            }

            // Get stored string for the text field if this one is recorded
            string str = recorded ? activeFloatFieldString : value.ToString(CultureInfo.InvariantCulture);

            // pass it in the text field
            string strValue = GUI.TextField(pos, str, FloatStyle);

            // Update stored value if this one is recorded
            if (recorded)
                activeFloatFieldString = strValue;

            // Try Parse if value got changed. If the string could not be parsed, ignore it and keep last value
            bool parsed = true;
            if (strValue != value.ToString(CultureInfo.InvariantCulture))
            {
                parsed = float.TryParse(strValue, out float newValue);
                if (parsed)
                    value = activeFloatFieldLastValue = newValue;
            }

            if (active && !recorded)
            {
                // Gained focus this frame
                activeFieldID = floatFieldID;
                activeFloatFieldString = strValue;
                activeFloatFieldLastValue = value;
            }
            else if (!active && recorded)
            {
                // Lost focus this frame
                activeFieldID = -1;
                if (!parsed)
                    value = strValue.ForceParseFloat();
            }

            return value;
        }

        /// <summary>
        /// Color input field for in-game cfg editing. Parses basic colors (e.g. "yellow"), as well as any
        /// string in the format "r, g, b, (a)". Ignores any character before or after the comma-separated floats.
        /// </summary>
        public static Color ColorField(Color value)
        {
            // Get rect and control for this float field for identification
            Rect pos = GUILayoutUtility.GetRect(new GUIContent(value.ToString()), GUI.skin.label,
                GUILayout.ExpandWidth(false), GUILayout.MinWidth(220));
            int colorFieldID = GUIUtility.GetControlID("ColorField".GetHashCode(), FocusType.Keyboard, pos) + 1;
            if (colorFieldID == 0)
                return value;

            // has the value been recorded?
            bool recorded = activeFieldID == colorFieldID;
            // is the field being edited?
            bool active = colorFieldID == GUIUtility.keyboardControl;

            if (active && recorded && activeColorFieldLastValue != value)
            {
                // Value has been modified externally
                activeColorFieldLastValue = value;
                activeColorFieldString = value.ToString();
            }

            // Get stored string for the text field if this one is recorded
            string str = recorded ? activeColorFieldString : value.ToString();

            // pass it in the text field
            string strValue = GUI.TextField(pos, str, ColorStyle);

            // Update stored value if this one is recorded
            if (recorded)
                activeColorFieldString = strValue;

            // Try Parse if value got changed. If the string could not be parsed, ignore it and keep last value
            bool parsed = true;
            if (strValue != value.ToString())
            {
                parsed = ColorUtility.TryParseHtmlString(strValue, out Color newValue);
                if (parsed)
                    value = activeColorFieldLastValue = newValue;
            }

            if (active && !recorded)
            {
                // Gained focus this frame
                activeFieldID = colorFieldID;
                activeColorFieldString = strValue;
                activeColorFieldLastValue = value;
            }
            else if (!active && recorded)
            {
                // Lost focus this frame
                activeFieldID = -1;
                if (!parsed)
                    value = strValue.ForceParseColor(activeColorFieldLastValue);
            }

            return value;
        }
        public static float SliderField(float value, float min, float max)
        {
            // Get rect and control for this float field for identification
            Rect pos = GUILayoutUtility.GetRect(new GUIContent(value.ToString()), GUI.skin.label,
                GUILayout.ExpandWidth(false), GUILayout.MinWidth(220));
            int colorFieldID = GUIUtility.GetControlID("SliderField".GetHashCode(), FocusType.Keyboard, pos) + 1;
            if (colorFieldID == 0)
                return value;

            // has the value been recorded?
            bool recorded = activeFieldID == colorFieldID;
            // is the field being edited?
            bool active = colorFieldID == GUIUtility.keyboardControl;

            if (active && recorded && activeSliderFieldLastValue != value)
            {
                // Value has been modified externally
                activeSliderFieldLastValue = value;
                activeSliderFieldString = value.ToString();
            }

            // Get stored string for the text field if this one is recorded
            string str = recorded ? activeSliderFieldString : value.ToString();

            // pass it in the text field
            string strValue = GUI.HorizontalSlider(pos, value, min, max, HighLogic.Skin.horizontalSlider, HighLogic.Skin.horizontalSliderThumb).ToString("F4");

            // Update stored value if this one is recorded
            if (recorded)
                activeSliderFieldString = strValue;

            // Try Parse if value got changed. If the string could not be parsed, ignore it and keep last value
            bool parsed = true;
            if (strValue != value.ToString())
            {
                parsed = float.TryParse(strValue, out float newValue);
                if (parsed)
                    value = activeSliderFieldLastValue = newValue;
            }

            if (active && !recorded)
            {
                // Gained focus this frame
                activeFieldID = colorFieldID;
                activeSliderFieldString = strValue;
                activeSliderFieldLastValue = value;
            }
            else if (!active && recorded)
            {
                // Lost focus this frame
                activeFieldID = -1;
                if (!parsed)
                    value = strValue.ForceParseFloat();
            }

            return value;
        }
        /// <summary>
        /// Special-purpose Text input field for in-game editing. Meant for dds or png textures' paths.
        /// Check if texture file exists before applying the value.
        /// </summary>
        public static string TexField(string value)
        {
            // Get rect and control for this float field for identification
            Rect pos = GUILayoutUtility.GetRect(new GUIContent(value.ToString(CultureInfo.InvariantCulture)),
                GUI.skin.label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(200));

            int floatFieldID = GUIUtility.GetControlID("StringField".GetHashCode(), FocusType.Keyboard, pos) + 1;
            if (floatFieldID == 0)
                return value;

            // has the value been recorded?
            bool recorded = activeFieldID == floatFieldID;
            // is the field being edited?
            bool active = floatFieldID == GUIUtility.keyboardControl;

            if (active && recorded && activeTexFieldLastValue != value)
            {
                // Value has been modified externally
                activeTexFieldLastValue = value;
                activeTexFieldString = value.ToString(CultureInfo.InvariantCulture);
            }

            // Get stored string for the text field if this one is recorded
            string str = recorded ? activeTexFieldString : value.ToString(CultureInfo.InvariantCulture);

            // pass it in the text field
            string strValue = GUI.TextField(pos, str, FloatStyle);

            // Update stored value if this one is recorded
            if (recorded)
                activeTexFieldString = strValue;

            // Try Parse if value got changed. If the string could not be parsed, ignore it and keep last value
            bool parsed = true;
            if (strValue != value.ToString(CultureInfo.InvariantCulture))
            {

                value = activeTexFieldLastValue = strValue;
            }

            if (active && !recorded)
            {
                // Gained focus this frame
                activeFieldID = floatFieldID;
                activeTexFieldString = strValue;
                activeTexFieldLastValue = value;
            }
            else if (!active && recorded)
            {
                // Lost focus this frame
                activeFieldID = -1;
                if (!parsed)
                    value = strValue;
            }

            return value;
        }

        /// <summary>
        /// Float input field with label. Displays the label on the left, and the input field on the right.
        /// </summary>
        public static float FloatField(string label, float value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + " [float] ", GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();

            value = FloatField(value);

            if (GUILayout.Button("Undo", ResetButtonStyle))
                value = (float)Utils.GetVariableOriginalValue(Utils.VarFromLabels[label]);

            GUILayout.EndHorizontal();
            return value;
        }

        /// <summary>
        /// Int input field with label. Displays the label on the left, and the input field on the right.
        /// Just a FloatField, but with returned value being rounded to nearest int.
        /// </summary>
        public static int IntField(string label, float value, int minValue, int maxValue)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + " [float] ", GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();

            value = FloatField(value);

            GUILayout.EndHorizontal();
            return Mathf.Clamp(Mathf.RoundToInt(value), minValue, maxValue);
        }

        /// <summary>
        /// Color input field with label. Displays the label on the left, and the input field on the right.
        /// </summary>
        public static Color ColorField(string label, Color value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + " [Color] ", GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();

            value = ColorField(value);

            if (GUILayout.Button("Undo", ResetButtonStyle))
                value = (Color)Utils.GetVariableOriginalValue(Utils.VarFromLabels[label]);

            GUILayout.EndHorizontal();
            return value;
        }
        public static float SliderField(string label, float value, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + " [" + value.ToString() + "] ", GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();

            value = SliderField(value, min, max);


            GUILayout.EndHorizontal();
            return value;
        }
        /// <summary>
        /// Texture path input field with label. Displays the label on the left, and the input field on the right.
        /// </summary>
        public static string TexField(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + " [Texture] ", GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();

            value = TexField(value);

            if (GUILayout.Button("Undo", ResetButtonStyle))
                value = (string)Utils.GetVariableOriginalValue(Utils.VarFromLabels[label]);

            GUILayout.EndHorizontal();
            return value;
        }
        public static Vector3 VectorField(string label, Vector3 value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + " [float] ", GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();

            value = VectorField(value);


            GUILayout.EndHorizontal();
            return new Vector3(value.x, value.y, value.z);
        }
    }
    public static class Parsers
    {
        private const string ColorStringPattern = @"(\d\.\d*)[^\d]*(\d\.\d*)[^\d]*(\d\.\d*)[^\d]*(\d\.\d*)?";

        /// <summary>
        /// Forces to parse to float by cleaning string if necessary
        /// </summary>
        public static float ForceParseFloat(this string str)
        {
            // try parse
            if (float.TryParse(str, out float value))
                return value;

            // Clean string if it could not be parsed
            bool recordedDecimalPoint = false;
            var charList = new List<char>(str);
            for (int cnt = 0; cnt < charList.Count; cnt++)
            {
                UnicodeCategory type = CharUnicodeInfo.GetUnicodeCategory(str[cnt]);
                if (type != UnicodeCategory.DecimalDigitNumber)
                {
                    charList.RemoveRange(cnt, charList.Count - cnt);
                    break;
                }

                if (str[cnt] != '.') continue;

                if (recordedDecimalPoint)
                {
                    charList.RemoveRange(cnt, charList.Count - cnt);
                    break;
                }

                recordedDecimalPoint = true;
            }

            // Parse again
            if (charList.Count == 0)
                return 0;
            str = new string(charList.ToArray());
            if (!float.TryParse(str, out value))
                Debug.LogError("Could not parse " + str);

            return value;
        }

        public static Color ForceParseColor(this string str, Color defaultValue)
        {
            Color newValue;

            if (ColorUtility.TryParseHtmlString(str, out newValue))
                return newValue;

            Regex r = new Regex(ColorStringPattern);
            Match m = r.Match(str);

            if (!m.Success)
                return defaultValue;


            float[] col = { 1f, 1f, 1f, 1f };
            for (int i = 0; i < m.Groups.Count - 1; i++)
            {
                // groups[0] is the entire match, groups[1] is the first group 
                col[i] = Mathf.Clamp01(m.Groups[i + 1].ToString().ForceParseFloat());
            }

            return new Color(col[0], col[1], col[2], col[3]);
        }
    }
    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class ConfigUtil
    {
        public static void SaveAllToConfig(string bodyName)
        {
            Debug.Log("Starting");
            bool exists = false;
            string filePath = KSPUtil.ApplicationRootPath + "GameData/Parallax/Exports/";
            FileInfo fileInfo = new FileInfo(filePath);
            if (fileInfo.Directory.Attributes == FileAttributes.Directory)
            {
                exists = true;
            }
            if (!exists)
            {
                ScatterLog.Log("[Exception] File path: " + filePath + " does not exist!");
                return;
            }
            ConfigNode node = new ConfigNode();
            ScatterBody body = ScatterBodies.scatterBodies[bodyName];
            ConfigNode parallaxScattersNode = node.AddNode("ParallaxScatters-EXPORTED //Remove -EXPORTED when turning this into a config");
            parallaxScattersNode.AddValue("body", body.bodyName);
            parallaxScattersNode.AddValue("minimumSubdivision", body.minimumSubdivision);
            string path = filePath + body.bodyName + ".cfg";
            foreach (string scatterKey in body.scatters.Keys)
            {
                Scatter scatter = body.scatters[scatterKey];
                ConfigNode scatterNode;
                if (!scatter.shared)
                {
                    scatterNode = parallaxScattersNode.AddNode("Scatter");
                }
                else
                {
                    scatterNode = parallaxScattersNode.AddNode("SharedScatter");
                }
                scatterNode.AddValue("name", scatter.scatterName.Split('-')[1].Replace("-", string.Empty));
                scatterNode.AddValue("model", scatter.model);
                if (!scatter.shared)
                {
                    scatterNode.AddValue("updateFPS", scatter.updateFPS);
                    scatterNode.AddValue("alignToTerrainNormal", scatter.alignToTerrainNormal);
                    scatterNode.AddValue("cullingRange", scatter.cullingRange);
                    scatterNode.AddValue("cullingLimit", scatter.cullingLimit);
                    scatterNode.AddValue("maxObjects", scatter.maxObjects);
                }
                
                scatterNode.AddValue("shadowMode", scatter.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.On ? "forcedFull" : "standard");
                if (scatter.shared) { scatterNode.AddValue("parent", scatter.sharedParent); }
                
                if (!scatter.shared)
                {
                    ConfigNode subdivisionNode = scatterNode.AddNode("SubdivisionSettings");
                    subdivisionNode.AddValue("subdivisionLevel", scatter.properties.subdivisionSettings.level);
                    subdivisionNode.AddValue("subdivisionRangeMode", scatter.properties.subdivisionSettings.mode.ToString());
                    subdivisionNode.AddValue("subdivisionRange", scatter.properties.subdivisionSettings.range);
                    subdivisionNode.AddValue("minimumSubdivision", scatter.properties.subdivisionSettings.minLevel);
                    ConfigNode distNoiseNode = scatterNode.AddNode("DistributionNoise");
                    DistributionNoise noiseDist = scatter.properties.scatterDistribution.noise;
                    distNoiseNode.AddValue("mode", noiseDist.noiseMode.ToString());
                    if (noiseDist.noiseMode == DistributionNoiseMode.Persistent)
                    {
                        distNoiseNode.AddValue("_Frequency", noiseDist._Frequency);
                        distNoiseNode.AddValue("_Persistence", noiseDist._Persistence);
                        distNoiseNode.AddValue("_Lacunarity", noiseDist._Lacunarity);
                        distNoiseNode.AddValue("_Octaves", noiseDist._Octaves);
                        distNoiseNode.AddValue("_Seed", noiseDist._Seed);
                        distNoiseNode.AddValue("_NoiseType", noiseDist._NoiseType);
                        if (noiseDist._NoiseQuality == NoiseQuality.Low) { distNoiseNode.AddValue("_NoiseQuality", "Low"); }
                        if (noiseDist._NoiseQuality == NoiseQuality.Standard) { distNoiseNode.AddValue("_NoiseQuality", "Standard"); }
                        if (noiseDist._NoiseQuality == NoiseQuality.High) { distNoiseNode.AddValue("_NoiseQuality", "High"); }
                    }
                    else
                    {
                        distNoiseNode.AddValue("_SizeNoiseScale", noiseDist._SizeNoiseScale);
                        distNoiseNode.AddValue("_ColorNoiseScale", noiseDist._ColorNoiseScale);
                        distNoiseNode.AddValue("_SizeNoiseOffset", noiseDist._SizeNoiseOffset);
                    }
                }
                ConfigNode distributionNode = scatterNode.AddNode("Distribution");
                Distribution dist = scatter.properties.scatterDistribution;
                if (!scatter.shared)
                {
                    
                    distributionNode.AddValue("_Seed", dist._Seed);
                    distributionNode.AddValue("_SpawnChance", dist._SpawnChance);
                    distributionNode.AddValue("_Range", dist._Range);
                    distributionNode.AddValue("_PopulationMultiplier", dist._PopulationMultiplier);
                    distributionNode.AddValue("_SizeNoiseStrength", dist._SizeNoiseStrength);
                    distributionNode.AddValue("_MinScale", dist._MinScale);
                    distributionNode.AddValue("_MaxScale", dist._MaxScale);
                    distributionNode.AddValue("_CutoffScale", dist._CutoffScale);
                    distributionNode.AddValue("_SteepPower", dist._SteepPower);
                    distributionNode.AddValue("_SteepContrast", dist._SteepContrast);
                    distributionNode.AddValue("_SteepMidpoint", dist._SteepMidpoint);
                    distributionNode.AddValue("_NormalDeviance", dist._MaxNormalDeviance);
                    distributionNode.AddValue("_MinAltitude", dist._MinAltitude);
                    distributionNode.AddValue("_MaxAltitude", dist._MaxAltitude);
                    distributionNode.AddValue("_RangePow", dist._RangePow);
                    if (scatter.properties.subdivisionSettings.mode == SubdivisionMode.NearestQuads) { distributionNode.AddValue("_AltitudeFadeRange", dist._RangePow); }
                }
                
                ConfigNode lodNode = distributionNode.AddNode("LODs");
                foreach (Grass.LOD lod in dist.lods.lods)
                {
                    ConfigNode configLOD = lodNode.AddNode("LOD");
                    configLOD.AddValue("model", lod.modelName);
                    configLOD.AddValue("range", lod.range);
                    configLOD.AddValue("billboard", lod.isBillboard);
                    configLOD.AddValue("_MainTex", lod.mainTexName);
                    configLOD.AddValue("_BumpMap", lod.normalName);
                }
                ConfigNode materialNode = scatterNode.AddNode("Material");
                ScatterMaterial mat = scatter.properties.scatterMaterial;
                materialNode.AddValue("shader", mat.shader.name);
                materialNode.AddValue("_MainColor", mat._MainColor);
                materialNode.AddValue("_SubColor", mat._SubColor);
                materialNode.AddValue("_ColorNoiseStrength", mat._ColorNoiseStrength);
                SaveMaterialNode(materialNode, mat);
            }
            node.Save(path);
            

           
        }
        public static void SaveMaterialNode(ConfigNode node, ScatterMaterial scatterMaterial)
        {
            Dictionary<string, string> textures = scatterMaterial.Textures;
            Dictionary<string, float> floats = scatterMaterial.Floats;
            Dictionary<string, Vector3> vectors = scatterMaterial.Vectors;
            Dictionary<string, Vector2> scales = scatterMaterial.Scales;
            Dictionary<string, Color> colors = scatterMaterial.Colors;
            string[] texKeys = textures.Keys.ToArray();
            string[] floatKeys = floats.Keys.ToArray();
            string[] vectorKeys = vectors.Keys.ToArray();
            string[] scaleKeys = scales.Keys.ToArray();
            string[] colorKeys = colors.Keys.ToArray();
            for (int i = 0; i < texKeys.Length; i++)
            {
                node.AddValue(texKeys[i], textures[texKeys[i]]);
            }
            for (int i = 0; i < floatKeys.Length; i++)
            {
                node.AddValue(floatKeys[i], floats[floatKeys[i]]);
            }
            for (int i = 0; i < vectorKeys.Length; i++)
            {
                node.AddValue(vectorKeys[i], vectors[vectorKeys[i]]);
            }
            for (int i = 0; i < scaleKeys.Length; i++)
            {
                node.AddValue(scaleKeys[i], scales[scaleKeys[i]]);
            }
            for (int i = 0; i < colorKeys.Length; i++)
            {
                node.AddValue(colorKeys[i], colors[colorKeys[i]]);
            }
        }
    }
}