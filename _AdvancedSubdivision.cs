using PQSModExpansion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Parallax;

[assembly: KSPAssembly("AdvancedSubdivision", 1, 0)]
namespace AdvancedSubdivision
{
    using Steamworks;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using UnityEngine.UIElements;
    public class AdvancedSubdivisionGlobal
    {
        public static string situation = "main";
        public static int smallestIndex = 0;
        public static Vector3 centreWorldPos = Vector3.zero;
        public static GameObject[] subQuads = new GameObject[0];
        public static string[] quadDirection = new string[0]; //Triangle winding order, because for some reason KSP uses two different orders. Disgusting
    }
    public class AdvancedSubdivision : MonoBehaviour
    {
        public int subdivisionLevel = 32;
        public Vector3 lastCentre;
        public GameObject newQuad;
        public string situation = "main";
        public int smallestIndex = 0;
        public Vector3 centre;
        Vector3[] boundingVertices = new Vector3[0];
        Vector3[] boundingNormals = new Vector3[0];
        Color[] boundingColors = new Color[0];
        public Vector3[] boundingSquare = new Vector3[4];
        Vector3[] cutoutVertices = new Vector3[0];
        public int[] subQuadVertsToDelete = new int[4];
        int[] indicesOfTrisToDelete = new int[0];
        int boundingWidth = 0;
        int boundingHeight = 0;
        public Mesh originalMesh;
        string windingOrder = "clockwise";
        bool meshIsOriginal = false;
        void OnDestroy()
        {
            gameObject.GetComponent<MeshFilter>().mesh = Instantiate(originalMesh);
            Destroy(newQuad);
            Destroy(originalMesh);
        }
        void Start()
        {
            newQuad = new GameObject();
            newQuad.AddComponent<MeshFilter>();
            newQuad.AddComponent<MeshRenderer>();
            newQuad.GetComponent<MeshRenderer>().enabled = true;

            newQuad.transform.position = gameObject.transform.position;
            newQuad.transform.localScale = gameObject.transform.localScale;
            newQuad.transform.rotation = gameObject.transform.rotation;

            newQuad.GetComponent<MeshRenderer>().sharedMaterial = gameObject.GetComponent<MeshRenderer>().sharedMaterial;
            newQuad.layer = gameObject.layer;
            if (newQuad.GetComponent<MeshCollider>() != null)
            {
                Destroy(newQuad.GetComponent<MeshCollider>());
            }
            if (newQuad.GetComponent<Collider>() != null)
            {
                Destroy(newQuad.GetComponent<Collider>());
            }
            //MeshHandler.Subdivide(gameObject.GetComponent<MeshFilter>().mesh, 14, 14);
            originalMesh = Instantiate(gameObject.GetComponent<MeshFilter>().mesh);
            //newQuad.GetComponent<MeshRenderer>().material.SetColor("_WireColor", new Color(0, 0, 1, 1));
            //newQuad.GetComponent<MeshRenderer>().material.SetColor("_Color", new Color(0.2f, 0.5f, 0.4f, 1));
            windingOrder = AdvancedSubdivisionGlobal.quadDirection[0];

        }
        void FixedUpdate()
        {
            //return;
            //return;
            if (FlightGlobals.ActiveVessel == null || FlightGlobals.ActiveVessel.velocityD.magnitude > 25f)
            {
                newQuad.GetComponent<MeshRenderer>().enabled = false;
                if (meshIsOriginal == false)
                {
                    gameObject.GetComponent<MeshFilter>().sharedMesh = Instantiate(originalMesh);
                    meshIsOriginal = true;
                }
                UpdateSubQuads();
                return;
            }
            Vector3 position = Vector3.zero;
            Ray ray = new Ray();
            RaycastHit hit;
            ray.origin = FlightGlobals.ActiveVessel.transform.position;
            ray.direction = -Vector3.Normalize(FlightGlobals.ActiveVessel.transform.position - FlightGlobals.currentMainBody.transform.position);
            if (UnityEngine.Physics.Raycast(ray, out hit, 50, (int)(1 << 15)))
            {
            }
            else
            {
                newQuad.GetComponent<MeshRenderer>().enabled = false;
                if (meshIsOriginal == false)
                {
                    gameObject.GetComponent<MeshFilter>().sharedMesh = Instantiate(originalMesh);
                    meshIsOriginal = true;
                }
                UpdateSubQuads();
                return;
            }
            newQuad.GetComponent<MeshRenderer>().enabled = true;
            newQuad.transform.position = gameObject.transform.position;
            Mesh mesh = Instantiate(originalMesh);
            Vector4[] square = GetSquare(mesh.vertices, hit.point);
            centre = (transform.TransformPoint(square[0]) + transform.TransformPoint(square[1]) + transform.TransformPoint(square[2]) + transform.TransformPoint(square[3])) / 4;
            
            if (centre == lastCentre)
            {
                Destroy(mesh);  //Can't be having memory leaks now can we
                return;
            }
            else
            {
                lastCentre = centre;
                AdvancedSubdivisionGlobal.centreWorldPos = centre;
                gameObject.GetComponent<MeshFilter>().sharedMesh = mesh;
            }
            boundingVertices = GetBoundingVertices((int)square[0].w, mesh);

            int[] tris = originalMesh.triangles;
            int[] checkTriArray = new int[] { tris[0], tris[1], tris[2], tris[3], tris[4], tris[5] };   //First quad triangles
            CutoutTris(checkTriArray);

            int originalQuadIndexLength = (int)Mathf.Sqrt(mesh.vertexCount);
            var newQuadMeshFilter = newQuad.GetComponent<MeshFilter>();
            Destroy(newQuad.GetComponent<MeshFilter>().sharedMesh);
            Mesh quadMesh = new Mesh();
            quadMesh.vertices = boundingVertices;
            quadMesh.triangles = MeshHandler.CalculateTris(boundingWidth, boundingHeight, checkTriArray, windingOrder);
            quadMesh.uv = MeshHandler.CalculateUVs(boundingVertices.Length);
            quadMesh.normals = boundingNormals;
            quadMesh.colors = boundingColors;
            MeshHelper.Subdivide(quadMesh, subdivisionLevel);
            newQuadMeshFilter.sharedMesh = quadMesh;
            meshIsOriginal = false;
            UpdateSubQuads();
        }
        void UpdateSubQuads()
        {
            for (int i = 0; i < AdvancedSubdivisionGlobal.subQuads.Length; i++)
            {
                if (i != 0)
                {
                    if (AdvancedSubdivisionGlobal.subQuads[i] != null && AdvancedSubdivisionGlobal.subQuads[i].GetComponent<SubQuad>() != null)
                    {
                        AdvancedSubdivisionGlobal.subQuads[i].GetComponent<SubQuad>().indicesToDelete = subQuadVertsToDelete;
                        AdvancedSubdivisionGlobal.subQuads[i].GetComponent<SubQuad>().windingOrder = AdvancedSubdivisionGlobal.quadDirection[i];
                        AdvancedSubdivisionGlobal.subQuads[i].GetComponent<SubQuad>().subdivisionLevel = subdivisionLevel;
                        AdvancedSubdivisionGlobal.subQuads[i].GetComponent<SubQuad>().ForceUpdate();
                    }
                }
            }
        }
        Vector4[] GetSquare(Vector3[] verts, Vector3 worldCraftPos)
        {
            int indexLength = (int)Mathf.Sqrt(verts.Length);
            Vector3d meshPointInWorldSpace0 = transform.TransformPoint(verts[0]);
            Vector3d meshPointInWorldSpace1 = transform.TransformPoint(verts[indexLength + 1]);
            Vector3d normal0 = Vector3d.Normalize(meshPointInWorldSpace0 - FlightGlobals.currentMainBody.position);
            Vector3d normal1 = Vector3d.Normalize(meshPointInWorldSpace1 - FlightGlobals.currentMainBody.position);

            meshPointInWorldSpace0 = normal0 * FlightGlobals.currentMainBody.Radius;
            meshPointInWorldSpace1 = normal1 * FlightGlobals.currentMainBody.Radius;

            float distLimit = Vector3.Distance(meshPointInWorldSpace0, meshPointInWorldSpace1) * 1.33f; // *1.33f is just a tolerance

            Vector3 planetCraftPos = Vector3d.Normalize(FlightGlobals.ActiveVessel.GetWorldPos3D() - FlightGlobals.currentMainBody.position) * FlightGlobals.currentMainBody.Radius;
            //gameObject.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Tolerance", distLimit);
            //gameObject.GetComponent<MeshRenderer>().sharedMaterial.SetVector("_CraftPos", worldCraftPos);
            int[] indices = new int[2];
            List<Vector2> indicesAndDistances = new List<Vector2>();
            for (int i = 0; i < verts.Length; i++)
            {

                Vector3 vertexPositionInWorldSpace = Vector3d.Normalize(((Vector3d)transform.TransformPoint(verts[i])) - FlightGlobals.currentMainBody.position) * FlightGlobals.currentMainBody.Radius;

                float distance = Distance(vertexPositionInWorldSpace, planetCraftPos);
                if (distance <= distLimit)
                {
                    indicesAndDistances.Add(new Vector2(i, distance));          //Index, Distance. Sort by distance, select top 2
                }
            }
            if (indicesAndDistances.Count < 2)
            {
                Debug.Log("[Parallax Advanced Subdivision] Exception: I can't find enough nearby vertices :(");
            }
            indicesAndDistances = SortByDistance(indicesAndDistances);
            indices[0] = (int)indicesAndDistances[0].x;
            indices[1] = (int)indicesAndDistances[1].x;
            if (indices[0] > indices[1])
            {
                int temp = indices[1];                                          //Order by index
                indices[1] = indices[0];
                indices[0] = temp;
            }
            //Apply conditions now

            Vector3 thirdPoint = Vector3.zero;
            Vector3 fourthPoint = Vector3.zero;
            int index3 = 0;
            int index4 = 0;
            smallestIndex = 0;

            bool alreadyCompleted = false;

            if (indices[0] - indexLength < 0 && indices[0] == indices[1] - 1) //Situation 1
            {
                index3 = indices[0] + indexLength;
                index4 = indices[1] + indexLength;
                thirdPoint = verts[index3];
                fourthPoint = verts[index4];

                smallestIndex = indices[0];
                alreadyCompleted = true;
            }

            if (indices[0] + indexLength >= verts.Length && indices[0] == indices[1] - 1) //Situation 4
            {
                index3 = indices[0] - indexLength;
                index4 = indices[1] - indexLength;
                thirdPoint = verts[index3];
                fourthPoint = verts[index4];

                smallestIndex = indices[0] - indexLength;
                alreadyCompleted = true;
            }
            if (indices[0] == indices[1] - indexLength && indices[0] == 0)  //Only happens in bottom left corner (index 0)
            {
                index3 = indices[0] + 1;
                index4 = indices[1] + 1;
                thirdPoint = verts[index3];
                fourthPoint = verts[index4];

                smallestIndex = indices[0];
                alreadyCompleted = true;
            }


            if (indices[0] + 1 + indexLength == indices[1]) //Parallelogram quads
            {
                index3 = indices[0] + 1;
                index4 = indices[0] + indexLength;
                thirdPoint = verts[index3];
                fourthPoint = verts[index4];

                smallestIndex = indices[0];
                alreadyCompleted = true;
            }
            if (indices[0] - 1 + indexLength == indices[1])
            {
                index3 = indices[0] - 1;
                index4 = indices[0] + indexLength;
                thirdPoint = verts[index3];
                fourthPoint = verts[index4];

                smallestIndex = indices[0] - 1;
                alreadyCompleted = true;
            }

            if (alreadyCompleted == false && verts[indices[0]] == verts[indices[1] - 1] && Distance(transform.TransformPoint(verts[indices[0] + indexLength]), worldCraftPos) <= Distance(transform.TransformPoint(verts[indices[0] - indexLength]), worldCraftPos))
            {
                index3 = indices[0] + indexLength;
                index4 = indices[1] + indexLength;
                thirdPoint = verts[index3];
                fourthPoint = verts[index4];

                smallestIndex = indices[0];
            }
            if (alreadyCompleted == false && verts[indices[0]] == verts[indices[1] - indexLength] && Distance(transform.TransformPoint(verts[indices[0] + 1]), worldCraftPos) <= Distance(transform.TransformPoint(verts[indices[0] - 1]), worldCraftPos))
            {
                index3 = indices[0] + 1;
                index4 = indices[1] + 1;
                thirdPoint = verts[index3];
                fourthPoint = verts[index4];

                smallestIndex = indices[0];
            }
            if (alreadyCompleted == false && verts[indices[0]] == verts[indices[1] - indexLength] && Distance(transform.TransformPoint(verts[indices[0] - 1]), worldCraftPos) <= Distance(transform.TransformPoint(verts[indices[0] + 1]), worldCraftPos))
            {
                index3 = indices[0] - 1;
                index4 = indices[1] - 1;
                thirdPoint = verts[index3];
                fourthPoint = verts[index4];

                smallestIndex = indices[0] - 1;
            }
            if (alreadyCompleted == false && verts[indices[0]] == verts[indices[1] - 1] && Distance(transform.TransformPoint(verts[indices[0] - indexLength]), worldCraftPos) <= Distance(transform.TransformPoint(verts[indices[0] + indexLength]), worldCraftPos))
            {
                index3 = indices[0] - indexLength;
                index4 = indices[1] - indexLength;
                thirdPoint = verts[index3];
                fourthPoint = verts[index4];

                smallestIndex = indices[0] - indexLength;
            }
            Vector4 posAndIndex1 = new Vector4(verts[indices[0]].x, verts[indices[0]].y, verts[indices[0]].z, smallestIndex);
            Vector4 posAndIndex2 = new Vector4(verts[indices[1]].x, verts[indices[1]].y, verts[indices[1]].z, smallestIndex);
            Vector4 posAndIndex3 = new Vector4(thirdPoint.x, thirdPoint.y, thirdPoint.z, smallestIndex);
            Vector4 posAndIndex4 = new Vector4(fourthPoint.x, fourthPoint.y, fourthPoint.z, smallestIndex);

            boundingSquare[0] = posAndIndex1;
            boundingSquare[1] = posAndIndex2;
            boundingSquare[2] = posAndIndex3;
            boundingSquare[3] = posAndIndex4;

            indicesOfTrisToDelete = new int[4];
            indicesOfTrisToDelete[0] = indices[0];
            indicesOfTrisToDelete[1] = indices[1];
            indicesOfTrisToDelete[2] = index3;
            indicesOfTrisToDelete[3] = index4;

            subQuadVertsToDelete[0] = indices[0];
            subQuadVertsToDelete[1] = indices[1];
            subQuadVertsToDelete[2] = index3;
            subQuadVertsToDelete[3] = index4;
            subQuadVertsToDelete = SortByIndex(subQuadVertsToDelete);

            AdvancedSubdivisionGlobal.smallestIndex = smallestIndex;

            return new Vector4[] { posAndIndex1, posAndIndex2, posAndIndex3, posAndIndex4 };  //INDEX AND POSITIONS DO NOT MATCH!!! This is intended but don't forget!
        }
        public Vector3[] GetBoundingVertices(int leftMostIndex, Mesh mesh)
        {
            Vector3[] verts = mesh.vertices;
            Vector3[] normals = mesh.normals;
            Color[] colors = mesh.colors;
            int indexLength = (int)Mathf.Sqrt(verts.Length);
            int correctIndex = leftMostIndex - indexLength - 1;
            correctIndex--;


            situation = "corner";
            AdvancedSubdivisionGlobal.situation = situation;
            if (leftMostIndex == 0) //Top left
            {
                int realIndex = 0;
                situation = "topleft";
                boundingWidth = 2;
                boundingHeight = 2;
                AdvancedSubdivisionGlobal.situation = situation;
                return Corner(verts, normals, colors, realIndex - 1, indexLength);
            }
            if (leftMostIndex + 1 == indexLength - 1) //Top right
            {
                int realIndex = leftMostIndex - 1;
                situation = "topright";
                boundingWidth = 2;
                boundingHeight = 2;
                AdvancedSubdivisionGlobal.situation = situation;
                return Corner(verts, normals, colors, realIndex - 1, indexLength);
            }
            if (leftMostIndex == verts.Length - (indexLength * 2)) //Bottom left
            {
                int realIndex = verts.Length - (indexLength * 3);
                situation = "bottomleft";
                boundingWidth = 2;
                boundingHeight = 2;
                AdvancedSubdivisionGlobal.situation = situation;
                return Corner(verts, normals, colors, realIndex - 1, indexLength);
            }
            if (leftMostIndex + 1 + indexLength == verts.Length - 1) //Bottom right
            {
                int realIndex = leftMostIndex - indexLength - 1;
                situation = "bottomright";
                boundingWidth = 2;
                boundingHeight = 2;
                AdvancedSubdivisionGlobal.situation = situation;
                return Corner(verts, normals, colors, realIndex - 1, indexLength);
            }
            if (leftMostIndex % indexLength == 0)   //Left
            {
                int realIndex = leftMostIndex - indexLength;
                situation = "left";
                boundingWidth = 2;
                boundingHeight = 3;
                AdvancedSubdivisionGlobal.situation = situation;
                return LeftSide(verts, normals, colors, realIndex - 1, indexLength);
                //Only add up to 3 instead of 4 before adding the index length
            }
            if ((leftMostIndex + 2) % indexLength == 0) //Right
            {
                int realIndex = leftMostIndex - indexLength - 1;
                situation = "right";
                boundingWidth = 2;
                boundingHeight = 3;
                AdvancedSubdivisionGlobal.situation = situation;
                return RightSide(verts, normals, colors, realIndex - 1, indexLength);
            }
            if (leftMostIndex - indexLength < 0)    //Top
            {
                int realIndex = leftMostIndex - 1;
                situation = "top";
                boundingWidth = 3;
                boundingHeight = 2;
                AdvancedSubdivisionGlobal.situation = situation;
                return TopSide(verts, normals, colors, realIndex - 1, indexLength);
            }
            if (leftMostIndex + (indexLength * 2) >= verts.Length)    //Bottom
            {
                int realIndex = leftMostIndex - indexLength - 1;
                situation = "bottom";
                boundingWidth = 3;
                boundingHeight = 2;
                AdvancedSubdivisionGlobal.situation = situation;
                return BottomSide(verts, normals, colors, realIndex - 1, indexLength);
            }

            situation = "main";
            boundingWidth = 3;
            boundingHeight = 3;
            AdvancedSubdivisionGlobal.situation = situation;
            Vector3[] boundingVertices = new Vector3[16];
            boundingNormals = new Vector3[16];
            boundingColors = new Color[16];
            for (int i = 0; i < 16; i++)
            {
                correctIndex += 1;
                if ((i % 4 == 0) && i > 0)
                {
                    correctIndex += (indexLength - 4);  //Down, left 4
                }
                boundingVertices[i] = verts[correctIndex];
                boundingNormals[i] = normals[correctIndex];
                boundingColors[i] = colors[correctIndex];
            }
            return boundingVertices;
        }
        public float Distance(Vector3 p1, Vector3 p2)
        {
            return Vector3.Distance(p1, p2);
        }
        public List<Vector2> SortByDistance(List<Vector2> values)
        {
            Vector2 temp;
            for (int i = 0; i < values.Count; i++)
            {
                for (int b = 0; b < values.Count - 1; b++)
                {
                    if (values[b].y > values[b + 1].y)
                    {
                        temp = values[b + 1];
                        values[b + 1] = values[b];
                        values[b] = temp;
                    }
                }
            }
            return values;
        }
        public int[] SortByIndex(int[] values)
        {
            int temp;
            for (int i = 0; i < values.Length; i++)
            {
                for (int b = 0; b < values.Length - 1; b++)
                {
                    if (values[b] > values[b + 1])
                    {
                        temp = values[b + 1];
                        values[b + 1] = values[b];
                        values[b] = temp;
                    }
                }
            }
            return values;
        }
        public Vector3[] LeftSide(Vector3[] verts, Vector3[] normals, Color[] colors, int correctIndex, int indexLength)
        {
            Vector3[] boundingVertices = new Vector3[12];
            boundingNormals = new Vector3[12];
            boundingColors = new Color[12];
            for (int i = 0; i < 12; i++)
            {
                correctIndex += 1;
                if ((i % 3 == 0) && i > 0)
                {
                    correctIndex += (indexLength - 3);  //Down, left 3
                }
                boundingVertices[i] = verts[correctIndex];
                boundingNormals[i] = normals[correctIndex];
                boundingColors[i] = colors[correctIndex];
            }
            return boundingVertices;
        }
        public Vector3[] RightSide(Vector3[] verts, Vector3[] normals, Color[] colors, int correctIndex, int indexLength)
        {
            Vector3[] boundingVertices = new Vector3[12];
            boundingNormals = new Vector3[12];
            boundingColors = new Color[12];
            for (int i = 0; i < 12; i++)
            {
                correctIndex += 1;
                if ((i % 3 == 0) && i > 0)
                {
                    correctIndex += (indexLength - 3);  //Down, left 3
                }
                boundingVertices[i] = verts[correctIndex];
                boundingNormals[i] = normals[correctIndex];
                boundingColors[i] = colors[correctIndex];
            }
            return boundingVertices;
        }
        public Vector3[] TopSide(Vector3[] verts, Vector3[] normals, Color[] colors, int correctIndex, int indexLength)
        {
            Vector3[] boundingVertices = new Vector3[12];
            boundingNormals = new Vector3[12];
            boundingColors = new Color[12];
            for (int i = 0; i < 12; i++)
            {
                correctIndex += 1;
                if ((i % 4 == 0) && i > 0)
                {
                    correctIndex += (indexLength - 4);  //Down, left 3
                }
                boundingVertices[i] = verts[correctIndex];
                boundingNormals[i] = normals[correctIndex];
                boundingColors[i] = colors[correctIndex];
            }
            return boundingVertices;
        }
        public Vector3[] BottomSide(Vector3[] verts, Vector3[] normals, Color[] colors, int correctIndex, int indexLength)
        {
            Vector3[] boundingVertices = new Vector3[12];
            boundingNormals = new Vector3[12];
            boundingColors = new Color[12];
            for (int i = 0; i < 12; i++)
            {
                correctIndex += 1;
                if ((i % 4 == 0) && i > 0)
                {
                    correctIndex += (indexLength - 4);  //Down, left 3
                }
                boundingVertices[i] = verts[correctIndex];
                boundingNormals[i] = normals[correctIndex];
                boundingColors[i] = colors[correctIndex];
            }
            return boundingVertices;
        }
        public Vector3[] Corner(Vector3[] verts, Vector3[] normals, Color[] colors, int correctIndex, int indexLength)
        {
            Vector3[] boundingVertices = new Vector3[9];
            boundingNormals = new Vector3[9];
            boundingColors = new Color[9];
            for (int i = 0; i < 9; i++)
            {
                correctIndex += 1;
                if ((i % 3 == 0) && i > 0)
                {
                    correctIndex += (indexLength - 3);  //Down, left 3
                }
                boundingVertices[i] = verts[correctIndex];
                boundingNormals[i] = normals[correctIndex];
                boundingColors[i] = colors[correctIndex];
            }
            return boundingVertices;
        }
        public void CutoutTris(int[] checkTriArray)
        {
            Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;
            int indexLength = (int)Mathf.Sqrt(mesh.vertices.Length) - 1;
            int[] newTris = new int[indexLength * indexLength * 2 * 3];   //Subtract the amount of triangles in the square we're removing
            int found = 0;
            if (windingOrder == "anticlockwise")
            {
                for (int y = 0; y < indexLength; y++)
                {
                    for (int x = 0; x < indexLength; x++)
                    {
                        int a = y * (indexLength + 1) + x;
                        int b = (y + 1) * (indexLength + 1) + x;
                        int c = b + 1;
                        int d = a + 1;

                        int startIndex = y * indexLength * 2 * 3 + x * 2 * 3;

                        if (indicesOfTrisToDelete.Contains(a) || indicesOfTrisToDelete.Contains(b) || indicesOfTrisToDelete.Contains(c) || indicesOfTrisToDelete.Contains(d))
                        {
                            found += 6;
                        }
                        else
                        {
                            //startIndex -= found;

                            newTris[startIndex] = d;
                            newTris[startIndex + 1] = a;
                            newTris[startIndex + 2] = b;

                            newTris[startIndex + 3] = b;
                            newTris[startIndex + 4] = c;
                            newTris[startIndex + 5] = d;
                        }

                    }
                }
            }
            else
            {
                for (int y = 0; y < indexLength; y++)
                {
                    for (int x = 0; x < indexLength; x++)
                    {
                        int a = y * (indexLength + 1) + x;
                        int b = (y + 1) * (indexLength + 1) + x;
                        int c = b + 1;
                        int d = a + 1;

                        int startIndex = y * indexLength * 2 * 3 + x * 2 * 3;

                        if (indicesOfTrisToDelete.Contains(a) || indicesOfTrisToDelete.Contains(b) || indicesOfTrisToDelete.Contains(c) || indicesOfTrisToDelete.Contains(d))
                        {
                            found += 6;
                        }
                        else
                        {
                            //startIndex -= found;

                            newTris[startIndex] = a;
                            newTris[startIndex + 1] = b;
                            newTris[startIndex + 2] = c;

                            newTris[startIndex + 3] = c;
                            newTris[startIndex + 4] = d;
                            newTris[startIndex + 5] = a;
                        }

                    }
                }
            }
            
            mesh.triangles = newTris;
            gameObject.GetComponent<MeshFilter>().mesh = mesh;
        }
    }
    public class MeshHandler
    {
        public static Mesh Subdivide(Mesh mesh, int quadWidth, int quadHeight, string windingOrder)
        {
            //Debug.Log("Subdividing main quad");
            Vector3[] verts = mesh.vertices;
            Vector3[] normals = mesh.normals;
            Color[] colors = mesh.colors;
            int originalIndexLength = (int)Mathf.Sqrt(verts.Length);
            int indexLength = originalIndexLength + (originalIndexLength - 1);
            Vector3[] newVerts = new Vector3[indexLength * indexLength];
            Vector3[] newNormals = new Vector3[indexLength * indexLength];
            Color[] newColors = new Color[indexLength * indexLength];
            for (int y = 0; y < quadHeight + 1; y++)
            {
                for (int x = 0; x < quadWidth + 1; x++)
                {
                    int originalIndex = x + y * (quadHeight + 1);
                    int topLeftMost = (x * 2) + y * indexLength * 2;
                    newVerts[topLeftMost] = verts[originalIndex];
                    newNormals[topLeftMost] = normals[originalIndex];
                    newColors[topLeftMost] = colors[originalIndex];
                    Vector3 one = Vector3.up;

                    Vector3 newVert1 = Vector3.zero;
                    Vector3 newVert2 = Vector3.zero;
                    Vector3 newNormal1 = Vector3.zero;
                    Vector3 newNormal2 = Vector3.zero;
                    Color newColor1 = Color.black;
                    Color newColor2 = Color.black;
                    if (x < quadWidth)
                    {
                        newVert1 = (verts[originalIndex] + verts[originalIndex + 1]) / 2;
                        newNormal1 = (normals[originalIndex] + normals[originalIndex + 1]) / 2;
                        newColor1 = (colors[originalIndex] + colors[originalIndex + 1]) / 2;
                        newVerts[topLeftMost + 1] = newVert1;
                        newNormals[topLeftMost + 1] = newNormal1;
                        newColors[topLeftMost + 1] = newColor1;
                    }
                    if (y < quadHeight)
                    {
                        newVerts[topLeftMost + indexLength] = (verts[originalIndex] + verts[originalIndex + originalIndexLength]) / 2;
                        newNormals[topLeftMost + indexLength] = (normals[originalIndex] + normals[originalIndex + originalIndexLength]) / 2;
                        newColors[topLeftMost + indexLength] = (colors[originalIndex] + colors[originalIndex + originalIndexLength]) / 2;
                    }
                    if (x < quadWidth && y < quadHeight)
                    {
                        if (windingOrder == "clockwise")
                        {
                            newVert2 = (verts[originalIndex] + verts[originalIndex + originalIndexLength + 1]) / 2;
                            newVerts[topLeftMost + 1 + indexLength] = newVert2;

                            newNormal2 = (normals[originalIndex] + normals[originalIndex + originalIndexLength + 1]) / 2;
                            newNormals[topLeftMost + 1 + indexLength] = newNormal2;

                            newColor2 = (colors[originalIndex] + colors[originalIndex + originalIndexLength + 1]) / 2;
                            newColors[topLeftMost + 1 + indexLength] = newColor2;
                        }
                        else
                        {
                            newVert2 = (verts[originalIndex + 1] + verts[originalIndex + originalIndexLength]) / 2;
                            newVerts[topLeftMost + 1 + indexLength] = newVert2;

                            newNormal2 = (normals[originalIndex + 1] + normals[originalIndex + originalIndexLength]) / 2;
                            newNormals[topLeftMost + 1 + indexLength] = newNormal2;

                            newColor2 = (colors[originalIndex + 1] + colors[originalIndex + originalIndexLength]) / 2;
                            newColors[topLeftMost + 1 + indexLength] = newColor2;
                        }
                    }

                }
            }
            int[] firstQuadTris = new int[] { mesh.triangles[0], mesh.triangles[1], mesh.triangles[2] };
            mesh.vertices = newVerts;

            mesh.triangles = CalculateTris(quadWidth * 2, quadHeight * 2, firstQuadTris, windingOrder);
            mesh.uv = CalculateUVs(newVerts.Length);
            mesh.normals = newNormals;
            mesh.colors = newColors;
            
            

            return mesh;
        }
        public static Vector2[] CalculateUVs(int vertexCount)
        {
            Vector2[] uvs = new Vector2[vertexCount];
            int indexLength = (int)Mathf.Sqrt(vertexCount);
            for (int i = 0; i < vertexCount; i++)
            {
                int row = i / indexLength;
                int across = i % indexLength;
                uvs[i] = new Vector2((float)across / indexLength, (float)row / indexLength);
            }
            return uvs;
        }
       
        public static int[] CalculateTris(int quadWidth, int quadHeight, int[] firstQuadTris, string windingOrder)
        {
            if (windingOrder == "anticlockwise")
            {
                return CalculateAnticlockwiseTris(quadWidth, quadHeight);
            }
            else
            {
                return CalculateClockwiseTris(quadWidth, quadHeight);
            }
        }
        public static int[] CalculateAnticlockwiseTris(int quadWidth, int quadHeight)
        {
            int[] newTris = new int[quadWidth * quadHeight * 2 * 3];
            for (int y = 0; y < quadHeight; y++)
            {
                for (int x = 0; x < quadWidth; x++)
                {
                    int a = y * (quadWidth + 1) + x;
                    int b = (y + 1) * (quadWidth + 1) + x;
                    int c = b + 1;
                    int d = a + 1;

                    int startIndex = y * quadWidth * 2 * 3 + x * 2 * 3;
                    newTris[startIndex] = d;
                    newTris[startIndex + 1] = a;
                    newTris[startIndex + 2] = b;

                    newTris[startIndex + 3] = b;
                    newTris[startIndex + 4] = c;
                    newTris[startIndex + 5] = d;
                }
            }
            return newTris;
        }
        public static int[] CalculateClockwiseTris(int quadWidth, int quadHeight)
        {
            int[] newTris = new int[quadWidth * quadHeight * 2 * 3];
            for (int y = 0; y < quadHeight; y++)
            {
                for (int x = 0; x < quadWidth; x++)
                {
                    int a = y * (quadWidth + 1) + x;
                    int b = (y + 1) * (quadWidth + 1) + x;
                    int c = b + 1;
                    int d = a + 1;

                    int startIndex = y * quadWidth * 2 * 3 + x * 2 * 3;
                    newTris[startIndex] = a;
                    newTris[startIndex + 1] = b;
                    newTris[startIndex + 2] = c;

                    newTris[startIndex + 3] = c;
                    newTris[startIndex + 4] = d;
                    newTris[startIndex + 5] = a;
                }
            }
            return newTris;
        }
    }
    public class SubQuad : MonoBehaviour
    {
        public int subdivisionLevel = 32;
        public bool furthest = false;
        public bool nearest = false;
        public string situation = "main";
        public int smallestIndex = 0;
        public Vector3 centreWorldPos = Vector3.zero;
        public Vector3[] boundingVertices = new Vector3[0];
        public Vector3[] boundingNormals = new Vector3[0];
        public Color[] boundingColors = new Color[0];
        public Vector3[] cutoutVertices = new Vector3[0];
        int[] cutoutTris = new int[0];
        public int[] indicesToDelete = new int[0];
        public GameObject newQuad;
        public Mesh originalMesh;
        int boundingWidth = 0;
        int boundingHeight = 0;
        public string windingOrder = "clockwise";
        bool meshIsOriginal = false;
        void OnDestroy()
        {
            gameObject.GetComponent<MeshFilter>().mesh = Instantiate(originalMesh);
            Destroy(newQuad);
            Destroy(originalMesh);
        }
        void Start()
        {
            newQuad = new GameObject();
            newQuad.AddComponent<MeshFilter>();
            newQuad.AddComponent<MeshRenderer>();
            newQuad.GetComponent<MeshRenderer>().enabled = true;
            newQuad.GetComponent<MeshRenderer>().sharedMaterial = gameObject.GetComponent<MeshRenderer>().sharedMaterial;
            newQuad.transform.position = gameObject.transform.position;
            newQuad.transform.localScale = gameObject.transform.localScale;
            newQuad.transform.rotation = gameObject.transform.rotation;

            newQuad.layer = gameObject.layer;
            if (newQuad.GetComponent<MeshCollider>() != null)
            {
                Destroy(newQuad.GetComponent<MeshCollider>());
            }
            if (newQuad.GetComponent<Collider>() != null)
            {
                Destroy(newQuad.GetComponent<Collider>());
            }

            originalMesh = Instantiate(gameObject.GetComponent<MeshFilter>().mesh);
        }
        public void ForceUpdate()
        {
            if (newQuad == null || gameObject == null || FlightGlobals.ActiveVessel == null || FlightGlobals.ActiveVessel.velocityD.magnitude > 25f)
            {
                if (newQuad != null)
                {
                    if (newQuad.GetComponent<MeshRenderer>() != null)
                    {
                        newQuad.GetComponent<MeshRenderer>().enabled = false;
                    }
                }
                
                if (meshIsOriginal == false && gameObject.GetComponent<MeshFilter>() != null && gameObject.GetComponent<MeshFilter>().sharedMesh != null)
                {
                    if (originalMesh != null)
                    {
                        gameObject.GetComponent<MeshFilter>().sharedMesh = Instantiate(originalMesh);
                    }
                    meshIsOriginal = true;
                }
                return;
            }
            Ray ray = new Ray();
            RaycastHit hit;
            ray.origin = FlightGlobals.ActiveVessel.transform.position;
            ray.direction = -Vector3.Normalize(FlightGlobals.ActiveVessel.transform.position - FlightGlobals.currentMainBody.transform.position);
            if (UnityEngine.Physics.Raycast(ray, out hit, 50, (int)(1 << 15)))
            {
            }
            else
            {
                newQuad.GetComponent<MeshRenderer>().enabled = false;
                if (meshIsOriginal == false)
                {
                    gameObject.GetComponent<MeshFilter>().sharedMesh = Instantiate(originalMesh);
                    meshIsOriginal = true;
                }

                return;
            }
            newQuad.transform.position = gameObject.transform.position;
            newQuad.GetComponent<MeshRenderer>().enabled = true;
            situation = AdvancedSubdivisionGlobal.situation;
            smallestIndex = AdvancedSubdivisionGlobal.smallestIndex;
            Mesh mesh = Instantiate(originalMesh);
            centreWorldPos = AdvancedSubdivisionGlobal.centreWorldPos;
            if (situation == "main" && newQuad.GetComponent<MeshFilter>().sharedMesh != null)
            {
                
                gameObject.GetComponent<MeshFilter>().sharedMesh = mesh;
                Destroy(newQuad.GetComponent<MeshFilter>().sharedMesh);
            }
            if (situation == "main")
            {
                gameObject.GetComponent<MeshFilter>().sharedMesh = mesh;
                return;
            }
            gameObject.GetComponent<MeshFilter>().sharedMesh = mesh;
            if (situation != "topleft" && situation != "topright" && situation != "bottomleft" && situation != "bottomright" && furthest == true)
            {
                Destroy(newQuad.GetComponent<MeshFilter>().sharedMesh);
                return;
            }
            GetBoundingVertices(mesh);
            int indexLength = (int)(Mathf.Sqrt(mesh.vertexCount));
            int[] ThisQTris = mesh.triangles;
            int[] checkTriArray = new int[] { ThisQTris[0], ThisQTris[1], ThisQTris[2], ThisQTris[3], ThisQTris[4], ThisQTris[5] };
            int[] tris = MeshHandler.CalculateTris(boundingWidth, boundingHeight, checkTriArray, windingOrder);
            mesh.triangles = CutoutTris(indexLength - 1, indexLength - 1, checkTriArray, cutoutTris);
            gameObject.GetComponent<MeshFilter>().sharedMesh = mesh;

            var meshFilter = newQuad.GetComponent<MeshFilter>();
            var quadMesh = new Mesh();
            quadMesh.vertices = boundingVertices;
            quadMesh.normals = boundingNormals;
            quadMesh.colors = boundingColors;
            quadMesh.triangles = tris;
            quadMesh.uv = MeshHandler.CalculateUVs(boundingVertices.Length);
            MeshHelper.Subdivide(quadMesh, subdivisionLevel);
            meshFilter.mesh = quadMesh;
        }

        Vector3[] GetBoundingVertices(Mesh mesh)
        {

            Vector3[] verts = mesh.vertices;
            Vector3[] normals = mesh.normals;
            Color[] colors = mesh.colors;
            int indexLength = (int)(Mathf.Sqrt(mesh.vertexCount));
            string localSituation = "none";
            if (situation == "left" && nearest == true)    //We want the right side of this quad then
            {
                //We are closest and sideways, we need to determine which side we're at
                //Adjacent quad so we only need 4 squares (2 verts x 4 verts)
                int realIndex = smallestIndex + (indexLength - 2) - indexLength;
                boundingVertices = LeftRight(verts, normals, colors, realIndex - 1, indexLength);
                boundingWidth = 1;
                boundingHeight = 3;
                cutoutVertices = new Vector3[] { verts[indicesToDelete[0] + (indexLength - 1)], verts[indicesToDelete[2] + (indexLength - 1)] };
                cutoutTris = new int[] { indicesToDelete[0] + (indexLength - 1), indicesToDelete[2] + (indexLength - 1) };
                localSituation = "left";

            }
            if (situation == "right" && nearest == true)
            {
                int realIndex = smallestIndex - (indexLength * 2) + 2;
                boundingVertices = LeftRight(verts, normals, colors, realIndex - 1, indexLength);
                boundingWidth = 1;
                boundingHeight = 3;
                cutoutVertices = new Vector3[] { verts[indicesToDelete[1] - (indexLength - 1)], verts[indicesToDelete[3] - (indexLength - 1)] };
                cutoutTris = new int[] { indicesToDelete[1] - (indexLength - 1), indicesToDelete[3] - (indexLength - 1) };
                localSituation = "right";
            }
            if (situation == "top" && nearest == true)
            {
                int realIndex = smallestIndex + (indexLength * (indexLength - 2)) - 1;
                boundingVertices = TopBottom(verts, normals, colors, realIndex - 1, indexLength);
                boundingWidth = 3;
                boundingHeight = 1;
                cutoutVertices = new Vector3[] { verts[indicesToDelete[0] + (indexLength * (indexLength - 1))], verts[indicesToDelete[1] + (indexLength * (indexLength - 1))] };
                cutoutTris = new int[] { indicesToDelete[0] + (indexLength * (indexLength - 1)), indicesToDelete[1] + (indexLength * (indexLength - 1)) };
                localSituation = "top";
            }
            if (situation == "bottom" && nearest == true)
            {
                int realIndex = smallestIndex - (indexLength * (indexLength - 2)) - 1;
                boundingVertices = TopBottom(verts, normals, colors, realIndex - 1, indexLength);
                boundingWidth = 3;
                boundingHeight = 1;
                cutoutVertices = new Vector3[] { verts[indicesToDelete[2] - (indexLength * (indexLength - 1))], verts[indicesToDelete[3] - (indexLength * (indexLength - 1))] };
                cutoutTris = new int[] { indicesToDelete[2] - (indexLength * (indexLength - 1)), indicesToDelete[3] - (indexLength * (indexLength - 1)) };
                localSituation = "bottom";
            }

            if (situation == "topleft" && furthest == true)
            {
                int realIndex = indexLength * (indexLength - 2) + indexLength - 2;
                boundingVertices = FarCorner(verts, normals, colors, realIndex - 1, indexLength);
                boundingWidth = 1;
                boundingHeight = 1;
                cutoutVertices = new Vector3[] { verts[indexLength * indexLength - 1] };
                cutoutTris = new int[] { indexLength * indexLength - 1 };
                localSituation = "corner";
            }
            if (situation == "topright" && furthest == true)
            {
                int realIndex = (indexLength * (indexLength - 2));
                boundingVertices = FarCorner(verts, normals, colors, realIndex - 1, indexLength);
                boundingWidth = 1;
                boundingHeight = 1;
                cutoutVertices = new Vector3[] { verts[indexLength * (indexLength - 1)] };
                cutoutTris = new int[] { indexLength * (indexLength - 1) };
                localSituation = "corner";
            }
            if (situation == "bottomleft" && furthest == true)
            {
                int realIndex = indexLength - 2;
                boundingVertices = FarCorner(verts, normals, colors, realIndex - 1, indexLength);
                boundingWidth = 1;
                boundingHeight = 1;
                cutoutVertices = new Vector3[] { verts[indexLength - 1] };
                cutoutTris = new int[] { indexLength - 1 };
                localSituation = "corner";
            }
            if (situation == "bottomright" && furthest == true)
            {
                int realIndex = 0;
                boundingVertices = FarCorner(verts, normals, colors, realIndex - 1, indexLength);
                boundingWidth = 1;
                boundingHeight = 1;
                cutoutVertices = new Vector3[] { verts[0] };
                cutoutTris = new int[] { 0 };
                localSituation = "corner";
            }

            //These are annoying as we cannot get quad relative position - Have to do BOTH and discard the one with the greater distance
            //These are a real bitch to calculate, my head is hurting. I hate tiling. I hate working with meshes. Why do I do this lol
            if (situation == "topleft" && furthest == false)
            {
                //ROOT indices are indexLength - 2 and indexLength * (indexLength - 2)
                int realIndex1 = indexLength * (indexLength - 2);
                int realIndex2 = indexLength - 2;

                float dist1 = Vector3.Distance(centreWorldPos, transform.TransformPoint(verts[realIndex1]));
                float dist2 = Vector3.Distance(centreWorldPos, transform.TransformPoint(verts[realIndex2]));
                if (dist1 >= dist2)
                {
                    //Use realIndex2 - VerticalCorner
                    boundingVertices = VerticalCorner(verts, normals, colors, realIndex2 - 1, indexLength);
                    boundingWidth = 1;
                    boundingHeight = 2;
                    cutoutVertices = new Vector3[] { verts[indexLength - 1], verts[indexLength * 2 - 1] };
                    cutoutTris = new int[] { indexLength - 1, indexLength * 2 - 1 };
                    localSituation = "vertical";
                }
                else
                {
                    //Use realIndex1 - HoriCorner
                    boundingVertices = HorizontalCorner(verts, normals, colors, realIndex1 - 1, indexLength);
                    boundingWidth = 2;
                    boundingHeight = 1;
                    cutoutVertices = new Vector3[] { verts[indexLength * (indexLength - 1)], verts[indexLength * (indexLength - 1) + 1] };
                    cutoutTris = new int[] { indexLength * (indexLength - 1), indexLength * (indexLength - 1) + 1 };
                    localSituation = "horizontal";
                }
            }
            if (situation == "topright" && furthest == false)
            {
                //ROOT indices are 0, indexLength * (IndexLength - 2) - 2
                int realIndex2 = 0;
                int realIndex1 = indexLength * (indexLength - 1) - 3;

                float dist1 = Vector3.Distance(centreWorldPos, transform.TransformPoint(verts[realIndex1]));
                float dist2 = Vector3.Distance(centreWorldPos, transform.TransformPoint(verts[realIndex2]));
                if (dist1 >= dist2)
                {
                    //Use realIndex2 - VerticalCorner
                    boundingVertices = VerticalCorner(verts, normals, colors, realIndex2 - 1, indexLength);
                    boundingWidth = 1;
                    boundingHeight = 2;
                    cutoutVertices = new Vector3[] { verts[0], verts[indexLength] };
                    cutoutTris = new int[] { 0, indexLength };
                    localSituation = "vertical";
                }
                else
                {
                    //Use realIndex1 - HoriCorner
                    boundingVertices = HorizontalCorner(verts, normals, colors, realIndex1 - 1, indexLength);
                    boundingWidth = 2;
                    boundingHeight = 1;
                    cutoutVertices = new Vector3[] { verts[indexLength * indexLength - 1], verts[indexLength * indexLength - 2] };
                    cutoutTris = new int[] { indexLength * indexLength - 1, indexLength * indexLength - 2 };
                    localSituation = "horizontal";
                }
            }
            if (situation == "bottomleft" && furthest == false)
            {
                //ROOT indices are 0 and indexLength * (IndexLength - 2) - 2
                int realIndex1 = 0;
                int realIndex2 = indexLength * (indexLength - 2) - 2;

                float dist1 = Vector3.Distance(centreWorldPos, transform.TransformPoint(verts[realIndex1]));
                float dist2 = Vector3.Distance(centreWorldPos, transform.TransformPoint(verts[realIndex2]));
                if (dist1 >= dist2)
                {
                    //Use realIndex2 - VerticalCorner
                    boundingVertices = VerticalCorner(verts, normals, colors, realIndex2 - 1, indexLength);
                    boundingWidth = 1;
                    boundingHeight = 2;
                    cutoutVertices = new Vector3[] { verts[indexLength * indexLength - 1], verts[indexLength * (indexLength - 1) - 1] };
                    cutoutTris = new int[] { indexLength * indexLength - 1, indexLength * (indexLength - 1) - 1 };
                    localSituation = "vertical";
                }
                else
                {
                    //Use realIndex1 - HoriCorner
                    boundingVertices = HorizontalCorner(verts, normals, colors, realIndex1 - 1, indexLength);
                    boundingWidth = 2;
                    boundingHeight = 1;
                    cutoutVertices = new Vector3[] { verts[0], verts[1] };
                    cutoutTris = new int[] { 0, 1 };
                    localSituation = "horizontal";
                }
            }
            if (situation == "bottomright" && furthest == false)
            {
                //ROOT indices are indexLength - 3 and indexLength * (indexLength - 3)
                int realIndex2 = indexLength * (indexLength - 3);
                int realIndex1 = indexLength - 3;

                float dist1 = Vector3.Distance(centreWorldPos, transform.TransformPoint(verts[realIndex1]));
                float dist2 = Vector3.Distance(centreWorldPos, transform.TransformPoint(verts[realIndex2]));
                if (dist1 >= dist2)
                {
                    //Use realIndex2 - VerticalCorner
                    boundingVertices = VerticalCorner(verts, normals, colors, realIndex2 - 1, indexLength);
                    boundingWidth = 1;
                    boundingHeight = 2;
                    cutoutVertices = new Vector3[] { verts[indexLength * (indexLength - 1)], verts[indexLength * (indexLength - 2)] };
                    cutoutTris = new int[] { indexLength * (indexLength - 1), indexLength * (indexLength - 2) };
                    localSituation = "vertical";
                }
                else
                {
                    //Use realIndex1 - HoriCorner
                    boundingVertices = HorizontalCorner(verts, normals, colors, realIndex1 - 1, indexLength);
                    boundingWidth = 2;
                    boundingHeight = 1;
                    cutoutVertices = new Vector3[] { verts[indexLength - 1], verts[indexLength - 2] };
                    cutoutTris = new int[] { indexLength - 1, indexLength - 2 };
                    localSituation = "horizontal";
                }
            }
            //int[] quadTris = CutoutTris(boundingWidth, boundingHeight);
            return boundingVertices;
            
        }
        public int[] CutoutTris(int quadWidth, int quadHeight, int[] checkTriArray, int[] subQuadTrisToDelete)
        {
            Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;
            int[] newTris = new int[quadWidth * quadHeight * 2 * 3];
            int indexLength = (int)Mathf.Sqrt(mesh.vertices.Length) - 1;
            if (windingOrder == "anticlockwise")
            {
                for (int y = 0; y < quadHeight; y++)
                {
                    for (int x = 0; x < quadWidth; x++)
                    {
                        int a = y * (quadWidth + 1) + x;
                        int b = (y + 1) * (quadWidth + 1) + x;
                        int c = b + 1;
                        int d = a + 1;

                        int startIndex = y * quadWidth * 2 * 3 + x * 2 * 3;

                        if (subQuadTrisToDelete.Contains(a) || subQuadTrisToDelete.Contains(b) || subQuadTrisToDelete.Contains(c) || subQuadTrisToDelete.Contains(d))
                        {
                        }
                        else
                        {
                            //startIndex -= found;

                            newTris[startIndex] = d;
                            newTris[startIndex + 1] = a;
                            newTris[startIndex + 2] = b;

                            newTris[startIndex + 3] = b;
                            newTris[startIndex + 4] = c;
                            newTris[startIndex + 5] = d;
                        }

                    }
                }
            }
            else
            {
                for (int y = 0; y < quadHeight; y++)
                {
                    for (int x = 0; x < quadWidth; x++)
                    {
                        int a = y * (quadWidth + 1) + x;
                        int b = (y + 1) * (quadWidth + 1) + x;
                        int c = b + 1;
                        int d = a + 1;

                        int startIndex = y * quadWidth * 2 * 3 + x * 2 * 3;

                        if (subQuadTrisToDelete.Contains(a) || subQuadTrisToDelete.Contains(b) || subQuadTrisToDelete.Contains(c) || subQuadTrisToDelete.Contains(d))
                        {
                        }
                        else
                        {
                            //startIndex -= found;

                            newTris[startIndex] = a;
                            newTris[startIndex + 1] = b;
                            newTris[startIndex + 2] = c;

                            newTris[startIndex + 3] = c;
                            newTris[startIndex + 4] = d;
                            newTris[startIndex + 5] = a;
                        }

                    }
                }
            }
            
            return newTris;
        }
        public Vector3[] VerticalCorner(Vector3[] verts, Vector3[] normals, Color[] colors, int correctIndex, int indexLength)
        {
            Vector3[] boundingVertices = new Vector3[6];
            boundingNormals = new Vector3[6];
            boundingColors = new Color[6];
            for (int i = 0; i < 6; i++)
            {
                correctIndex += 1;
                if ((i % 2 == 0) && i > 0)
                {
                    correctIndex += (indexLength - 2);  //Down, left 2
                }
                boundingVertices[i] = verts[correctIndex];
                boundingNormals[i] = normals[correctIndex];
                boundingColors[i] = colors[correctIndex];
            }
            return boundingVertices;
        }
        public Vector3[] HorizontalCorner(Vector3[] verts, Vector3[] normals, Color[] colors, int correctIndex, int indexLength)
        {
            Vector3[] boundingVertices = new Vector3[6];
            boundingNormals = new Vector3[6];
            boundingColors = new Color[6];
            for (int i = 0; i < 6; i++)
            {
                correctIndex += 1;
                if ((i % 3 == 0) && i > 0)
                {
                    correctIndex += (indexLength - 3);  //Down, left 4
                }
                boundingVertices[i] = verts[correctIndex];
                boundingNormals[i] = normals[correctIndex];
                boundingColors[i] = colors[correctIndex];
            }
            return boundingVertices;
        }
        public Vector3[] LeftRight(Vector3[] verts, Vector3[] normals, Color[] colors, int correctIndex, int indexLength)
        {
            Vector3[] boundingVertices = new Vector3[8];
            boundingNormals = new Vector3[8];
            boundingColors = new Color[8];
            for (int i = 0; i < 8; i++)
            {
                correctIndex += 1;
                if ((i % 2 == 0) && i > 0)
                {
                    correctIndex += (indexLength - 2);  //Down, left 2
                }
                boundingVertices[i] = verts[correctIndex];
                boundingNormals[i] = normals[correctIndex];
                boundingColors[i] = colors[correctIndex];
            }
            return boundingVertices;
        }
        public Vector3[] TopBottom(Vector3[] verts, Vector3[] normals, Color[] colors, int correctIndex, int indexLength)
        {
            Vector3[] boundingVertices = new Vector3[8];
            boundingNormals = new Vector3[8];
            boundingColors = new Color[8];
            for (int i = 0; i < 8; i++)
            {
                correctIndex += 1;
                if ((i % 4 == 0) && i > 0)
                {
                    correctIndex += (indexLength - 4);  //Down, left 4
                }
                boundingVertices[i] = verts[correctIndex];
                boundingNormals[i] = normals[correctIndex];
                boundingColors[i] = colors[correctIndex];
            }
            return boundingVertices;
        }
        public Vector3[] FarCorner(Vector3[] verts, Vector3[] normals, Color[] colors, int correctIndex, int indexLength)
        {
            correctIndex++;
            Vector3[] boundingVertices = new Vector3[4];
            boundingNormals = new Vector3[4];
            boundingColors = new Color[4];

            boundingVertices[0] = verts[correctIndex];
            boundingVertices[1] = verts[correctIndex + 1];
            boundingVertices[2] = verts[correctIndex + indexLength];
            boundingVertices[3] = verts[correctIndex + indexLength + 1];

            boundingNormals[0] = normals[correctIndex];
            boundingNormals[1] = normals[correctIndex + 1];
            boundingNormals[2] = normals[correctIndex + indexLength];
            boundingNormals[3] = normals[correctIndex + indexLength + 1];

            boundingColors[0] = colors[correctIndex];
            boundingColors[1] = colors[correctIndex + 1];
            boundingColors[2] = colors[correctIndex + indexLength];
            boundingColors[3] = colors[correctIndex + indexLength + 1];

            return boundingVertices;
        }
        public int[] CalculateTris(int count, string situation)
        {
            if (count == 4) //Far corner quad
            {
                int[] tris = new int[6];
                tris[0] = 0;
                tris[1] = 2;
                tris[2] = 3;

                tris[3] = 1;
                tris[4] = 0;
                tris[5] = 3;

                return tris;
            }
            if (count == 6 && situation == "horizontal")
            {
                int[] tris = new int[12];

                tris[0] = 0;
                tris[1] = 3;
                tris[2] = 4;

                tris[3] = 1;
                tris[4] = 0;
                tris[5] = 4;

                tris[6] = 1;
                tris[7] = 4;
                tris[8] = 5;

                tris[9] = 2;
                tris[10] = 1;
                tris[11] = 5;

                return tris;
            }
            if (count == 6 && situation == "vertical")
            {
                int[] tris = new int[12];

                tris[0] = 0;
                tris[1] = 2;
                tris[2] = 3;

                tris[3] = 1;
                tris[4] = 0;
                tris[5] = 3;

                tris[6] = 3;
                tris[7] = 2;
                tris[8] = 5;

                tris[9] = 2;
                tris[10] = 4;
                tris[11] = 5;

                return tris;
            }
            if (count == 8 && (situation == "left" || situation == "right"))
            {
                int[] tris = new int[18];

                tris[0] = 0;
                tris[1] = 2;
                tris[2] = 3;

                tris[3] = 1;
                tris[4] = 0;
                tris[5] = 3;

                tris[6] = 2;
                tris[7] = 4;
                tris[8] = 5;

                tris[9] = 3;
                tris[10] = 2;
                tris[11] = 5;

                tris[12] = 4;
                tris[13] = 6;
                tris[14] = 7;

                tris[15] = 5;
                tris[16] = 4;
                tris[17] = 7;

                return tris;
            }
            if (count == 8 && (situation == "top" || situation == "bottom"))
            {
                int[] tris = new int[18];

                tris[0] = 0;
                tris[1] = 4;
                tris[2] = 5;

                tris[3] = 1;
                tris[4] = 0;
                tris[5] = 5;

                tris[6] = 1;
                tris[7] = 5;
                tris[8] = 6;

                tris[9] = 2;
                tris[10] = 1;
                tris[11] = 6;

                tris[12] = 2;
                tris[13] = 6;
                tris[14] = 7;

                tris[15] = 3;
                tris[16] = 2;
                tris[17] = 7;

                return tris;
            }
            return new int[] { 0, 2, 1 };   //Something went horribly wrong
        }
    }
    public static class MeshHelper
    {
        static List<Vector3> vertices;
        static List<Vector3> normals;
        static List<Color> colors;
        static List<Vector2> uv;
        static List<Vector2> uv2;
        static List<Vector2> uv3;
        static List<Vector4> tangents;

        static List<int> indices;
        static Dictionary<uint, int> newVectices;

        static void InitArrays(Mesh mesh)
        {
            vertices = new List<Vector3>(mesh.vertices);
            normals = new List<Vector3>(mesh.normals);
            tangents = new List<Vector4>(mesh.tangents);
            colors = new List<Color>(mesh.colors);
            uv = new List<Vector2>(mesh.uv);
            uv2 = new List<Vector2>(mesh.uv2);
            uv3 = new List<Vector2>(mesh.uv3);
            indices = new List<int>();
        }
        static void CleanUp()
        {
            vertices = null;
            normals = null;
            colors = null;
            uv = null;
            uv2 = null;
            uv3 = null;
            indices = null;
            tangents = null;
        }

        #region Subdivide4 (2x2)
        static int GetNewVertex4(int i1, int i2)
        {
            int newIndex = vertices.Count;
            uint t1 = ((uint)i1 << 16) | (uint)i2;
            uint t2 = ((uint)i2 << 16) | (uint)i1;
            if (newVectices.ContainsKey(t2))
                return newVectices[t2];
            if (newVectices.ContainsKey(t1))
                return newVectices[t1];

            newVectices.Add(t1, newIndex);

            vertices.Add((vertices[i1] + vertices[i2]) * 0.5f);
            if (normals.Count > 0)
                normals.Add((normals[i1] + normals[i2]).normalized);
            if (tangents.Count > 0)
                tangents.Add((tangents[i1] + tangents[i2]).normalized);
            if (colors.Count > 0)
                colors.Add((colors[i1] + colors[i2]) * 0.5f);
            if (uv.Count > 0)
                uv.Add((uv[i1] + uv[i2]) * 0.5f);
            if (uv2.Count > 0)
                uv2.Add((uv2[i1] + uv2[i2]) * 0.5f);
            if (uv3.Count > 0)
                uv3.Add((uv3[i1] + uv3[i2]) * 0.5f);

            return newIndex;
        }
        public static void Subdivide4(Mesh mesh)
        {
            newVectices = new Dictionary<uint, int>();

            InitArrays(mesh);

            int[] triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int i1 = triangles[i + 0];
                int i2 = triangles[i + 1];
                int i3 = triangles[i + 2];

                int a = GetNewVertex4(i1, i2);
                int b = GetNewVertex4(i2, i3);
                int c = GetNewVertex4(i3, i1);
                indices.Add(i1); indices.Add(a); indices.Add(c);
                indices.Add(i2); indices.Add(b); indices.Add(a);
                indices.Add(i3); indices.Add(c); indices.Add(b);
                indices.Add(a); indices.Add(b); indices.Add(c); // center triangle
            }
            mesh.Clear();
            mesh.vertices = vertices.ToArray();
            if (normals.Count > 0)
                mesh.normals = normals.ToArray();
            if (colors.Count > 0)
                mesh.colors = colors.ToArray();
            if (uv.Count > 0)
                mesh.uv = uv.ToArray();
            if (uv2.Count > 0)
                mesh.uv2 = uv2.ToArray();
            if (uv3.Count > 0)
                mesh.uv3 = uv3.ToArray();
            if (tangents.Count > 0)
                mesh.tangents = tangents.ToArray();

            mesh.triangles = indices.ToArray();

            CleanUp();
            return;
        }
        #endregion Subdivide4 (2x2)

        #region Subdivide9 (3x3)
        static int GetNewVertex9(int i1, int i2, int i3)
        {
            int newIndex = vertices.Count;

            // center points don't go into the edge list
            if (i3 == i1 || i3 == i2)
            {
                uint t1 = ((uint)i1 << 16) | (uint)i2;
                if (newVectices.ContainsKey(t1))
                    return newVectices[t1];
                newVectices.Add(t1, newIndex);
            }

            // calculate new vertex
            vertices.Add((vertices[i1] + vertices[i2] + vertices[i3]) / 3.0f);
            if (normals.Count > 0)
                normals.Add((normals[i1] + normals[i2] + normals[i3]).normalized);
            if (tangents.Count > 0)
                tangents.Add((tangents[i1] + tangents[i2] + tangents[i3]).normalized);
            if (colors.Count > 0)
                colors.Add((colors[i1] + colors[i2] + colors[i3]) / 3.0f);
            if (uv.Count > 0)
                uv.Add((uv[i1] + uv[i2] + uv[i3]) / 3.0f);
            if (uv2.Count > 0)
                uv2.Add((uv2[i1] + uv2[i2] + uv2[i3]) / 3.0f);
            if (uv3.Count > 0)
                uv3.Add((uv3[i1] + uv3[i2] + uv3[i3]) / 3.0f);
            return newIndex;
        }
        public static void Subdivide9(Mesh mesh)
        {
            newVectices = new Dictionary<uint, int>();

            InitArrays(mesh);

            int[] triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int i1 = triangles[i + 0];
                int i2 = triangles[i + 1];
                int i3 = triangles[i + 2];

                int a1 = GetNewVertex9(i1, i2, i1);
                int a2 = GetNewVertex9(i2, i1, i2);
                int b1 = GetNewVertex9(i2, i3, i2);
                int b2 = GetNewVertex9(i3, i2, i3);
                int c1 = GetNewVertex9(i3, i1, i3);
                int c2 = GetNewVertex9(i1, i3, i1);

                int d = GetNewVertex9(i1, i2, i3);

                indices.Add(i1); indices.Add(a1); indices.Add(c2);
                indices.Add(i2); indices.Add(b1); indices.Add(a2);
                indices.Add(i3); indices.Add(c1); indices.Add(b2);
                indices.Add(d); indices.Add(a1); indices.Add(a2);
                indices.Add(d); indices.Add(b1); indices.Add(b2);
                indices.Add(d); indices.Add(c1); indices.Add(c2);
                indices.Add(d); indices.Add(c2); indices.Add(a1);
                indices.Add(d); indices.Add(a2); indices.Add(b1);
                indices.Add(d); indices.Add(b2); indices.Add(c1);
            }
            mesh.Clear();
            mesh.vertices = vertices.ToArray();
            if (normals.Count > 0)
                mesh.normals = normals.ToArray();
            if (colors.Count > 0)
                mesh.colors = colors.ToArray();
            if (uv.Count > 0)
                mesh.uv = uv.ToArray();
            if (uv2.Count > 0)
                mesh.uv2 = uv2.ToArray();
            if (uv3.Count > 0)
                mesh.uv3 = uv3.ToArray();
            if (tangents.Count > 0)
                mesh.tangents = tangents.ToArray();

            mesh.triangles = indices.ToArray();

            CleanUp();
        }
        #endregion Subdivide9 (3x3)
        public static void Subdivide(Mesh mesh, int level)
        {
            if (level < 2)
                return;
            while (level > 1)
            {
                // remove prime factor 3
                while (level % 3 == 0)
                {
                    Subdivide9(mesh);
                    level /= 3;
                }
                // remove prime factor 2
                while (level % 2 == 0)
                {
                    Subdivide4(mesh);
                    level /= 2;
                }
                // try to approximate. All other primes are increased by one
                // so they can be processed
                if (level > 3)
                    level++;
            }

        }
    }
}
