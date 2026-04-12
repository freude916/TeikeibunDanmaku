using MegaCrit.Sts2.Core.Models;

namespace TeikeibunDanmaku.Blackboard;

public class CardState: IBoardState
{
    [DataField] public required string ModelId { get; init; }

    static CardState FromCardModel(CardModel model)
    {
        return new CardState()
        {
            ModelId = model.Id.Entry
        };
    }
}