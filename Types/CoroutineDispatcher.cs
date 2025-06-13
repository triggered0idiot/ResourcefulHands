using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ResourcefulHands;

public class CoroutineDispatcher : MonoBehaviour
{
    private static CoroutineDispatcher Instance;
    private static Dictionary<string, Action> updateActions = new Dictionary<string, Action>();

    public static void Dispatch(IEnumerator routine)
    {
        if (Instance == null)
        {
            Instance = new GameObject("CoroutineDispatcher").AddComponent<CoroutineDispatcher>();
            DontDestroyOnLoad(Instance);
        }
        
        Instance.StartCoroutine(routine);
    }

    public static string AddToUpdate(Action action)
    {
        if (Instance == null)
        {
            Instance = new GameObject("CoroutineDispatcher").AddComponent<CoroutineDispatcher>();
            DontDestroyOnLoad(Instance);
        }
        
        string guid = Guid.NewGuid().ToString();
        updateActions.Add(guid, action);
        return guid;
    }
    public static void RemoveFromUpdate(string guid)
    {
        if(updateActions.ContainsKey(guid))
            updateActions.Remove(guid);
    }

    public void LateUpdate()
    {
        foreach (var updateAction in updateActions.Values)
            updateAction();
    }
}