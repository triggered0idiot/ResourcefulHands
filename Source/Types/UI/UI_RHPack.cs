using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ResourcefulHands;

public class UI_RHPack : MonoBehaviour
{
    public RawImage? Icon; //  IconContainer/Icon
    
    public TextMeshProUGUI? Title; //  Title/
    public TextMeshProUGUI? Author; //  AuthorPanel/Author
    public Button? SteamAuthor; //  AuthorPanel/ViewOnSteam
    public Image? SteamSep; //  AuthorPanel/Sep
    public TextMeshProUGUI? Description; //  Desc/
    public TextMeshProUGUI? Guid; //  Guid/

    public Button? Up; //  Up/Arrow/
    public Button? Down; //  Down/Arrow/
    
    public Button? DisableToggle; //  Disable/
    public Image? DisableOff; //  Disable/Toggle/OffImg
    public Image? DisableOn; //  Disable/Toggle/ToggleImg
    
    public Button? FolderButton; //  Folder/
    
    public Button? LeftToggle; //  LeftHand/
    public Image? LeftToggleOff; //  LeftHand/Toggle/OffImg
    public Image? LeftToggleOn; //  LeftHand/Toggle/ToggleImg
    
    public Button? RightToggle; //  RightHand/
    public Image? RightToggleOff; //  RightHand/Toggle/OffImg
    public Image? RightToggleOn; //  RightHand/Toggle/ToggleImg

    private ResourcePack _pack = null!;
    
    public void Awake()
    {
        Icon = transform.FindAt<RawImage>("IconContainer/Icon");
        
        Title = transform.FindAt<TextMeshProUGUI>("Title");
        Description = transform.FindAt<TextMeshProUGUI>("Desc");
        Guid = transform.FindAt<TextMeshProUGUI>("Guid");
        
        Author = transform.FindAt<TextMeshProUGUI>("AuthorPanel/Author");
        SteamAuthor = transform.FindAt<Button>("AuthorPanel/ViewOnSteam");
        SteamSep = transform.FindAt<Image>("AuthorPanel/Sep");
        
        
        Up = transform.FindAt<Button>("Icons/Up");
        Down = transform.FindAt<Button>("Icons/Down");
        
        DisableToggle = transform.FindAt<Button>("Icons/Disable");
        DisableOff = transform.FindAt<Image>("Icons/Disable/Toggle/OffImg");
        DisableOn = transform.FindAt<Image>("Icons/Disable/Toggle/ToggleImg");
        
        FolderButton = transform.FindAt<Button>("Icons/OpenFolder");
        
        LeftToggle = transform.FindAt<Button>("Icons/LeftHand");
        LeftToggleOff = transform.FindAt<Image>("Icons/LeftHand/Toggle/OffImg");
        LeftToggleOn = transform.FindAt<Image>("Icons/LeftHand/Toggle/ToggleImg");
        
        RightToggle = transform.FindAt<Button>("Icons/RightHand");
        RightToggleOff = transform.FindAt<Image>("Icons/RightHand/Toggle/OffImg");
        RightToggleOn = transform.FindAt<Image>("Icons/RightHand/Toggle/ToggleImg");
    }

    // Loads the pack's values into the ui, this should only be called once.
    public void Load(ResourcePack pack, bool active = true)
    {
        _pack = pack;

        Icon!.texture = _pack.Icon;
        
        Title!.text = _pack.name;
        Author!.text = _pack.author;
        Description!.text = _pack.desc;
        Guid!.text = _pack.guid;

        DisableOff!.enabled = !active;
        DisableOn!.enabled = active;

        bool hasHands = _pack.HasHandTextures();
        LeftToggle!.gameObject?.SetActive(hasHands);
        RightToggle!.gameObject?.SetActive(hasHands); 
        
        SteamAuthor!.onClick?.AddListener(() =>
        { SteamFriends.OpenUserOverlay(pack.steamId, "steamid"); });
        if (pack.steamId <= 0)
        {
            SteamAuthor!.gameObject?.SetActive(false);
            SteamSep!.gameObject?.SetActive(false);
        }

        void Refresh() // put here for ease of access, additionally doesn't really need to be its own outside function
        {
            if (RHConfig.LazyManip)
            {
                Plugin.RefreshAllAssets(false);
            }
            else
                UI_RHPacksList.Instance?.ReloadPacks();
        }
        
        Up!.onClick?.AddListener(() =>
        {
            ResourcePacksManager.MovePack(_pack, true);
            UI_RHPacksList.Instance?.BuildList();

            Refresh();
        });
        Down!.onClick?.AddListener(() =>
        {
            ResourcePacksManager.MovePack(_pack, false);
            UI_RHPacksList.Instance?.BuildList();
            
            Refresh();
        });
        DisableToggle!.onClick?.AddListener(() =>
        {
            _pack.IsActive = !_pack.IsActive;
            ResourcePacksManager.Save();
            UI_RHPacksList.Instance?.BuildList();
            
            Refresh();
        });
        FolderButton!.onClick?.AddListener(() =>
        {
            Application.OpenURL("file://" + _pack.PackPath.Replace("\\", "/"));
        });
        LeftToggle!.onClick?.AddListener(() =>
        {
            if (RHConfig.PackPrefs.LeftHandPack == _pack.guid)
            {
                RHSpriteManager.ClearHandsOverride(RHSpriteManager.GetHandPrefix(0));
                RHConfig.PackPrefs.LeftHandPack = "";
            }
            else
            {
                RHSpriteManager.OverrideHands(pack.guid, RHSpriteManager.GetHandPrefix(0));
                RHConfig.PackPrefs.LeftHandPack = pack.guid;
            }
            
            UI_RHPacksList.Instance?.UpdateAllHandToggleStates();
            Refresh();
        });
        RightToggle!.onClick?.AddListener(() =>
        {
            if (RHConfig.PackPrefs.RightHandPack == _pack.guid)
            {
                RHSpriteManager.ClearHandsOverride(RHSpriteManager.GetHandPrefix(1));
                RHConfig.PackPrefs.RightHandPack = "";
            }
            else
            {
                RHSpriteManager.OverrideHands(pack.guid, RHSpriteManager.GetHandPrefix(1));
                RHConfig.PackPrefs.RightHandPack = pack.guid;
            }

            UI_RHPacksList.Instance?.UpdateAllHandToggleStates();
            Refresh();
        });
    }

    public void UpdateHandToggleState()
    {
        if (RHConfig.PackPrefs.LeftHandPack == _pack.guid)
        {
            LeftToggleOn!.enabled = true;
            LeftToggleOff!.enabled = false;
        }
        else
        {
            LeftToggleOn!.enabled = false;
            LeftToggleOff!.enabled = true;
        }
        if (RHConfig.PackPrefs.RightHandPack == _pack.guid)
        {
            RightToggleOn!.enabled = true;
            RightToggleOff!.enabled = false;
        }
        else
        {
            RightToggleOn!.enabled = false;
            RightToggleOff!.enabled = true;
        }
    }
}