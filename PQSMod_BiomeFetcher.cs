using Kopernicus.ConfigParser.Attributes;
using Kopernicus.ConfigParser.BuiltinTypeParsers;
using Kopernicus.ConfigParser.Enumerations;
using Kopernicus.Configuration.ModLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grass
{
    public class ScatterBiomeData   //Stores the biome per-vertex for each quad
    {
        public static Dictionary<string, string[]> quadBiomeData = new Dictionary<string, string[]>();    //Fetching the biome data is pretty expensive. Only do it once, then sample it later in the distribution mod
    }
    public class PQSMod_BiomeFetcher : PQSMod
    {
        CelestialBody body;
        public override void OnSetup()
        {
            body = FlightGlobals.GetBodyByName(sphere.name);
        }
        public override void OnQuadPreBuild(PQ quad)
        {
            ScatterBiomeData.quadBiomeData.Add(quad.name, new string[225]);
        }
        public override void OnVertexBuild(PQS.VertexBuildData data)
        {
            ScatterBiomeData.quadBiomeData[data.buildQuad.name][data.vertIndex] = ResourceUtilities.GetBiome(data.latitude, data.longitude, body).name;
        }
        public override void OnQuadDestroy(PQ quad)
        {
            if (ScatterBiomeData.quadBiomeData.ContainsKey(quad.name))
            {
                ScatterBiomeData.quadBiomeData.Remove(quad.name);
            }
        }
    }
    [RequireConfigType(ConfigType.Node)]
    public class BiomeFetcher : ModLoader<PQSMod_BiomeFetcher>
    {
        [ParserTarget("order", Optional = true)]
        public NumericParser<int> order
        {
            get { return Mod.order; }
            set { Mod.order = int.MaxValue - 4; }   //This has to execute before everything else
        }
    }
}
