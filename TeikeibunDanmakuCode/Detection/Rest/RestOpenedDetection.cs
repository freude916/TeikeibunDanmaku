using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using TeikeibunDanmaku.Timepoints;

namespace TeikeibunDanmaku.Detection.Rest;

[HarmonyPatch(typeof(RestSiteSynchronizer), nameof(RestSiteSynchronizer.BeginRestSite))]
public static class RestOpenedDetection
{
    [HarmonyPostfix]
    public static void BeginRestSitePostfix(RestSiteSynchronizer __instance)
    {
        ArgumentNullException.ThrowIfNull(__instance);

        try
        {
            var options = __instance.GetLocalOptions();
            if (options == null || options.Count == 0)
                return;

            RestOpenedTimepoint.FromOptions(options).Publish();
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error(
                $"RestOpenedDetection failed. ExceptionType={ex.GetType().FullName}, HResult=0x{ex.HResult:X8}"
            );
            if (!string.IsNullOrWhiteSpace(ex.StackTrace))
                MainFile.Logger.Error(ex.StackTrace);
        }
    }
}
