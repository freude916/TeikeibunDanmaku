using MegaCrit.Sts2.Core.Models;
using TeikeibunDanmaku.Core.Blackboard;

namespace TeikeibunDanmaku.Timepoints;

public sealed class ShopSeenTimepoint : Timepoint<CardState>
{
    public const string TimepointId = "shop.seen";

    public override string Id => TimepointId;

    public static ShopSeenTimepoint From(CardModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return new ShopSeenTimepoint
        {
            State = CardState.FromCardModel(model)
        };
    }
}
