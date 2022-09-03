using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Grass
{
    public static class LoadOnDemand
    {
        public static Dictionary<string, Texture> activeTextures = new Dictionary<string, Texture>(); //Store textures by filepath here
        public delegate void BodyChange(string from, string to);

        public static bool loadFinished = false;   //Hold off anything until this is true

        public static void OnBodyChange(string to)
        {
            //Now load all textures for this body
            Debug.Log("Body change! " + to);
            Unload();
            if (ScatterBodies.scatterBodies.ContainsKey(to))
            {
                Load(to);
            }
        }
        public static void Unload()
        {
            string[] keys = activeTextures.Keys.ToArray();
            for (int i = 0; i < keys.Length; i++)
            {
                ScatterLog.Log("[OnDemand] Unloading " + keys[i]);
                UnityEngine.Object.Destroy(activeTextures[keys[i]]);
                activeTextures.Remove(keys[i]);
            }
            activeTextures.Clear();
        }
        public static void Load(string name)
        {
            ScatterBody scatterBody = ScatterBodies.scatterBodies[name];
            string[] keys = scatterBody.scatters.Keys.ToArray();
            for (int i = 0; i < keys.Length; i++)
            {
                Debug.Log("Key: " + i + ", " + keys[i]);
                Scatter scatter = scatterBody.scatters[keys[i]];
                string[] texKeys = scatter.properties.scatterMaterial.Textures.Keys.ToArray();
                for (int b = 0; b < texKeys.Length; b++)
                {
                    if (!activeTextures.ContainsKey(scatter.scatterName + "-" + texKeys[b]))
                    {
                        ScatterLog.Log("[OnDemand] Loading texture: " + scatter.properties.scatterMaterial.Textures[texKeys[b]]);
                        activeTextures.Add(scatter.scatterName + "-" + texKeys[b], LoadTexture(scatter.properties.scatterMaterial.Textures[texKeys[b]]));
                    }
                    
                }
                for (int b = 0; b < scatter.properties.scatterDistribution.lods.lods.Length; b++)
                {
                    string texname = scatter.properties.scatterDistribution.lods.lods[b].mainTexName;
                    string normName = scatter.properties.scatterDistribution.lods.lods[b].normalName;   //I really need to rewrite this
                    if (texname != "parent")
                    {
                        if (!activeTextures.ContainsKey(scatter.scatterName + "-" + texname))
                        {
                            activeTextures.Add(scatter.scatterName + "-" + texname, LoadTexture(texname));
                        }
                            
                    }
                    if (normName != "parent")
                    {
                        if (!activeTextures.ContainsKey(scatter.scatterName + "-" + normName))
                        {
                            activeTextures.Add(scatter.scatterName + "-" + normName, LoadTexture(normName));
                        }

                    }

                }
            }
            loadFinished = true;
        }
        public static Texture2D LoadTexture(string name)
        {
            //return new Texture2D(1, 1);

            ScatterLog.Log("Loading Parallax Scatter Texture: " + name);
            string filePath = KSPUtil.ApplicationRootPath + "GameData/" + name;
            if (!File.Exists(filePath))
            {
                ScatterLog.Log("[Exception] Unable to find this file: " + name);
                return Texture2D.whiteTexture;
            }

            if (name.EndsWith(".dds"))
            {
                return LoadDDSTexture(filePath);
            }
            else
            {
                return LoadPNGTexture(filePath);
            }
        }
        public static Texture2D LoadPNGTexture(string url)
        {
            Texture2D tex;
            tex = new Texture2D(2, 2);
            tex.LoadRawTextureData(File.ReadAllBytes(url));
            tex.Apply(true, true);
            return tex;
        }
        public static Texture2D LoadDDSTexture(string url)
        {
            byte[] data = File.ReadAllBytes(url);
            byte ddsSizeCheck = data[4];
            if (ddsSizeCheck != 124)
            {
                Debug.Log("This DDS texture is invalid - Unable to read the size check value from the header.");
            }


            int height = data[13] * 256 + data[12];
            int width = data[17] * 256 + data[16];


            int DDS_HEADER_SIZE = 128;
            byte[] dxtBytes = new byte[data.Length - DDS_HEADER_SIZE];
            Buffer.BlockCopy(data, DDS_HEADER_SIZE, dxtBytes, 0, data.Length - DDS_HEADER_SIZE);
            int mipMapCount = (data[28]) | (data[29] << 8) | (data[30] << 16) | (data[31] << 24);

            TextureFormat format = TextureFormat.DXT1;
            if (data[84] == 'D')
            {

                if (data[87] == 49) //Also char '1'
                {
                    format = TextureFormat.DXT1;
                }
                else if (data[87] == 53)    //Also char '5'
                {
                    format = TextureFormat.DXT5;
                }
                else
                {
                    Debug.Log("Texture is not a DXT 1 or DXT5");
                }
            }
            Texture2D texture;
            if (mipMapCount == 1)
            {
                texture = new Texture2D(width, height, format, false);
            }
            else
            {
                texture = new Texture2D(width, height, format, true);
            }
         
            try
            {
                texture.LoadRawTextureData(dxtBytes);
            }
            catch
            {
                Debug.Log("CRITICAL ERROR: Parallax has halted the OnDemand loading process because texture.LoadRawTextureData(dxtBytes) would have resulted in overread");
                Debug.Log("Please check the format for this texture and refer to the wiki if you're unsure:");
            }
            texture.Apply(true, true);  //Recalculate mips, mark as no longer readable (to save memory)

            return (texture);
        }
    }
}
