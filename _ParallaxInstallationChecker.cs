using System;
using System.IO;
using UnityEngine;
namespace ParallaxInstallChecker
{
    [KSPAddon(KSPAddon.Startup.Instantly, false)]
    public class Checker : MonoBehaviour
    {
        string path = "";
        bool meetsKopernicus = false;
        bool meetsStockTextures = false;
        bool meetsParallax = false;
        bool dependencyAdvancedSubdivision = false;
        bool dependencySubdivMod = false;
        bool dependencyCore = false;
        bool dependencyQualityLibrary = false;
        public void Start()
        {
            path = Path.Combine(KSPUtil.ApplicationRootPath + "GameData/");
            GetVersion();
            PreValidate();
        }
        public void GetVersion()
        {
            bool hasVersion = false;
            int maj = Versioning.version_major;
            int min = Versioning.version_minor;
            string versionString = maj.ToString() + "." + min.ToString();
            string[] supportedVersions = { "1.11", "1.10" };    //If you're decompiling to change the version, don't bother. There's no lock, this is simply advice
            foreach (string s in supportedVersions)
            {
                if (s == versionString)
                {
                    hasVersion = true;
                }
            }
            if (hasVersion == false)
            {
                VisualLog("WARNING: Parallax is not running on a supported version of KSP - Bug reports from this version will be invalid. You have been warned!");
                Debug.Log("[Parallax] DidntReadInstallationInstructionsException: Parallax is not supported on this version of KSP.");
            }
        }
        public void PreValidate()
        {
            Validate(path + "Kopernicus/Config/System.cfg", "Kopernicus", out meetsKopernicus);
            Validate(path + "Parallax/License.md", "Parallax Core", out meetsParallax);
            Validate(path + "Parallax_StockTextures/ParallaxTerrain.cfg", "Parallax (Stock Textures)", out meetsStockTextures);
            Validate(path + "Parallax/AdvancedSubdivision.dll", "Advanced Subdivision", out dependencyAdvancedSubdivision);
            Validate(path + "Parallax/PQSModExpansion.dll", "Subdivision PQSMod", out dependencySubdivMod);
            Validate(path + "Parallax/ParallaxQualityLibrary.dll", "Parallax Quality Library", out dependencyQualityLibrary);
            Log("Finished validating your Parallax install.");
           
            Finish();
        }
        public void Validate(string path, string reason, out bool meets)
        {
            meets = false;
            Log("Attempting to validate " + reason + " at: ");
            Log("\t" + path);
            if (File.Exists(path))
            {
                meets = true;
                Log("\tParallax has met dependency: " + reason);
            }
            else
            {
                meets = false;
                Log("\tException: Parallax has failed to meet dependency: " + reason);
                if (reason == "Parallax (Stock Textures)")
                {
                    Log("\tIf you are not playing with a planet mod, you need to install these.");
                }
            }
        }
        public void Finish()
        {
            if (meetsKopernicus == true)
            {
                VisualLog("Parallax has met dependency: Kopernicus");
            }
            else
            {
                VisualLog("<color=#f0871f>Parallax has not met dependency: Kopernicus</color>");
            }
            if (meetsParallax == true)
            {
                VisualLog("Parallax has met dependency: Parallax (Core)");
            }
            else
            {
                VisualLog("<color=#f0871f>Parallax has not met dependency: Parallax (CORE)</color>");
            }
            if (meetsStockTextures == true)
            {
                VisualLog("Parallax has met dependency: Parallax (Stock Textures)");
            }
            else
            {
                VisualLog("<color=#f0871f>Parallax has not met dependency: Parallax (Stock Textures)</color>");
                VisualLog("<color=#f0871f>If you are running Parallax with a planet mod, you can ignore this.</color>");
            }
            if (dependencyQualityLibrary == true)
            {
                VisualLog("Parallax has met dependency: Parallax Quality Library");
            }
            else
            {
                VisualLog("<color=#f0871f>Parallax has not met dependency: Parallax Quality Library</color>");
            }
            if (dependencySubdivMod == true)
            {
                VisualLog("Parallax has met dependency: Subdivision PQSMod");
            }
            else
            {
                VisualLog("<color=#f0871f>Parallax has not met dependency: Subdivision PQSMod</color>");
            }
            if (dependencyAdvancedSubdivision == true)
            {
                VisualLog("Parallax has met dependency: Advanced Subdivision");
            }
            else
            {
                VisualLog("<color=#f0871f>Parallax has not met dependency: Advanced Subdivision</color>");
            }
            
        }
        public void VisualLog(string message)
        {
            ScreenMessages.PostScreenMessage(message, 20f, ScreenMessageStyle.UPPER_LEFT);
        }
        public void Log(string message)
        {
            Debug.Log("[Parallax Checker] " + message);

        }
    }
}
