using MegaCrit.Sts2.Core.Entities.RestSite;

namespace TeikeibunDanmaku.Core.Blackboard;

public sealed class RestOpenedState : IBoardState
{
    [DataField("火堆选项数量")] public required int OptionCount { get; init; }

    [DataField("火堆选项ID列表")] public required IReadOnlyList<string> OptionIds { get; init; }

    [DataField("火堆选项名称列表")] public required IReadOnlyList<string> OptionNames { get; init; }

    public static RestOpenedState FromOptions(IEnumerable<RestSiteOption> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var optionCount = 0;
        var ids = new List<string>();
        var names = new List<string>();

        foreach (var option in options)
        {
            if (option == null)
                continue;

            optionCount++;

            if (!string.IsNullOrWhiteSpace(option.OptionId))
                ids.Add(option.OptionId.Trim());

            var name = option.Title.GetFormattedText();
            if (!string.IsNullOrWhiteSpace(name))
                names.Add(name.Trim());
        }

        return new RestOpenedState
        {
            OptionCount = optionCount,
            OptionIds = ids,
            OptionNames = names
        };
    }
}
