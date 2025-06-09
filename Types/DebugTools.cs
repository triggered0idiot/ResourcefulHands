using System;
using UnityEngine;

namespace ResourcefulHands;

// TODO: add debug tools
public class DebugTools : MonoBehaviour
{
    public static DebugTools Instance;

    public void Awake()
    {
        Instance = this;
    }

    public void OnGUI()
    {
        
    }
}