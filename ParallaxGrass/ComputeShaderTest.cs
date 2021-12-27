using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ComputeShaderTest : MonoBehaviour
{
    public ComputeShader computeShader;
    public ComputeBuffer computeBuffer;
    public ComputeBuffer grassBuffer;
    public ComputeBuffer triangleBuffer;
    public float posX;

    public int population = 16;
    [Range(5, 100)]
    public float range = 60f;
    public float cachedRange = 60f;
    [Range(1, 512)]
    public int populationMultiplier = 1;
    public int cachedPopulationMultiplier = 1;
    // Start is called before the first frame update
    struct ObjectData
    {
        public Vector3 pos;
        //public Vector4 rot;
        public Vector3 scale;
    }
    public struct GrassData
    {
        public Matrix4x4 mat;
        public Vector4 color;
        public static int Size()
        {
            return
                sizeof(float) * 4 * 4 + // matrix;
                sizeof(float) * 4;     // color
        }
    }
    struct Triangle
    {
        public int index;
    }
    void Update()
    {
        if (cachedPopulationMultiplier != populationMultiplier)
        {
            cachedPopulationMultiplier = populationMultiplier;
            Start();
        }
        if (cachedRange != range)
        {
            cachedRange = range;
            Start();
        }
    }
    void Start()
    {
        float time = Time.realtimeSinceStartup;
        //BUFFER SIZE is No. float values * 4
        int vertCount = gameObject.GetComponent<MeshFilter>().sharedMesh.vertexCount;

        ObjectData[] data = new ObjectData[vertCount];
        Vector3[] verts = gameObject.GetComponent<MeshFilter>().sharedMesh.vertices;
        int[] triangles = gameObject.GetComponent<MeshFilter>().sharedMesh.triangles;
        Triangle[] tris = new Triangle[triangles.Length];

        for (int i = 0; i < verts.Length; i++)
        {
            data[i].pos = verts[i];
            data[i].scale = Vector3.one;
        }
        for (int i = 0; i < tris.Length; i++)
        {
            tris[i].index = triangles[i];
        }

        computeBuffer = new ComputeBuffer(vertCount, (3 + 3) * 4);
        grassBuffer = new ComputeBuffer((triangles.Length / 3) * populationMultiplier, GrassData.Size());
        triangleBuffer = new ComputeBuffer(tris.Length, sizeof(int));

        computeBuffer.SetData(data);
        triangleBuffer.SetData(tris);
        

        int kernel = computeShader.FindKernel("CSMain2");

        computeShader.SetInt("populationMult", populationMultiplier);
        computeShader.SetFloat("_Range", range);
        computeShader.SetMatrix(Shader.PropertyToID("objectToWorld"), transform.localToWorldMatrix);
        computeShader.SetBuffer(kernel, "Objects", computeBuffer);
        computeShader.SetBuffer(kernel, "Grass", grassBuffer);
        computeShader.SetBuffer(kernel, "Tris", triangleBuffer);

        computeShader.Dispatch(kernel, (tris.Length / 3), 1, 1);
        

        GrassData[] output = new GrassData[(triangles.Length / 3) * populationMultiplier];
        grassBuffer.GetData(output);

        computeBuffer.Release();
        grassBuffer.Release();
        triangleBuffer.Release();

        GrassData[] props = new GrassData[output.Length];
        for (int i = 0; i < output.Length; i++)
        {
            props[i].mat = output[i].mat;//Matrix4x4.TRS(transform.TransformPoint(output[i].pos), Quaternion.Euler(180, 0, 180), new Vector3(0.1f,0.1f,0.1f));//output[i].mat;
            props[i].color = output[i].color;// new Vector4(1, 1, 1, 1);
        }

        Debug.Log("Done!");

        time = Time.realtimeSinceStartup - time;
        Debug.Log("Time elapsed: " + time);

        PostCompute pc = gameObject.GetComponent<PostCompute>();
        pc.Setup(props);

        //Indirect(output);
        //Populate(output);
        //Routine();
    }
   
    
    void Populate(GrassData[] objects)
    {
        //Quaternion.FromToRotation(Vector3.up, )
        Matrix4x4 actualMat = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(56, 28, 1), new Vector3(0.25f, 0.75f, 2f));
        for (int i = 0; i < objects.Length; i++)
        {
            Vector3 pos = objects[i].mat.GetColumn(3);
            Vector3 forward = objects[i].mat.GetColumn(2);
            Vector3 up = objects[i].mat.GetColumn(1);
            Quaternion rot = Quaternion.LookRotation(forward, up);
            Debug.Log(objects[i].mat);
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            
            go.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            go.transform.position = pos;
            go.transform.rotation = rot;
            go.transform.localScale = objects[i].mat.lossyScale;
        }
        Debug.Log(actualMat + " <- actual matrix");
    }
    void Populate(ObjectData[] objects)
    {
        for (int i = 0; i < objects.Length; i++)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            go.transform.position = transform.TransformPoint(objects[i].pos);
        }
    }
}