using Kopernicus.ConfigParser.Attributes;
using Kopernicus.ConfigParser.BuiltinTypeParsers;
using Kopernicus.ConfigParser.Enumerations;
using Kopernicus.Configuration.ModLoader;
using LibNoise;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Grass
{

    public class ScatterData    //ONCE per planet
    {
        public Dictionary<string, DistributionData> distributionData = new Dictionary<string, DistributionData>();    //Scatter name, distribution for that scatter
        public int dataLength;
    }
    public struct DistributionData
    {
        public Dictionary<string, float[]> data;    //Contains noise data per quad for this scatter
        public float frequency;
        public float lacunarity;
        public float persistence;
        public int octaves;
        public int seed;
        public PQSMod_VertexHeightNoise.NoiseType noiseType;
        public NoiseQuality noiseQuality;
        public IModule noiseMap;
    }
    public class PQSMod_ScatterDistribute : PQSMod
    {
        public PQSMod_VertexHeightNoise.NoiseType noiseType;
        public int seed = 1111;
        public int octaves = 4;
        public NoiseQuality mode = NoiseQuality.Standard;
        private IModule noiseMap;
        public float min = 0;
        public float max = 0;
        //public static Dictionary<string, float[]> distributionData = new Dictionary<string, float[]>();
        public static ScatterData scatterData = new ScatterData();
        Dictionary<string, Scatter> scatters;
        string[] keys;
        float initialTime;
        public static bool alreadySetupSpaceCenter = false;
        public enum NoiseType
        {
            Perlin,
            RidgedMultifractal,
            Billow
        }
        public override void OnSetup()
        {
            if (alreadySetupSpaceCenter) { return; }
            scatters = ScatterBodies.scatterBodies[sphere.name].scatters;
            scatterData.dataLength = scatters.Values.Count;
            keys = scatters.Keys.ToArray();
            for (int i = 0; i < scatters.Values.Count; i++)
            {
                string scatterName = keys[i];
                if (scatters[scatterName].properties.scatterDistribution.noise.noiseMode != DistributionNoiseMode.NonPersistent) 
                {
                    DistributionData data = new DistributionData();
                    data.frequency = scatters[scatterName].properties.scatterDistribution.noise._Frequency;
                    data.lacunarity = scatters[scatterName].properties.scatterDistribution.noise._Lacunarity;
                    data.persistence = scatters[scatterName].properties.scatterDistribution.noise._Persistence;
                    data.octaves = (int)scatters[scatterName].properties.scatterDistribution.noise._Octaves;
                    data.seed = (int)scatters[scatterName].properties.scatterDistribution.noise._Seed;
                    data.data = new Dictionary<string, float[]>();
                    data.noiseType = (PQSMod_VertexHeightNoise.NoiseType)scatters[scatterName].properties.scatterDistribution.noise._NoiseType;
                    data.noiseQuality = scatters[scatterName].properties.scatterDistribution.noise._NoiseQuality;
                    data.noiseMap = GetNoiseType(data.noiseType, data);
                    if (!scatterData.distributionData.ContainsKey(scatterName))
                    {
                        scatterData.distributionData.Add(scatterName, data);
                    }
                    else
                    {
                        scatterData.distributionData[scatterName] = data;
                    }
                }

            }
            octaves = 6;
            
            this.requirements = PQS.ModiferRequirements.MeshColorChannel;

            if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
            {
                alreadySetupSpaceCenter = true;
            }
            
        }
        public IModule GetNoiseType(PQSMod_VertexHeightNoise.NoiseType type, DistributionData data)
        {
            IModule map;
            switch (type)
            {
                default:
                    map = new Perlin((double)data.frequency, (double)data.lacunarity, (double)data.persistence, data.octaves, data.seed, data.noiseQuality);
                    return map;
                case PQSMod_VertexHeightNoise.NoiseType.RidgedMultifractal:
                    map = new RidgedMultifractal((double)data.frequency, (double)data.lacunarity, data.octaves, data.seed, data.noiseQuality);
                    return map;
                case PQSMod_VertexHeightNoise.NoiseType.Billow:
                    map = new Billow((double)data.frequency, (double)data.lacunarity, (double)data.persistence, data.octaves, data.seed, data.noiseQuality);
                    return map;
            }
        }
        string buildQuadName;
        public override void OnQuadPreBuild(PQ quad)
        {
            initialTime = Time.realtimeSinceStartup;

            for (int i = 0; i < scatters.Values.Count; i++)
            {
                string scatterName = keys[i];
                if (scatters[scatterName].properties.scatterDistribution.noise.noiseMode != DistributionNoiseMode.NonPersistent)
                {
                    DistributionData data = scatterData.distributionData[scatterName];
                    if (!data.data.ContainsKey(quad.name))
                    {
                        buildQuadName = quad.name;
                        data.data.Add(quad.name, new float[225]);
                    }
                }
            }
            //distributionData.Add(quad.name, new float[225]);
            
        }
        public override void OnVertexBuildHeight(PQS.VertexBuildData data)
        {
            if (data.buildQuad == null)
            {
                return;
            }
            
            for (int i = 0; i < scatters.Values.Count; i++)
            {
                
                string scatterName = keys[i];
                Scatter scatter = scatters[scatterName];
                if (scatter.properties.scatterDistribution.noise.noiseMode != DistributionNoiseMode.NonPersistent && scatter.properties.scatterDistribution.noise.useNoiseProfile == null)
                {
                    DistributionData distData = scatterData.distributionData[scatterName];
                    double noise = distData.noiseMap.GetValue(data.directionFromCenter) * 0.5 + 0.5;

                    distData.data[data.buildQuad.name][data.vertIndex] = (float)noise;
                    
                    
                }
            }
        }
        public override void OnQuadDestroy(PQ quad)
        {
            for (int i = 0; i < scatters.Values.Count; i++)
            {
                string scatterName = keys[i];
                if (scatters[scatterName].properties.scatterDistribution.noise.noiseMode != DistributionNoiseMode.NonPersistent)
                {
                    DistributionData distData = scatterData.distributionData[scatterName];
                    if (distData.data.ContainsKey(quad.name))
                    {
                        distData.data.Remove(quad.name);
                    }
                }
            }
        }
        public override void OnQuadBuilt(PQ quad)
        {

        }
    
    }
    [RequireConfigType(ConfigType.Node)]
    public class ScatterDistribute : ModLoader<PQSMod_ScatterDistribute>
    {
        [ParserTarget("order", Optional = true)]
        public NumericParser<int> order
        {
            get { return Mod.order; }
            set { Mod.order = int.MaxValue - 1; }
        }
    }
}
