using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;
using TeikeibunDanmaku.Timepoints;

namespace TeikeibunDanmaku.Detection.Run;

[HarmonyPatch(typeof(RunManager), nameof(RunManager.InitializeNewRun))]
public static class RunStartedDetection
{
    [HarmonyPostfix]
    public static void InitializeNewRunPostfix(RunManager __instance)
    {
        ArgumentNullException.ThrowIfNull(__instance);

        var runState = __instance.State;
        if (runState == null)
            return;

        try
        {
            RunStartedTimepoint.From(runState).Publish();
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error(
                $"RunStartedDetection failed. ExceptionType={ex.GetType().FullName}, HResult=0x{ex.HResult:X8}"
            );
            if (!string.IsNullOrWhiteSpace(ex.StackTrace))
                MainFile.Logger.Error(ex.StackTrace);
        }
    }
}
