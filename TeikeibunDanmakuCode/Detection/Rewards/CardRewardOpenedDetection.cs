using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using TeikeibunDanmaku.Timepoints;

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

            RewardOpenTimepoint.FromCards(cards).Publish();

            foreach (var card in cards) // cards.Count == 0 就自动跳过了呗
            {
                RewardSeenTimepoint.From(card).Publish();
            }
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
