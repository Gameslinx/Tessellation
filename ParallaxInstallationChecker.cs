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
        public void Start()
        {
            path = Path.Combine(KSPUtil.ApplicationRootPath + "GameData/");

            //if (Application.platform == RuntimePlatform.OSXPlayer)
            //{
            //    path = Application.dataPath.Remove(Application.dataPath.Length - 16, 16) + "GameData/";
            //}
            //else
            //{
            //    path = Application.dataPath.Remove(Application.dataPath.Length - 12, 12) + "GameData/";
            //}
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
            Validate(path + "Parallax/License.md", "Parallax (Core)", out meetsParallax);
            Validate(path + "Parallax_StockTextures/ParallaxTerrain.cfg", "Parallax (Stock Textures)", out meetsStockTextures);
            Log("Finished validating your Parallax install.");
           
            Finish();
            VisualLog("Please note that Parallax Collisions are experimental! Should you wish to turn these off, you can do so in the ParallaxGlobal config file.");
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
            
        }
        public void VisualLog(string message)
        {
            ScreenMessages.PostScreenMessage(message, 20f);
        }
        public void Log(string message)
        {
            Debug.Log("[Parallax Checker] " + message);

        }
    }
}
