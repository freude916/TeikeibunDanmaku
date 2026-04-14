using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using TeikeibunDanmaku.Timepoints;

namespace TeikeibunDanmaku.Detection.Deck;

[HarmonyPatch(typeof(RunState), nameof(RunState.AddCard), typeof(CardModel))]
public static class DeckAddDetection
{
    [HarmonyPostfix]
    public static void AddCardPostfix(CardModel card)
    {
        Publish(card);
    }

    [HarmonyPatch(typeof(RunState), nameof(RunState.AddCard), typeof(CardModel), typeof(MegaCrit.Sts2.Core.Entities.Players.Player))]
    [HarmonyPostfix]
    public static void AddCardWithOwnerPostfix(CardModel card)
    {
        Publish(card);
    }

    private static void Publish(CardModel card)
    {
        ArgumentNullException.ThrowIfNull(card);

        if (card.Owner?.Creature?.IsPlayer != true)
            return;

        try
        {
            DeckAddTimepoint.From(card).Publish();
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error(
                $"DeckAddDetection failed. ExceptionType={ex.GetType().FullName}, HResult=0x{ex.HResult:X8}"
            );
            if (!string.IsNullOrWhiteSpace(ex.StackTrace))
                MainFile.Logger.Error(ex.StackTrace);
        }
    }
}
