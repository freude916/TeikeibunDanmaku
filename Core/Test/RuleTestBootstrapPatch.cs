using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;

namespace TeikeibunDanmaku.Core.Test;

[HarmonyPatch(typeof(OneTimeInitialization), nameof(OneTimeInitialization.ExecuteEssential))]
public static class RuleTestBootstrapPatch
{
    [HarmonyPostfix]
    public static void ExecuteEssentialPostfix()
    {
        try
        {
            RuleTest.TestCardRules();
            MainFile.Logger.Info("Rule test rules installed after ModelDb initialization.");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error(
                $"Rule test bootstrap failed. ExceptionType={ex.GetType().FullName}, HResult=0x{ex.HResult:X8}"
            );
            if (!string.IsNullOrWhiteSpace(ex.StackTrace))
            {
                MainFile.Logger.Error(ex.StackTrace);
            }
        }
    }
}
