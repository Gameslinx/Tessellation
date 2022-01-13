using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using ParallaxGrass;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Linq;

namespace ScatterConfiguratorUtils
{
    public class Utils
    {
        public static void DestroyComputeBufferSafe(ComputeBuffer buffer)
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
        public static void SafetyCheckDispose(ComputeBuffer buffer, string nameThisBufferSomethingUseful)
        {
            if (buffer != null)
            {
                buffer.Dispose();
            }
            if (buffer == null)
            {
                //ScatterLog.Log("Exception performing dispose safety check on " + nameThisBufferSomethingUseful + " because it is null!");
            }
        }
        public static uint[] GenerateArgs(Mesh mesh, int count)
        {
            if (count == 0)
            {
                Debug.Log("Exception: Count is 0");
                return null;
            }
            uint[] args = new uint[5];
            args[0] = (uint)mesh.GetIndexCount(0);
            args[1] = (uint)count;
            args[2] = (uint)mesh.GetIndexStart(0);
            args[3] = (uint)mesh.GetBaseVertex(0);
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
        public static Material GetSubObjectMaterial(Scatter scatter, int index)
        {
            
            if (index >= scatter.subObjectCount)
            {
                Debug.Log("Returning standard shader for index " + index);
                return new Material(Shader.Find("Standard"));
            }
            else
            {
                SubObject sub = scatter.subObjects[index];
                Material mat = new Material(sub.properties.material.shader);
                Debug.Log("Shader is " + sub.properties.material.shader);
                mat.SetTexture("_MainTex", Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(t => t.name == sub.properties.material._MainTex));
                Debug.Log("Texture is " + sub.properties.material._MainTex);
                mat.SetTexture("_BumpMap", Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(t => t.name == sub.properties.material._BumpMap));
                mat.SetFloat("_Shininess", sub.properties.material._Shininess);
                mat.SetColor("_SpecColor", sub.properties.material._SpecColor);
                mat.SetFloat("_WaveSpeed", 0);
                mat.SetFloat("_HeightCutoff", -1);
                mat.SetFloat("_HeightFactor", 0);
                mat.SetColor("_Color", new Color(1, 1, 1));
                mat.SetVector("_PlanetOrigin", FlightGlobals.ActiveVessel.transform.position);
                return mat;
            }
        }
        public static Mesh GetSubObjectMesh(Scatter scatter, int index)
        {
            if (index >= scatter.subObjectCount)
            {
                return null;
            }
            else
            {
                SubObject sub = scatter.subObjects[index];
                GameObject go = GameDatabase.Instance.GetModel(sub.properties.model);
                Mesh mesh = GameObject.Instantiate( go.GetComponent<MeshFilter>().mesh);
                return mesh;
            }
        }
        
        public static readonly Dictionary<string, string>
            VarFromLabels = new Dictionary<string, string>
            {
                {nameof(scatterBody.properties.scatterDistribution._Range),  nameof(scatterBody.properties.scatterDistribution._Range)},
                {nameof(scatterBody.properties.scatterDistribution._PopulationMultiplier),  nameof(scatterBody.properties.scatterDistribution._PopulationMultiplier)},
                {nameof(scatterBody.properties.scatterDistribution._SizeNoiseStrength),  nameof(scatterBody.properties.scatterDistribution._SizeNoiseStrength)},
                {nameof(scatterBody.properties.scatterDistribution._SizeNoiseScale),  nameof(scatterBody.properties.scatterDistribution._SizeNoiseScale)},
                {nameof(scatterBody.properties.scatterDistribution._SizeNoiseOffset),  nameof(scatterBody.properties.scatterDistribution._SizeNoiseOffset)},
                {nameof(scatterBody.properties.scatterDistribution._MaxScale),  nameof(scatterBody.properties.scatterDistribution._MaxScale)},
                {nameof(scatterBody.properties.scatterDistribution._MinScale),  nameof(scatterBody.properties.scatterDistribution._MinScale)},
                {nameof(scatterBody.properties.scatterDistribution._CutoffScale),  nameof(scatterBody.properties.scatterDistribution._CutoffScale)},
                {nameof(scatterBody.properties.scatterDistribution.updateRate),  nameof(scatterBody.properties.scatterDistribution.updateRate)},

                {nameof(scatterBody.properties.scatterMaterial._MainColor),  nameof(scatterBody.properties.scatterMaterial._MainColor)},
                {nameof(scatterBody.properties.scatterMaterial._SubColor),  nameof(scatterBody.properties.scatterMaterial._SubColor)},
                {nameof(scatterBody.properties.scatterMaterial._ColorNoiseScale),  nameof(scatterBody.properties.scatterMaterial._ColorNoiseScale)},
                {nameof(scatterBody.properties.scatterMaterial._ColorNoiseStrength),  nameof(scatterBody.properties.scatterMaterial._ColorNoiseStrength)},

                {nameof(scatterBody.properties.scatterWind._WindSpeed),  nameof(scatterBody.properties.scatterWind._WindSpeed)},
                {nameof(scatterBody.properties.scatterWind._WaveSpeed),  nameof(scatterBody.properties.scatterWind._WaveSpeed)},
                {nameof(scatterBody.properties.scatterWind._WaveAmp),  nameof(scatterBody.properties.scatterWind._WaveAmp)},
                {nameof(scatterBody.properties.scatterWind._HeightCutoff),  nameof(scatterBody.properties.scatterWind._HeightCutoff)},
                {nameof(scatterBody.properties.scatterWind._HeightFactor),  nameof(scatterBody.properties.scatterWind._HeightFactor)},
                 {nameof(scatterBody.properties.scatterDistribution._LODRange),  nameof(scatterBody.properties.scatterDistribution._LODRange)}
            };

        public static readonly Dictionary<string, string>
            LabelFromVar = new Dictionary<string, string>
            {
                {nameof(scatterBody.properties.scatterDistribution._Range),  nameof(scatterBody.properties.scatterDistribution._Range)},
                {nameof(scatterBody.properties.scatterDistribution._PopulationMultiplier),  nameof(scatterBody.properties.scatterDistribution._PopulationMultiplier)},
                {nameof(scatterBody.properties.scatterDistribution._SizeNoiseStrength),  nameof(scatterBody.properties.scatterDistribution._SizeNoiseStrength)},
                {nameof(scatterBody.properties.scatterDistribution._SizeNoiseScale),  nameof(scatterBody.properties.scatterDistribution._SizeNoiseScale)},
                {nameof(scatterBody.properties.scatterDistribution._SizeNoiseOffset),  nameof(scatterBody.properties.scatterDistribution._SizeNoiseOffset)},
                {nameof(scatterBody.properties.scatterDistribution._MaxScale),  nameof(scatterBody.properties.scatterDistribution._MaxScale)},
                {nameof(scatterBody.properties.scatterDistribution._MinScale),  nameof(scatterBody.properties.scatterDistribution._MinScale)},
                {nameof(scatterBody.properties.scatterDistribution._CutoffScale),  nameof(scatterBody.properties.scatterDistribution._CutoffScale)},
                {nameof(scatterBody.properties.scatterDistribution.updateRate),  nameof(scatterBody.properties.scatterDistribution.updateRate)},

                {nameof(scatterBody.properties.scatterMaterial._MainColor),  nameof(scatterBody.properties.scatterMaterial._MainColor)},
                {nameof(scatterBody.properties.scatterMaterial._SubColor),  nameof(scatterBody.properties.scatterMaterial._SubColor)},
                {nameof(scatterBody.properties.scatterMaterial._ColorNoiseScale),  nameof(scatterBody.properties.scatterMaterial._ColorNoiseScale)},
                {nameof(scatterBody.properties.scatterMaterial._ColorNoiseStrength),  nameof(scatterBody.properties.scatterMaterial._ColorNoiseStrength)},


                {nameof(scatterBody.properties.scatterWind._WindSpeed),  nameof(scatterBody.properties.scatterWind._WindSpeed)},
                {nameof(scatterBody.properties.scatterWind._WaveSpeed),  nameof(scatterBody.properties.scatterWind._WaveSpeed)},
                {nameof(scatterBody.properties.scatterWind._WaveAmp),  nameof(scatterBody.properties.scatterWind._WaveAmp)},
                {nameof(scatterBody.properties.scatterWind._HeightCutoff),  nameof(scatterBody.properties.scatterWind._HeightCutoff)},
                {nameof(scatterBody.properties.scatterWind._HeightFactor),  nameof(scatterBody.properties.scatterWind._HeightFactor)},
                {nameof(scatterBody.properties.scatterDistribution._LODRange),  nameof(scatterBody.properties.scatterDistribution._LODRange)}
            };
        private static Scatter scatterBody => ParallaxConfiguratorMain.BodyScatter;

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

        private static readonly GUIStyle TexStyle = new GUIStyle(GUI.skin.textArea) { wordWrap = true };
        private static readonly GUIStyle ResetButtonStyle = new GUIStyle(GUI.skin.button) { fontSize = 9 };
        private static readonly GUIStyle FloatStyle = new GUIStyle(GUI.skin.textArea);
        private static readonly GUIStyle ColorStyle = new GUIStyle(GUI.skin.textArea);

        /// <summary>
        /// Float input field for in-game cfg editing. Behaves exactly like UnityEditor.EditorGUILayout.FloatField
        /// </summary>
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

        /// <summary>
        /// Special-purpose Text input field for in-game editing. Meant for dds or png textures' paths.
        /// Check if texture file exists before applying the value.
        /// </summary>
        public static string TexField(string value)
        {
            // Get rect and control for this float field for identification
            Rect pos = GUILayoutUtility.GetRect(new GUIContent(value), GUI.skin.label, GUILayout.ExpandWidth(false),
                GUILayout.MinWidth(200));
            int texFieldID = GUIUtility.GetControlID("TexField".GetHashCode(), FocusType.Keyboard, pos) + 1;
            if (texFieldID == 0)
                return value;

            // has the value been recorded?
            bool recorded = activeFieldID == texFieldID;
            // is the field being edited?
            bool active = texFieldID == GUIUtility.keyboardControl;

            if (active && recorded && activeTexFieldLastValue != value)
            {
                // Value has been modified externally
                activeTexFieldLastValue = value;
                activeTexFieldString = string.Copy(value);
            }

            // Get stored string for the text field if this one is recorded
            string str = recorded ? activeTexFieldString : string.Copy(value);

            // pass it in the text field
            string strValue = GUI.TextField(pos, str, TexStyle);

            // Update stored value if this one is recorded
            if (recorded)
                activeTexFieldString = strValue;

            // Try Parse if value got changed. If the string could not be parsed, ignore it and keep last value
            bool valid = true;
            if (strValue != value)
            {
                string path = KSPUtil.ApplicationRootPath + "GameData/" + strValue;

                if (!System.IO.File.Exists(path))
                    valid = false;

                if (!path.EndsWith(".dds", StringComparison.OrdinalIgnoreCase) &&
                    !path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    valid = false;

                if (valid)
                    value = activeTexFieldLastValue = strValue;
            }

            if (active && !recorded)
            {
                // Gained focus this frame
                activeFieldID = texFieldID;
                activeTexFieldString = strValue;
                activeTexFieldLastValue = value;
            }
            else if (!active && recorded)
            {
                // Lost focus this frame
                activeFieldID = -1;
                if (!valid)
                    value = activeTexFieldLastValue;
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
}