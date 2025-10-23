using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ResourcefulHands;

// TODO: this needs alotta work to be effective
public class RHDebugTools : MonoBehaviour
{
    public static RHDebugTools? Instance;
    public static bool isOn = false;

    private static List<AudioClip> _playingClips = new List<AudioClip>();
    private GUIStyle _style = GUIStyle.none;
    private bool _enableNextFrame = false;

    internal static void Create()
    {
        Instance = new GameObject("DebugTools").AddComponent<RHDebugTools>();
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
        
        _playingClips.Add(clip);
        yield return new WaitForSeconds(1.0f);
        if(clip)
            _playingClips.Remove(clip);
    }
    
    public void Awake()
    {
        Instance = this;
        if (RHConfig.AlwaysDebug)
            isOn = true;
        
        _style = new GUIStyle
        {
            fontSize = 32,
            fontStyle = FontStyle.Bold,
            normal = new GUIStyleState { textColor = Color.white }
        };

        SceneManager.sceneUnloaded += arg0 =>
        {
            _enableNextFrame |= isOn;
            isOn = false;
        };
        SceneManager.sceneLoaded += (arg0, mode) =>
        {
            if (mode != LoadSceneMode.Single) return;
            _playingClips.Clear();
            _enableNextFrame |= isOn;
            isOn = false;
        };
    }

    public void OnGUI()
    {
        if(!isOn) return;

        var prevColor = GUI.contentColor;
        
        GUI.contentColor = Color.white;
        GUILayout.Label("Recent sounds:", _style);
        for (int i = _playingClips.Count - 1; i >= 0; i--)
        {
            if (_playingClips[i] == null)
                _playingClips.RemoveAt(i);
        }
        foreach (var clip in _playingClips)
            GUILayout.Label(clip.name, _style);
        
        GUI.contentColor = prevColor;
    }

    public void LateUpdate()
    {
        if (!_enableNextFrame) return;
        
        isOn = true;
        _enableNextFrame = false;
    }
}


[HarmonyPatch(typeof(UnityEngine.Object))]
public static class DEBUG_InstantiatePatches
{
    private static void OnInstantiated(UnityEngine.Object result, UnityEngine.Object original)
    {
        if (result == null) return;
        
        void PatchObject(UnityEngine.Object obj)
        {
            switch (obj)
            {
                case AudioSource audio:
                    if(audio is { isActiveAndEnabled: true, playOnAwake: true })
                        RHDebugTools.QueueSound(audio.clip);
                    break;
            }
        }
        
        switch (result)
        {
            case GameObject go:
            {
                Component[] comps = go.GetComponentsInChildren<Component>();
                foreach (Component comp in comps)
                    PatchObject(comp);
                break;
            }
            case Component component:
            {
                Component[] comps = component.GetComponentsInChildren<Component>();
                foreach (Component comp in comps)
                    PatchObject(comp);
                break;
            }
        }
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(nameof(UnityEngine.Object.Instantiate), new[] { typeof(UnityEngine.Object) })]
    private static void Postfix_1(UnityEngine.Object __result, UnityEngine.Object original) 
        => OnInstantiated(__result, original);

    [HarmonyPostfix]
    [HarmonyPatch(nameof(UnityEngine.Object.Instantiate), new[] { typeof(UnityEngine.Object), typeof(UnityEngine.SceneManagement.Scene) })]
    private static void Postfix_2(UnityEngine.Object __result, UnityEngine.Object original)
        => OnInstantiated(__result, original);

    [HarmonyPostfix]
    [HarmonyPatch(nameof(UnityEngine.Object.Instantiate), new[] { typeof(UnityEngine.Object), typeof(Transform) })]
    private static void Postfix_3(UnityEngine.Object __result, UnityEngine.Object original)
        => OnInstantiated(__result, original);

    [HarmonyPostfix]
    [HarmonyPatch(nameof(UnityEngine.Object.Instantiate), new[] { typeof(UnityEngine.Object), typeof(Transform), typeof(bool) })]
    private static void Postfix_4(UnityEngine.Object __result, UnityEngine.Object original)
        => OnInstantiated(__result, original);

    [HarmonyPostfix]
    [HarmonyPatch(nameof(UnityEngine.Object.Instantiate), new[] { typeof(UnityEngine.Object), typeof(Vector3), typeof(Quaternion) })]
    private static void Postfix_5(UnityEngine.Object __result, UnityEngine.Object original)
        => OnInstantiated(__result, original);

    [HarmonyPostfix]
    [HarmonyPatch(nameof(UnityEngine.Object.Instantiate), new[] { typeof(UnityEngine.Object), typeof(Vector3), typeof(Quaternion), typeof(Transform) })]
    private static void Postfix_6(UnityEngine.Object __result, UnityEngine.Object original)
        => OnInstantiated(__result, original);
}

[HarmonyPatch(typeof(AudioSource))]
[HarmonyPriority(Priority.Low)]
public static class DEBUG_AudioSourcePatches
{
    // CODE FROM: Patches.cs
    
    // Patch Play()
    [HarmonyPatch(nameof(AudioSource.Play), [])]
    [HarmonyPrefix]
    private static void Play_NoArgs_Postfix(AudioSource __instance)
        => LogClip(src:__instance);

    // Patch Play(double delay)
    [HarmonyPatch(nameof(AudioSource.Play), new[] { typeof(double) })]
    [HarmonyPrefix]
    private static void Play_DelayDouble_Postfix(AudioSource __instance)
        => LogClip(src:__instance);

    // Patch Play(ulong delaySamples)
    [HarmonyPatch(nameof(AudioSource.Play), new[] { typeof(ulong) })]
    [HarmonyPrefix]
    private static void Play_DelayUlong_Postfix(AudioSource __instance)
        => LogClip(src:__instance);
    
    // Patch PlayOneShot(AudioClip)
    [HarmonyPatch(nameof(AudioSource.PlayOneShot), typeof(AudioClip))]
    [HarmonyPrefix]
    private static void PlayOneShot_ClipOnly_Postfix(AudioSource __instance, ref AudioClip __0)
    {
        LogClip(clip:__0);
    }

    // Patch PlayOneShot(AudioClip, float volumeScale)
    [HarmonyPatch(nameof(AudioSource.PlayOneShot), typeof(AudioClip), typeof(float))]
    [HarmonyPrefix]
    private static void PlayOneShot_ClipAndVolume_Postfix(AudioSource __instance, ref AudioClip __0)
    {
        LogClip(clip:__0);
    }
    
    // Shared logic
    private static void LogClip(AudioSource src = null!, AudioClip clip = null!)
    {
        if(!RHDebugTools.isOn) return;
        
        if(src != null) clip = src.clip;
        if(clip == null) return;
        
        RHDebugTools.QueueSound(clip);
    }
}