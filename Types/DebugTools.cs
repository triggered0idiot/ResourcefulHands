using System;
using UnityEngine;

namespace ResourcefulHands;

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