using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace ResourcefulHands;

// TODO: add debug tools
public class DebugTools : MonoBehaviour
{
    public static DebugTools Instance;
    public static bool isOn = false;

    private static List<AudioClip> _playingClips = new List<AudioClip>();
    private GUIStyle _style = GUIStyle.none;

    internal static void Create()
    {
        Instance = new GameObject("DebugTools").AddComponent<DebugTools>();
        DontDestroyOnLoad(Instance);
    }
    
    public static void QueueSound(AudioClip clip, bool force = false)
    {
        CoroutineDispatcher.Dispatch(_queueSound(clip, force));
    }
    private static IEnumerator _queueSound(AudioClip clip, bool force)
    {
        if(!force && _playingClips.Contains(clip))
            yield break;
        
        RHLog.Debug($"Queuing sound '{clip.name}'");
        _playingClips.Add(clip);
        yield return new WaitForSeconds(1.0f);
        if(clip)
            _playingClips.Remove(clip);
    }
    
    public void Awake()
    {
        Instance = this;
#if DEBUG
        isOn = true;
#endif
        
        _style = new GUIStyle
        {
            fontSize = 32,
            fontStyle = FontStyle.Bold
        };
    }

    public void OnGUI()
    {
        if(!isOn) return;
        
        GUILayout.Label("Recent sounds:", _style);
        foreach (var clip in _playingClips)
        {
            GUILayout.Label(clip.name, _style);
        }
    }
}


[HarmonyPatch(typeof(AudioSource))]
[HarmonyPriority(Priority.Low)]
public static class DEBUG_AudioSourcePatches
{
    [HarmonyPatch(methodName:"set_clip")]
    [HarmonyPostfix]
    private static void Setter_Postfix(AudioSource __instance, ref AudioClip value)
    {
        if (value == null) return;
        
        if (__instance.playOnAwake && Time.timeSinceLevelLoad <= 0.25f)
        {
            DebugTools.QueueSound(value);
        }
    }
    
    [HarmonyPatch(methodName:"get_clip")]
    [HarmonyPostfix]
    private static void Getter_Postfix(AudioSource __instance, ref AudioClip __result)
    {
        if (__result == null) return;
        
        if (__instance.playOnAwake && Time.timeSinceLevelLoad <= 0.25f)
        {
            DebugTools.QueueSound(__result);
        }
    }
    
    [HarmonyPatch(methodName:"Play", argumentTypes: [typeof(double)])]
    [HarmonyPostfix]
    private static void Play_Prefix(AudioSource __instance, double delay)
    {
        if(__instance.clip == null) return;
        
        DebugTools.QueueSound(__instance.clip);
    }
    
    [HarmonyPatch(methodName:"PlayHelper", argumentTypes: [typeof(AudioSource), typeof(ulong)])]
    [HarmonyPostfix]
    private static void PlayHelper_Prefix(AudioSource source, ulong delay)
    {
        if(source.clip == null) return;
        
        DebugTools.QueueSound(source.clip);
    }
}