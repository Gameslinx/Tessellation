using Kopernicus.ConfigParser.Attributes;
using Kopernicus.ConfigParser.BuiltinTypeParsers;
using Kopernicus.ConfigParser.Enumerations;
using Kopernicus.Configuration.ModLoader;
using Kopernicus.Configuration.Parsing;
using LibNoise;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;

namespace Grass
{

    public class ScatterData    //ONCE per planet
    {
        public Dictionary<string, DistributionData> distributionData = new Dictionary<string, DistributionData>();    //Distribution data per scatter
        public Dictionary<PQ, List<string>> perQuadBiomeData = new Dictionary<PQ, List<string>>();
        public int dataLength;
    }
    public struct QuadDistributionData
    {
        public float[] data;   //Noise
        //public List<string> biomes;   //List of biomes within this quad. If a biome is not in this list, some scatters on this quad do not need processing
    }
    public struct DistributionData
    {
        public Dictionary<PQ, QuadDistributionData> data;    //Contains noise and biome data per quad for this scatter
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
        public float min = 0;
        public float max = 0;
        //public static Dictionary<string, float[]> distributionData = new Dictionary<string, float[]>();
        public static ScatterData scatterData = new ScatterData();
        Dictionary<string, Scatter> scatters;
        string[] keys;
        float initialTime;
        public string bodyName;

        public bool hasBlockMap = false;
        public MapSO blockMap;
        public enum NoiseType
        {
            Perlin,
            RidgedMultifractal,
            Billow
        }
        public override void OnSetup()
        {
            //if (alreadySetupSpaceCenter) { return; }
            bodyName = sphere.name;
            
            scatters = ScatterBodies.scatterBodies[sphere.name].scatters;
            scatterData.dataLength = scatters.Values.Count;
            keys = scatters.Keys.ToArray();
            for (int i = 0; i < scatters.Values.Count; i++)
            {
                string scatterName = keys[i];
                if (scatters[scatterName].shared) { continue; }
                if (scatters[scatterName].properties.scatterDistribution.noise.noiseMode != DistributionNoiseMode.NonPersistent) 
                {
                    DistributionData data = new DistributionData();
                    data.frequency = scatters[scatterName].properties.scatterDistribution.noise._Frequency;
                    data.lacunarity = scatters[scatterName].properties.scatterDistribution.noise._Lacunarity;
                    data.persistence = scatters[scatterName].properties.scatterDistribution.noise._Persistence;
                    data.octaves = (int)scatters[scatterName].properties.scatterDistribution.noise._Octaves;
                    data.seed = (int)scatters[scatterName].properties.scatterDistribution.noise._Seed;
                    data.data = new Dictionary<PQ, QuadDistributionData>();
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
                else    //We only want the biome blacklist
                {
                    DistributionData data = new DistributionData();
                    data.data = new Dictionary<PQ, QuadDistributionData>();
                    if (!scatterData.distributionData.ContainsKey(scatterName))
                    {
                        scatterData.distributionData.Add(scatterName, data);
                    }
                }
            }
            octaves = 6;
            
            this.requirements = PQS.ModiferRequirements.MeshColorChannel;

            
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
            scatterData.perQuadBiomeData.Add(quad, new List<string>()); //Biomes
            for (int i = 0; i < scatters.Values.Count; i++)
            {
                if (scatters[keys[i]].shared) { continue; }
                if (scatters[keys[i]].properties.scatterDistribution.noise.noiseMode != DistributionNoiseMode.NonPersistent)
                {
                    DistributionData data = scatterData.distributionData[keys[i]];
                    if (!data.data.ContainsKey(quad))
                    {
                        buildQuadName = quad.name;
                        QuadDistributionData quadData = new QuadDistributionData();
                        quadData.data = new float[225];
                        //quadData.biomes = new List<string>();
                        data.data.Add(quad, quadData);
                    }
                }
                else
                {
                    DistributionData data = scatterData.distributionData[keys[i]];
                    if (!data.data.ContainsKey(quad))
                    {
                        buildQuadName = quad.name;
                        QuadDistributionData quadData = new QuadDistributionData();
                        //quadData.biomes = new List<string>();                       //Just want the biomes that the quad is in, so that we can skip adding it on non-persistent scatters
                        data.data.Add(quad, quadData);
                    }
                }
            }
            //distributionData.Add(quad.name, new float[225]);
        }
        string thisBiome = "";
        string thisScatterName = "";
        public override void OnVertexBuildHeight(PQS.VertexBuildData data)
        {
            
            if (data.buildQuad == null)
            {
                return;
            }
            thisBiome = GetBiome(data.latitude, data.longitude, bodyName);
            if (!scatterData.perQuadBiomeData[data.buildQuad].Contains(thisBiome))
            {
                scatterData.perQuadBiomeData[data.buildQuad].Add(thisBiome);            //Add biome to quad data
            }

            float noiseMult = 1;
            if (!data.allowScatter) { noiseMult = 0; }
            if (hasBlockMap)
            {
                noiseMult = 1 - this.blockMap.GetPixelFloat(data.u, data.v);  //White on the map = blocked, and nothing will spawn there
            }
            

            for (int i = 0; i < keys.Length; i++)
            {
                thisScatterName = keys[i];
                Scatter scatter = scatters[thisScatterName];
                if (scatter.shared) { continue; }
                DistributionData distData = scatterData.distributionData[thisScatterName];

                if (scatter.properties.scatterDistribution.blacklist.fastBiomes.ContainsKey(thisBiome))    //Don't generate noise on blacklisted biome, just add to the biome list and skip
                {
                    continue; //Skip noise generation - In blacklisted biome
                }
                if (scatter.properties.scatterDistribution.noise.noiseMode != DistributionNoiseMode.NonPersistent && scatter.properties.scatterDistribution.noise.useNoiseProfile == null)
                {
                    double noise = distData.noiseMap.GetValue(data.directionFromCenter) * 0.5 + 0.5;
                    if (thisBiome != null && scatter.properties.scatterDistribution.blacklist.fastBiomes.ContainsKey(thisBiome))
                    {
                        noise = 0;
                    }
                    distData.data[data.buildQuad].data[data.vertIndex] = (float)noise * noiseMult;
                }
                //else if (scatter.properties.scatterDistribution.noise.noiseMode == DistributionNoiseMode.NonPersistent)
                //{
                //    //Biome already added
                //}
            }
        }
        private string GetBiome(double latitude, double longitude, string sphereName)
        {
            latitude = (ClampLat(((ClampRadians(latitude) / 0.01745329238474369))));
            longitude = (ClampLon((((ClampRadians(longitude) / 0.01745329238474369) - 90) * -1)));

            latitude = ResourceUtilities.Deg2Rad(ClampLat(latitude));   //Yooo Squad uses stupid coordinate systems? Am I surprised? Noooo! Fuck me, this shit is disgusting
            longitude = ResourceUtilities.Deg2Rad(ClampLon(longitude));

            string thisBiome = Kopernicus.Components.PQSMod_BiomeSampler.GetCachedBiome(latitude * 57.2957795131, longitude * 57.2957795131, FlightGlobals.GetBodyByName(sphereName));
            return thisBiome;
        }
        private static double ClampRadians(double angle)
        {
            while (angle > 6.283185307179586)
            {
                angle -= 6.283185307179586;
            }
            while (angle < 0.0)
            {
                angle += 6.283185307179586;
            }
            return angle;
        }
        private static double ClampLat(double lat)
        {
            return (lat + 180.0 + 90.0) % 180.0 - 90.0;
        }

        private static double ClampLon(double lon)
        {
            return (lon + 360.0 + 180.0) % 360.0 - 180.0;
        }
        public override void OnQuadDestroy(PQ quad)
        {
            if (scatterData.perQuadBiomeData.ContainsKey(quad))
            {
                scatterData.perQuadBiomeData.Remove(quad);
            }
            for (int i = 0; i < scatters.Values.Count; i++)
            {
                if (i > keys.Length - 1) { return; }    //Only required for when creating a new scatter
                string scatterName = keys[i];
                if (scatters[scatterName].shared) { continue; }
                if (scatters[scatterName].properties.scatterDistribution.noise.noiseMode != DistributionNoiseMode.NonPersistent)
                {
                    DistributionData distData = scatterData.distributionData[scatterName];
                    
                    if (distData.data.ContainsKey(quad))
                    {
                        distData.data.Remove(quad);
                    }
                }
            }
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
        [ParserTarget("blockMap", Optional = true)]
        public MapSOParserGreyScale<MapSO> BlockMap
        {
            get { return Mod.blockMap; }
            set { Mod.blockMap = value; Mod.hasBlockMap = true; }
        }
    }
}
