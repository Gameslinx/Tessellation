using UnityEngine;
using System.Collections;

public class PostCompute : MonoBehaviour
{
    public int population;
    public float range = 100;
    public float viewDist;

    public Material material;
    public Mesh mesh;

    private ComputeBuffer meshPropertiesBuffer;
    private ComputeBuffer argsBuffer;

    private Bounds bounds;
    bool setup = false;

    // Mesh Properties struct to be read from the GPU.
    // Size() is a convenience funciton which returns the stride of the struct.

    public void Setup(ComputeShaderTest.GrassData[] props)
    {
        GameObject go = GameObject.Find("ComputeMesh");

        Mesh mesh = Instantiate(go.GetComponent<MeshFilter>().mesh);//CreateQuad();
        this.mesh = mesh;

        // Boundary surrounding the meshes we will be drawing.  Used for occlusion.
        bounds = new Bounds(transform.position, Vector3.one * (range + 1));

        InitializeBuffers(props);
    }

    private void InitializeBuffers(ComputeShaderTest.GrassData[] props)
    {


        //int seed = gameObject.name[0];
        //Random.InitState(seed);
        float time = Time.realtimeSinceStartup;
        // Argument buffer used by DrawMeshInstancedIndirect.
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        // Arguments for drawing mesh.
        // 0 == number of triangle indices, 1 == population, others are only relevant if drawing submeshes.
        args[0] = (uint)mesh.GetIndexCount(0);
        args[1] = (uint)props.Length;
        args[2] = (uint)mesh.GetIndexStart(0);
        args[3] = (uint)mesh.GetBaseVertex(0);
        if (argsBuffer != null)
        {
            argsBuffer.Release();
        }
        if (meshPropertiesBuffer != null)
        {
            meshPropertiesBuffer.Release();
        }
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);

        // Initialize buffer with the given population.
        //MeshProperties[] properties = new MeshProperties[population];
        //MeshProperties[] properties = new MeshProperties[population];
        //for (int i = 0; i < population; i++)
        //{
        //    MeshProperties props = new MeshProperties();
        //    Vector3 position = new Vector3(Random.Range(-range, range), 0, Random.Range(-range, range));
        //    Quaternion rotation = Quaternion.Euler(Random.Range(-10, 10), Random.Range(-180, 180), Random.Range(-10, 10));
        //    Vector3 scale = Vector3.one * 2;
        //
        //    //float dist = Vector3.Distance(GameObject.Find("GrassCraft").transform.position, transform.TransformPoint(position));
        //    //if (dist > viewDist)
        //    //{
        //    //    scale = Vector3.zero;
        //    //}
        //
        //    props.mat = Matrix4x4.TRS(position, rotation, scale);
        //    props.color = Color.Lerp(Color.red, Color.blue, Random.value);
        //
        //    properties[i] = props;
        //}

        meshPropertiesBuffer = new ComputeBuffer(props.Length, ComputeShaderTest.GrassData.Size());
        meshPropertiesBuffer.SetData(props);
        material.SetBuffer("_Properties", meshPropertiesBuffer);
        Debug.Log("This took " + (Time.realtimeSinceStartup - time) + " seconds");
        setup = true;
        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer);
        //argsBuffer.Release();
        //meshPropertiesBuffer.Release();
    }

    //private Mesh CreateQuad(float width = 1f, float height = 1f)
    //{
    //    
    //}

    private void Start()
    {
        
    }

    private void Update()
    {
        if (mesh == null || setup == false)
        { Debug.Log("aaa"); }
        //InitializeBuffers();
        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer);
    }

    private void OnDisable()
    {
        // Release gracefully.
        if (meshPropertiesBuffer != null)
        {
            meshPropertiesBuffer.Release();
        }
        meshPropertiesBuffer = null;

        if (argsBuffer != null)
        {
            argsBuffer.Release();
        }
        argsBuffer = null;
    }
}