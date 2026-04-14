using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Models;

namespace TeikeibunDanmaku.Detection.Combat;

[HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.CardDrawn))]
public static class CombatCardDrawnDetection
{
    [HarmonyPostfix]
    public static void CardDrawnPostfix(CombatState combatState, CardModel card, bool fromHandDraw)
    {
        ArgumentNullException.ThrowIfNull(combatState);
        ArgumentNullException.ThrowIfNull(card);

        if (!card.Owner.Creature.IsPlayer)
            return;
        
        try
        {
            // CardPlayDanmakuEngine.OnCardDrawn(card, fromHandDraw);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error(
                $"CombatCardDrawnDetection failed. ExceptionType={ex.GetType().FullName}, HResult=0x{ex.HResult:X8}"
            );
            if (!string.IsNullOrWhiteSpace(ex.StackTrace))
                MainFile.Logger.Error(ex.StackTrace);
        }
    }
}
