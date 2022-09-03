using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Grass
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class ObjectPool : MonoBehaviour
    {
        public static List<GameObject> gameObjects = new List<GameObject>();
        public static PhysicMaterial material;
        //When removing from the list, use removeAt(count - 1) to avoid big CPU time reordering the list
        //Return by just using .Add()
        //I hate optimization I've been at it for days
        void Awake()
        {
            GameObject.DontDestroyOnLoad(this);
        }
        void Start()
        {
            ObjectPool.material = new PhysicMaterial();
            ObjectPool.material.dynamicFriction = 0.9f;
            ObjectPool.material.staticFriction = 0.9f;
            ObjectPool.material.frictionCombine = PhysicMaterialCombine.Maximum;
            ObjectPool.material.bounciness = 0;
            ObjectPool.material.bounceCombine = PhysicMaterialCombine.Average;

            for (int i = 0; i < 250; i++)
            {
                GameObject go = new GameObject();
                MeshCollider comp = go.AddComponent<MeshCollider>();
                comp.sharedMaterial = ObjectPool.material;
                go.AddComponent<AutoDisabler>();

                //go.AddComponent<MeshRenderer>();
                //go.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Standard"));
                //go.AddComponent<MeshFilter>();


                go.SetActive(false);
                GameObject.DontDestroyOnLoad(go);
                gameObjects.Add(go);
            }
        }
        public static GameObject FetchObject()   //Has components? Return true. If the object was instantiated instead, return false
        {
            if (gameObjects.Count > 0)
            {
                GameObject go = gameObjects[gameObjects.Count - 1];
                gameObjects.RemoveAt(gameObjects.Count - 1);
                return go;
            }
            else
            {
                GameObject go = new GameObject();
                go.AddComponent<MeshCollider>();
                go.AddComponent<AutoDisabler>();

                //go.AddComponent<MeshRenderer>();
                //go.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Standard"));
                //go.AddComponent<MeshFilter>();

                MeshCollider comp = go.AddComponent<MeshCollider>();
                comp.sharedMaterial = ObjectPool.material;
                GameObject.DontDestroyOnLoad(go);
                return go;
            }
        }
        public static void ReturnObject(GameObject go)   //Returned shaders will have their values set, but this shouldn't matter as they won't be dispatched here
        {
            go.transform.parent = null;
            gameObjects.Add(go);
        }
    }
}
