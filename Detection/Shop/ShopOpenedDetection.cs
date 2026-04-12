using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace TeikeibunDanmaku.Detection.Shop;

[HarmonyPatch(typeof(NMerchantInventory), nameof(NMerchantInventory.Open))]
public static class ShopOpenedDetection
{
    [HarmonyPostfix]
    public static void OpenPostfix(NMerchantInventory __instance)
    {
        ArgumentNullException.ThrowIfNull(__instance);

        if (NMerchantRoom.Instance == null)
            return;

        var inventory = __instance.Inventory;
        if (inventory == null)
            return;

        try
        {
            // CardPlayDanmakuEngine.OnShopOpened(inventory);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error(
                $"ShopOpenedDetection failed. ExceptionType={ex.GetType().FullName}, HResult=0x{ex.HResult:X8}"
            );
            if (!string.IsNullOrWhiteSpace(ex.StackTrace))
                MainFile.Logger.Error(ex.StackTrace);
        }
    }
}
