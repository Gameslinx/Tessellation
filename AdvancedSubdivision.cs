
//This isn't implemented yet, but the source is here
//Concept is simple - Select a 4x4 square containing 16 vertices from a quad's mesh, duplicate it into a new mesh and subdivide it
//instead of subdividing the massive quad and having needless amounts of vertices

//Probably gonna need some help with this one unfortunately, it's not something I think I can do alone!

using PQSModExpansion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AdvancedSubdivision
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class Controller : MonoBehaviour //Controls advanced subdivision - This doesn't work yet, but the DLL will be in the release in case anyone wants to experiment.
    {
        float nearestQuadDistance = 10000;
        Vector3 nearestQuadPos = Vector3.zero;
        Vector3 craftPos = Vector3.zero;
        GameObject nearestQuad = null;
        public void Update()
        {
            bool flag = Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.Alpha9);
            if (flag)
            {
                if (FlightGlobals.ActiveVessel == null || QuadMeshDictionary.subdividedQuadList.Count == 0)
                {
                    //VESSEL IS NULL / NO QUADS NEARBY (VESSEL IN AIR?)
                    return;
                }

                //CALCULATE CLOSEST QUAD
                GetClosestQuad();

                //CALCULATE DISTANCE BETWEEN QUAD CENTER AND CRAFT TO GET LOCAL POSITION OF CRAFT ON THE QUAD
                Vector3 craftLocalPos = craftPos - nearestQuadPos;
                Vector3[] localVerts = GetSmallestBoundingVertices(craftLocalPos);
                Vector3 centre = (localVerts[0] + localVerts[1] + localVerts[2] + localVerts[3]) / 4;
                Vector3[] newVerts = GetBoundingVertices(centre);

            }
            
            
        }
        public void GetClosestQuad()
        {
            nearestQuadDistance = 10000;
            craftPos = FlightGlobals.ActiveVessel.transform.position;
            foreach (KeyValuePair<string, GameObject> quad in QuadMeshDictionary.subdividedQuadList)
            {
                float distToVessel = Vector3.Distance(quad.Value.transform.position, craftPos);
                if (distToVessel < nearestQuadDistance)
                {
                    nearestQuadDistance = distToVessel;
                    nearestQuad = quad.Value;
                    nearestQuadPos = quad.Value.transform.position;
                }
            }
        }
        public Vector3[] GetBoundingVertices(Vector3 localVertCentre)
        {
            Vector3[] boundingVertices = new Vector3[16];    //Square surrounding craft containing 16 vertices
            Mesh quadMesh = Instantiate(nearestQuad.GetComponent<MeshFilter>().sharedMesh);
            Vector3[] verts = quadMesh.vertices;
            float vertLength = Vector3.Distance(verts[0], verts[1]); //Length of a quad within the subdivided quad
            float distLimit = Mathf.Sqrt((vertLength * vertLength) + (vertLength * vertLength)) * 1.5f + 1f;
            Debug.Log("DistLimit for larger bounding: " + distLimit);
            int found = 0;
            for (int i = 0; i < verts.Length; i++)
            {
                float localDistance = Vector3.Distance(localVertCentre, verts[i]);
                if (localDistance <= distLimit)
                {
                    //Found one of the 16 vertices around the centre point
                    boundingVertices[found] = verts[i];
                    found++;
                }
            }
            if (found != 16)
            {
                Debug.Log("Something is wrong - 16 vertices were not found!");
                Debug.Log(found + " were located");
            }
            return boundingVertices;
        }
        public Vector3[] GetSmallestBoundingVertices(Vector3 craftLocalPos)
        {
            Vector3[] boundingVertices = new Vector3[4];    //Closest 4 vertices to the craft
            Mesh quadMesh = Instantiate(nearestQuad.GetComponent<MeshFilter>().sharedMesh);
            Vector3[] verts = quadMesh.vertices;
            float vertLength = Vector3.Distance(verts[0], verts[1]);
            float distLimit = Mathf.Sqrt((vertLength * vertLength) + (vertLength * vertLength)) + 1f;
            Debug.Log("DistLimit for smallest bounding: " + distLimit);
            int found = 0;
            for (int i = 0; i < verts.Length; i++)
            {
                float localDistance = Vector3.Distance(craftLocalPos, verts[i]);
                if (localDistance <= distLimit)
                {
                    //One of the four vertices
                    boundingVertices[found] = verts[i]; //Store local position of vertex as a bounding vert
                    found++;
                }
            }
            if (found != 4)
            {
                Debug.Log("Something is wrong - 4 vertices were not found!");
                Debug.Log(found + " were located");
            }
            return boundingVertices;
        }
    }
}
