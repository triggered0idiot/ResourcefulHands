using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

namespace ResourcefulHands;

public class CoroutineDispatcher : MonoBehaviour
{
    private static CoroutineDispatcher? _instance;
    private static Dictionary<string, Action> updateActions = new Dictionary<string, Action>();
    public static Queue<Action> threadQueue = new Queue<Action>();

    public static void Dispatch(IEnumerator routine)
    {
        if (_instance == null)
        {
            _instance = new GameObject("CoroutineDispatcher").AddComponent<CoroutineDispatcher>();
            DontDestroyOnLoad(_instance);
        }
        
        _instance.StartCoroutine(routine);
    }

    /// <summary>
    /// Same as <see cref="RunOnMainThread"/>, however used to halt execution of the current side thread until the action has ran on the main.
    /// </summary>
    /// <param name="action"></param>
    /// <param name="lineNumber"></param>
    /// <param name="file"></param>
    public static async Task RunOnMainThreadAndWait(Action action,
        [CallerLineNumber] int lineNumber = 0,
        [CallerFilePath] string file = "")
    {
        bool hasRan = false;
        threadQueue.Enqueue(() =>
        {
            RHLog.Debug($"Running [{Path.GetFileName(file)}:{lineNumber}] on the main thread...");
            try
            { action(); }
            catch (Exception e)
            { RHLog.Error(e); }
            hasRan = true;
        });

        while (!hasRan)
            await Task.Delay(16); // wait ~60fps
        
        RHLog.Debug($"[{Path.GetFileName(file)}:{lineNumber}] has executed!");
    }
    
    /// <summary>
    /// Queues the action to run on the next LateUpdate call i.e. the main unity thread.
    /// </summary>
    /// <param name="action"></param>
    public static void RunOnMainThread(Action action)
    {
        threadQueue.Enqueue(action);
    }
    
    /// <summary>
    /// Same as <see cref="RunOnMainThread"/>, however this actually checks if the current thread is the main.
    /// </summary>
    /// <param name="action"></param>
    public static void RunOnMainThreadOrCurrent(Action action)
    {
        if (Plugin.IsMainThread)
        {
            action();
            return;
        }
        
        threadQueue.Enqueue(action);
    }

    public static string AddToUpdate(Action action)
    {
        if (_instance == null)
        {
            _instance = new GameObject("CoroutineDispatcher").AddComponent<CoroutineDispatcher>();
            DontDestroyOnLoad(_instance);
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

        while (threadQueue.Count > 0)
        {
            Action action = threadQueue.Dequeue();
            if (action != null)
                action();
        }
    }
}