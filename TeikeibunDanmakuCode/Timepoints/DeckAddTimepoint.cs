using MegaCrit.Sts2.Core.Models;
using TeikeibunDanmaku.Core.Blackboard;

namespace TeikeibunDanmaku.Timepoints;

public sealed class DeckAddTimepoint : Timepoint<CardState>
{
    public const string TimepointId = "deck.add";
    public const string TimepointDisplayName = "牌组加入卡牌";

    public override string Id => TimepointId;
    public override string DisplayName => TimepointDisplayName;

    public static DeckAddTimepoint From(CardModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return new DeckAddTimepoint
        {
            State = CardState.FromCardModel(model)
        };
    }
}
