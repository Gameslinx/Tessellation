using Kopernicus.ConfigParser.Attributes;
using Kopernicus.ConfigParser.BuiltinTypeParsers;
using Kopernicus.ConfigParser.Enumerations;
using Kopernicus.Configuration.ModLoader;
using ParallaxQualityLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ParallaxOptimized
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class QuadRangeCheck : MonoBehaviour
    {
        public delegate void RangeCheck();
        public static event RangeCheck OnQuadRangeCheck;
        void Awake()
        {
            DontDestroyOnLoad(this);
        }
        public void FixedUpdate()   //Could be made into a coroutine or queue because we don't really need to check this every physics frame
        {
            if (OnQuadRangeCheck != null)
            {
                OnQuadRangeCheck();
            }
        }
    }

    public static class SubdivisionQuadData
    {
        public static Dictionary<PQ, SubdivisionData> quadData = new Dictionary<PQ, SubdivisionData>();
    }
    public class PQSMod_Subdivide : PQSMod
    {
        public int subdivisionLevel = 1;
        public float subdivisionRadius = 100;
        public override void OnQuadBuilt(PQ quad)
        {
            SubdivisionQuadData.quadData.Add(quad, new SubdivisionData(quad, subdivisionLevel, subdivisionRadius, (quad.subdivision == quad.sphereRoot.maxLevel) && (GameSettings.TERRAIN_SHADER_QUALITY == 3)));
        }
        public override void OnQuadDestroy(PQ quad)
        {
            if (SubdivisionQuadData.quadData.ContainsKey(quad))
            {
                SubdivisionQuadData.quadData[quad].Cleanup();
                SubdivisionQuadData.quadData.Remove(quad);
            }
        }
    }
    [RequireConfigType(ConfigType.Node)]
    public class Subdivide : ModLoader<PQSMod_Subdivide>
    {
        [ParserTarget("subdivisionLevel", Optional = false)]
        public NumericParser<int> subdivisionLevel
        {
            get { return Mod.subdivisionLevel; }
            set { Mod.subdivisionLevel = value; }
        }
        [ParserTarget("subdivisionRadius", Optional = false)]
        public NumericParser<float> subdivisionRadius
        {
            get { return Mod.subdivisionRadius; }
            set { Mod.subdivisionRadius = value; }
        }
        [ParserTarget("order", Optional = false)]
        public NumericParser<int> order
        {
            get { return Mod.order; }
            set { Mod.order = int.MaxValue - 2; }
        }
    }
}
