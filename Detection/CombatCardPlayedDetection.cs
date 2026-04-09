using HarmonyLib;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Entities.Cards;
using TeikeibunDanmaku.Core;

namespace TeikeibunDanmaku.Detection;

[HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.CardPlayFinished))]
public static class CombatCardPlayedDetection
{
    [HarmonyPostfix]
    public static void CardPlayFinishedPostfix(CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay);

        // 同一张牌在 replay/multi-play 情况下会有多次 finish，先只处理首个条目，避免刷屏。
        if (!cardPlay.IsFirstInSeries)
            return;

        try
        {
            CardPlayDanmakuEngine.OnCardPlayed(cardPlay);
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
