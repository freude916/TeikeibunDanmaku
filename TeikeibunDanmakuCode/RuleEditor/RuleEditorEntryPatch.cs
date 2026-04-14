using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;

namespace TeikeibunDanmaku.RuleEditor;

[HarmonyPatch(typeof(NGame), nameof(NGame._Ready))]
public static class RuleEditorEntryPatch
{
    [HarmonyPostfix]
    public static void Postfix(NGame __instance)
    {
        if (__instance == null)
        {
            return;
        }

        try
        {
            __instance.AddChild(new RuleEditorEntryNode());
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"Rule editor init failed: {ex.Message}");
            if (!string.IsNullOrWhiteSpace(ex.StackTrace))
            {
                MainFile.Logger.Error(ex.StackTrace);
            }
        }
    }
}
