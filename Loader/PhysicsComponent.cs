using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;


public class Physics : MonoBehaviour
{
    // Start is called before the first frame update
    GameObject plane;
    public LayerMask ignore;
    private Ray rayPosY;
    private Ray rayNegY;
    private Ray rayPosX;
    private Ray rayNegX;
    private Ray rayPosZ;
    private Ray rayNegZ;
    private Ray approximateRay;
    private RaycastHit hitPosY;
    private RaycastHit hitNegY;
    private RaycastHit hitPosX;
    private RaycastHit hitNegX;
    private RaycastHit hitPosZ;
    private RaycastHit hitNegZ;
    private RaycastHit hitApproximateRay;
    Vector3 samplePoint;
    Vector3 nextPoint;
    Vector3 sampleNormal;
    Vector2 _ST;
    Texture2D tex;
    Texture2D normalLow;
    Texture2D normalMid;
    Texture2D normalHigh;
    Texture2D normalSteep;
    float lastDisplacement = 10000;
    Vector3 camDisplacement;
    float _Displacement_Scale = 0;
    Vector3 refVel1 = Vector3.zero;
    Vector3 refVel2 = Vector3.zero;
    Vector3 smoothDampRotation = Vector3.zero;
    Vector3 normalDir = Vector3.zero;
    float minDistance = 10;
    Vector3 planeVelocity = Vector3.zero;
    float blendLowStart;
    float blendLowEnd;
    float blendHighStart;
    float blendHighEnd;
    float steepPower;
    Vector3 planetOrigin;
    float planetRadius;
    void Start()
    {

        
        plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        //plane.GetComponent<MeshRenderer>().enabled = false;

        //rayCaster.transform.localScale = new Vector3(0.00001f, 0.00001f, 0.00001f);
        plane.SetActive(true);
        plane.layer = 2;
        plane.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        plane.transform.rotation = Quaternion.Euler(0f, 0, 0f);
        Material a = GameObject.Find("Terrain").GetComponent<Terrain>().materialTemplate;
        tex = (a.GetTexture("_DispTex") as Texture2D);
        normalLow = (a.GetTexture("_BumpMap") as Texture2D);
        normalMid = (a.GetTexture("_BumpMapMid") as Texture2D);
        normalHigh = (a.GetTexture("_BumpMapHigh") as Texture2D);
        normalSteep = (a.GetTexture("_BumpMapSteep") as Texture2D);
        _ST = (a.GetTextureScale("_SurfaceTexture"));
        blendLowStart = a.GetFloat("_LowStart");
        blendLowEnd = a.GetFloat("_LowEnd");
        blendHighStart = a.GetFloat("_HighStart");
        blendHighEnd = a.GetFloat("_HighEnd");
        steepPower = a.GetFloat("_SteepPower");
        planetOrigin = a.GetVector("_PlanetOrigin");
        planetRadius = a.GetFloat("_PlanetRadius");

        Debug.Log(tex.width);
        //Camera.main.transform.parent = plane.transform;
        camDisplacement = Camera.main.transform.position - gameObject.transform.position;
        gameObject.GetComponent<Rigidbody>().AddForce(new Vector3(0f, 0, 0f));
        
        GameObject.Find("Normal").transform.parent = gameObject.transform;
        GameObject.Find("Normal").transform.position = gameObject.transform.position;
    }
    void FixedUpdate()
    {
        GameObject.Find("Velocity").transform.up = Vector3.Normalize(gameObject.GetComponent<Rigidbody>().velocity);
    }
    public Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles)
    {
        Vector3 dir = point - pivot; // get point direction relative to pivot
        dir = Quaternion.Euler(angles) * dir; // rotate it
        point = dir + pivot; // calculate rotated point
        return point; // return it
    }
    // Update is called once per frame
    void Update()
    {

        _Displacement_Scale = GameObject.Find("Terrain").GetComponent<Terrain>().materialTemplate.GetFloat("_displacement_scale");

        GameObject.Find("Velocity").transform.position = gameObject.transform.position;

        Camera.main.transform.forward = Vector3.SmoothDamp(Camera.main.transform.forward, Vector3.Normalize(gameObject.transform.position - Camera.main.transform.position), ref refVel1, 0.6f);
        Camera.main.transform.position = Vector3.SmoothDamp(Camera.main.transform.position, RotatePointAroundPivot(gameObject.transform.position + camDisplacement, gameObject.transform.position, new Vector3(0, 2f, 0)), ref refVel2, 0.3f);

        var mousePos = Input.mousePosition;
        var wantedPos = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 6));

        //gameObject.transform.position = wantedPos;

        rayNegY.origin = gameObject.transform.position;
        rayNegY.direction = new Vector3(0, -1, 0);

        rayPosY.origin = gameObject.transform.position;
        rayPosY.direction = new Vector3(0, 1, 0);

        rayPosX.origin = gameObject.transform.position;
        rayPosX.direction = new Vector3(1, 0, 0);

        rayNegX.origin = gameObject.transform.position;
        rayNegX.direction = new Vector3(-1, 0, 0);

        rayPosZ.origin = gameObject.transform.position;
        rayPosZ.direction = new Vector3(0, 0, 1);

        rayNegZ.origin = gameObject.transform.position;
        rayNegZ.direction = new Vector3(0, 0, -1);
        minDistance = 10;
        Vector3 mask = new Vector3(0, 0, 0);
        if (UnityEngine.Physics.Raycast(rayNegY, out hitNegY, 10f) && hitNegY.distance < minDistance)
        {
            samplePoint = hitNegY.point;
            sampleNormal = hitNegY.normal;
            minDistance = hitNegY.distance;
            mask = new Vector3(0, -1, 0);
        }
        if (UnityEngine.Physics.Raycast(rayPosY, out hitPosY, 10f) && hitPosY.distance < minDistance)
        {
            samplePoint = hitPosY.point;
            sampleNormal = hitPosY.normal;
            minDistance = hitPosY.distance;
            mask = new Vector3(0, 1, 0);
        }
        if (UnityEngine.Physics.Raycast(rayNegX, out hitNegX, 10f) && hitNegX.distance < minDistance)
        {
            samplePoint = hitNegX.point;
            sampleNormal = hitNegX.normal;
            minDistance = hitNegX.distance;
            mask = new Vector3(-1, 0, 0);
        }
        if (UnityEngine.Physics.Raycast(rayPosX, out hitPosX, 10f) && hitPosX.distance < minDistance)
        {
            samplePoint = hitPosX.point;
            sampleNormal = hitPosX.normal;
            minDistance = hitPosX.distance;
            mask = new Vector3(1, 0, 0);
        }
        if (UnityEngine.Physics.Raycast(rayNegZ, out hitNegZ, 10f) && hitNegZ.distance < minDistance)
        {
            samplePoint = hitNegZ.point;
            sampleNormal = hitNegZ.normal;
            minDistance = hitNegZ.distance;
            mask = new Vector3(0, 0, -1);
        }
        if (UnityEngine.Physics.Raycast(rayPosZ, out hitPosZ, 10f) && hitPosZ.distance < minDistance)
        {
            samplePoint = hitPosZ.point;
            sampleNormal = hitPosZ.normal;
            minDistance = hitPosZ.distance;
            mask = new Vector3(0, 0, 1);
        }
        //if (minDistance < 10)
        //{
        approximateRay.origin = gameObject.transform.position;
        approximateRay.direction = -sampleNormal;
        if (UnityEngine.Physics.Raycast(approximateRay, out hitApproximateRay, 10f))
        {
            samplePoint = hitApproximateRay.point;
            sampleNormal = hitApproximateRay.normal;
        }
            
        //}
        float displacement = GetDisplacement(new Vector2(gameObject.transform.position.x, gameObject.transform.position.y), tex, _ST);
        if (lastDisplacement == 10000)
        {
            lastDisplacement = displacement;    //First contact with the ground
        }


        //Calculate force required to decelerate the gameObject
        //We have:
        // - Change in height and therefore change in energy
        // - Using K = 1/2 MV^2, we can calculate the velocity change
        // - Using E = MGH, we can calculate the energy change
        // - Sim. Eq. results in V = sqrt(2GH)

        Vector3 disp = new Vector3(displacement * _Displacement_Scale * sampleNormal.x, displacement * _Displacement_Scale * sampleNormal.y, displacement * _Displacement_Scale * sampleNormal.z);
        //Debug.Log("Displacement Difference: " + ((displacement * _Displacement_Scale) - (lastDisplacement * _Displacement_Scale)));

        plane.transform.position = samplePoint + disp;
        lastDisplacement = displacement;
        //GetClosestPointOnCollider(gameObject.transform.position);
    }
    public void GetClosestPointOnCollider(Vector3 position)
    {
        var collider = GameObject.Find("Terrain").GetComponent<TerrainCollider>();

        if (!collider)
        {
            return; // nothing to do without a collider
        }

        Vector3 closestPoint = collider.ClosestPoint(position);
        plane.transform.position = closestPoint;
        
    }
    float GetDisplacement(Vector2 uv, Texture2D tex, Vector2 ScaleTransform)
    {
        float displacement = SampleBiplanarTextureCPU(tex, ScaleTransform, nextPoint);
        Vector3 surfaceNormal = new Vector3(0, -1, 0);
        float distance = Vector3.Distance(samplePoint, nextPoint);

        
        return displacement;
    }
    Vector2 Clamp(Vector2 sampleuV)
    {
        return new Vector2(sampleuV.x % 1, sampleuV.y % 1);
    }
    void OnCollisionEnter(Collision other)
    {
        
       //if (other.tag == "PhysicsPlane")
       //{
       //    Debug.Log("Colliding!!!");
       //}

    }
    Vector3 GetNormal(Color[] data, float width)
    {
        /// normalized size of one texel. this would be 1/1024.0 if using 1024x1024 bitmap. 
        float texelSize = 1 / width;

        float n = data[1].r;
        float s = data[7].r;
        float e = data[5].r;
        float w = data[3].r;


        Vector3 ew = Vector3.Normalize(new Vector3(2 * texelSize, e - w, 0));
        Vector3 ns = Vector3.Normalize(new Vector3(0, s - n, 2 * texelSize));
        Vector3 result = Vector3.Cross(ew, ns);

        return result;
    }
    float SampleBiplanarTextureCPU(Texture2D tex, Vector2 _ST, Vector3 nextPoint)
    {
        
        //abs(dot(normalize(o.world_vertex - _PlanetOrigin), normalize(o.normalDir))); (from shader)
        float slope = Mathf.Abs(Vector3.Dot(Vector3.Normalize(samplePoint - planetOrigin), Vector3.Normalize(sampleNormal)));
        slope = Mathf.Pow(slope, steepPower);
        float blendLow = heightBlendLow(samplePoint);
        float blendHigh = heightBlendHigh(samplePoint);
        float midPoint = (Vector3.Distance(samplePoint, planetOrigin) - planetRadius) / (blendHighStart + blendLowEnd);
        Vector3 n = new Vector3(Math.Abs(sampleNormal.x), Math.Abs(sampleNormal.y), Math.Abs(sampleNormal.z));

        // determine major axis (in x; yz are following axis)
        Vector3 ma = (n.x > n.y && n.x > n.z) ? new Vector3(0, 1, 2) :
                   (n.y > n.z) ? new Vector3(1, 2, 0) :
                                          new Vector3(2, 0, 1);
        // determine minor axis (in x; yz are following axis)
        Vector3 mi = (n.x < n.y && n.x < n.z) ? new Vector3(0, 1, 2) :
                   (n.y < n.z) ? new Vector3(1, 2, 0) :
                                          new Vector3(2, 0, 1);
        // determine median axis (in x;  yz are following axis)
        Vector3 me = (new Vector3(3,3,3)) - mi - ma;

        int UVx = (int)(samplePoint[(int)ma.y] * (tex.width / 1) * _ST.x);
        int UVy = (int)(samplePoint[(int)ma.z] * (tex.height / 1) * _ST.y);
        int UVMex = (int)(samplePoint[(int)me.y] * (tex.width / 1) * _ST.x);
        int UVMey = (int)(samplePoint[(int)me.z] * (tex.width / 1) * _ST.y);
        Color x = Color.black;
        Color y = Color.black;

        Color x2 = Color.black;
        Color y2 = Color.black;
        Color x3 = Color.black;
        Color y3 = Color.black;
        Color x4 = Color.black;
        Color y4 = Color.black;
        Color x5 = Color.black;
        Color y5 = Color.black;

        if (tex.isReadable)
        {
            x = tex.GetPixel(UVx, UVy, 0);
            y = tex.GetPixel(UVMex, UVMey, 0);
        }
        else { Debug.Log("<color=#ffffff>Displacement is not readable!"); }
        if (normalLow.isReadable)
        {
            x2 = normalLow.GetPixel(UVx, UVy, 0);
            y2 = normalLow.GetPixel(UVMex, UVMey, 0);
        }
        else { Debug.Log("<color=#ffffff>Normal Map Low is not readable!"); }
        if (normalMid.isReadable)
        {
            x3 = normalMid.GetPixel(UVx, UVy, 0);
            y3 = normalMid.GetPixel(UVMex, UVMey, 0);
        }
        else { Debug.Log("<color=#ffffff>Normal Map Mid is not readable!"); }
        if (normalHigh.isReadable)
        {
            x4 = normalHigh.GetPixel(UVx, UVy, 0);
            y4 = normalHigh.GetPixel(UVMex, UVMey, 0);
        }
        else { Debug.Log("<color=#ffffff>Normal Map High is not readable!"); }
        if (normalSteep.isReadable)
        {
            x5 = normalSteep.GetPixel(UVx, UVy, 0);
            y5 = normalSteep.GetPixel(UVMex, UVMey, 0);
        }
        else { Debug.Log("<color=#ffffff>Normal Map Steep is not readable!"); }




        Vector2 w = new Vector2(n[(int)ma.x], n[(int)me.x]);
        // make local support
        w = (w - new Vector2(0.5773f, 0.5773f)) / new Vector2((1.0f - 0.5773f), (1.0f - 0.5773f));
        w = new Vector2(Mathf.Clamp(w.x, 0, 1), Mathf.Clamp(w.y, 0, 1));
        // shape transition
        w = new Vector2(Mathf.Pow(w.x, (float)(1 / 8.0)), Mathf.Pow(w.y, (float)(1 / 8.0)) );
        //Replace the 1 above with Strength
        // blend and return
        Vector4 finalCol = (x * w.x + y * w.y) / (w.x + w.y);
        Vector4 normalMapLow = (x2 * w.x + y2 * w.y) / (w.x + w.y);
        Vector4 normalMapMid = (x3 * w.x + y3 * w.y) / (w.x + w.y);
        Vector4 normalMapHigh = (x4 * w.x + y4 * w.y) / (w.x + w.y);
        Vector4 normalMapSteep = (x5 * w.x + y5 * w.y) / (w.x + w.y);
        Vector4 finalCol2 = LerpSurfaceNormal(normalMapLow, normalMapMid, normalMapHigh, normalMapSteep, midPoint, slope, blendLow, blendHigh);



        Vector3 lastNormalDir = normalDir;
        
        normalDir = - Vector3.Normalize(new Vector3((1 - (finalCol2.y / 0.5f)), -finalCol2.x, (1 - (finalCol2.w / 0.5f)))); //Normal calc here
        Debug.Log("Sampled values: " + finalCol2.ToString("F3"));
        normalDir = (normalDir + sampleNormal) / 2;
        Debug.DrawLine(gameObject.transform.position, samplePoint, Color.red, 0.1f);
         
        //normalDir = Vector3.SmoothDamp(lastNormalDir, normalDir, ref smoothDampRotation, 0.01f);
        Debug.Log("Normal Dir: " + normalDir.ToString("F3"));//
        GameObject.Find("Normal").transform.up = normalDir;
        plane.transform.up = normalDir;

        float finalColLow = finalCol.x;
        float finalColMid = finalCol.y;
        float finalColHigh = finalCol.z;
        float finalColSteep = finalCol.w;

        //Debug.DrawLine(transform.position, 100 * -sampleNormal, Color.red, 1000f, true);

        float displacement = LerpSurfaceColor(finalColLow, finalColMid, finalColHigh, finalColSteep, midPoint, slope, blendLow, blendHigh);

        //return finalCol.x;
        return displacement;
    }
    Vector4 LerpSurfaceNormal(Vector4 low, Vector4 mid, Vector4 high, Vector4 steep, float midPoint, float slope, float blendLow, float blendHigh)
    {
        Vector4 col;
        if (midPoint < 0.5)
        {
            col = Vector4.Lerp(low, mid, 1 - blendLow);
        }
        else
        {
            col = Vector4.Lerp(mid, high, blendHigh);
        }
        col = Vector4.Lerp(col, steep, 1 - slope);
        return col;
    }
    float heightBlendLow(Vector3 worldPos)
    {
        float terrainHeight = Vector3.Distance(worldPos, planetOrigin) - planetRadius;

        float blendLow = Mathf.Clamp((terrainHeight - blendLowEnd) / (blendLowStart - blendLowEnd), 0, 1);
        return blendLow;
    }
    float heightBlendHigh(Vector3 worldPos)
    {
        float terrainHeight = Vector3.Distance(worldPos, planetOrigin) - planetRadius;

        float blendHigh = Mathf.Clamp((terrainHeight - blendHighStart) / (blendHighEnd - blendHighStart), 0, 1);
        return blendHigh;
    }
    Vector3 SampleBiplanarTextureCPUNormal(Texture2D tex, Vector2 _ST)
    {
        Vector3 n = new Vector3(Math.Abs(sampleNormal.x), Math.Abs(sampleNormal.y), Math.Abs(sampleNormal.z));

        // determine major axis (in x; yz are following axis)
        Vector3 ma = (n.x > n.y && n.x > n.z) ? new Vector3(0, 1, 2) :
                   (n.y > n.z) ? new Vector3(1, 2, 0) :
                                          new Vector3(2, 0, 1);
        // determine minor axis (in x; yz are following axis)
        Vector3 mi = (n.x < n.y && n.x < n.z) ? new Vector3(0, 1, 2) :
                   (n.y < n.z) ? new Vector3(1, 2, 0) :
                                          new Vector3(2, 0, 1);
        // determine median axis (in x;  yz are following axis)
        Vector3 me = (new Vector3(3, 3, 3)) - mi - ma;

        Color x = tex.GetPixel((int)(samplePoint[(int)ma.y] * tex.width * _ST.x), (int)(samplePoint[(int)ma.z] * tex.height * _ST.y));
        Color y = tex.GetPixel((int)(samplePoint[(int)me.y] * tex.width * _ST.x), (int)(samplePoint[(int)me.z] * tex.height * _ST.y));
        Color z = tex.GetPixel((int)(samplePoint[(int)ma.y] * tex.width * _ST.x), (int)(samplePoint[(int)ma.z] * tex.height * _ST.y));
        Color w1 = tex.GetPixel((int)(samplePoint[(int)me.y] * tex.width * _ST.x), (int)(samplePoint[(int)me.z] * tex.height * _ST.y));

        //float4 x = tex2D(sam, float2(p[biplanarCoords[0].y], p[biplanarCoords[0].z]) * (scale / UVDistortion), float2(dpdx[biplanarCoords[0].y], dpdx[biplanarCoords[0].z]) * (scale / UVDistortion), float2(dpdy[biplanarCoords[0].y], dpdy[biplanarCoords[0].z]) * (scale / UVDistortion));
        //float4 y = tex2D(sam, float2(p[biplanarCoords[2].y], p[biplanarCoords[2].z]) * (scale / UVDistortion), float2(dpdx[biplanarCoords[2].y], dpdx[biplanarCoords[2].z]) * (scale / UVDistortion), float2(dpdy[biplanarCoords[2].y], dpdy[biplanarCoords[2].z]) * (scale / UVDistortion));
        //
        //float4 z = tex2D(sam, float2(p[biplanarCoords[0].y], p[biplanarCoords[0].z]) * (scale / nextUVDist), float2(dpdx[biplanarCoords[0].y], dpdx[biplanarCoords[0].z]) * (scale / nextUVDist), float2(dpdy[biplanarCoords[0].y], dpdy[biplanarCoords[0].z]) * (scale / nextUVDist));
        //float4 w1 = tex2D(sam, float2(p[biplanarCoords[2].y], p[biplanarCoords[2].z]) * (scale / nextUVDist), float2(dpdx[biplanarCoords[2].y], dpdx[biplanarCoords[2].z]) * (scale / nextUVDist), float2(dpdy[biplanarCoords[2].y], dpdy[biplanarCoords[2].z]) * (scale / nextUVDist));

        //x = Mathf.Lerp(x, z, percentage);
        //y = Mathf.Lerp(y, w1, percentage);

        // blend factors
        Vector2 w = new Vector2(n[(int)ma.x], n[(int)me.x]);
        // make local support
        w = (w - new Vector2(0.5773f, 0.5773f)) / new Vector2((1.0f - 0.5773f), (1.0f - 0.5773f));
        w = new Vector2(Mathf.Clamp(w.x, 0, 1), Mathf.Clamp(w.y, 0, 1));
        // shape transition
        w = new Vector2(Mathf.Pow(w.x, (float)(1 / 8.0)), Mathf.Pow(w.y, (float)(1 / 8.0)));
        //Replace the 1 above with Strength
        // blend and return
        Vector4 finalCol = (x * w.x + y * w.y) / (w.x + w.y);
        finalCol.w = 1;
        return finalCol;

        float finalColLow = finalCol.x;
        float finalColMid = finalCol.y;
        float finalColHigh = finalCol.z;
        float finalColSteep = finalCol.w;

        float displacement = LerpSurfaceColor(finalColLow, finalColMid, finalColHigh, finalColSteep, 0.5f, 0, 1, 0);

        //return displacement;
    }
    float LerpSurfaceColor(float low, float mid, float high, float steep, float midPoint, float slope, float blendLow, float blendHigh)
    {
        float col;
        if (midPoint < 0.5)
        {
            col = Mathf.Lerp(low, mid, 1 - blendLow);
        }
        else
        {
            col = Mathf.Lerp(mid, high, blendHigh);
        }
        col = Mathf.Lerp(col, steep, 1 - slope);
        return col;
    }
}
