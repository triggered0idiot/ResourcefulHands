using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using ResourcefulHands.Patches;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ResourcefulHands;

// TODO: test for/fix crash when quitting game (unsure but this has happened at-least twice, possible due to the use of DebugTools.cs?)

[BepInPlugin(GUID, "Resourceful Hands", "0.9.70")] // Resourceful Hands
public class Plugin : BaseUnityPlugin
{
    public const string GUID = "triggeredidiot.wkd.resourcefulhands";
    public const string ModifiedStr = " [rh modified asset]";

    private static AssetBundle? _assets;
    public static AssetBundle? Assets
    {
        get
        {
            if (_assets != null) return _assets;
            
            Assembly assembly = Assembly.GetExecutingAssembly();
            string resourceName = $"ResourcefulHands.rh_assets.bundle";
            using Stream? stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
                _assets = AssetBundle.LoadFromStream(stream);

            CorruptionTexture = (_assets?.LoadAsset<Texture2D>("Corruption1"));
            Icon = (_assets?.LoadAsset<Texture2D>("icon"));
            IconGray = (_assets?.LoadAsset<Texture2D>("gray_icon"));
            
            return _assets;
        }
        private set => _assets = value;
    }
    public static Texture2D? CorruptionTexture;
    public static Texture2D? Icon;
    public static Texture2D? IconGray;
    
    public static Plugin Instance { get; private set; } = null!;
    internal static ManualLogSource Log { get; private set; } = null!; // create log source for RHLog
    
    public static bool IsDemo
    {
        get
        {
            try
            {
                var appid = Steamworks.SteamClient.AppId;
#if DEBUG
                RHLog.Info($"Appid: {appid}");
#endif
                if (appid.Value == 3218540) // 3195790 = full game, 3218540 = demo
                    return true;
            }catch(Exception e){RHLog.Error(e);}
            return false;
        }
    }
    
    public Harmony? Harmony { get; private set; }

    internal static void RefreshTextures() // TODO: refresh without fucky manipulation tatics?
    {
        RHSpriteManager.ClearSpriteCache();
        
        // do some manipulation to the variables to trigger the harmony patches to replace them
        List<Material> allMaterials = Resources.FindObjectsOfTypeAll<Material>().ToList();
        foreach (var renderer in FindObjectsOfType<Renderer>(includeInactive: true))
            allMaterials.AddRange(renderer.sharedMaterials.Where(mat => !allMaterials.Contains(mat)));
        int mainTex = Shader.PropertyToID("_MainTex");
        
        // special checks for the mass texture
        int corruptTextureID = Shader.PropertyToID("_CORRUPTTEXTURE");
        Texture2D? corruptTexture = ResourcePacksManager.GetTextureFromPacks("_CORRUPTTEXTURE");
        
        foreach (var material in allMaterials)
        {
            if (material == null) continue;
            
            if (material.HasTexture(mainTex))
                material.mainTexture = material.mainTexture;
                
            // TODO: is a try catch necessary here?
            try
            { material.SetTexture(corruptTextureID, corruptTexture); }
            catch (Exception e)
            { RHLog.Error(e); }
        }

        foreach (var spriteR in FindObjectsOfType<SpriteRenderer>(includeInactive: true))
            spriteR.sprite = spriteR.sprite;
        
        foreach (var img in FindObjectsOfType<Image>(includeInactive: true)) // TODO: fix the logo getting fucked up
        {
            img.sprite = img.sprite;
            img.overrideSprite = img.overrideSprite;
        }
    }
    
    internal static void RefreshSounds()
    {
        // do some manipulation to the variables to trigger the harmony patches to replace them
        List<AudioSource> allAudioSources = Resources.FindObjectsOfTypeAll<AudioSource>().ToList();

        foreach (var audioSource in allAudioSources)
            AudioSourcePatches.SwapClip(audioSource);
    }

    // TODO: remove jank
    internal static int targetFps = 60;
    public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public static bool IsMainThread => System.Threading.Thread.CurrentThread.ManagedThreadId == mainThreadId;
    internal static int mainThreadId;
    public void Awake()
    {
        mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
        
        Log = Logger;
        Instance = this;
        RHConfig.InitConfigs();
        
        if(IsWindows && RHConfig.ColoredConsole)
            AnsiSupport.EnableConsoleColors();
        
        RHLog.Debug("Patching...");
        Harmony = new Harmony(GUID);
        Harmony.PatchAll();

        RHLog.Debug("Hooking loaded event...");
        bool hasLoadedIntro = false;
        SceneManager.sceneLoaded += (scene, mode) =>
        {
            targetFps = Application.targetFrameRate;
            RHLog.Debug("Evaluating newly loaded scene...");
            if(!scene.name.ToLower().Contains("intro") && !hasLoadedIntro)
            {
                hasLoadedIntro = true;
                RHLog.Info("Loading internal assets...");
                Assets?.LoadAllAssets();

                if (RHConfig.UseOldSprReplace)
                {
                    RHLog.Info("Hooking sprite replacer...");
                    CoroutineDispatcher.AddToUpdate(() =>
                    {
                        var spriteRenderers = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
                        
                        foreach (var sr in spriteRenderers)
                            SpriteRendererPatches.Patch(sr);
                    });
                }
                else // TODO: eventually improve this to edit animators or sum?
                {
                    RHLog.Info("Queuing sprite replacer...");
                    CoroutineDispatcher.RunOnMainThread(() => //create isolated local context
                    {
                        var spriteRenderers = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
                        float lastTick = Time.time;
                        float tickSpace = 0.35f;
                        
                        Coroutine? c = null;
                        void CreateCoroutine()
                        {
                            if (c != null)
                                CoroutineDispatcher.StopDispatch(c);
                            
                            IEnumerator PollSpriteRenderers()
                            {
                                while (true)
                                {
                                    spriteRenderers = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
                                    lastTick = Time.time;
                                    yield return new WaitForSeconds(tickSpace);
                                }
                                // ReSharper disable once IteratorNeverReturns
                            }
                            c = CoroutineDispatcher.Dispatch(PollSpriteRenderers());
                        }
                        CreateCoroutine();
                        
                        CoroutineDispatcher.AddToUpdate(() =>
                        {
                            if (Time.time - lastTick > tickSpace * 32.0f)
                            {
                                RHLog.Warning("Sprite finder thread has been dead for a while, restarting...");
                                CreateCoroutine();
                            }
                            
                            foreach (var sr in spriteRenderers)
                                SpriteRendererPatches.Patch(sr);
                        });
                    });
                }
                
                RHLog.Info("Loading debug tools...");
                RHDebugTools.Create();
            }
            
            if (!hasLoadedIntro)
                return;
            
            RHLog.Info("Checking packs state...");
            if (ResourcePacksManager.HasPacksChanged)
                ResourcePacksManager.ReloadPacks(callback:(() =>
                { RHSettingsManager.ShowNotice("Packs have been auto reloaded!"); }));
            
            RHLog.Info("Refreshing custom commands...");
            RHCommands.RefreshCommands();

            RHLog.Info("Loading settings menu...");
            RHSettingsManager.LoadCustomSettings();
            
            RHLog.Info("Refreshing assets...");
            RefreshTextures();
            RefreshSounds();
        };
        
        RHLog.Message("Resourceful Hands has loaded!");
    }
}
// amongus