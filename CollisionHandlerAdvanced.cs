using ParallaxGrass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;

namespace Grass
{
    //Store all objects from all scatters on a quad in a list of positions
    //Query craft position, craft bound and obtain a position
    //If craft bound + mesh bound is in range, enable the gameobject

    //Create component on GameObject which checks against a bool "enabled"
    //OnEnable, add the GameObject to an event
    //Run the octree query and set "enabled" (and enable) all GameObjects that are in range
    //After the query has run on the octree, run event which checks all active GameObjects to see if it still has "enabled" set to true
    //If it's false, disable itself

    //REMEMBER: THE QUAD MOVES and so do all the objects on it. Store the quad's original position, use its new position to calculate the vector which
    //will transform the craft's current position to where it would be without shader offset changes.
    //Basically query tree with VesselPosition + (oldQuadPosition - newQuadPosition) 

    public static class QuadColliderData
    {
        public static Dictionary<PQ, List<Position>> data = new Dictionary<PQ, List<Position>>();   //So that the Octree can retrieve position data

        public delegate void QuadDataCreated(PQ quad, List<Position> data);
        public static event QuadDataCreated onQuadColliderDataCreated;

        public delegate void QuadDataDestroyed(PQ quad);
        public static event QuadDataDestroyed onQuadColliderDataDestroyed;

        public static void DataCreated(PQ quad, List<Position> data)
        {
            if (onQuadColliderDataCreated != null) { onQuadColliderDataCreated(quad, data); }
        }
        public static void DataRemoved(PQ quad)
        {
            if (onQuadColliderDataDestroyed != null) { onQuadColliderDataDestroyed(quad); }
        }
    }
    public class AutoDisabler : MonoBehaviour
    {
        public int lifeTime = 3;
        public bool alwaysOn = false;
        public bool active = false;
        void OnEnable()
        {
            lifeTime = 3;
            active = true;
        }
        void FixedUpdate()
        {
            if (alwaysOn) { return; }
            lifeTime--;
            if (lifeTime == 0)
            {
                active = false;
                ObjectPool.ReturnObject(this.gameObject);
                gameObject.SetActive(false);
            }
        }
    }
    public class Position
    {
        public float bound;                 //Max mesh size in one dimension
        public GameObject collider;         //The actual collider gameObject, but this is null when the craft is not in range
        public AutoDisabler autoDisabler;   //The component which turns off the collider if it has not been in range recently
        public Scatter scatter;             //The scatter this position belongs to

        public Vector3 worldPos;            //Only assigned on GO creation - In order to use this position use (worldPos + (quadOriginalPosition - quad.gameObject.transform.position))
        public Quaternion rot;              //GameObject rotation
        public Vector3 scale;               //GameObject scale
        public Mesh collisionMesh;          

        public Vector3 quadOriginalPosition;
        public PQ quad;
        bool active = false;

        public Position(ref Vector3 worldPos, Quaternion rot, Vector3 scale, float bound, Scatter scatter, Mesh collisionMesh, ref Vector3 quadOriginalPosition, PQ quad)
        {
            this.worldPos = worldPos;
            this.rot = rot;
            this.scale = scale;
            this.bound = bound;
            this.scatter = scatter;
            this.collisionMesh = collisionMesh;
            this.quadOriginalPosition = quadOriginalPosition;
            this.quad = quad;
        }
        public void CreateGameObject()  //Called by the octree
        {
            
            if (autoDisabler != null)
            {
                if (autoDisabler.active)
                {
                    autoDisabler.lifeTime = 3;
                    autoDisabler.alwaysOn = scatter.alwaysCollideable;
                    return;
                }
                //Otherwise it's inactive, which means the GO is in the pool so we need to fetch a new one
            }
            collider = ObjectPool.FetchObject();
            autoDisabler = collider.GetComponent<AutoDisabler>();
            autoDisabler.alwaysOn = scatter.alwaysCollideable;
            Vector3 actualWorldPos = worldPos + (quad.gameObject.transform.position - quadOriginalPosition);    //Transform from old quad position to new quad position, parent and forget
            collider.transform.position = actualWorldPos;
            collider.transform.rotation = rot;
            collider.transform.localScale = scale;

            collider.GetComponent<MeshCollider>().sharedMesh = collisionMesh;
            //collider.GetComponent<MeshFilter>().sharedMesh = collisionMesh;

            collider.layer = quad.gameObject.layer;
            collider.tag = quad.gameObject.tag;
            collider.transform.parent = quad.transform;

            collider.SetActive(true);
            active = true;
        }
        public void DestroyGameObject()
        {
            if (active)
            {
                active = false;
                if (collider != null && autoDisabler != null && autoDisabler.active)
                {
                    collider.SetActive(false);
                    autoDisabler.alwaysOn = false;
                    ObjectPool.ReturnObject(collider);
                }
            }
        }
    }
    public class CollisionHandlerAdvanced   //Per quad
    {
        PQ quad;
        List<Position> positions = new List<Position>();
        Dictionary<Scatter, PositionData[]> scatterData = new Dictionary<Scatter, PositionData[]>();
        OctTree tree;
        int timesDataAdded = 0;
        public List<Vector3> nearbyPoints = new List<Vector3>();
        //public Dictionary<Scatter, Mesh> meshesOnThisQuad = new Dictionary<Scatter, Mesh>();    //Change to meshes on the planet later for small optimization
        
        float rangeLimit;   //Range at which quads will attempt to generate colliders - Physics Range Extender use this

        public int maxDataCount = 0;

        public bool cleaned = false;
        public bool initialized = false;
        public bool allDataPresent = false;

        public Bounds vesselBoundingBox;
        public OctBoundingBox vesselSearchBox;
        private float maxBounds;

        private Vector3 quadOriginalPosition;   //Original position of quad in world space when the octree was created

        public CollisionHandlerAdvanced(ref PQ quad, float rangeLimit)
        {
            this.quad = quad;
            this.rangeLimit = rangeLimit;
            ScatterManagerPlus.OnQuadPhysicsCheck += RangeCheck;
           
        }
        public void AddData(Scatter scatter, PositionData[] data, Matrix4x4 inverseQTW, Vector3 oldShaderOffset)   //Add data in quad LOCAL SPACE
        {
            if (scatterData.ContainsKey(scatter))
            {
                scatterData.Remove(scatter);
            }
            scatterData.Add(scatter, data);
            for (int i = 0; i < data.Length; i++)
            {
                Vector3 pos = data[i].mat.GetColumn(3);
                pos -= oldShaderOffset;
                pos = inverseQTW.MultiplyPoint(pos);    //Now in local space relative to quad
                scatterData[scatter][i].pos = pos;
            }

            timesDataAdded++;
            if (timesDataAdded == maxDataCount) //All data from the quad is now present
            {
                allDataPresent = true;
                RangeCheck();
            }
        }
        float craftDist = 0;
        float minDist = 0;
        float maxMeshBounds = 0;
        public void CreateData(Scatter scatter, PositionData[] data)
        {
            GameObject model = scatter.collisionMesh != null ? GameDatabase.Instance.GetModel(scatter.collisionMesh) : GameDatabase.Instance.GetModel(scatter.model);
            Mesh mesh = GameObject.Instantiate(model.GetComponent<MeshFilter>().mesh);
            UnityEngine.Object.Destroy(model);

            Matrix4x4 quadToWorld = quad.gameObject.transform.localToWorldMatrix;
            for (int i = 0; i < data.Length; i++)
            {
                Vector3 pos = data[i].pos;
                pos = quadToWorld.MultiplyPoint(pos);   //"old quad" location
                Quaternion rotation = Quaternion.LookRotation(data[i].mat.GetColumn(2), data[i].mat.GetColumn(1));
                Vector3 scale = new Vector3(data[i].mat.GetColumn(0).magnitude, data[i].mat.GetColumn(1).magnitude, data[i].mat.GetColumn(2).magnitude);

                float meshBound = Mathf.Max((data[i].mat.MultiplyPoint(mesh.bounds.min) - data[i].mat.MultiplyPoint(mesh.bounds.max)).magnitude, 10.0f);   //Length of mesh in world space
                if (meshBound > maxMeshBounds && !scatter.alwaysCollideable) { maxMeshBounds = meshBound; }

                Position thisObject = new Position(ref pos, rotation, scale, meshBound, scatter, mesh, ref quadOriginalPosition, quad);   //Object has correct world space AT THS TIME THIS METHOD WAS CALLED. Transform craft search position to the quad's position AT THIS TIME
                positions.Add(thisObject);
                if (scatter.alwaysCollideable) {  thisObject.CreateGameObject(); thisObject.autoDisabler.alwaysOn = true; }
            }
            
        }
        public void GetNearestRangeToALoadedCraft()
        {
            minDist = 100000;
            for (int i = 0; i < FlightGlobals.VesselsLoaded.Count; i++)
            {
                if (ScatterGlobalSettings.onlyQueryControllable && !FlightGlobals.VesselsLoaded[i].isCommandable) { continue; }
                float heightFromSurface = Mathf.Min(FlightGlobals.VesselsLoaded[i].heightFromTerrain, (float)FlightGlobals.VesselsLoaded[i].altitude);
                if (heightFromSurface > 100 || FlightGlobals.VesselsLoaded[i].speed > 250) { continue; }
                craftDist = Vector3.Distance(FlightGlobals.VesselsLoaded[i].transform.position, quad.gameObject.transform.position);
                if (craftDist < minDist) { minDist = craftDist; }
            }
        }
        public void CreateOctree()  //tree.Insert() creates a lot of garbage, idk why, but worth looking into because it causes a 15ms stutter
        {
            Profiler.BeginSample("Create Octree");
            OctBoundingBox box = OctTreeUtils.GetBounds(quad.gameObject.GetComponent<Renderer>().bounds);   //This position is different when querying
            tree = new OctTree(box, quad);
            for (int i = 0; i < positions.Count; i++)
            {
                tree.Insert(ref positions[i].worldPos, i);  //We can use the index when querying to return a position and get the GO, bounds and worldpos easily
            }
            Profiler.EndSample();
        }
        public void RangeCheck()
        {
            GetNearestRangeToALoadedCraft();
            if (minDist < rangeLimit)
            {
                if (FlightGlobals.VesselsLoaded.Count == 0) { return; }   //Don't generate colliders when the craft is going mega speed
                if (!initialized && allDataPresent)
                {
                    Scatter[] keys = scatterData.Keys.ToArray();
                    quadOriginalPosition = quad.gameObject.transform.position;
                    for (int i = 0; i < keys.Length; i++)
                    {
                        CreateData(keys[i], scatterData[keys[i]]);
                    }
                    QuadColliderData.data.Add(quad, positions);
                    QuadColliderData.DataCreated(quad, positions);
                    CreateOctree();
                    initialized = true;
                }
                if (initialized)
                {
                    nearbyPoints.Clear();
                    for (int i = 0; i < FlightGlobals.VesselsLoaded.Count; i++)
                    {
                        if (ScatterGlobalSettings.onlyQueryControllable && !FlightGlobals.VesselsLoaded[i].isCommandable) { continue; }
                        maxBounds = Mathf.Max(Mathf.Max(FlightGlobals.VesselsLoaded[i].vesselSize.x, FlightGlobals.VesselsLoaded[i].vesselSize.y), FlightGlobals.VesselsLoaded[i].vesselSize.z) * 2.4f;
                        maxBounds = Mathf.Max(maxMeshBounds, maxBounds); //If the craft is too small, use bounds of the largest, non-always-active mesh to prevent pop-in
                        vesselBoundingBox = new Bounds(FlightGlobals.VesselsLoaded[i].transform.position + (quadOriginalPosition - quad.gameObject.transform.position), new Vector3(maxBounds, maxBounds, maxBounds));
                        
                        vesselSearchBox = OctTreeUtils.GetBounds(vesselBoundingBox);
                        tree.QueryRange(ref vesselSearchBox, ref nearbyPoints);
                    }
                }
            }
            else
            {
                if (initialized)
                {
                    DestroyColliders();
                    return;
                }
            }
        }
        
        
        public void DestroyColliders()
        {
            //if (QuadColliderData.data.ContainsKey(quad))
            //{
            //    List<Position> data = QuadColliderData.data[quad];
            //    for (int i = 0; i < data.Count; i++)
            //    {
            //        data[i].RemoveGameObject();
            //    }
            //}
            for (int i = 0; i < positions.Count; i++)
            {
                positions[i].DestroyGameObject();
            }
            positions.Clear();
            if (QuadColliderData.data.ContainsKey(quad))
            {
                QuadColliderData.data[quad].Clear();
                QuadColliderData.data.Remove(quad);
                QuadColliderData.DataRemoved(quad);
            }
            tree = null;
            initialized = false;
            //allDataPresent = false;
            //timesDataAdded = 0;
        }
        public void Cleanup()
        {
            //if (QuadColliderData.data.ContainsKey(quad))
            //{
            //    List<Position> data = QuadColliderData.data[quad];
            //    for (int i = 0; i < data.Count; i++)
            //    {
            //        data[i].RemoveGameObject();
            //    }
            //}

            
            ScatterManagerPlus.OnQuadPhysicsCheck -= RangeCheck;
            for (int i = 0; i < positions.Count; i++)
            {
                positions[i].DestroyGameObject();
            }
            positions.Clear();
            scatterData.Clear();
            if (QuadColliderData.data.ContainsKey(quad))
            {
                QuadColliderData.data[quad].Clear();
                QuadColliderData.data.Remove(quad);
            }

            
            tree = null;
            initialized = false;
            cleaned = true;
            allDataPresent = false;
            timesDataAdded = 0;
        }
    }
}
