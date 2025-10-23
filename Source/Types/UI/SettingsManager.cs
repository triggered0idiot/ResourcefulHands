using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ResourcefulHands;

public static class RHSettingsManager
{
    private static TMP_FontAsset? _fontAsset = null;
    public static TMP_FontAsset? UiFont {
        get
        {
            if (_fontAsset != null) return _fontAsset;
            
            var fonts = Resources.FindObjectsOfTypeAll<TMPro.TMP_FontAsset>();
            foreach (var font in fonts)
            {
                if (!string.Equals(font.name, "Ticketing SDF", StringComparison.CurrentCultureIgnoreCase)) continue;
                    
                _fontAsset = font;
                break;
            }
            return _fontAsset;
        }
    }
    
    /// Creates and attaches the custom settings menu (pack selection) to the settings window.
    public static void LoadCustomSettings()
    {
        var scene = SceneManager.GetActiveScene();
        var settingsMenu = Object.FindObjectsOfType<UI_SettingsMenu>(true).FirstOrDefault(m => m.gameObject.scene == scene);
        if (settingsMenu != null && Plugin.Assets != null) // right now i don't think there is a "standard" way to inject a custom menu into settings, so this will prolly break if another mod does this too
        {
            CoroutineDispatcher.Dispatch(LoadCustomSettings_Internal(settingsMenu));
        }
    }
    
    private static IEnumerator LoadCustomSettings_Internal(UI_SettingsMenu settingsMenu)
    {
        yield return new WaitForSecondsRealtime(1.0f);
        
        RHLog.Info("Loading custom settings menu...");
        if (Plugin.Assets == null)
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
                GameObject button = Object.Instantiate(Plugin.Assets.LoadAsset<GameObject>("Packs"),
                    tabGroup.transform, false);
                Button buttonButton = button.GetComponentInChildren<Button>();
                TextMeshProUGUI buttonTmp = button.GetComponentInChildren<TextMeshProUGUI>();

                GameObject menu = Object.Instantiate(Plugin.Assets.LoadAsset<GameObject>("Pack Settings"),
                    tabGroup.transform.parent, false);
                
                Transform buttons = menu.transform.Find("ButtonsHolder");
                Button reloadButton = buttons.Find("Reload")
                    .GetComponentInChildren<Button>();
                Button enableAllButton = buttons.Find("EnableAll")
                    .GetComponentInChildren<Button>();
                Button disableAllButton = buttons.Find("DisableAll")
                    .GetComponentInChildren<Button>();
                Button openFolder = buttons.Find("OpenFolder")
                    .GetComponentInChildren<Button>();
                
                openFolder!.onClick?.AddListener(() => Application.OpenURL("file://" + RHConfig.PacksFolder.Replace("\\", "/")));
                
                enableAllButton.onClick.AddListener(() =>
                {
                    ResourcePacksManager.LoadedPacks.ForEach(p => p.IsActive = true);
                    UI_RHPacksList.Instance?.ReloadPacks();
                });
                disableAllButton.onClick.AddListener(() =>
                {
                    ResourcePacksManager.LoadedPacks.ForEach(p => p.IsActive = false);
                    UI_RHPacksList.Instance?.ReloadPacks();
                });
                
                reloadButton.onClick.AddListener(() =>
                {
                    UI_RHPacksList.Instance?.ReloadPacks();
                });
                
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
                        if (!child.name.ToLower().Contains("title")) continue;
                        
                        TextMeshProUGUI title = child.GetComponentInChildren<TextMeshProUGUI>();
                        if (!title) continue;
                        
                        GameObject copiedTitle = Object.Instantiate(child.gameObject, menu.transform, true);
                        var tmp = copiedTitle.GetComponentInChildren<TextMeshProUGUI>();
                        tmp.text = "PACKS";
                        
                        TextMeshProUGUI[] texts = menu.GetComponentsInChildren<TextMeshProUGUI>(includeInactive: true);
                        foreach (var text in texts)
                            text.font = tmp.font;
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
    
    private static GameObject CreateCanvas()
    {
        GameObject canvasGO = new GameObject("GenericCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        canvas.sortingOrder = 1024;

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<EventSystem>();
            eventSystemGO.AddComponent<StandaloneInputModule>();
        }

        Object.DontDestroyOnLoad(canvasGO);
        return canvasGO;
    }
    
    // used to show once that the rhconfig folder is out of date
    // TODO: make an option to hide this forever
    public static bool HasShownRHConfigNotice = false;
    /// <summary>
    /// Shows a full screen popup that has a title and description with an ok button.
    /// This doesn't have a logo on it.
    /// </summary>
    /// <param name="title">The title to show. (placed at the top)</param>
    /// <param name="text">The description to show. (placed roughly at the center)</param>
    public static void ShowPopup(string title, string text)
    {
        GameObject? popupPrefab = Plugin.Assets?.LoadAsset<GameObject>("Popup-Root");
        if (popupPrefab == null) return;
        
        var canvas = CreateCanvas();
        var popup = Object.Instantiate(popupPrefab, canvas.transform, false);

        var titleText = popup.transform.FindAt<TextMeshProUGUI>("Popup/Title");
        var descText = popup.transform.FindAt<TextMeshProUGUI>("Popup/Desc");
        var okButton = popup.GetComponentInChildren<Button>(includeInactive:true);

        var font = UiFont ?? titleText!.font;
        
        titleText!.text = title;
        titleText!.font = font;
        
        descText!.text = text;
        descText!.font = font;
        
        okButton!.onClick?.AddListener(() =>
            { Object.Destroy(canvas, 0.125f); }
        );
        okButton!.GetComponentInChildren<TextMeshProUGUI>()!.font = font;
        
        popup.SetActive(true);
    }
    
    /// <summary>
    /// Shows a notification-esque message that slides in at the top left of the screen.
    /// It has the RH logo at the left and text on the right.
    /// </summary>
    /// <param name="text"></param>
    public static void ShowNotice(string text)
    {
        GameObject? popupPrefab = Plugin.Assets?.LoadAsset<GameObject>("Notice-Root");
        if (popupPrefab == null) return;
        
        var canvas = CreateCanvas();
        var popup = Object.Instantiate(popupPrefab, canvas.transform, false);

        var descText = popup.transform.FindAt<TextMeshProUGUI>("Popup/Desc");
        descText!.text = text;
        descText!.font = UiFont ?? descText!.font;
        
        popup.SetActive(true);
        Object.Destroy(canvas, 5.45f);
    }
}