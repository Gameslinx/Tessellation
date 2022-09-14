using Grass;
using Grass.DebugStuff;
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
    public struct PositionData
    {
        //public Vector3 pos;
        public Matrix4x4 mat;
        //public Vector4 color;
        public static int Size()
        {
            return
                sizeof(float) * 16;
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
        public Dictionary<Scatter, ScatterCompute> comps = new Dictionary<Scatter, ScatterCompute>();
        public MeshData data;
        public MeshData subdividedData;
        string sphereName;
        public Mesh subMesh;
        public float quadWidth;
        public float sqrQuadWidth;
        public bool subdivisionAlreadyRequested = false; //ScatterCompute calls back to here to subdivide the quad only when it gets in range, and only once

        public ComputeBuffer vertexBuffer;
        public ComputeBuffer normalBuffer;
        public ComputeBuffer triBuffer;

        public ComputeBuffer subdividedVertexBuffer;
        public ComputeBuffer subdividedNormalBuffer;
        public ComputeBuffer subdividedTriangleBuffer;
        public Scatter[] scatters;
        public bool cleaned = false;

        public CollisionHandlerAdvanced collisionHandler;
        
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

            
            
            //subdividedData.vertices = subMesh.vertices;
            //subdividedData.normals = subMesh.normals;
            //subdividedData.tris = subMesh.triangles;
            sphereName = quad.sphereRoot.name;
            scatters = ScatterBodies.scatterBodies[sphereName].scatters.Values.ToArray();
            if (quad.subdivision == quad.sphereRoot.maxLevel)
            {
                collisionHandler = new CollisionHandlerAdvanced(ref quad, quadWidth * 1.5f);
                collisionHandler.maxDataCount = GetColliderDataCount();
            }
            ScatterManagerPlus.OnQuadRangeCheck += RangeCheck;
            RangeCheck();
            //InitializeScatters();
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
        public int GetColliderDataCount()   //Get number of scatters with colliders in this biome
        {
            int count = 0;
            for (int i = 0; i < scatters.Length; i++)
            {
                if (scatters[i].collideable && !InBlacklistedBiome(PQSMod_ScatterDistribute.scatterData.perQuadBiomeData[quad], scatters[i].properties.scatterDistribution.blacklist.fastBiomes))
                {
                    count++;
                }
            }
            return count;
        }
        public void DetermineQuadWidth()                        //Get distance between opposite diagonal verts on the quad to estimate its hypotenuse
        {
            if (data.vertices == null) { return; } //Yet again, wait for subdivision to complete :)
            Vector3 pos1 = quad.gameObject.transform.TransformPoint(data.vertices[0]);  //Top left
            Vector3 pos2 = quad.gameObject.transform.TransformPoint(data.vertices[data.vertices.Length - 1]); //Bottom right
            quadWidth = Vector3.Distance(pos1, pos2) / 1.5f;   //Distance from centre to corner
            sqrQuadWidth = quadWidth * quadWidth;

            //if (quad.subdivision == quad.sphereRoot.maxLevel)
            //{
            //    for (int i = 0; i < data.vertices.Length; i++)
            //    {
            //        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            //        go.transform.position = quad.gameObject.transform.TransformPoint(data.vertices[i]);
            //        go.transform.parent = quad.transform;
            //        go.GetComponent<MeshRenderer>().material.SetColor("_Color", Color.white * ((float)i / 225.0f));
            //        go.transform.localScale = Vector3.one * 60f;
            //    }
            //}

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
        public float GetNearestRangeToALoadedCraft()
        {
            float minDist = 100000;
            for (int i = 0; i < FlightGlobals.VesselsLoaded.Count; i++)
            {
                if (ScatterGlobalSettings.onlyQueryControllable && !FlightGlobals.VesselsLoaded[i].isCommandable) { continue; }
                float craftDist = Vector3.Distance(FlightGlobals.VesselsLoaded[i].transform.position, quad.gameObject.transform.position);
                if (craftDist < minDist) { minDist = craftDist; }
            }
            return minDist;
        }
        float dist = 0;
        bool containsComp = false;
        Scatter rangeCheckScatter;
        public void RangeCheck()
        {
            if (cleaned) { return; }
            dist = (GlobalPoint.originPoint - quad.transform.position).sqrMagnitude - sqrQuadWidth;
            for (int i = 0; i < scatters.Length; i++)
            {
                rangeCheckScatter = scatters[i];
                if (rangeCheckScatter.shared) { continue; }
                if ((quad.subdivision >= rangeCheckScatter.properties.subdivisionSettings.minLevel))  //Quad in subdivision limit - Will be auto destroyed on OnQuadDestroy so don't need to else {} here
                {
                    containsComp = comps.TryGetValue(rangeCheckScatter, out ScatterCompute comp);
      
                    if ((dist < rangeCheckScatter.properties.scatterDistribution._SqrRange) || (quad.subdivision == quad.sphereRoot.maxLevel && rangeCheckScatter.collideable))          //Quad in range limit - Clean it up when it's out of range
                    {
                        //Start the scatter
                        if (!containsComp)
                        {
                            if (InBlacklistedBiome(PQSMod_ScatterDistribute.scatterData.perQuadBiomeData[quad], rangeCheckScatter.properties.scatterDistribution.blacklist.fastBiomes))
                            {
                                continue;   //Don't add this one :)
                            }
                            if (rangeCheckScatter.properties.scatterDistribution.noise.noiseMode == DistributionNoiseMode.NonPersistent)
                            {
                                SubdivideQuad();
                            }
                            
                            ScatterComponent manager = ScatterManagerPlus.scatterComponents[sphereName].Find(x => x.scatter.scatterName == rangeCheckScatter.scatterName);
                            ScatterCompute newComp = new ScatterCompute(rangeCheckScatter, quad, manager, this, sphereName, ref data, ref subdividedData, ref collisionHandler);
                            comps.Add(rangeCheckScatter, newComp);
                        }
                    }
                    else    //Outside of range, clean up the scatter
                    {
                        if (containsComp)
                        {
                            if (!comp.cleaned)
                            {
                                comp.Cleanup();
                            }
                            comps.Remove(rangeCheckScatter);
                        }
                    }
                }
            }
        }
       
        public void Cleanup()   //Called when the quad is destroyed. Purge anything memory intensive, because this won't be destroyed for a while
        {
            if (vertexBuffer != null) { vertexBuffer.Release(); vertexBuffer = null; }
            if (normalBuffer != null) { normalBuffer.Release(); normalBuffer = null; }
            if (triBuffer != null) { triBuffer.Release(); triBuffer = null; }
            if (subdividedVertexBuffer != null) { subdividedVertexBuffer.Release(); subdividedVertexBuffer = null; }
            if (subdividedNormalBuffer != null) { subdividedNormalBuffer.Release(); subdividedNormalBuffer = null; }
            if (subdividedTriangleBuffer != null) { subdividedTriangleBuffer.Release(); subdividedTriangleBuffer = null; }
            if (collisionHandler != null) { collisionHandler.Cleanup(); }
            foreach (ScatterCompute scatter in comps.Values)
            {
                scatter.Cleanup();
            }
            comps.Clear();
            cleaned = true;
            ScatterManagerPlus.OnQuadRangeCheck -= RangeCheck;
            //quad.gameObject.GetComponent<MeshRenderer>().material.SetColor("_Color", new Color(0, 0, 0));
        }
    }
    public class ScatterCompute
    {
        public Scatter scatter;
        public Properties properties;
        public PQ quad;
        public ScatterComponent pqsMod;
        public QuadData parent;                 //Not great technique, but access the mesh data without duplicating its arrays each time
        public MeshData meshData;

        ComputeShader distribute;
        ComputeShader evaluate;

        ComputeShader trim;


        int distributeIndex;
        int evaluateIndex;
        int trimIndex;

        public ComputeBuffer vertexBuffer;  //References the buffers in QuadData
        public ComputeBuffer normalBuffer;
        public ComputeBuffer triBuffer;

        public ComputeBuffer noiseBuffer;
        public ComputeBuffer positionBuffer;

        public ComputeBuffer distributeCountBuffer;
        public ComputeBuffer indirectArgs;

        public ComputeBuffer trimBuffer;

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
        public bool subdivided = false;
        public QuadDistributionData distributionData;
        public bool updateTime = true;
        public PositionData[] transformData;            //Stores position, transform and colour data for each scatter object
        
        public CollisionHandlerAdvanced collisionHandler;
        public Matrix4x4 oqtw = Matrix4x4.identity;
        public Vector3 oldShaderOffset = Vector3.zero;

        public ScatterCompute(Scatter scatter, PQ quad, ScatterComponent pqsMod, QuadData parent, string sphereName, ref MeshData meshData, ref MeshData subdividedData, ref CollisionHandlerAdvanced collisionHandler)
        {
            this.scatter = scatter;
            this.quad = quad;
            this.pqsMod = pqsMod;
            this.parent = parent;
            this.sphereName = sphereName;
            this.meshData = meshData;   //Contains either regular or subdivided mesh
            quadWidth = parent.quadWidth;
            this.collisionHandler = collisionHandler;
            if (scatter.properties.subdivisionSettings.mode == SubdivisionMode.NearestQuads)
            {
                this.meshData = subdividedData;
            }
            else
            {
                this.meshData = meshData;
            }
            Setup();
            Start();
        }
        public Vector3 GetTotalMemoryUsage()
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

            float capacity = positionBuffer.count * positionBuffer.stride;  //Memory usage
            float actual = (float)count[0] * positionBuffer.stride;         //Memory usage if count was object count

            return new Vector3(total / (1024f * 1024f), capacity / (1024f * 1024f), actual / (1024f * 1024f));    //total memory usage, PB capacity, PB actual memory usage if cap = count
        }
        public void Setup()
        {
            pqsMod.OnForceEvaluate += EvaluatePositions;
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
            //distribute = pqsMod.InstantiateNew(scatter.properties.scatterDistribution.noise.noiseMode);
            //distributeIndex = distribute.FindKernel("DistributePoints");
            //if (cleaned) { UpdateQueue(); return; }
            //InitializeGenerate();
            //PrepareGenerate();
            //DispatchGenerate();
            //return;

            

            if (ComputeShaderAvailable())   //Generate immediately
            {
                distribute = pqsMod.computePool[pqsMod.computePool.Count - 1];  //Take last item on list to prevent re-ordering
                if (distribute == null) { distribute = pqsMod.InstantiateNew(scatter.properties.scatterDistribution.noise.noiseMode); } //Only happens in the space center scene, because dontdestroyonload doesn't work for compute shaders or something idk but yeah, number of total compute shaders does stay the same
                pqsMod.computePool.RemoveAt(pqsMod.computePool.Count - 1);
                if (cleaned) { UpdateQueue(); return; }
                distributeIndex = distribute.FindKernel("DistributePoints");
                
                InitializeGenerate();
                PrepareGenerate();
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
        public void SetupComputeShaders() 
        {
            trim = GameObject.Instantiate(ScatterShaderHolder.GetCompute("Trim"));
            trimIndex = trim.FindKernel("CSMain");

            evaluate = GameObject.Instantiate(ScatterShaderHolder.GetCompute("EvaluatePoints"));
            evaluateIndex = evaluate.FindKernel("EvaluatePoints");

            int maxLevel = quad.sphereRoot.maxLevel;
            if (meshData.tris != null) { triCount = meshData.tris.Length; } else { triCount = 1; }  //Wait for subdivision to determine tri count
            
            quadSubdivDifference = ((maxLevel - quad.subdivision) * 3) + 1;
            memoryUsage = (int)(triCount * properties.scatterDistribution._PopulationMultiplier * quadSubdivDifference * properties.scatterDistribution.noise._MaxStacks);
            generateDispatchAmount = Mathf.CeilToInt((((float)triCount) / 3f) / 32f);
        }
        public void InitializeGenerate()
        {
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
        public void PrepareGenerate() //Each time a quad is built, generate scatter positions
        {
            Utils.SetDistributionVars(ref distribute, scatter, quad.gameObject.transform, quadSubdivDifference, triCount, sphereName, distributeIndex);
            
            
            if (updateTime)
            {
                distribute.SetBool("updateTime", true);
            }
            else
            {
                distribute.SetBool("updateTime", false);
            }
        }
        public void DispatchGenerate()
        {
            positionBuffer.SetCounterValue(0);
            if (scatter == null) { Debug.Log("Scatter is null lmao"); }
            distribute.SetVector("_ShaderOffset", -(Vector3)FloatingOrigin.TerrainShaderOffset);
            oldShaderOffset = FloatingOrigin.TerrainShaderOffset;
            oqtw = quad.gameObject.transform.localToWorldMatrix;
            distribute.Dispatch(distributeIndex, generateDispatchAmount, scatter.properties.scatterDistribution.noise._MaxStacks, 1);
            ComputeBuffer.CopyCount(positionBuffer, distributeCountBuffer, 0);
            
            currentlyReadingDist = true;
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11)
            {
                AsyncGPUReadback.Request(distributeCountBuffer, AwaitDistributeReadback);
            }
            else
            {
                DirectDistributeReadback();
            }
        }
        private void AwaitDistributeReadback(AsyncGPUReadbackRequest req)
        {
            if (cleaned) { UpdateQueue(); return; }    //Quad was cleaned up in the time it took for the GPU to finish generating positions on this quad. L + No speed
            if (req.hasError)
            {
                ScatterLog.Log("[Exception] Async GPU Readback error! (In GeneratePositions())");
                UpdateQueue();
                return;
            }
            objectCount = req.GetData<int>(0).ToArray()[0];
            objectCount = (int)Mathf.Min(objectCount, positionBuffer.count);    //Buffer can sometimes be smaller than its counter at low spawn chances
            workGroups[0] = Mathf.CeilToInt(((float)objectCount) / 32f);
            indirectArgs.SetData(workGroups);
            currentlyReadingDist = false;

            if (scatter.collideable && quad.subdivision == quad.sphereRoot.maxLevel && !collisionHandler.allDataPresent)
            {
                transformData = new PositionData[objectCount];
                positionBuffer.GetData(transformData);
                collisionHandler.AddData(scatter, transformData, oqtw.inverse, oldShaderOffset);
            }
            InitializeTrim();
            InitializeEvaluate();
            UpdateQueue();          //Dequeue and process the next quad
        }
        private void DirectDistributeReadback()
        {
            if (cleaned) { UpdateQueue(); return; }    //Quad was cleaned up in the time it took for the GPU to finish generating positions on this quad. L + No speed

            int[] objectCountData = new int[1];
            distributeCountBuffer.GetData(objectCountData);
            objectCount = (int)Mathf.Min(objectCountData[0], positionBuffer.count);    //Buffer can sometimes be smaller than its counter at low spawn chances
            workGroups[0] = Mathf.CeilToInt(((float)objectCount) / 32f);
            indirectArgs.SetData(workGroups);
            currentlyReadingDist = false;

            if (scatter.collideable && quad.subdivision == quad.sphereRoot.maxLevel && !collisionHandler.allDataPresent)
            {
                transformData = new PositionData[objectCount];
                positionBuffer.GetData(transformData);
                collisionHandler.AddData(scatter, transformData, oqtw.inverse, oldShaderOffset);
            }
            InitializeTrim();
            InitializeEvaluate();
            UpdateQueue();
        }
        public void UpdateQueue()   //Return compute shader to the queue and process the next quad
        {
            //return;
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
        public void InitializeTrim()
        {
            
            if (currentlyReadingDist) { return; }
            if (objectCount == 0) { return; }
            if (!pqsMod.buffersCreated) { Debug.Log("[Exception] Buffers not created, returning"); return; }

            //Debug.Log("Initializing trim");
            if (trimBuffer != null) { trimBuffer.Release(); }
            trimBuffer = new ComputeBuffer(objectCount, PositionData.Size(), ComputeBufferType.Structured);
            trim.SetBuffer(trimIndex, "Input", positionBuffer);
            trim.SetBuffer(trimIndex, "Output", trimBuffer);
            trim.SetInt("maxCount", objectCount);

            //float oldImpact = ((float)positionBuffer.count * (float)PositionData.Size()) / (1024f * 1024f);
            //float newImpact = (((float)objectCount * (float)PositionData.Size()) / (1024f * 1024f));
            //float percChange = (((oldImpact - newImpact) / oldImpact) * 100f);

            trim.Dispatch(trimIndex, Mathf.CeilToInt((float)objectCount / 32f), 1, 1);
            positionBuffer.Release();
            
            //Debug.Log("Memory impact (MB): " + newImpact);   //In MB
            //Debug.Log(" - Old memory impact (MB): " + oldImpact);   //In MB
            //Debug.Log(" - Saving (%): " + percChange + "\n");
            
        }
        public void InitializeEvaluate()
        {
            if (currentlyReadingDist) { return; }
            if (objectCount == 0) { return; }
            if (!pqsMod.buffersCreated) { Debug.Log("[Exception] Buffers not created, returning"); return; }
            Utils.SetEvaluationVars(ref evaluate, scatter, quad.gameObject.transform, objectCount);
            evaluate.SetBuffer(evaluateIndex, "Grass", Buffers.activeBuffers[scatter.scatterName].buffer);
            evaluate.SetBuffer(evaluateIndex, "Positions", trimBuffer);
            evaluate.SetBuffer(evaluateIndex, "FarGrass", Buffers.activeBuffers[scatter.scatterName].farBuffer);
            evaluate.SetBuffer(evaluateIndex, "FurtherGrass", Buffers.activeBuffers[scatter.scatterName].furtherBuffer);
            
            initializedEvaluate = true;
            EvaluatePositions();
        }
        public void EvaluatePositions()
        {
            if (!quad.isVisible && scatter.properties.scatterDistribution.noise.noiseMode != DistributionNoiseMode.FixedAltitude) { return; }
            if (currentlyReadingDist) { return; }
            if (!initializedEvaluate) { return; }
            if (objectCount == 0) { return; }
            if (!quadMeshRenderer.isVisible && scatter.properties.scatterDistribution.noise.noiseMode != DistributionNoiseMode.FixedAltitude) { return; }
            if ((quad.gameObject.transform.position - GlobalPoint.originPoint).sqrMagnitude - parent.sqrQuadWidth > scatter.properties.scatterDistribution._SqrRange) { return; }

            evaluate.SetVector("_ShaderOffset", -((Vector3)FloatingOrigin.TerrainShaderOffset));
            evaluate.SetVector("_CameraPos", ActiveBuffers.cameraPos);
            evaluate.SetVector("_CraftPos", GlobalPoint.originPoint);
            evaluate.SetFloat("_CurrentTime", Time.timeSinceLevelLoad);
            evaluate.SetFloats("_CameraFrustumPlanes", ActiveBuffers.planeNormals);
            evaluate.DispatchIndirect(evaluateIndex, indirectArgs, 0);
            //pqsMod.pc.Setup(Buffers.activeBuffers[scatter.scatterName].buffer, Buffers.activeBuffers[scatter.scatterName].farBuffer, Buffers.activeBuffers[scatter.scatterName].furtherBuffer, scatter);

        }
        public void Cleanup()
        {
            pqsMod.OnForceEvaluate -= EvaluatePositions;
            GameEvents.onVesselChange.Remove(OnVesselSwitch);
            GameEvents.onVesselWillDestroy.Remove(OnVesselDestroyed);

            
            if (noiseBuffer != null) { noiseBuffer.Release(); noiseBuffer = null; }
            if (distributeCountBuffer != null) { distributeCountBuffer.Release(); distributeCountBuffer = null; }
            if (indirectArgs != null) { indirectArgs.Release(); indirectArgs = null; }
            if (positionBuffer != null) { positionBuffer.Release(); positionBuffer = null; }
            if (trimBuffer != null) { trimBuffer.Release(); trimBuffer = null; }
            //if (dummyCount != null) { dummyCount.Release(); }

            //UnityEngine.Object.Destroy(distribute);
            UnityEngine.Object.Destroy(evaluate);
            UnityEngine.Object.Destroy(trim);
            //UnityEngine.Object.Destroy(distribute);
            //if (collisionHandler != null) { collisionHandler.Cleanup(); collisionHandler = null; }
            //ShaderPool.ReturnShader(distribute, shaderType);
            //ShaderPool.ReturnShader(evaluate, ComputeShaderType.evaluate);
            subdivided = false;
            cleaned = true;
        }
        
    }
}
