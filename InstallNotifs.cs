using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Grass
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class InstallNotifs : MonoBehaviour
    {
        public static void PostSettings()
        {
            ScreenMessages.PostScreenMessage(
                "Parallax Install Configuration\n" +
                "Terrain quality: " + GetQuality(GameSettings.TERRAIN_SHADER_QUALITY) + "\n" + 
                "Scatters enabled: " + ScatterGlobalSettings.enableScatters + "\n" +
                "Colliders enabled: " + ScatterGlobalSettings.enableCollisions,
                20f
                );
        }
        public static string GetQuality(int level)
        {
            if (level == 0)
            {
                return "Low (Tessellation Disabled)";
            }
            if (level == 1)
            {
                return "Med (Tessellation Disabled)";
            }
            if (level == 2)
            {
                return "High (Tessellation Disabled)";
            }
            return "Ultra (Tessellation Enabled)";
        }
        public void Start()
        {
            ScatterLog.Log("Checking install...");
            string filePath = Path.Combine(KSPUtil.ApplicationRootPath + "GameData/");
            Exists(filePath + "Parallax/Grass.dll", "Grass.dll is missing. \nIt should be located in GameData/Parallax", "Parallax (Core) is installed incorrectly");
            Exists(filePath + "Parallax/ParallaxOptimized.dll", "ParallaxOptimized.dll is missing. \nIt should be located in GameData/Parallax", "Parallax (Core) is missing or installed incorrectly");
            Exists(filePath + "Parallax/ParallaxQualityLibrary.dll", "ParallaxQualityLibrary.dll is missing. \nIt should be located in GameData/Parallax", "Parallax (Core) is missing or installed incorrectly");

            Exists(filePath + "Parallax_StockTextures/_Scatters/ShaderBank.cfg", "ShaderBank.cfg is missing. \nIt should be located in GameData/Parallax_StockTextures/_Scatters", "Parallax_ScatterTextures is missing or installed incorrectly");
            Exists(filePath + "Parallax_StockTextures/ParallaxTerrain.cfg", "ParallaxTerrain.cfg is missing. \nIt should be located in GameData/Parallax_StockTextures", "Parallax_StockTextures is missing or installed incorrectly");

            CheckCondition(SystemInfo.supportsComputeShaders, "Compute Shaders");

            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Direct3D11)
            {
                CheckCondition(SystemInfo.supportsAsyncGPUReadback, "Supports Async GPU Readback");
            }
            CheckSettingCondition(GameSettings.TERRAIN_SHADER_QUALITY > 1, "Terrain Shader Quality", "High/Ultra", (GameSettings.TERRAIN_SHADER_QUALITY == 0 ? "Low" : "Medium"));
        }
        public void Exists(string path, string messageIfFailed, string potentialCause)
        {
            if (!File.Exists(path))
            {
                PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "Parallax Error", "Parallax Error", "Unable to locate a critical part of the Parallax installation: \n" + messageIfFailed + "\n" + "Potential cause: " + potentialCause + "\n\n\nRefer to the troubleshooting/installation guide on the Parallax forum page for help.", "Ignore", true, HighLogic.UISkin);
            }
        }
        public void CheckCondition(bool supported, string condition)
        {
            if (!supported)
            {
                PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "Parallax Error", "Parallax Error", "Your system does not support " + condition + "\nPlease refer to the system requirements on the forum page", "Ignore", true, HighLogic.UISkin);
            }
        }
        public void CheckSettingCondition(bool met, string condition, string recommended, string current)
        {
            if (!met)
            {
                PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "Parallax Warning", "Parallax Warning", "This setting is not set correctly: " + condition + "\nIt is set to '" + current + "', but should be set to '" + recommended + "'", "Ignore", true, HighLogic.UISkin);
            }
        }
    }
}
