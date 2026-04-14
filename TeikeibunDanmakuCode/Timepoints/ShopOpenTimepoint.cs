using MegaCrit.Sts2.Core.Models;
using TeikeibunDanmaku.Core.Blackboard;

namespace TeikeibunDanmaku.Timepoints;

public sealed class ShopOpenTimepoint : Timepoint<CardPoolOpenState>
{
    public const string TimepointId = "shop.open";
    public const string TimepointDisplayName = "商店打开";

    public override string Id => TimepointId;
    public override string DisplayName => TimepointDisplayName;

    public static ShopOpenTimepoint FromCards(IEnumerable<CardModel> cards)
    {
        ArgumentNullException.ThrowIfNull(cards);

        return new ShopOpenTimepoint
        {
            State = CardPoolOpenState.FromCards(cards)
        };
    }
}
