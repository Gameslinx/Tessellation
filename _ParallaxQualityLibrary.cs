using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[assembly: KSPAssembly("ParallaxQualityLibrary", 1, 0)]
namespace ParallaxQualityLibrary
{
    public class ParallaxPropertyHolder
    {
        //Holds all possible shader properties
        

    }
    public class Parallax
    {
        public Material parallaxMaterial { get; set; }
        public string shaderName;
        public string[] shaderVars;
        public string[] globalVars;
        public Dictionary<string, string> specificVars; //Dictionary<ValueToReplace, Value> - Replace _SurfaceTexture with _SurfaceTextureMid when filling out the parallax bodies
        public static Parallax DetermineVersion(bool scaled, bool part, int quality)
        {
            if (scaled == true)
            {
               

            }
            if (part == true)
            {
                
            }
            if (scaled == false && part == false)
            {
                //if (quality == 0)
                //{
                //    return new ParallaxLow();
                //}
                //if (quality == 1 || quality == 2)
                //{
                //    return new ParallaxMed();
                //}
                if (quality == 3)
                {
                    //return new ParallaxUltra();

                    //Determine Parallax version for the quad and return it here
                }
            }
            //ParallaxLog.Log("Unable to determine shader quality - Automatically setting shader quality to low");
            return new ParallaxFullUltra();
        }
    }
    public class ParallaxFull : Parallax
    {
        //Can contain ultra, med and low
    }
    public class ParallaxSingle : Parallax
    {

    }
    public class ParallaxSingleSteep : Parallax
    {

    }
    public class ParallaxDoubleLow : Parallax
    {

    }
    public class ParallaxDoubleHigh : Parallax
    {

    }
    public class ParallaxFullUltra : ParallaxFull
    {
        //Shader Variables
        public ParallaxFullUltra()
        {
            shaderName = "Custom/ParallaxFULLUltra";
            specificVars = new Dictionary<string, string>();
            shaderVars = new string[]
            {
                "_SurfaceTexture",
                "_SurfaceTextureMid",
                "_SurfaceTextureHigh",
                "_SurfaceTextureScale",
                "_SteepTex",
                "_BumpMap",
                "_BumpMapMid",
                "_BumpMapHigh",
                "_BumpMapSteep",
                "_DispTex",
                "_InfluenceMap",
                "_Metallic",
                "_MetallicTint",
                "_Gloss",
                "_NormalSpecularInfluence",
                "_SteepPower",
                "_SteepContrast",
                "_SteepMidpoint",
                "_displacement_scale",
                "_displacement_offset",
                "_Hapke",
                "_LowStart",
                "_LowEnd",
                "_HighStart",
                "_HighEnd",
                "_EmissionColor"
            };
            globalVars = new string[]
            {
                "_TessellationEdgeLength",
                "_TessellationRange",
                "_MaxTessellation"
            };
            //ParallaxLog.Log("Using Parallax (Ultra)");
        }
    }
    public class ParallaxSingleUltra : ParallaxSingle
    {
        public ParallaxSingleUltra(string altitude)
        {
            shaderName = "Custom/ParallaxSINGLEUltra";
            if (altitude == "low")
            {
                specificVars = new Dictionary<string, string>()
                {
                    { "_SurfaceTexture", "_SurfaceTexture" },    //Don't replace for low
                    { "_BumpMap", "_BumpMap" }
                };
            }
            if (altitude == "mid")
            {
                specificVars = new Dictionary<string, string>()
                {
                    { "_SurfaceTexture", "_SurfaceTextureMid" },
                    { "_BumpMap", "_BumpMapMid" }
                };
            }
            if (altitude == "high")
            {
                specificVars = new Dictionary<string, string>()
                {
                    { "_SurfaceTexture", "_SurfaceTextureHigh" },
                    { "_BumpMap", "_BumpMapHigh" }
                };
            }
            shaderVars = new string[]
            {
                "_SurfaceTexture",
                "_SurfaceTextureScale",
                "_BumpMap",
                "_DispTex",
                "_InfluenceMap",
                "_Metallic",
                "_MetallicTint",
                "_Gloss",
                "_NormalSpecularInfluence",
                "_displacement_scale",
                "_displacement_offset",
                "_Hapke",
                "_LowStart",
                "_LowEnd",
                "_HighStart",
                "_HighEnd",
                "_EmissionColor"
            };
            globalVars = new string[]
            {
                "_TessellationEdgeLength",
                "_TessellationRange",
                "_MaxTessellation"
            };
        }
    }
    public class ParallaxSingleSteepUltra : ParallaxSingleSteep
    {
        public ParallaxSingleSteepUltra(string altitude)
        {
            shaderName = "Custom/ParallaxSINGLESTEEPUltra";
            if (altitude == "low")
            {
                specificVars = new Dictionary<string, string>()
                {
                    { "_SurfaceTexture", "_SurfaceTexture" },    //Don't replace for low
                    { "_BumpMap", "_BumpMap" }
                };
            }
            if (altitude == "mid")
            {
                specificVars = new Dictionary<string, string>()
                {
                    { "_SurfaceTexture", "_SurfaceTextureMid" },
                    { "_BumpMap", "_BumpMapMid" }
                };
            }
            if (altitude == "high")
            {
                specificVars = new Dictionary<string, string>()
                {
                    { "_SurfaceTexture", "_SurfaceTextureHigh" },
                    { "_BumpMap", "_BumpMapHigh" }
                };
            }
            shaderVars = new string[]
            {
                "_SurfaceTexture",
                "_SteepTex",
                "_SurfaceTextureScale",
                "_BumpMap",
                "_BumpMapSteep",
                "_DispTex",
                "_InfluenceMap",
                "_Metallic",
                "_MetallicTint",
                "_Gloss",
                "_NormalSpecularInfluence",
                "_SteepPower",
                "_SteepContrast",
                "_SteepMidpoint",
                "_displacement_scale",
                "_displacement_offset",
                "_Hapke",
                "_LowStart",
                "_LowEnd",
                "_HighStart",
                "_HighEnd",
                "_EmissionColor"
            };
            globalVars = new string[]
            {
                "_TessellationEdgeLength",
                "_TessellationRange",
                "_MaxTessellation"
            };
        }
    }
    public class ParallaxDoubleLowUltra : ParallaxDoubleLow
    {
        public ParallaxDoubleLowUltra()
        {
            shaderName = "Custom/ParallaxDOUBLELOWUltra";
            specificVars = new Dictionary<string, string>()
            {
                //{ "_SurfaceTextureLower", "_SurfaceTexture" },
                //{ "_SurfaceTextureHigher", "_SurfaceTextureMid" },
                //{ "_BumpMapLower", "_BumpMap" },
                //{ "_BumpMapHigher", "_BumpMapMid" }
            };
            shaderVars = new string[]
            {
                "_SurfaceTexture",
                "_SurfaceTextureMid",
                "_SteepTex",
                "_SurfaceTextureScale",
                "_BumpMap",
                "_BumpMapMid",
                "_BumpMapSteep",
                "_DispTex",
                "_InfluenceMap",
                "_Metallic",
                "_MetallicTint",
                "_Gloss",
                "_NormalSpecularInfluence",
                "_SteepPower",
                "_SteepContrast",
                "_SteepMidpoint",
                "_displacement_scale",
                "_displacement_offset",
                "_Hapke",
                "_LowStart",
                "_LowEnd",
                "_HighStart",
                "_HighEnd",
                "_EmissionColor"
            };
            globalVars = new string[]
            {
                "_TessellationEdgeLength",
                "_TessellationRange",
                "_MaxTessellation"
            };
        }
    }
    public class ParallaxDoubleHighUltra : ParallaxDoubleHigh
    {
        public ParallaxDoubleHighUltra()
        {
            shaderName = "Custom/ParallaxDOUBLEHIGHUltra";
            specificVars = new Dictionary<string, string>()
            {
                //{ "_SurfaceTextureLower", "_SurfaceTextureMid" },
                //{ "_SurfaceTextureHigher", "_SurfaceTextureHigh" },
                //{ "_BumpMapLower", "_BumpMapMid" },
                //{ "_BumpMapHigher", "_BumpMapHigh" }
            };
            shaderVars = new string[]
            {
                "_SurfaceTextureMid",
                "_SurfaceTextureHigh",
                "_SteepTex",
                "_SurfaceTextureScale",
                "_BumpMapMid",
                "_BumpMapHigh",
                "_BumpMapSteep",
                "_DispTex",
                "_InfluenceMap",
                "_Metallic",
                "_MetallicTint",
                "_Gloss",
                "_NormalSpecularInfluence",
                "_SteepPower",
                "_SteepContrast",
                "_SteepMidpoint",
                "_displacement_scale",
                "_displacement_offset",
                "_Hapke",
                "_LowStart",
                "_LowEnd",
                "_HighStart",
                "_HighEnd",
                "_EmissionColor"
            };
            globalVars = new string[]
            {
                "_TessellationEdgeLength",
                "_TessellationRange",
                "_MaxTessellation"
            };
        }
    }

        //ParallaxMed
    public class ParallaxFullMed : ParallaxFull
    {
        //Shader Variables
        public ParallaxFullMed()
        {
            shaderName = "Custom/ParallaxFULLMed";
            specificVars = new Dictionary<string, string>();
            shaderVars = new string[]
            {
            "_SurfaceTexture",
            "_SurfaceTextureMid",
            "_SurfaceTextureHigh",
            "_SurfaceTextureScale",
            "_SteepTex",
            "_BumpMap",
            "_BumpMapMid",
            "_BumpMapHigh",
            "_BumpMapSteep",
            "_InfluenceMap",
            "_Metallic",
            "_MetallicTint",
            "_Gloss",
            "_NormalSpecularInfluence",
            "_SteepPower",
            "_SteepContrast",
            "_SteepMidpoint",
            "_Hapke",
            "_LowStart",
            "_LowEnd",
            "_HighStart",
            "_HighEnd",
            "_EmissionColor"
            };
            globalVars = new string[]
            {

            };
            //ParallaxLog.Log("Using Parallax (Med)");
        }
    }
    public class ParallaxSingleMed : ParallaxSingle
    {
        public ParallaxSingleMed(string altitude)
        {
            shaderName = "Custom/ParallaxSINGLEMed";
            if (altitude == "low")
            {
                specificVars = new Dictionary<string, string>()
            {
                { "_SurfaceTexture", "_SurfaceTexture" },    //Don't replace for low
                { "_BumpMap", "_BumpMap" }
            };
            }
            if (altitude == "mid")
            {
                specificVars = new Dictionary<string, string>()
            {
                { "_SurfaceTexture", "_SurfaceTextureMid" },
                { "_BumpMap", "_BumpMapMid" }
            };
            }
            if (altitude == "high")
            {
                specificVars = new Dictionary<string, string>()
            {
                { "_SurfaceTexture", "_SurfaceTextureHigh" },
                { "_BumpMap", "_BumpMapHigh" }
            };
            }
            shaderVars = new string[]
            {
            "_SurfaceTexture",
            "_SurfaceTextureScale",
            "_BumpMap",
            "_InfluenceMap",
            "_Metallic",
            "_MetallicTint",
            "_Gloss",
            "_NormalSpecularInfluence",
            "_Hapke",
            "_LowStart",
            "_LowEnd",
            "_HighStart",
            "_HighEnd",
            "_EmissionColor"
            };
            globalVars = new string[]
            {

            };
        }
    }
    public class ParallaxSingleSteepMed : ParallaxSingleSteep
    {
        public ParallaxSingleSteepMed(string altitude)
        {
            shaderName = "Custom/ParallaxSINGLESTEEPMed";
            if (altitude == "low")
            {
                specificVars = new Dictionary<string, string>()
            {
                { "_SurfaceTexture", "_SurfaceTexture" },    //Don't replace for low
                { "_BumpMap", "_BumpMap" }
            };
            }
            if (altitude == "mid")
            {
                specificVars = new Dictionary<string, string>()
            {
                { "_SurfaceTexture", "_SurfaceTextureMid" },
                { "_BumpMap", "_BumpMapMid" }
            };
            }
            if (altitude == "high")
            {
                specificVars = new Dictionary<string, string>()
            {
                { "_SurfaceTexture", "_SurfaceTextureHigh" },
                { "_BumpMap", "_BumpMapHigh" }
            };
            }
            shaderVars = new string[]
            {
            "_SurfaceTexture",
            "_SteepTex",
            "_SurfaceTextureScale",
            "_BumpMap",
            "_BumpMapSteep",
            "_InfluenceMap",
            "_Metallic",
            "_MetallicTint",
            "_Gloss",
            "_NormalSpecularInfluence",
            "_SteepPower",
            "_SteepContrast",
            "_SteepMidpoint",
            "_Hapke",
            "_LowStart",
            "_LowEnd",
            "_HighStart",
            "_HighEnd",
            "_EmissionColor"
            };
            globalVars = new string[]
            {

            };
        }
    }
    public class ParallaxDoubleLowMed : ParallaxDoubleLow
    {
        public ParallaxDoubleLowMed()
        {
            shaderName = "Custom/ParallaxDOUBLELOWMed";
            specificVars = new Dictionary<string, string>()
            {
                //{ "_SurfaceTextureLower", "_SurfaceTexture" },
                //{ "_SurfaceTextureHigher", "_SurfaceTextureMid" },
                //{ "_BumpMapLower", "_BumpMap" },
                //{ "_BumpMapHigher", "_BumpMapMid" }
            };
            shaderVars = new string[]
            {
            "_SurfaceTexture",
            "_SurfaceTextureMid",
            "_SteepTex",
            "_SurfaceTextureScale",
            "_BumpMap",
            "_BumpMapMid",
            "_BumpMapSteep",
            "_InfluenceMap",
            "_Metallic",
            "_MetallicTint",
            "_Gloss",
            "_NormalSpecularInfluence",
            "_SteepPower",
            "_SteepContrast",
            "_SteepMidpoint",
            "_Hapke",
            "_LowStart",
            "_LowEnd",
            "_HighStart",
            "_HighEnd",
            "_EmissionColor"
            };
            globalVars = new string[]
            {

            };
        }
    }
    public class ParallaxDoubleHighMed : ParallaxDoubleHigh
    {
        public ParallaxDoubleHighMed()
        {
            shaderName = "Custom/ParallaxDOUBLEHIGHMed";
            specificVars = new Dictionary<string, string>()
            {
                //{ "_SurfaceTextureLower", "_SurfaceTextureMid" },
                //{ "_SurfaceTextureHigher", "_SurfaceTextureHigh" },
                //{ "_BumpMapLower", "_BumpMapMid" },
                //{ "_BumpMapHigher", "_BumpMapHigh" }
            };
            shaderVars = new string[]
            {
            "_SurfaceTextureMid",
            "_SurfaceTextureHigh",
            "_SteepTex",
            "_SurfaceTextureScale",
            "_BumpMapMid",
            "_BumpMapHigh",
            "_BumpMapSteep",
            "_InfluenceMap",
            "_Metallic",
            "_MetallicTint",
            "_Gloss",
            "_NormalSpecularInfluence",
            "_SteepPower",
            "_SteepContrast",
            "_SteepMidpoint",
            "_Hapke",
            "_LowStart",
            "_LowEnd",
            "_HighStart",
            "_HighEnd",
            "_EmissionColor"
            };
            globalVars = new string[]
            {

            };
        }


    }



    //ParallaxLow




    public class ParallaxFullLow : ParallaxFull
    {
        //Shader Variables
        public ParallaxFullLow()
        {
            shaderName = "Custom/ParallaxFULLLow";
            specificVars = new Dictionary<string, string>();
            shaderVars = new string[]
            {
                "_SurfaceTexture",
                "_SurfaceTextureMid",
                "_SurfaceTextureHigh",
                "_SurfaceTextureScale",
                "_SteepTex",
                "_BumpMap",
                "_BumpMapMid",
                "_BumpMapHigh",
                "_BumpMapSteep",
                "_Metallic",
                "_MetallicTint",
                "_Gloss",
                "_NormalSpecularInfluence",
                "_SteepPower",
                "_SteepContrast",
                "_SteepMidpoint",
                "_Hapke",
                "_LowStart",
                "_LowEnd",
                "_HighStart",
                "_HighEnd",
                "_EmissionColor"
            };
            globalVars = new string[]
            {

            };
            //ParallaxLog.Log("Using Parallax (Low)");
        }
    }
    public class ParallaxSingleLow : ParallaxSingle
    {
        public ParallaxSingleLow(string altitude)
        {
            shaderName = "Custom/ParallaxSINGLELow";
            if (altitude == "low")
            {
                specificVars = new Dictionary<string, string>()
                {
                    { "_SurfaceTexture", "_SurfaceTexture" },    //Don't replace for low
                    { "_BumpMap", "_BumpMap" }
                };
            }
            if (altitude == "mid")
            {
                specificVars = new Dictionary<string, string>()
                {
                    { "_SurfaceTexture", "_SurfaceTextureMid" },
                    { "_BumpMap", "_BumpMapMid" }
                };
            }
            if (altitude == "high")
            {
                specificVars = new Dictionary<string, string>()
                {
                    { "_SurfaceTexture", "_SurfaceTextureHigh" },
                    { "_BumpMap", "_BumpMapHigh" }
                };
            }
            shaderVars = new string[]
            {
                "_SurfaceTexture",
                "_SurfaceTextureScale",
                "_BumpMap",
                "_Metallic",
                "_MetallicTint",
                "_Gloss",
                "_NormalSpecularInfluence",
                "_Hapke",
                "_LowStart",
                "_LowEnd",
                "_HighStart",
                "_HighEnd",
                "_EmissionColor"
            };
            globalVars = new string[]
            {

            };
        }
    }
    public class ParallaxSingleSteepLow : ParallaxSingleSteep
    {
        public ParallaxSingleSteepLow(string altitude)
        {
            shaderName = "Custom/ParallaxSINGLESTEEPLow";
            if (altitude == "low")
            {
                specificVars = new Dictionary<string, string>()
                {
                    { "_SurfaceTexture", "_SurfaceTexture" },    //Don't replace for low
                    { "_BumpMap", "_BumpMap" }
                };
            }
            if (altitude == "mid")
            {
                specificVars = new Dictionary<string, string>()
                {
                    { "_SurfaceTexture", "_SurfaceTextureMid" },
                    { "_BumpMap", "_BumpMapMid" }
                };
            }
            if (altitude == "high")
            {
                specificVars = new Dictionary<string, string>()
                {
                    { "_SurfaceTexture", "_SurfaceTextureHigh" },
                    { "_BumpMap", "_BumpMapHigh" }
                };
            }
            shaderVars = new string[]
            {
                "_SurfaceTexture",
                "_SteepTex",
                "_SurfaceTextureScale",
                "_BumpMap",
                "_BumpMapSteep",
                "_Metallic",
                "_MetallicTint",
                "_Gloss",
                "_NormalSpecularInfluence",
                "_SteepPower",
                "_SteepContrast",
                "_SteepMidpoint",
                "_Hapke",
                "_LowStart",
                "_LowEnd",
                "_HighStart",
                "_HighEnd",
                "_EmissionColor"
            };
            globalVars = new string[]
            {

            };
        }
    }
    public class ParallaxDoubleLowLow : ParallaxDoubleLow
    {
        public ParallaxDoubleLowLow()
        {
            shaderName = "Custom/ParallaxDOUBLELOWLow";
            specificVars = new Dictionary<string, string>()
            {
                //{ "_SurfaceTextureLower", "_SurfaceTexture" },
                //{ "_SurfaceTextureHigher", "_SurfaceTextureMid" },
                //{ "_BumpMapLower", "_BumpMap" },
                //{ "_BumpMapHigher", "_BumpMapMid" }
            };
            shaderVars = new string[]
            {
                "_SurfaceTexture",
                "_SurfaceTextureMid",
                "_SteepTex",
                "_SurfaceTextureScale",
                "_BumpMap",
                "_BumpMapMid",
                "_BumpMapSteep",
                "_Metallic",
                "_MetallicTint",
                "_Gloss",
                "_NormalSpecularInfluence",
                "_SteepPower",
                "_SteepContrast",
                "_SteepMidpoint",
                "_Hapke",
                "_LowStart",
                "_LowEnd",
                "_HighStart",
                "_HighEnd",
                "_EmissionColor"
            };
            globalVars = new string[]
            {

            };
        }
    }
    public class ParallaxDoubleHighLow : ParallaxDoubleHigh
    {
        public ParallaxDoubleHighLow()
        {
            shaderName = "Custom/ParallaxDOUBLEHIGHLow";
            specificVars = new Dictionary<string, string>()
            {
                //{ "_SurfaceTextureLower", "_SurfaceTextureMid" },
                //{ "_SurfaceTextureHigher", "_SurfaceTextureHigh" },
                //{ "_BumpMapLower", "_BumpMapMid" },
                //{ "_BumpMapHigher", "_BumpMapHigh" }
            };
            shaderVars = new string[]
            {
                "_SurfaceTextureMid",
                "_SurfaceTextureHigh",
                "_SteepTex",
                "_SurfaceTextureScale",
                "_BumpMapMid",
                "_BumpMapHigh",
                "_BumpMapSteep",
                "_Metallic",
                "_MetallicTint",
                "_Gloss",
                "_NormalSpecularInfluence",
                "_SteepPower",
                "_SteepContrast",
                "_SteepMidpoint",
                "_Hapke",
                "_LowStart",
                "_LowEnd",
                "_HighStart",
                "_HighEnd",
                "_EmissionColor"
            };
            globalVars = new string[]
            {

            };
        }
    }



    public class ParallaxBody
    {
        public string bodyName = "Unnamed";
        public Parallax full { get; set; }
        public Parallax singleLow { get; set; }
        public Parallax singleMid { get; set; }
        public Parallax singleHigh { get; set; }
        public Parallax singleSteepLow { get; set; }
        public Parallax singleSteepMid { get; set; }
        public Parallax singleSteepHigh { get; set; }
        public Parallax doubleLow { get; set; }
        public Parallax doubleHigh { get; set; }

        public string _SurfaceTexture { get; set; }
        public string _SurfaceTextureMid { get; set; }
        public string _SurfaceTextureHigh { get; set; }
        public string _SteepTex { get; set; }

        public string _BumpMap { get; set; }
        public string _BumpMapMid { get; set; }
        public string _BumpMapHigh { get; set; }
        public string _BumpMapSteep { get; set; }

        public string _InfluenceMap { get; set; }
        public string _DispTex { get; set; }

        public float _SurfaceTextureScale { get; set; }

        public float _LowStart { get; set; }
        public float _LowEnd { get; set; }
        public float _HighStart { get; set; }
        public float _HighEnd { get; set; }

        public float _displacement_scale { get; set; }
        public float _displacement_offset { get; set; }

        public float _Metallic { get; set; }
        public float _Gloss { get; set; }

        public Color _MetallicTint { get; set; }
        public float _NormalSpecularInfluence { get; set; }
        public float _SteepPower { get; set; }
        public float _SteepContrast { get; set; }
        public float _SteepMidpoint { get; set; }
        public float _Hapke { get; set; }
        public Color _EmissionColor { get; set; }

        public bool hasEmission = false;

        public ParallaxBody(string name, int qualityLevel)
        {
            bodyName = name;
            //Set the shader vars, then use these vars to set the materials
            if (qualityLevel == 3)
            {
                full = new ParallaxFullUltra();
                singleLow = new ParallaxSingleUltra("low");
                singleMid = new ParallaxSingleUltra("mid");
                singleHigh = new ParallaxSingleUltra("high");
                singleSteepLow = new ParallaxSingleSteepUltra("low");
                singleSteepMid = new ParallaxSingleSteepUltra("mid");
                singleSteepHigh = new ParallaxSingleSteepUltra("high");
                doubleLow = new ParallaxDoubleLowUltra();
                doubleHigh = new ParallaxDoubleHighUltra();
            }
            if (qualityLevel == 2 || qualityLevel == 1)
            {
                full = new ParallaxFullMed();
                singleLow = new ParallaxSingleMed("low");
                singleMid = new ParallaxSingleMed("mid");
                singleHigh = new ParallaxSingleMed("high");
                singleSteepLow = new ParallaxSingleSteepMed("low");
                singleSteepMid = new ParallaxSingleSteepMed("mid");
                singleSteepHigh = new ParallaxSingleSteepMed("high");
                doubleLow = new ParallaxDoubleLowMed();
                doubleHigh = new ParallaxDoubleHighMed();
            }
            if (qualityLevel == 0)
            {
                full = new ParallaxFullLow();
                singleLow = new ParallaxSingleLow("low");
                singleMid = new ParallaxSingleLow("mid");
                singleHigh = new ParallaxSingleLow("high");
                singleSteepLow = new ParallaxSingleSteepLow("low");
                singleSteepMid = new ParallaxSingleSteepLow("mid");
                singleSteepHigh = new ParallaxSingleSteepLow("high");
                doubleLow = new ParallaxDoubleLowLow();
                doubleHigh = new ParallaxDoubleHighLow();
            }
            ParallaxLog.Log("Created body: " + bodyName);
        }
    }
    public static class ParallaxGlobal
    {
        public static int _TessellationMax = 64;
        public static int _TessellationEdgeLength = 32;
        public static int _TessellationRange = 25;

        public static bool _TessellateLighting = true;

        public static bool _UseReflections = false;
        public static int _ReflectionRefreshRate = 60;
        public static string _ReflectionTimeSlicing = "Instantly";
        public static int _ReflectionResolution = 256;
    }
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class LoadParallaxGlobal : MonoBehaviour
    {
        public void Start()
        {
            UrlDir.UrlConfig[] globalNodes = GameDatabase.Instance.GetConfigs("ParallaxGlobalConfig");
            if (globalNodes.Length > 1)
            {
                Debug.Log("Multiple Parallax global configs detected!");
            }
            UrlDir.UrlConfig globalNode = globalNodes[0];
            ConfigNode node = globalNode.config.GetNode("TessellationSettings");
            GetTessellationSettings(node);
            node = globalNode.config.GetNode("ReflectionSettings");
            GetReflectionSettings(node);
            node = globalNode.config.GetNode("LightingSettings");
            GetLightingSettings(node);
            ParallaxLog.Log("Global Settings:");
            ParallaxLog.SubLog("Max Tessellation: " + ParallaxGlobal._TessellationMax);
            ParallaxLog.SubLog("Tessellation Edge Length: " + ParallaxGlobal._TessellationEdgeLength);
            ParallaxLog.SubLog("Tessellation Range: " + ParallaxGlobal._TessellationRange);

            ParallaxLog.SubLog("Reflections: " + ParallaxGlobal._UseReflections);
            ParallaxLog.SubLog("Reflection Refresh Rate: " + ParallaxGlobal._ReflectionRefreshRate);
            ParallaxLog.SubLog("Reflection Time Slicing: " + ParallaxGlobal._ReflectionTimeSlicing);
            ParallaxLog.SubLog("Reflection Resolution: " + ParallaxGlobal._ReflectionResolution);

            ParallaxLog.SubLog("Tessellate Lighting: " + ParallaxGlobal._TessellateLighting);
        }
        public void GetTessellationSettings(ConfigNode node)
        {
            try
            {
                ParallaxGlobal._TessellationMax = int.Parse(node.GetValue("maxTessellation"));
                ParallaxGlobal._TessellationRange = int.Parse(node.GetValue("range"));
                ParallaxGlobal._TessellationEdgeLength = int.Parse(node.GetValue("edgeLength"));
            }
            catch { ParallaxLog.Log("[Exception] Error in Parallax global config: TessellationSettings"); }
        }
        public void GetReflectionSettings(ConfigNode node)
        {
            try
            {
                ParallaxGlobal._UseReflections = bool.Parse(node.GetValue("reflections"));
                ParallaxGlobal._ReflectionRefreshRate = int.Parse(node.GetValue("refreshRate"));
                ParallaxGlobal._ReflectionTimeSlicing = node.GetValue("timeSlicing");
                ParallaxGlobal._ReflectionResolution = int.Parse(node.GetValue("resolution"));
            }
            catch { ParallaxLog.Log("[Exception] Error in Parallax global config: ReflectionSettings"); }
        }
        public void GetLightingSettings(ConfigNode node)
        {
            try
            {
                ParallaxGlobal._TessellateLighting = bool.Parse(node.GetValue("tessellateLighting"));
            }
            catch { ParallaxLog.Log("[Exception] Error in Parallax global config: LightingSettings"); }
        }
    }
    public static class ParallaxLog
    {
        public static void Log(string msg)
        {
            Debug.Log("[Parallax] " + msg);
        }
        public static void SubLog(string msg)
        {
            Debug.Log("[Parallax] - " + msg);
        }
    }
    
}
