using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;

namespace TeikeibunDanmaku.Detection.Rewards;

[HarmonyPatch(typeof(NCardRewardSelectionScreen), nameof(NCardRewardSelectionScreen.AfterOverlayShown))]
public static class CardRewardOpenedDetection
{
    [HarmonyPostfix]
    public static void AfterOverlayShownPostfix(NCardRewardSelectionScreen __instance)
    {
        ArgumentNullException.ThrowIfNull(__instance);

        try
        {
            var cardRow = __instance.GetNodeOrNull<Godot.Control>("UI/CardRow");
            if (cardRow == null)
                return;

            var cards = cardRow
                .GetChildren()
                .OfType<NGridCardHolder>()
                .Select(holder => holder.CardModel)
                .OfType<CardModel>()
                .ToList();

            if (cards.Count == 0)
                return;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error(
                $"CardRewardOpenedDetection failed. ExceptionType={ex.GetType().FullName}, HResult=0x{ex.HResult:X8}"
            );
            if (!string.IsNullOrWhiteSpace(ex.StackTrace))
                MainFile.Logger.Error(ex.StackTrace);
        }
    }
}
