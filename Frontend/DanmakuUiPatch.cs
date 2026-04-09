using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;

namespace TeikeibunDanmaku.Frontend;

[HarmonyPatch(typeof(NGlobalUi))]
public static class DanmakuUiPatch
{
    private const string FrontendMetaKey = "teikeibun_danmaku_frontend";
    private static bool _disabledAfterFailure;
    private static readonly Dictionary<ulong, DanmakuFrontendView> FrontendViews = [];

    [HarmonyPatch("Initialize")]
    [HarmonyPostfix]
    public static void InitializePostfix(NGlobalUi __instance, RunState runState)
    {
        ArgumentNullException.ThrowIfNull(__instance);
        _ = runState;

        if (_disabledAfterFailure)
            return;

        if (FindFrontend(__instance) != null)
            return;

        try
        {
            var frontendRoot = new Control();
            frontendRoot.SetMeta(FrontendMetaKey, true);
            __instance.AddChild(frontendRoot);
            FrontendViews[frontendRoot.GetInstanceId()] = new DanmakuFrontendView(frontendRoot);
            frontendRoot.TreeExited += () => OnFrontendRootTreeExited(frontendRoot);
            MainFile.Logger.Info("Danmaku frontend initialized.");
        }
        catch (Exception ex)
        {
            _disabledAfterFailure = true;
            MainFile.Logger.Error(
                $"Danmaku frontend disabled after initialization failure. ExceptionType={ex.GetType().FullName}, HResult=0x{ex.HResult:X8}");

            if (!string.IsNullOrWhiteSpace(ex.StackTrace))
                MainFile.Logger.Error(ex.StackTrace);
        }
    }

    private static Node? FindFrontend(Node parent)
    {
        foreach (var child in parent.GetChildren())
        {
            if (child is Node node && node.HasMeta(FrontendMetaKey))
                return node;
        }

        return null;
    }

    private static void OnFrontendRootTreeExited(Node frontendRoot)
    {
        if (FrontendViews.Remove(frontendRoot.GetInstanceId(), out var view))
            view.Dispose();
    }
}
