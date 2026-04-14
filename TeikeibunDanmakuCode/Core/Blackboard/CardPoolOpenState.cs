using MegaCrit.Sts2.Core.Models;

namespace TeikeibunDanmaku.Core.Blackboard;

public sealed class CardPoolOpenState : IBoardState
{
    [DataField("终端数量")] public required int TerminalCount { get; init; }

    [DataField("流派列表")] public required IReadOnlyList<string> Archetypes { get; init; }

    public static CardPoolOpenState FromCards(IEnumerable<CardModel> cards)
    {
        ArgumentNullException.ThrowIfNull(cards);

        var terminalCount = 0;
        var archetypes = new List<string>();

        foreach (var card in cards)
        {
            if (card == null)
            {
                continue;
            }

            var profile = CardArchetypeCatalog.Resolve(card);
            if (profile.IsTerminal)
            {
                terminalCount++;
            }

            foreach (var archetype in profile.Archetypes)
            {
                if (string.IsNullOrWhiteSpace(archetype))
                {
                    continue;
                }

                var value = archetype.Trim();
                archetypes.Add(value);
            }
        }

        return new CardPoolOpenState
        {
            TerminalCount = terminalCount,
            Archetypes = archetypes
        };
    }
}
