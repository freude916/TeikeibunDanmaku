using MegaCrit.Sts2.Core.Models;
using TeikeibunDanmaku.Core.Blackboard;

namespace TeikeibunDanmaku.Timepoints;

public sealed class ShopSeenTimepoint : Timepoint<CardState>
{
    public const string TimepointId = "shop.seen";
    public const string TimepointDisplayName = "商店卡牌出现";

    public override string Id => TimepointId;
    public override string DisplayName => TimepointDisplayName;

    public static ShopSeenTimepoint From(CardModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return new ShopSeenTimepoint
        {
            State = CardState.FromCardModel(model)
        };
    }
}
