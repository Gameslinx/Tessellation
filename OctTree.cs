using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;

namespace Grass //Use an octree to quickly get the nearby points to the craft
{
    public class LineController : MonoBehaviour
    {
        private LineRenderer lr;
        private List<Vector3> points = new List<Vector3>();
        public float maxObjects = 0;
        public void Begin()
        {
            lr = gameObject.AddComponent<LineRenderer>();
            lr.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
            lr.material.SetColor("_Color", new Color(1, 0, 0, 1));
            lr.widthMultiplier = 1f;
            lr.startColor = Color.red;
            lr.endColor = Color.red;
            lr.useWorldSpace = true;
            //lr.transform.parent = gameObject.transform;
        }
        public void New()
        {
            points.Clear();
            lr.positionCount = 0;
        }
        public void SetUpLine(Vector3[] points)
        {
            lr.positionCount += points.Length;
            for (int i = 0; i < points.Length; i++)
            {
                this.points.Add(points[i]);
            }
        }
        public void Update()
        {
            for (int i = 0; i < points.Count; i++)
            {
                lr.SetPosition(i, points[i]);
            }
        }
        public void AddPoint(Vector3 point)
        {
            points.Add(point);
            lr.positionCount++;
        }
        public float GetPercentage()
        {
            return points.Count / maxObjects;
        }
        public void Recreate(List<Vector3> points)
        {
            lr.positionCount = points.Count;
            this.points = points;
        }
        void OnDestroy()
        {
            Destroy(gameObject.GetComponent<LineRenderer>());
        }
    }
    public static class OctTreeUtils
    {
        public static OctBoundingBox GetBounds(Bounds bounds)
        {
            var p1 = new Vector3(bounds.min.x, bounds.min.y, bounds.min.z);
            var p3 = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);
            var p4 = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);
            var p5 = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);

            Vector3 length = p4 - p1;
            Vector3 width = p4 - p3;
            Vector3 height = p5 - p1;

            return new OctBoundingBox(bounds.center, length / 2, width / 2, height / 2);
        }
    }

    public struct OctBoundingBox
    {
        public Vector3 center;
        public Vector3 halfLength;
        public Vector3 halfWidth;
        public Vector3 halfDepth;
        public OctBoundingBox(Vector3 center, Vector3 halfLength, Vector3 halfWidth, Vector3 halfDepth)
        {
            this.center = center;
            this.halfLength = halfLength;
            this.halfWidth = halfWidth;
            this.halfDepth = halfDepth;
        }
        public bool ContainsPoint(Vector3 point)    //If contained within the coordinate system, this is easy to calculate
        {
            if (!(Vector3.Dot((center - halfLength - halfWidth - halfDepth) - (center - halfLength + halfWidth - halfDepth), (center - halfLength + halfWidth - halfDepth)) < Vector3.Dot((center - halfLength - halfWidth - halfDepth) - (center - halfLength + halfWidth - halfDepth), point) && Vector3.Dot((center - halfLength - halfWidth - halfDepth) - (center - halfLength + halfWidth - halfDepth), point) < Vector3.Dot((center - halfLength - halfWidth - halfDepth) - (center - halfLength + halfWidth - halfDepth), (center - halfLength - halfWidth - halfDepth)))) { return false; }
            if (!(Vector3.Dot((center - halfLength - halfWidth - halfDepth) - (center + halfLength - halfWidth - halfDepth), (center + halfLength - halfWidth - halfDepth)) < Vector3.Dot((center - halfLength - halfWidth - halfDepth) - (center + halfLength - halfWidth - halfDepth), point) && Vector3.Dot((center - halfLength - halfWidth - halfDepth) - (center + halfLength - halfWidth - halfDepth), point) < Vector3.Dot((center - halfLength - halfWidth - halfDepth) - (center + halfLength - halfWidth - halfDepth), (center - halfLength - halfWidth - halfDepth)))) { return false; }
            if (!(Vector3.Dot((center - halfLength - halfWidth - halfDepth) - (center - halfLength - halfWidth + halfDepth), (center - halfLength - halfWidth + halfDepth)) < Vector3.Dot((center - halfLength - halfWidth - halfDepth) - (center - halfLength - halfWidth + halfDepth), point) && Vector3.Dot((center - halfLength - halfWidth - halfDepth) - (center - halfLength - halfWidth + halfDepth), point) < Vector3.Dot((center - halfLength - halfWidth - halfDepth) - (center - halfLength - halfWidth + halfDepth), (center - halfLength - halfWidth - halfDepth)))) { return false; }

            //Here is the code for what happens above
            //I chose for the above approach simply for garbage optimization which sped up tree generation by about 10x
            //It's still not perfect, and there is still a lot of garbage when inserting objects into the tree, but it happens only once so I just
            //deal with it

            //Vector3 p1 = (center - halfLength - halfWidth - halfDepth);
            //Vector3 p2 = (center - halfLength + halfWidth - halfDepth);
            //Vector3 p4 = (center + halfLength - halfWidth - halfDepth);
            //Vector3 p5 = (center - halfLength - halfWidth + halfDepth);
            //
            //Vector3 u = p1 - p2;
            //Vector3 v = p1 - p4;
            //Vector3 w = p1 - p5;
            //
            //float ux = Vector3.Dot(u, point);
            //float vx = Vector3.Dot(v, point);
            //float wx = Vector3.Dot(w, point);
            //
            //float up1 = Vector3.Dot(u, p1);
            //float up2 = Vector3.Dot(u, p2);
            //
            //float vp1 = Vector3.Dot(v, p1);
            //float vp4 = Vector3.Dot(v, p4);
            //
            //float wp1 = Vector3.Dot(w, p1);
            //float wp5 = Vector3.Dot(w, p5);
            //
            //if (!(up2 < ux && ux < up1)) { return false; }
            //if (!(vp4 < vx && vx < vp1)) { return false; }
            //if (!(wp5 < wx && wx < wp1)) { return false; }

            return true;
        }
        public bool FastContainsPoint(Vector3 point, float meshBound)    //Just a simple distance calculation
        {
            float diagDist = ((center) - (center + halfLength + halfWidth + halfDepth)).sqrMagnitude + meshBound;
            if ((point - center).sqrMagnitude < diagDist) { return true; }
            return false;
        }
        public bool IntersectsBounds(OctBoundingBox searchBounds)  //Would be faster if octree was axis aligned. This is approximate, favouring speed over accuracy
        {
            float diag = Vector3.Magnitude(halfLength + halfWidth + halfDepth);
            float searchDiag = Vector3.Magnitude(searchBounds.halfLength + searchBounds.halfWidth + searchBounds.halfDepth);
            float distance = (searchBounds.center - center).sqrMagnitude;
            float lim = diag + searchDiag;
            if (distance < (lim * lim)) { return true; }
            return false;
        }
        public void DrawBounds(int subdivisionLevel, PQ quad)   //For debug purposes only
        {
            float level = subdivisionLevel;

            var lc = quad.gameObject.GetComponent<LineController>();
            lc.AddPoint(center - halfLength + halfWidth + halfDepth);
            lc.AddPoint(center - halfLength - halfWidth + halfDepth);
            lc.AddPoint(center + halfLength - halfWidth + halfDepth);
            lc.AddPoint(center + halfLength + halfWidth + halfDepth);
            lc.AddPoint(center - halfLength + halfWidth + halfDepth);
            lc.AddPoint(center - halfLength + halfWidth - halfDepth);
            lc.AddPoint(center - halfLength - halfWidth - halfDepth);
            lc.AddPoint(center + halfLength - halfWidth - halfDepth);
            lc.AddPoint(center + halfLength + halfWidth - halfDepth);
            lc.AddPoint(center - halfLength + halfWidth - halfDepth);
            lc.AddPoint(center + halfLength + halfWidth - halfDepth);
            lc.AddPoint(center + halfLength + halfWidth + halfDepth);
            lc.AddPoint(center + halfLength + halfWidth - halfDepth);
            lc.AddPoint(center + halfLength - halfWidth - halfDepth);
            lc.AddPoint(center + halfLength - halfWidth + halfDepth);
            lc.AddPoint(center + halfLength - halfWidth - halfDepth);
            lc.AddPoint(center - halfLength - halfWidth - halfDepth);
            lc.AddPoint(center - halfLength - halfWidth + halfDepth);
            lc.AddPoint(center - halfLength - halfWidth - halfDepth);
        }
    }
    public class OctTree
    {
        OctBoundingBox bounds;
        int nodeCapacity = 4;
        //List<Vector3> points = new List<Vector3>();
        List<int> indices = new List<int>();
        OctTree upperTopLeft;
        OctTree upperTopRight;
        OctTree upperBottomLeft;
        OctTree upperBottomRight;
        OctTree lowerTopLeft;
        OctTree lowerTopRight;
        OctTree lowerBottomLeft;
        OctTree lowerBottomRight;
        bool subdivided = false;
        PQ quad;

        public OctTree(OctBoundingBox bounds, PQ quad)
        {
            this.bounds = bounds;
            this.quad = quad;
        }
        public bool Insert(ref Vector3 point, int index)
        {
            if (!bounds.ContainsPoint(point)) { return false; }
            if (indices.Count < nodeCapacity && !subdivided)
            {
                indices.Add(index);
                //GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                //go.GetComponent<Collider>().enabled = false;
                //go.transform.position = point;
                //go.transform.localScale = Vector3.one * 9;
                //go.transform.parent = quad.transform;
                return true;
            }
            if (!subdivided)
            {
                Subdivide();
            }

            if (upperTopLeft.Insert(ref point, index)) { return true; }
            if (upperTopRight.Insert(ref point, index)) { return true; }
            if (upperBottomLeft.Insert(ref point, index)) { return true; }
            if (upperBottomRight.Insert(ref point, index)) { return true; }

            if (lowerTopLeft.Insert(ref point, index)) { return true; }
            if (lowerTopRight.Insert(ref point, index)) { return true; }
            if (lowerBottomLeft.Insert(ref point, index)) { return true; }
            if (lowerBottomRight.Insert(ref point, index)) { return true; }

            return false;   //Something messed up real bad
        }
        public void Subdivide()
        {
            upperTopLeft = new OctTree(new OctBoundingBox(bounds.center - bounds.halfLength / 2 + bounds.halfWidth / 2 + bounds.halfDepth / 2, bounds.halfLength / 2, bounds.halfWidth / 2, bounds.halfDepth / 2),  quad);
            upperTopRight = new OctTree(new OctBoundingBox(bounds.center + bounds.halfLength / 2 + bounds.halfWidth / 2 + bounds.halfDepth / 2, bounds.halfLength / 2, bounds.halfWidth / 2, bounds.halfDepth / 2),  quad);
            upperBottomLeft = new OctTree(new OctBoundingBox(bounds.center - bounds.halfLength / 2 - bounds.halfWidth / 2 + bounds.halfDepth / 2, bounds.halfLength / 2, bounds.halfWidth / 2, bounds.halfDepth / 2),  quad);
            upperBottomRight = new OctTree(new OctBoundingBox(bounds.center + bounds.halfLength / 2 - bounds.halfWidth / 2 + bounds.halfDepth / 2, bounds.halfLength / 2, bounds.halfWidth / 2, bounds.halfDepth / 2),  quad);

            lowerTopLeft = new OctTree(new OctBoundingBox(bounds.center - bounds.halfLength / 2 + bounds.halfWidth / 2 - bounds.halfDepth / 2, bounds.halfLength / 2, bounds.halfWidth / 2, bounds.halfDepth / 2),  quad);
            lowerTopRight = new OctTree(new OctBoundingBox(bounds.center + bounds.halfLength / 2 + bounds.halfWidth / 2 - bounds.halfDepth / 2, bounds.halfLength / 2, bounds.halfWidth / 2, bounds.halfDepth / 2),  quad);
            lowerBottomLeft = new OctTree(new OctBoundingBox(bounds.center - bounds.halfLength / 2 - bounds.halfWidth / 2 - bounds.halfDepth / 2, bounds.halfLength / 2, bounds.halfWidth / 2, bounds.halfDepth / 2),  quad);
            lowerBottomRight = new OctTree(new OctBoundingBox(bounds.center + bounds.halfLength / 2 - bounds.halfWidth / 2 - bounds.halfDepth / 2, bounds.halfLength / 2, bounds.halfWidth / 2, bounds.halfDepth / 2),  quad);

            subdivided = true;
        }
        public void QueryRange(ref OctBoundingBox range, ref List<Vector3> pointsInRange)
        {
            if (!bounds.IntersectsBounds(range)) { return; }
            for (int i = 0; i < indices.Count; i++)
            {
                Position pos = QuadColliderData.data[quad][indices[i]];
                if (range.FastContainsPoint(pos.worldPos, pos.bound))
                {
                    pointsInRange.Add(pos.worldPos);
                    pos.CreateGameObject();
                }
            }
            if (!subdivided) { return; }
            upperTopLeft.QueryRange(ref range, ref pointsInRange);
            upperTopRight.QueryRange(ref range, ref pointsInRange);
            upperBottomLeft.QueryRange(ref range, ref pointsInRange);
            upperBottomRight.QueryRange(ref range, ref pointsInRange);

            lowerTopLeft.QueryRange(ref range, ref pointsInRange);
            lowerTopRight.QueryRange(ref range, ref pointsInRange);
            lowerBottomLeft.QueryRange(ref range, ref pointsInRange);
            lowerBottomRight.QueryRange(ref range, ref pointsInRange);
        }
        public void DrawBounds()
        {
           bounds.DrawBounds(1, quad);
           if (subdivided)
           {
               upperTopLeft.DrawBounds();
               upperTopRight.DrawBounds();
               upperBottomLeft.DrawBounds();
               upperBottomRight.DrawBounds();
           
               lowerTopLeft.DrawBounds();
               lowerTopRight.DrawBounds();
               lowerBottomLeft.DrawBounds();
               lowerBottomRight.DrawBounds();
           }
        }
    }

    //public struct OctBoundingBox
    //{
    //    public Vector3 center;
    //    public Vector3 halfLength;
    //    public Vector3 halfWidth;
    //    public Vector3 halfDepth;
    //    public OctBoundingBox(Vector3 center, Vector3 halfLength, Vector3 halfWidth, Vector3 halfDepth)
    //    {
    //        this.center = center;
    //        this.halfLength = halfLength;
    //        this.halfWidth = halfWidth;
    //        this.halfDepth = halfDepth;
    //    }
    //    public bool ContainsPoint(Vector3 point)    //If contained within the coordinate system, this is easy to calculate
    //    {
    //
    //        Vector3 p1 = (center - halfLength - halfWidth - halfDepth);
    //        Vector3 p2 = (center - halfLength + halfWidth - halfDepth);
    //        Vector3 p4 = (center + halfLength - halfWidth - halfDepth);
    //        Vector3 p5 = (center - halfLength - halfWidth + halfDepth);
    //
    //        Vector3 u = p1 - p2;
    //        Vector3 v = p1 - p4;
    //        Vector3 w = p1 - p5;
    //
    //        float ux = Vector3.Dot(u, point);
    //        float vx = Vector3.Dot(v, point);
    //        float wx = Vector3.Dot(w, point);
    //
    //        float up1 = Vector3.Dot(u, p1);
    //        float up2 = Vector3.Dot(u, p2);
    //
    //        float vp1 = Vector3.Dot(v, p1);
    //        float vp4 = Vector3.Dot(v, p4);
    //
    //        float wp1 = Vector3.Dot(w, p1);
    //        float wp5 = Vector3.Dot(w, p5);
    //
    //        if (!(up2 < ux && ux < up1)) { return false; }
    //        if (!(vp4 < vx && vx < vp1)) { return false; }
    //        if (!(wp5 < wx && wx < wp1)) { return false; }
    //
    //        return true;
    //    }
    //
    //    public bool IntersectsBounds(OctBoundingBox searchBounds, Transform otw)
    //    {
    //        float diag = Vector3.Distance(otw.localToWorldMatrix.MultiplyPoint(center), otw.localToWorldMatrix.MultiplyPoint(center - halfLength + halfWidth + halfDepth));
    //        float searchDiag = Vector3.Distance(searchBounds.center, searchBounds.center - searchBounds.halfLength + searchBounds.halfWidth + searchBounds.halfDepth);
    //        float distance = Vector3.Distance(searchBounds.center, otw.localToWorldMatrix.MultiplyPoint(center));
    //        if (distance < diag + searchDiag) { return true; }
    //        return false;
    //    }
    //    public void DrawBounds(Transform otw)
    //    {
    //        Vector3[] points = new Vector3[10];
    //
    //        points[0] = otw.localToWorldMatrix.MultiplyPoint(center - halfLength + halfWidth + halfDepth);
    //        points[1] = otw.localToWorldMatrix.MultiplyPoint(center - halfLength - halfWidth + halfDepth);
    //        points[2] = otw.localToWorldMatrix.MultiplyPoint(center + halfLength - halfWidth + halfDepth);
    //        points[3] = otw.localToWorldMatrix.MultiplyPoint(center + halfLength + halfWidth + halfDepth);
    //
    //        points[4] = otw.localToWorldMatrix.MultiplyPoint(center - halfLength + halfWidth + halfDepth);
    //
    //        points[5] = otw.localToWorldMatrix.MultiplyPoint(center - halfLength + halfWidth - halfDepth);
    //        points[6] = otw.localToWorldMatrix.MultiplyPoint(center - halfLength - halfWidth - halfDepth);
    //        points[7] = otw.localToWorldMatrix.MultiplyPoint(center + halfLength - halfWidth - halfDepth);
    //        points[8] = otw.localToWorldMatrix.MultiplyPoint(center + halfLength + halfWidth - halfDepth);
    //
    //        points[9] = otw.localToWorldMatrix.MultiplyPoint(center - halfLength + halfWidth - halfDepth);
    //
    //        LineController.Instance.SetUpLine(points);
    //
    //        //Gizmos.DrawLine(otw.localToWorldMatrix.MultiplyPoint(center + halfLength + halfWidth + halfDepth), otw.localToWorldMatrix.MultiplyPoint(center - halfLength + halfWidth + halfDepth));
    //        //Gizmos.DrawLine(otw.localToWorldMatrix.MultiplyPoint(center + halfLength - halfWidth + halfDepth), otw.localToWorldMatrix.MultiplyPoint(center + halfLength + halfWidth + halfDepth));
    //        //Gizmos.DrawLine(otw.localToWorldMatrix.MultiplyPoint(center - halfLength - halfWidth + halfDepth), otw.localToWorldMatrix.MultiplyPoint(center + halfLength - halfWidth + halfDepth));
    //        //Gizmos.DrawLine(otw.localToWorldMatrix.MultiplyPoint(center - halfLength - halfWidth + halfDepth), otw.localToWorldMatrix.MultiplyPoint(center - halfLength + halfWidth + halfDepth));
    //        //
    //        //Gizmos.DrawLine(otw.localToWorldMatrix.MultiplyPoint(center + halfLength + halfWidth - halfDepth), otw.localToWorldMatrix.MultiplyPoint(center - halfLength + halfWidth - halfDepth));
    //        //Gizmos.DrawLine(otw.localToWorldMatrix.MultiplyPoint(center + halfLength - halfWidth - halfDepth), otw.localToWorldMatrix.MultiplyPoint(center + halfLength + halfWidth - halfDepth));
    //        //Gizmos.DrawLine(otw.localToWorldMatrix.MultiplyPoint(center - halfLength - halfWidth - halfDepth), otw.localToWorldMatrix.MultiplyPoint(center + halfLength - halfWidth - halfDepth));
    //        //Gizmos.DrawLine(otw.localToWorldMatrix.MultiplyPoint(center - halfLength - halfWidth - halfDepth), otw.localToWorldMatrix.MultiplyPoint(center - halfLength + halfWidth - halfDepth));
    //        //
    //        //Gizmos.DrawLine(otw.localToWorldMatrix.MultiplyPoint(center - halfLength + halfWidth + halfDepth), otw.localToWorldMatrix.MultiplyPoint(center - halfLength + halfWidth - halfDepth));
    //        //Gizmos.DrawLine(otw.localToWorldMatrix.MultiplyPoint(center + halfLength + halfWidth + halfDepth), otw.localToWorldMatrix.MultiplyPoint(center + halfLength + halfWidth - halfDepth));
    //        //Gizmos.DrawLine(otw.localToWorldMatrix.MultiplyPoint(center - halfLength - halfWidth + halfDepth), otw.localToWorldMatrix.MultiplyPoint(center - halfLength - halfWidth - halfDepth));
    //        //Gizmos.DrawLine(otw.localToWorldMatrix.MultiplyPoint(center + halfLength - halfWidth + halfDepth), otw.localToWorldMatrix.MultiplyPoint(center + halfLength - halfWidth - halfDepth));
    //        //Gizmos.color = Color.green;
    //    }
    //}
    //public class OctTree
    //{
    //    float planetRadius;
    //    OctBoundingBox bounds;
    //    int nodeCapacity = 4;
    //    List<Vector3> points = new List<Vector3>();
    //    OctTree upperTopLeft;
    //    OctTree upperTopRight;
    //    OctTree upperBottomLeft;
    //    OctTree upperBottomRight;
    //    OctTree lowerTopLeft;
    //    OctTree lowerTopRight;
    //    OctTree lowerBottomLeft;
    //    OctTree lowerBottomRight;
    //    bool subdivided = false;
    //    Transform otw;
    //    public OctTree(OctBoundingBox bounds, Transform otw)
    //    {
    //        this.bounds = bounds;
    //        this.otw = otw;
    //    }
    //    public bool Insert(Vector3 point)
    //    {
    //        if (!bounds.ContainsPoint(point)) { return false; }
    //        if (points.Count < nodeCapacity && !subdivided)
    //        {
    //            points.Add(point);
    //            return true;
    //        }
    //        if (!subdivided)
    //        {
    //            Subdivide();
    //        }
    //
    //        if (upperTopLeft.Insert(point)) { return true; }
    //        if (upperTopRight.Insert(point)) { return true; }
    //        if (upperBottomLeft.Insert(point)) { return true; }
    //        if (upperBottomRight.Insert(point)) { return true; }
    //
    //        if (lowerTopLeft.Insert(point)) { return true; }
    //        if (lowerTopRight.Insert(point)) { return true; }
    //        if (lowerBottomLeft.Insert(point)) { return true; }
    //        if (lowerBottomRight.Insert(point)) { return true; }
    //
    //        return false;   //Something fucked up real bad
    //    }
    //    public void Subdivide()
    //    {
    //        upperTopLeft = new OctTree(new OctBoundingBox(bounds.center - bounds.halfLength / 2 + bounds.halfWidth / 2 + bounds.halfDepth / 2, bounds.halfLength / 2, bounds.halfWidth / 2, bounds.halfDepth / 2), otw);
    //        upperTopRight = new OctTree(new OctBoundingBox(bounds.center + bounds.halfLength / 2 + bounds.halfWidth / 2 + bounds.halfDepth / 2, bounds.halfLength / 2, bounds.halfWidth / 2, bounds.halfDepth / 2), otw);
    //        upperBottomLeft = new OctTree(new OctBoundingBox(bounds.center - bounds.halfLength / 2 - bounds.halfWidth / 2 + bounds.halfDepth / 2, bounds.halfLength / 2, bounds.halfWidth / 2, bounds.halfDepth / 2), otw);
    //        upperBottomRight = new OctTree(new OctBoundingBox(bounds.center + bounds.halfLength / 2 - bounds.halfWidth / 2 + bounds.halfDepth / 2, bounds.halfLength / 2, bounds.halfWidth / 2, bounds.halfDepth / 2), otw);
    //
    //        lowerTopLeft = new OctTree(new OctBoundingBox(bounds.center - bounds.halfLength / 2 + bounds.halfWidth / 2 - bounds.halfDepth / 2, bounds.halfLength / 2, bounds.halfWidth / 2, bounds.halfDepth / 2), otw);
    //        lowerTopRight = new OctTree(new OctBoundingBox(bounds.center + bounds.halfLength / 2 + bounds.halfWidth / 2 - bounds.halfDepth / 2, bounds.halfLength / 2, bounds.halfWidth / 2, bounds.halfDepth / 2), otw);
    //        lowerBottomLeft = new OctTree(new OctBoundingBox(bounds.center - bounds.halfLength / 2 - bounds.halfWidth / 2 - bounds.halfDepth / 2, bounds.halfLength / 2, bounds.halfWidth / 2, bounds.halfDepth / 2), otw);
    //        lowerBottomRight = new OctTree(new OctBoundingBox(bounds.center + bounds.halfLength / 2 - bounds.halfWidth / 2 - bounds.halfDepth / 2, bounds.halfLength / 2, bounds.halfWidth / 2, bounds.halfDepth / 2), otw);
    //
    //        subdivided = true;
    //    }
    //    public void QueryRange(OctBoundingBox range, ref List<Vector3> pointsInRange)
    //    {
    //        if (!bounds.IntersectsBounds(range, otw)) { return; }
    //        for (int i = 0; i < points.Count; i++)
    //        {
    //            if (range.ContainsPoint(points[i]))
    //            {
    //                pointsInRange.Add(points[i]);
    //            }
    //        }
    //        if (!subdivided) { return; }
    //
    //        upperTopLeft.QueryRange(range, ref pointsInRange);
    //        upperTopRight.QueryRange(range, ref pointsInRange);
    //        upperBottomLeft.QueryRange(range, ref pointsInRange);
    //        upperBottomRight.QueryRange(range, ref pointsInRange);
    //
    //        lowerTopLeft.QueryRange(range, ref pointsInRange);
    //        lowerTopRight.QueryRange(range, ref pointsInRange);
    //        lowerBottomLeft.QueryRange(range, ref pointsInRange);
    //        lowerBottomRight.QueryRange(range, ref pointsInRange);
    //    }
    //    public void DrawBounds()
    //    {
    //        bounds.DrawBounds(otw);
    //        if (subdivided)
    //        {
    //            upperTopLeft.DrawBounds();
    //            upperTopRight.DrawBounds();
    //            upperBottomLeft.DrawBounds();
    //            upperBottomRight.DrawBounds();
    //
    //            lowerTopLeft.DrawBounds();
    //            lowerTopRight.DrawBounds();
    //            lowerBottomLeft.DrawBounds();
    //            lowerBottomRight.DrawBounds();
    //        }
    //    }
    //    //public void DrawPoints()
    //    //{
    //    //    foreach (Vector3 point in points)
    //    //    {
    //    //        Gizmos.DrawSphere(point, 0.1f);
    //    //    }
    //    //
    //    //    if (subdivided)
    //    //    {
    //    //        topLeft.DrawPoints();
    //    //        topRight.DrawPoints();
    //    //        bottomLeft.DrawPoints();
    //    //        bottomRight.DrawPoints();
    //    //    }
    //    //}
    //}
}
