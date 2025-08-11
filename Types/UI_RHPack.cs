using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ResourcefulHands;

public class UI_RHPack : MonoBehaviour
{
    public RawImage? Icon; //  IconContainer/Icon/
    
    public TextMeshProUGUI? Title; //  Title/
    public TextMeshProUGUI? Author; //  AuthorPanel/Author/
    public Button? SteamAuthor; //  AuthorPanel/ViewOnSteam/
    public TextMeshProUGUI? Description; //  Desc/
    public TextMeshProUGUI? Guid; //  Guid/

    public Button? Up; //  Up/Arrow/
    public Button? Down; //  Down/Arrow/
    public Button? EnableToggle; //  Disable/
    public Image? EnableOn; //  Disable/Toggle/ToggleImg
    public Image? EnableOff; //  Disable/Toggle/OffImg

    private TexturePack _pack = null!;
    
    private T? FindAt<T>(string path) where T : Component
    {
        Transform t = this.transform;
        string[] objectNames = path.Split('/');
        for (int i = 0; i < objectNames.Length; i++)
        {
            string objectName = objectNames[i];
            t = t.Find(objectName);
            if (t == null)
                return null;
        }

        return t.GetComponentInChildren<T>();
    }
    
    public void Awake()
    {
        Icon = FindAt<RawImage>("IconContainer/Icon");
        
        Title = FindAt<TextMeshProUGUI>("Title");
        Author = FindAt<TextMeshProUGUI>("AuthorPanel/Author");
        SteamAuthor = FindAt<Button>("AuthorPanel/ViewOnSteam");
        Description = FindAt<TextMeshProUGUI>("Desc");
        Guid = FindAt<TextMeshProUGUI>("Guid");
        
        Up = FindAt<Button>("Up/Arrow");
        Down = FindAt<Button>("Down/Arrow");
        EnableToggle = FindAt<Button>("Disable");
        EnableOff = FindAt<Image>("Disable/Toggle/OffImg");
        EnableOn = FindAt<Image>("Disable/Toggle/ToggleImg");
    }

    // Loads the pack's values into the ui, this should only be called once.
    public void Load(TexturePack pack, bool active = true)
    {
        _pack = pack;

        Icon!.texture = _pack.Icon;
        
        Title!.text = _pack.name;
        Author!.text = _pack.author;
        Description!.text = _pack.desc;
        Guid!.text = _pack.guid;

        EnableOff!.enabled = !active;
        EnableOn!.enabled = active;

        if (pack.steamid > 0)
        {
            SteamAuthor?.onClick.AddListener(() =>
            {
                SteamFriends.OpenUserOverlay(pack.steamid, "steamid");
            });
        }
        else
            SteamAuthor?.gameObject.SetActive(false);
        
        Up!.onClick.AddListener(() =>
        {
            RHCommands.MovePack(_pack, true);
            UI_RHPacksList.Instance?.BuildList();
            
            if (RHConfig.LazyManip?.Value ?? false)
            {
                Plugin.RefreshTextures();
                Plugin.RefreshSounds();
            }
            else
                ResourcePacksManager.ReloadPacks();
        });
        Down!.onClick.AddListener(() =>
        {
            RHCommands.MovePack(_pack, false);
            UI_RHPacksList.Instance?.BuildList();
            
            if (RHConfig.LazyManip?.Value ?? false)
            {
                Plugin.RefreshTextures();
                Plugin.RefreshSounds();
            }
            else
                ResourcePacksManager.ReloadPacks();
        });
        EnableToggle!.onClick.AddListener(() =>
        {
            _pack.IsActive = !_pack.IsActive;
            UI_RHPacksList.Instance?.BuildList();
            
            if (RHConfig.LazyManip?.Value ?? false)
            {
                Plugin.RefreshTextures();
                Plugin.RefreshSounds();
            }
            else
                ResourcePacksManager.ReloadPacks();
        });
    }
}