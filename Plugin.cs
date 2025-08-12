using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
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

[BepInPlugin(GUID, "Resourceful Hands", "0.9.61")] // Resourceful Hands
public class Plugin : BaseUnityPlugin
{
    public const string GUID = "triggeredidiot.wkd.resourcefulhands";
    public const string ModifiedStr = " [modified asset]";

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

    internal static void RefreshTextures()
    {
        SpriteRendererPatches._customSpriteCache.Clear();
        
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
            if (material != null)
            {
                if (material.HasTexture(mainTex))
                    material.mainTexture = material.mainTexture;

                // TODO: is a try catch necessary here?
                try
                { material.SetTexture(corruptTextureID, corruptTexture); }
                catch (Exception e)
                { RHLog.Error(e); }
            }
        }

        foreach (var spriteR in FindObjectsOfType<SpriteRenderer>(includeInactive: true))
            spriteR.sprite = spriteR.sprite;
    }
    
    internal static void RefreshSounds()
    {
        // do some manipulation to the variables to trigger the harmony patches to replace them
        List<AudioSource> allAudioSources = Resources.FindObjectsOfTypeAll<AudioSource>().ToList();
        
        foreach (var audioSource in allAudioSources)
        { audioSource.clip = audioSource.clip; }
        foreach (var audioSource in Object.FindObjectsOfType<AudioSource>(true))
        { audioSource.clip = audioSource.clip; }
    }

    IEnumerator LoadCustomSettings(UI_SettingsMenu settingsMenu)
    {
        yield return new WaitForSecondsRealtime(1.0f);
        
        RHLog.Info("Loading custom settings menu...");
        if (Assets == null)
        {
            RHLog.Warning("No assets?");
            yield break;
        }
        try
        {
            var tabGroups = settingsMenu.GetComponentsInChildren<UI_TabGroup>();
            UI_TabGroup? tabGroup = tabGroups.FirstOrDefault(tabGroup => tabGroup.name.ToLower() == "tab selection hor");
            if (tabGroup != null)
            {
                GameObject button = Object.Instantiate(Assets.LoadAsset<GameObject>("Packs"),
                    tabGroup.transform, false);
                Button buttonButton = button.GetComponentInChildren<Button>();
                TextMeshProUGUI buttonTmp = button.GetComponentInChildren<TextMeshProUGUI>();

                GameObject menu = Object.Instantiate(Assets.LoadAsset<GameObject>("Pack Settings"),
                    tabGroup.transform.parent, false);
                
                Button reloadButton = menu.transform.Find("Reload")
                    .GetComponentInChildren<Button>();
                reloadButton.onClick.AddListener(ResourcePacksManager.ReloadPacks);
                Button openFolder = menu.transform.Find("OpenFolder")
                    .GetComponentInChildren<Button>();
                openFolder.onClick.AddListener(() => Application.OpenURL("file://" + RHConfig.PacksFolder.Replace("\\", "/")));
                
                menu.AddComponent<UI_RHPacksList>();
                menu.SetActive(false);

                for (int i = 0; i < tabGroup.transform.childCount; i++)
                {
                    Transform child = tabGroup.transform.GetChild(i);
                    string cName = child.name.ToLower();
                    if (cName.StartsWith("lb") || cName.StartsWith("rb"))
                        child.gameObject.SetActive(false);
                }

                var prevTab = tabGroup.tabs.FirstOrDefault();
                if (prevTab != null)
                {
                    buttonTmp.font = prevTab.button.GetComponentInChildren<TextMeshProUGUI>().font;
                    for (int i = 0; i < prevTab.tabObject.transform.childCount; i++)
                    {
                        Transform child = prevTab.tabObject.transform.GetChild(i);
                        if (child.name.ToLower().Contains("title"))
                        {
                            TextMeshProUGUI title = child.GetComponentInChildren<TextMeshProUGUI>();
                            if (title)
                            {
                                GameObject copiedTitle = Object.Instantiate(child.gameObject, menu.transform, true);
                                var tmp = copiedTitle.GetComponentInChildren<TextMeshProUGUI>();
                                tmp.text = "PACKS";
                                
                                TextMeshProUGUI[] texts = menu.GetComponentsInChildren<TextMeshProUGUI>(includeInactive: true);
                                foreach (var text in texts)
                                    text.font = tmp.font;
                            }
                        }
                    }
                }

                var tab = new UI_TabGroup.Tab
                {
                    button = buttonButton,
                    name = "packs",
                    tabObject = menu
                };
                buttonButton.onClick.AddListener(() => { tabGroup.SelectTab("packs"); });
                tabGroup.tabs.Add(tab);
            }
        }
        catch (Exception e)
        {
            RHLog.Error("Failed to load custom settings menu:\n"+e.ToString());
        }
    }
    
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
        
        Harmony = new Harmony(GUID);
        Harmony.PatchAll();

        bool hasLoadedIntro = false;
        SceneManager.sceneLoaded += (scene, mode) =>
        {
            if(!scene.name.ToLower().Contains("intro") && !hasLoadedIntro)
            {
                hasLoadedIntro = true;
                RHLog.Info("Loading internal assets...");
                Assets?.LoadAllAssets();

                CoroutineDispatcher.AddToUpdate(() =>
                {
                    var spriteRenderers = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
                    foreach (var sr in spriteRenderers) // TODO: forgive me for my sins
                        sr.sprite = sr.sprite;
                });
                
                RHLog.Info("Loading debug tools...");
                DebugTools.Create();
            }
            
            if (!hasLoadedIntro)
                return;
            
            RHLog.Info("Checking packs state...");
            if (ResourcePacksManager.HasPacksChanged)
                ResourcePacksManager.ReloadPacks();
            
            RHLog.Info("Refreshing custom commands...");
            RHCommands.RefreshCommands();

            RHLog.Info("Loading settings menu...");
            var settingsMenu = Object.FindObjectsOfType<UI_SettingsMenu>(true).FirstOrDefault(m => m.gameObject.scene == scene);
            if (settingsMenu && Assets != null) // right now i don't think there is a "standard" way to inject a custom menu into settings, so this will prolly break if another mod does this too
            {
                CoroutineDispatcher.Dispatch(LoadCustomSettings(settingsMenu));
            }
            
            RHLog.Info("Refreshing assets...");
            RefreshTextures();
            RefreshSounds();
        };
        
        RHLog.Info("Resourceful Hands has loaded!");
    }
}
// amongus