using System.Collections;
using UnityEngine;

namespace ResourcefulHands;

public class CoroutineDispatcher : MonoBehaviour
{
    private static CoroutineDispatcher Instance;

    public static void Dispatch(IEnumerator routine)
    {
        if (Instance == null)
        {
            Instance = new GameObject("CoroutineDispatcher").AddComponent<CoroutineDispatcher>();
            DontDestroyOnLoad(Instance);
        }
        
        Instance.StartCoroutine(routine);
    }
}