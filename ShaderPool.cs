using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Grass
{
    public enum ComputeShaderType
    {
        distributeFixed,
        distributeNearest,
        distributeFixedToHeight,
        evaluate,
        divider
    }
    public class ShaderPool     //To avoid instantiating a compute shader for every scatter on each quad at runtime (which takes a fair amount of cpu time), grab them from a pool
    {                           //The instance MUST be returned to the pool when the quad is destroyed!!!
        public static List<ComputeShader> distributeNearestShaders = new List<ComputeShader>();     //Not many of these per planet, only store 150
        public static List<ComputeShader> distributeFixedShaders = new List<ComputeShader>();       //Many of these, store a lot
        public static List<ComputeShader> evaluateShaders = new List<ComputeShader>();              //A lot of these, store a lot
        public static List<ComputeShader> dividerShaders = new List<ComputeShader>();

        //When removing from the list, use removeAt(count - 1) to avoid big CPU time reordering the list
        //Return by just using .Add()
        //I hate optimization I've been at it for days
        public static ComputeShader FetchShader(ComputeShaderType type)
        {
            ComputeShader shader;
            if (distributeFixedShaders.Count == 0 || distributeNearestShaders.Count == 0 || evaluateShaders.Count == 0)
            {
                Debug.Log("[Exception] ShaderPool has run out of shaders!");
                Debug.Log("DistributeFixed: " + distributeFixedShaders.Count);
                Debug.Log("DistributeNearest: " + distributeNearestShaders.Count);
                Debug.Log("Evaluate: " + evaluateShaders.Count);
            }
            if (distributeFixedShaders.Count % 10 == 0)
            {
                Debug.Log("DF: " + distributeFixedShaders.Count);
            }
            if (distributeNearestShaders.Count % 10 == 0)
            {
                Debug.Log("DN: " + distributeNearestShaders.Count);
            }
            if (evaluateShaders.Count % 10 == 0)
            {
                Debug.Log("EV: " + evaluateShaders.Count);
            }
            if (type == ComputeShaderType.distributeFixed)
            {
                shader = distributeFixedShaders[distributeFixedShaders.Count - 1];
                distributeFixedShaders.RemoveAt(distributeFixedShaders.Count - 1);
                return shader;
            }
            else if (type == ComputeShaderType.distributeNearest)
            {
                shader = distributeNearestShaders[distributeNearestShaders.Count - 1];
                distributeNearestShaders.RemoveAt(distributeNearestShaders.Count - 1);
                return shader;
            }
            else if (type == ComputeShaderType.distributeFixedToHeight)
            {
                Debug.Log("Cry about it");
            }
            else if (type == ComputeShaderType.evaluate)
            {
                shader = evaluateShaders[evaluateShaders.Count - 1];
                evaluateShaders.RemoveAt(evaluateShaders.Count - 1);
                return shader;
            }
            else if (type == ComputeShaderType.divider)
            {
                shader = dividerShaders[dividerShaders.Count - 1];
                dividerShaders.RemoveAt(dividerShaders.Count - 1);
                return shader;
            }
            
            Debug.Log("[Exception] ShaderPool attempting to return null compute shader");
            return null;

        }
        public static void ReturnShader(ComputeShader shader, ComputeShaderType type)   //Returned shaders will have their values set, but this shouldn't matter as they won't be dispatched here
        {
            if (type == ComputeShaderType.distributeFixed)
            {
                distributeFixedShaders.Add(shader);
            }
            if (type == ComputeShaderType.distributeNearest)
            {
                distributeNearestShaders.Add(shader);
            }
            if (type == ComputeShaderType.evaluate)
            {
                evaluateShaders.Add(shader);
            }
            if (type == ComputeShaderType.divider)
            {
                dividerShaders.Add(shader);
            }
        }
    }
}
