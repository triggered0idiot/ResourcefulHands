﻿using System;
using System.Collections;
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

    public void OnDisable() {
        if (ResourcePacksManager.LoadedPacks.Count != 0)
        {
            ResourcePacksManager.SavePackOrder();
            ResourcePacksManager.SaveDisabledPacks();
        }
    }

    void ClearList()
    {
        if (container == null || packTemplate == null) return;
        
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
        }
    }
}