using Grass;
using ScatterConfiguratorUtils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace ParallaxGrass
{
    struct PositionData
    {
        public Vector3 pos;
        public Matrix4x4 mat;
        public Vector4 color;
        public static int Size()
        {
            return
                sizeof(float) * 23;
        }
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
    public class MeshData
    {
        public Vector3[] vertices;
        public Vector3[] normals;
        public int[] tris;
    }
    public class QuadData
    {
        public PQ quad;
        public Mesh mesh;
        public Dictionary<string, ScatterCompute> comps = new Dictionary<string, ScatterCompute>();
        public MeshData data;
        public MeshData subdividedData;
        string sphereName;
        public Mesh subMesh;
        public float quadWidth;
        public bool subdivisionAlreadyRequested = false; //ScatterCompute calls back to here to subdivide the quad only when it gets in range, and only once

        public ComputeBuffer vertexBuffer;
        public ComputeBuffer normalBuffer;
        public ComputeBuffer triBuffer;

        public ComputeBuffer subdividedVertexBuffer;
        public ComputeBuffer subdividedNormalBuffer;
        public ComputeBuffer subdividedTriangleBuffer;

        public QuadData(PQ pq)
        {
            quad = pq;
            mesh = quad.mesh;               //Accessing .vertices, .normals or .triangles forces a full duplication of the arrays. Lots of garbage, so let's just access it once, yeah?
            data = new MeshData();
            data.vertices = quad.mesh.vertices;
            data.normals = quad.mesh.normals;
            data.tris = quad.mesh.triangles;
            vertexBuffer = new ComputeBuffer(data.vertices.Length, 12, ComputeBufferType.Structured);
            normalBuffer = new ComputeBuffer(data.normals.Length, 12, ComputeBufferType.Structured);
            triBuffer = new ComputeBuffer(data.tris.Length, sizeof(int), ComputeBufferType.Structured);
            vertexBuffer.SetData(data.vertices);
            normalBuffer.SetData(data.normals);
            triBuffer.SetData(data.tris);
            subdividedData = new MeshData();
            subMesh = GameObject.Instantiate(mesh);
            DetermineQuadWidth();
            
            if (quad.subdivision == quad.sphereRoot.maxLevel)   //Only subdivide maxLevel quads
            {
                //Range check here
            }
            //subdividedData.vertices = subMesh.vertices;
            //subdividedData.normals = subMesh.normals;
            //subdividedData.tris = subMesh.triangles;
            sphereName = quad.sphereRoot.name;
            InitializeScatters();
        }
        public void InitializeScatters()    //Setup fixed and nearest scatters and assign them ScatterComputes
        {

            Scatter[] scatters = ScatterBodies.scatterBodies[sphereName].scatters.Values.ToArray();
            for (int i = 0; i < scatters.Length; i++)
            {

                ScatterComponent manager = ScatterManagerPlus.scatterComponents[sphereName].Find(x => x.scatter.scatterName == scatters[i].scatterName);
                if (quad.subdivision >= scatters[i].properties.subdivisionSettings.minLevel && !scatters[i].shared)    //Only add stuff if it's within the minimum subdivision level
                {
                    //If quad is only in 1 biome and that biome is blacklisted...
                    if (InBlacklistedBiome(PQSMod_ScatterDistribute.scatterData.perQuadBiomeData[quad], scatters[i].properties.scatterDistribution.blacklist.fastBiomes)) // PQSMod_ScatterDistribute.scatterData.distributionData[scatters[i].scatterName].data[quad].biomes.Count == 1 && scatters[i].properties.scatterDistribution.blacklist.fastBiomes.ContainsKey(PQSMod_ScatterDistribute.scatterData.distributionData[scatters[i].scatterName].data[quad].biomes[0]))
                    {
                        continue;   //Don't add this one :)
                    }
                    ScatterCompute comp = new ScatterCompute(scatters[i], quad, manager, this, sphereName, ref data, ref subdividedData);
                    comps.Add(scatters[i].scatterName, comp);
                }
            }
        }
        public bool InBlacklistedBiome(List<string> biomes, Dictionary<string, string> blacklist)
        {
            for (int i = 0; i < biomes.Count; i++)
            {
                if (blacklist.ContainsKey(biomes[i]))
                {
                    return true;
                }
            }
            return false;
        }
        public void DetermineQuadWidth()                        //Get distance between opposite diagonal verts on the quad to estimate its hypotenuse
        {
            if (data.vertices == null) { return; } //Yet again, wait for subdivision to complete :)
            Vector3 pos1 = quad.gameObject.transform.TransformPoint(data.vertices[0]);  //Top left
            Vector3 pos2 = quad.gameObject.transform.TransformPoint(data.vertices[data.vertices.Length - 1]); //Bottom right
            quadWidth = Vector3.Distance(pos1, pos2) / 2;   //Distance from centre to corner
        }
        public void SubdivideQuad()
        {
            
            if (subdivisionAlreadyRequested) { return; }    //Don't subdivide every frame the quad is in range lol

            MeshHelper.Subdivide(subMesh, 6);
            subdividedData.vertices = subMesh.vertices; //Automatically referenced, don't need to reassign
            subdividedData.normals = subMesh.normals;
            subdividedData.tris = subMesh.triangles;
            subdividedVertexBuffer = new ComputeBuffer(subdividedData.vertices.Length, 12, ComputeBufferType.Structured);
            subdividedNormalBuffer = new ComputeBuffer(subdividedData.normals.Length, 12, ComputeBufferType.Structured);
            subdividedTriangleBuffer = new ComputeBuffer(subdividedData.tris.Length, sizeof(int), ComputeBufferType.Structured);
            subdividedVertexBuffer.SetData(subdividedData.vertices);
            subdividedNormalBuffer.SetData(subdividedData.normals);
            subdividedTriangleBuffer.SetData(subdividedData.tris);
            subdivisionAlreadyRequested = true;
        }
        public void Cleanup()   //Called when the quad is destroyed. Purge anything memory intensive, because this won't be destroyed for a while
        {
            if (vertexBuffer != null) { vertexBuffer.Release(); vertexBuffer = null; }
            if (normalBuffer != null) { normalBuffer.Release(); normalBuffer = null; }
            if (triBuffer != null) { triBuffer.Release(); triBuffer = null; }
            if (subdividedVertexBuffer != null) { subdividedVertexBuffer.Release(); subdividedVertexBuffer = null; }
            if (subdividedNormalBuffer != null) { subdividedNormalBuffer.Release(); subdividedNormalBuffer = null; }
            if (subdividedTriangleBuffer != null) { subdividedTriangleBuffer.Release(); subdividedTriangleBuffer = null; }
            foreach (ScatterCompute scatter in comps.Values)
            {
                scatter.Cleanup();
            }
            comps.Clear();
        }
    }
    public class ScatterCompute
    {
        //public CollisionHandler collisionHandler;

        public Scatter scatter;
        public Properties properties;
        public PQ quad;
        public ScatterComponent pqsMod;
        public QuadData parent;                 //Not great technique, but access the mesh data without duplicating its arrays each time
        public MeshData meshData;

        ComputeShader distribute;
        ComputeShader evaluate;

        int distributeIndex;
        int evaluateIndex;

        public ComputeBuffer vertexBuffer;  //References the buffers in QuadData
        public ComputeBuffer normalBuffer;
        public ComputeBuffer triBuffer;

        public ComputeBuffer noiseBuffer;
        public ComputeBuffer positionBuffer;

        public ComputeBuffer distributeCountBuffer;
        public ComputeBuffer indirectArgs;

        //public ComputeBuffer dummyCount;

        int memoryUsage = 0;
        int quadSubdivDifference = 1;
        int triCount = 1;

        bool currentlyReadingDist = true;
        bool initializedEvaluate = false;
        public int objectCount = 0;
        int[] workGroups = new int[] {1, 1, 1};
        int generateDispatchAmount = 0;

        string sphereName;
        public int totalMem = 0;
        public float quadWidth = 0;
        public MeshRenderer quadMeshRenderer;
        public bool cleaned = false;
        public bool active = false;    //Set true when quad is in subdivision range. True by default for persistent scatters
        public bool subdivided = false;
        public QuadDistributionData distributionData;

        public ScatterCompute(Scatter scatter, PQ quad, ScatterComponent pqsMod, QuadData parent, string sphereName, ref MeshData meshData, ref MeshData subdividedData)
        {
            this.scatter = scatter;
            this.quad = quad;
            this.pqsMod = pqsMod;
            this.parent = parent;
            this.sphereName = sphereName;
            this.meshData = meshData;   //Contains either regular or subdivided mesh
            quadWidth = parent.quadWidth;
            if (scatter.properties.subdivisionSettings.mode == SubdivisionMode.NearestQuads)
            {
                this.meshData = subdividedData;
            }
            else
            {
                this.meshData = meshData;
            }
            Setup();
            if (scatter.properties.subdivisionSettings.mode == SubdivisionMode.FixedRange) { active = true; }
            if (active) { Start(); }
            
        }
        public float GetTotalMemoryUsage()
        {
            float total = 0;
            Debug.Log(" - Vertex Buffer: Count: " + vertexBuffer.count + ", Stride: " + vertexBuffer.stride + ", Total: " + ((float)(vertexBuffer.count * vertexBuffer.stride) / (1024f * 1024f)) + " MB");
            Debug.Log(" - Normal Buffer: Count: " + normalBuffer.count + ", Stride: " + normalBuffer.stride + ", Total: " + ((float)(normalBuffer.count * normalBuffer.stride) / (1024f * 1024f)) + " MB");
            Debug.Log(" - Triangle Buffer: Count: " + triBuffer.count + ", Stride: " + triBuffer.stride + ", Total: " + ((float)(triBuffer.count * triBuffer.stride) / (1024f * 1024f)) + " MB");
            Debug.Log(" - Noise Buffer: Count: " + noiseBuffer.count + ", Stride: " + noiseBuffer.stride + ", Total: " + ((float)(noiseBuffer.count * noiseBuffer.stride) / (1024f * 1024f)) + " MB");
            Debug.Log(" - Position Buffer: Count: " + positionBuffer.count + ", Stride: " + positionBuffer.stride + ", Total: " + ((float)(positionBuffer.count * positionBuffer.stride) / (1024f * 1024f)) + " MB");
            Debug.Log(" - DC Buffer: Count: " + distributeCountBuffer.count + ", Stride: " + distributeCountBuffer.stride + ", Total: " + ((float)(distributeCountBuffer.count * distributeCountBuffer.stride) / (1024f * 1024f)) + " MB");
            Debug.Log(" - Args Buffer: Count: " + indirectArgs.count + ", Stride: " + indirectArgs.stride + ", Total: " + ((float)(indirectArgs.count * indirectArgs.stride) / (1024f * 1024f)) + " MB");
            total += vertexBuffer.count * vertexBuffer.stride;
            total += normalBuffer.count * normalBuffer.stride;
            total += triBuffer.count * triBuffer.stride;
            total += noiseBuffer.count * noiseBuffer.stride;
            total += positionBuffer.count * positionBuffer.stride;
            total += distributeCountBuffer.count * distributeCountBuffer.stride;
            total += indirectArgs.count * indirectArgs.stride;

            int[] count = new int[1];
            ComputeBuffer tempCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.IndirectArguments);
            ComputeBuffer.CopyCount(positionBuffer, tempCountBuffer, 0);
            tempCountBuffer.GetData(count);
            Debug.Log(" - Position Buffer: Capacity: " + positionBuffer.count + ", Filled: " + count[0] + ", Usage: " + (((float)count[0] / (float)positionBuffer.count) * 100f) + "%");
            tempCountBuffer.Dispose();

            Debug.Log("Total (one quad) " + (total / (1024f * 1024f)) + " MB");

            

            return total / (1024f * 1024f);
        }
        public void Setup()
        {
            pqsMod.OnForceEvaluate += EvaluatePositions;
            pqsMod.OnRangeCheck += AlternativeStart;
            GameEvents.onVesselChange.Add(OnVesselSwitch);
            GameEvents.onVesselWillDestroy.Add(OnVesselDestroyed);
            distributionData = scatter.properties.subdivisionSettings.mode == SubdivisionMode.NearestQuads ? default(QuadDistributionData) : Utils.GetDistributionData(scatter, quad);
            quadMeshRenderer = quad.gameObject.GetComponent<MeshRenderer>();
            SetupComputeShaders();
            
        }
        public void OnVesselSwitch(Vessel craft)
        {
            Start();
        }
        public void OnVesselDestroyed(Vessel craft)
        {
            if (craft == FlightGlobals.ActiveVessel)
            {
                Start();
            }
            
        }
        public void Start()  //Subscribe events here. Determine subdivision level, if the quad needs subdividing, etc
        {
            //if (cleaned) { return; }
            if (ComputeShaderAvailable())   //Generate immediately
            {
                distribute = pqsMod.computePool[pqsMod.computePool.Count - 1];  //Take last item on list to prevent re-ordering
                pqsMod.computePool.RemoveAt(pqsMod.computePool.Count - 1);
                distributeIndex = distribute.FindKernel("DistributePoints");
                if (!active || cleaned) { UpdateQueue(); return; }
                InitializeGenerate();
                PrepareGenerate(true);
                DispatchGenerate();
            }
            else                            //Add to the quad queue and wait for a shader to become available
            {
                pqsMod.scatterQueue.Enqueue(this);
            }
        }
        public bool ComputeShaderAvailable()
        {
            if (pqsMod.computePool.Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public void AlternativeStart()
        {
            if (scatter.properties.subdivisionSettings.mode != SubdivisionMode.NearestQuads) { return; }
            if (!active)    //Run once - This is the range check
            {
                if (FlightGlobals.ready && Vector3.Distance(quad.gameObject.transform.position, GlobalPoint.originPoint) - quadWidth < scatter.properties.subdivisionSettings.range)   //Distance check - In range AND subdivided
                {
                    if (!subdivided) { parent.SubdivideQuad(); subdivided = true;  }
                    active = true;
                    triCount = meshData.tris.Length;
                    memoryUsage = (int)(triCount * properties.scatterDistribution._PopulationMultiplier * quadSubdivDifference * properties.scatterDistribution.noise._MaxStacks);
                    generateDispatchAmount = Mathf.CeilToInt((((float)triCount) / 3f) / 32f);
                    Start();
                    
                }
            }
            else
            {
                if (Vector3.Distance(quad.gameObject.transform.position, GlobalPoint.originPoint) - quadWidth >= scatter.properties.subdivisionSettings.range)   //Distance check - Not in range OR not subdivided
                {
                    active = false;
                    Hibernate();
                }
            }
        }
        
        public void SetupComputeShaders() 
        {
            //distribute = Utils.GetCorrectComputeShader(scatter, out shaderType);
            evaluate = GameObject.Instantiate(ScatterShaderHolder.GetCompute("EvaluatePoints"));
            //distributeIndex = distribute.FindKernel("DistributePoints");
            evaluateIndex = evaluate.FindKernel("EvaluatePoints");

            int maxLevel = quad.sphereRoot.maxLevel;
            if (meshData.tris != null) { triCount = meshData.tris.Length; } else { triCount = 1; }  //Wait for subdivision to determine tri count
            
            quadSubdivDifference = ((maxLevel - quad.subdivision) * 3) + 1;
            memoryUsage = (int)(triCount * properties.scatterDistribution._PopulationMultiplier * quadSubdivDifference * properties.scatterDistribution.noise._MaxStacks);
            generateDispatchAmount = Mathf.CeilToInt((((float)triCount) / 3f) / 32f);
        }
        public void InitializeGenerate()
        {
            if (!active) { return; }
            if (noiseBuffer != null) { noiseBuffer.Release(); }
            if (distributeCountBuffer != null) { distributeCountBuffer.Release(); }
            if (indirectArgs != null) { indirectArgs.Release(); }
            if (positionBuffer != null) { positionBuffer.Release(); }
            

            int numObjects = (int)((float)((meshData.tris.Length / 3) * (int)scatter.properties.scatterDistribution._PopulationMultiplier * quadSubdivDifference * scatter.properties.scatterDistribution.noise._MaxStacks) * scatter.properties.scatterDistribution._SpawnChance);
            if (numObjects == 0) { numObjects = 1; }

            if (scatter.properties.subdivisionSettings.mode == SubdivisionMode.NearestQuads)
            {
                vertexBuffer = parent.subdividedVertexBuffer;
                normalBuffer = parent.subdividedNormalBuffer;
                triBuffer = parent.subdividedTriangleBuffer;
            }
            else
            {
                vertexBuffer = parent.vertexBuffer;
                normalBuffer = parent.normalBuffer;
                triBuffer = parent.triBuffer;
            }

            positionBuffer = new ComputeBuffer(numObjects, PositionData.Size(), ComputeBufferType.Append);
            noiseBuffer = distributionData.data != null ? new ComputeBuffer(distributionData.data.Length, sizeof(float), ComputeBufferType.Structured) : new ComputeBuffer(1, sizeof(float), ComputeBufferType.Structured); //new ComputeBuffer(distributionNoise.Length, sizeof(float), ComputeBufferType.Structured);
            distributeCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.IndirectArguments);
            indirectArgs = new ComputeBuffer(1, sizeof(int) * 3, ComputeBufferType.IndirectArguments);

            //dummyCount = new ComputeBuffer(1, sizeof(int), ComputeBufferType.IndirectArguments);

            totalMem = vertexBuffer.stride * vertexBuffer.count + normalBuffer.stride * normalBuffer.count + positionBuffer.stride * positionBuffer.count + triBuffer.stride * triBuffer.count + noiseBuffer.stride * noiseBuffer.count + distributeCountBuffer.stride * distributeCountBuffer.count;

            //vertexBuffer.SetData(meshData.vertices);
            //triBuffer.SetData(meshData.tris);
            if (distributionData.data != null) { noiseBuffer.SetData(distributionData.data); }; //Otherwise null
            //normalBuffer.SetData(meshData.normals);

            distribute.SetBuffer(distributeIndex, "Objects", vertexBuffer);
            distribute.SetBuffer(distributeIndex, "Tris", triBuffer);
            distribute.SetBuffer(distributeIndex, "Noise", noiseBuffer);
            distribute.SetBuffer(distributeIndex, "Positions", positionBuffer);
            distribute.SetBuffer(distributeIndex, "Normals", normalBuffer);
        }
        public void PrepareGenerate(bool updateTime) //Each time a quad is built, generate scatter positions
        {
            Utils.SetDistributionVars(ref distribute, scatter, quad.gameObject.transform, quadSubdivDifference, triCount, sphereName, distributeIndex);
            if (updateTime)
            {
                distribute.SetFloat("updateTime", 1);
            }
            else
            {
                distribute.SetFloat("updateTime", 0);
            }
        }
        public void DispatchGenerate()
        {
            if (!active) { return; }
            positionBuffer.SetCounterValue(0);
            if (scatter == null) { Debug.Log("Scatter is null lmao"); }
            distribute.SetVector("_ShaderOffset", -(Vector3)FloatingOrigin.TerrainShaderOffset);
            distribute.Dispatch(distributeIndex, generateDispatchAmount, scatter.properties.scatterDistribution.noise._MaxStacks, 1);
            ComputeBuffer.CopyCount(positionBuffer, distributeCountBuffer, 0);
            AsyncGPUReadback.Request(distributeCountBuffer, AwaitDistributeReadback);
            currentlyReadingDist = true;
            
        }
        private void AwaitDistributeReadback(AsyncGPUReadbackRequest req)
        {
            if (cleaned || !active) { UpdateQueue(); return; }    //Quad was cleaned up in the time it took for the GPU to finish generating positions on this quad. L + No speed
            if (req.hasError)
            {
                ScatterLog.Log("[Exception] Async GPU Readback error! (In GeneratePositions())");
                UpdateQueue();
                return;
            }
            objectCount = req.GetData<int>(0).ToArray()[0];
            workGroups[0] = Mathf.CeilToInt(((float)objectCount) / 32f);
            indirectArgs.SetData(workGroups);
            currentlyReadingDist = false;

            Debug.Log("Readback: " + scatter.scatterName);
            InitializeEvaluate();
            UpdateQueue();          //Dequeue and process the next quad
        }
        public void UpdateQueue()   //Return compute shader to the queue and process the next quad
        {
            pqsMod.computePool.Add(distribute);
            while (pqsMod.scatterQueue.Count > 0)   //Run until a quad has not been cleaned
            {
                ScatterCompute nextItem = pqsMod.scatterQueue.Dequeue();
                if (!nextItem.cleaned)  //Quad was destroyed / cleaned while this one was being processed
                {
                    nextItem.Start();   
                    break;
                }
            }
        }
        public void InitializeEvaluate()
        {
            if (currentlyReadingDist) { return; }
            if (objectCount == 0) { return; }
            if (!pqsMod.buffersCreated) { Debug.Log("[Exception] Buffers not created for " + scatter.scatterName); return; }
            Debug.Log("Evaluate initialized: " + scatter.scatterName + " at offset " + FloatingOrigin.TerrainShaderOffset.ToString("F3"));
            evaluate.SetBuffer(evaluateIndex, "Grass", Buffers.activeBuffers[scatter.scatterName].buffer);
            evaluate.SetBuffer(evaluateIndex, "Positions", positionBuffer);
            evaluate.SetBuffer(evaluateIndex, "FarGrass", Buffers.activeBuffers[scatter.scatterName].farBuffer);
            evaluate.SetBuffer(evaluateIndex, "FurtherGrass", Buffers.activeBuffers[scatter.scatterName].furtherBuffer);

            Utils.SetEvaluationVars(ref evaluate, scatter, quad.gameObject.transform, objectCount);
            initializedEvaluate = true;

            EvaluatePositions();
        }
        public void EvaluatePositions()
        {
            if (!quad.isVisible) { return; }
            if (currentlyReadingDist) { return; }
            if (!initializedEvaluate) { return; }
            if (objectCount == 0) { return; }
            if (!quadMeshRenderer.isVisible) { return; }
            if (Vector3.Distance(quad.gameObject.transform.position, GlobalPoint.originPoint) - quadWidth > scatter.properties.scatterDistribution._Range) { return; }
            if (FlightGlobals.ActiveVessel == null) { return; }
            if (!active) { return; }
            //Debug.Log("Dispatching evaluate indirectly");
            
            evaluate.SetVector("_ShaderOffset", -((Vector3)FloatingOrigin.TerrainShaderOffset));
            evaluate.SetVector("_CameraPos", ActiveBuffers.cameraPos);
            evaluate.SetVector("_CraftPos", GlobalPoint.originPoint);
            evaluate.SetFloat("_CurrentTime", Time.timeSinceLevelLoad);
            evaluate.SetFloats("_CameraFrustumPlanes", ActiveBuffers.planeNormals);
            

            evaluate.DispatchIndirect(evaluateIndex, indirectArgs, 0);
        }
        public void Hibernate() //Used in range check to clean up data that could be used again
        {
            if (noiseBuffer != null) { noiseBuffer.Release(); }
            if (distributeCountBuffer != null) { distributeCountBuffer.Release(); }
            if (indirectArgs != null) { indirectArgs.Release(); }
            if (positionBuffer != null) { positionBuffer.Release(); }

            active = false;
        }
        public void Cleanup()
        {
            //Debug.Log("Cleanup called for scatter: " + scatter.scatterName);   
           
            //Events
            pqsMod.OnForceEvaluate -= EvaluatePositions;
            pqsMod.OnRangeCheck -= AlternativeStart;
            GameEvents.onVesselChange.Remove(OnVesselSwitch);
            GameEvents.onVesselWillDestroy.Remove(OnVesselDestroyed);

            
            if (noiseBuffer != null) { noiseBuffer.Release(); noiseBuffer = null; }
            if (distributeCountBuffer != null) { distributeCountBuffer.Release(); distributeCountBuffer = null; }
            if (indirectArgs != null) { indirectArgs.Release(); indirectArgs = null; }
            if (positionBuffer != null) { positionBuffer.Release(); positionBuffer = null; }

            //if (dummyCount != null) { dummyCount.Release(); }

            //UnityEngine.Object.Destroy(distribute);
            UnityEngine.Object.Destroy(evaluate);
            //if (collisionHandler != null) { collisionHandler.Cleanup(); collisionHandler = null; }
            //ShaderPool.ReturnShader(distribute, shaderType);
            //ShaderPool.ReturnShader(evaluate, ComputeShaderType.evaluate);

            active = false;
            subdivided = false;
            cleaned = true;
        }
        
    }
}
