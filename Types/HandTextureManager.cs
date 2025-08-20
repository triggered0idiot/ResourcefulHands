using System.Collections.Generic;

namespace ResourcefulHands.Types;

/// <summary>
/// Manager for hand-specific texture pack assignments
/// </summary>
public static class HandTextureManager
{
    private static readonly Dictionary<int, string> HandTexturePacks = new();
    
    public static bool HasCustomTexturePack(int handId)
    {
        return HandTexturePacks.ContainsKey(handId) && !string.IsNullOrEmpty(HandTexturePacks[handId]);
    }
    
    public static string GetHandTexturePackGuid(int handId)
    {
        return HandTexturePacks.GetValueOrDefault(handId, "");
    }
    
    public static void AssignTexturePackToHand(int handId, string packGuid)
    {
        if (string.IsNullOrEmpty(packGuid))
        {
            HandTexturePacks.Remove(handId);
            RHLog.Info($"Removed texture pack from hand {handId}");
        }
        else
        {
            HandTexturePacks[handId] = packGuid;
            RHLog.Info($"Assigned texture pack '{packGuid}' to hand {handId}");
        }
    }
    
    public static void ClearAllHandTexturePacks()
    {
        HandTexturePacks.Clear();
        RHLog.Info("Cleared all hand texture pack assignments");
    }
}
