using System.Collections.Generic;
using UnityEngine;
namespace nostra.SarvotamSolutions.SquardShooterMultiplayer
{
    public class ObjectPoolManager : MonoBehaviour
    {
        private Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>();
        private Dictionary<string, GameObject> prefabDictionary = new Dictionary<string, GameObject>(); // Store prefabs for dynamic creation

        public SoundManage soundmanger;
        public void CreatePool(string key, GameObject prefab, int initialSize, Transform parent)
        {
            if (!poolDictionary.ContainsKey(key))
            {
                Queue<GameObject> objectPool = new Queue<GameObject>();

                for (int i = 0; i < initialSize; i++)
                {
                    GameObject obj = Instantiate(prefab, parent);
                    if (obj.GetComponent<Bullet>())
                    {
                        obj.GetComponent<Bullet>().objectPolling = this;
                    }
                    for (int j = 0; j < obj.transform.childCount; j++)
                    {
                        if (obj.transform.GetChild(j).GetComponent<AudioSource>())
                            soundmanger.bullets.Add(obj.transform.GetChild(j).GetComponent<AudioSource>());

                    }

                    obj.SetActive(false);
                    objectPool.Enqueue(obj);
                }

                poolDictionary[key] = objectPool;
                prefabDictionary[key] = prefab; // Store the prefab reference
            }
        }

        public GameObject GetFromPool(string key, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (poolDictionary.ContainsKey(key))
            {
                // If the pool is empty, create a new object dynamically
                if (poolDictionary[key].Count == 0)
                {
                    Debug.LogWarning($"Pool empty for key: {key}. Creating a new instance.");
                    if (prefabDictionary.ContainsKey(key))
                    {
                        GameObject newObj = Instantiate(prefabDictionary[key], position, rotation, parent);
                        newObj.SetActive(true);
                        if (newObj.GetComponent<AudioSource>())
                        {
                            for (int i = 0; i < newObj.transform.childCount; i++)
                            {
                                if (newObj.transform.GetChild(i).GetComponent<AudioSource>())
                                    soundmanger.bullets.Add(newObj.transform.GetChild(i).GetComponent<AudioSource>());

                            }

                        }
                        return newObj;
                    }
                    else
                    {
                        Debug.LogError($"Prefab not found for key: {key}. Ensure it's registered.");
                        return null;
                    }
                }

                // Retrieve an object from the pool
                GameObject obj = poolDictionary[key].Dequeue();
                obj.transform.SetPositionAndRotation(position, rotation);
                obj.SetActive(true);
                return obj;
            }
            else
            {
                Debug.LogError($"No pool found for key: {key}. Ensure it's created first.");
                return null;
            }
        }



        public void ReturnToPool(string key, GameObject obj)
        {
            if (!poolDictionary.ContainsKey(key))
            {
                Debug.LogWarning($"No pool found for key: {key}");
                return;
            }

            obj.SetActive(false);
            poolDictionary[key].Enqueue(obj);
        }
    }
}