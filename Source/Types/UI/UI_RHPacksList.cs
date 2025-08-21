using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ResourcefulHands;

public class UI_RHPacksList : MonoBehaviour
{
    public static UI_RHPacksList? Instance;
    
    public ScrollRect? scrollRect;
    public Transform? container;
    public UI_RHPack? packTemplate;

    public void Awake()
    {
        Instance = this;
        scrollRect = this.GetComponentInChildren<ScrollRect>();
        container = scrollRect.content;
        packTemplate = container.Find("Pack").gameObject.AddComponent<UI_RHPack>();
        packTemplate.gameObject.SetActive(false);
    }

    // this attempts to refresh any ui elements in the packs list
    IEnumerator EnableCoroutine()
    {
        container?.gameObject.SetActive(false);
        yield return new WaitForSecondsRealtime(0.075f);
        container?.gameObject.SetActive(true);
    }

    public void OnEnable()
    {
        BuildList();
        CoroutineDispatcher.Dispatch(EnableCoroutine());
    }

    public void OnDisable()
    {
        if (ResourcePacksManager.LoadedPacks.Count == 0) return;
        
        ResourcePacksManager.SavePackOrder();
        ResourcePacksManager.SaveDisabledPacks();
    }
    
    public static List<UI_RHPack> LoadedPacks { get; private set; } = new List<UI_RHPack>();

    void ClearList()
    {
        if (container == null || packTemplate == null) return;
        
        LoadedPacks.Clear();
        for (int i = 0; i < container.childCount; i++)
        {
            Transform child = container.GetChild(i);
            if (child == packTemplate.transform) continue;

            Destroy(child.gameObject);
        }
    }
    
    public void BuildList()
    {
        ClearList();
        foreach (var pack in ResourcePacksManager.LoadedPacks)
        {
            var newPackUI = Instantiate(packTemplate, container);
            if(newPackUI == null) continue;
            
            var pack1 = pack;
            newPackUI.Load(pack, ResourcePacksManager.ActivePacks.FirstOrDefault(p => p == pack1) != null);
            newPackUI.gameObject.name = pack.guid;
            newPackUI.gameObject.SetActive(true);
            LoadedPacks.Add(newPackUI);
        }

        UpdateAllHandToggleStates();
    }

    /// Calling this shows the ui blocker
    public void ReloadPacks()
    {
        GameObject? reloadOverlay =
            transform.FindAt<RawImage>("Scroll View/Viewport/ReloadNotice")?.gameObject;
        if(reloadOverlay != null)
            reloadOverlay.SetActive(true);
        ResourcePacksManager.ReloadPacks(waitTillReady:true, callback: () =>
        {
            if(reloadOverlay != null)
                reloadOverlay.SetActive(false);
            if (!ResourcePacksManager.IsUsingRHPacksFolder || RHSettingsManager.HasShownRHConfigNotice) return;
                        
            RHSettingsManager.HasShownRHConfigNotice = true;
            RHSettingsManager.ShowPopup("Warning - RHPacks", "Some packs that you are using use the RHPacks folder, it is recommended that they use the plugins folder instead.\nPlease notify the developers of these packs if they don't know already.");
        });
    }
    
    public void UpdateAllHandToggleStates()
    {
        foreach (var loadedPack in UI_RHPacksList.LoadedPacks)
            loadedPack?.UpdateHandToggleState();
    }
}