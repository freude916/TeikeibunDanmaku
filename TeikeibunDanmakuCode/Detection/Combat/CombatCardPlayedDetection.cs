using HarmonyLib;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Entities.Cards;
using TeikeibunDanmaku.Timepoints;

namespace TeikeibunDanmaku.Detection.Combat;

[HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.CardPlayFinished))]
public static class CombatCardPlayedDetection
{
    [HarmonyPostfix]
    public static void CardPlayFinishedPostfix(CombatHistory __instance, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(__instance);
        ArgumentNullException.ThrowIfNull(cardPlay);
        ArgumentNullException.ThrowIfNull(cardPlay.Card);

        // 一个系列的最后一次 finish 再触发，这样能拿到整张牌完整的伤害上下文。
        if (!cardPlay.IsLastInSeries)
            return;

        if (!cardPlay.Card.Owner.Creature.IsPlayer)
            return;

        try
        {
            CardPlayedTimepoint.From(cardPlay).Publish();
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error(
                $"CombatCardPlayedDetection failed. ExceptionType={ex.GetType().FullName}, HResult=0x{ex.HResult:X8}"
            );
            if (!string.IsNullOrWhiteSpace(ex.StackTrace))
                MainFile.Logger.Error(ex.StackTrace);
        }
    }
}
