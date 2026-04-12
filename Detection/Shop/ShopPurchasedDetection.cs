using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace TeikeibunDanmaku.Detection.Shop;

[HarmonyPatch(typeof(MerchantEntry), nameof(MerchantEntry.InvokePurchaseCompleted))]
public static class ShopPurchasedDetection
{
    [HarmonyPostfix]
    public static void InvokePurchaseCompletedPostfix(MerchantEntry __instance, MerchantEntry entry)
    {
        ArgumentNullException.ThrowIfNull(__instance);
        ArgumentNullException.ThrowIfNull(entry);

        var inventory = NMerchantRoom.Instance?.Room?.Inventory;
        if (inventory == null)
            return;

        if (!inventory.AllEntries.Any(shopEntry => ReferenceEquals(shopEntry, entry)))
            return;

        try
        {
            // CardPlayDanmakuEngine.OnShopPurchased(inventory, entry);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error(
                $"ShopPurchasedDetection failed. ExceptionType={ex.GetType().FullName}, HResult=0x{ex.HResult:X8}"
            );
            if (!string.IsNullOrWhiteSpace(ex.StackTrace))
                MainFile.Logger.Error(ex.StackTrace);
        }
    }
}
