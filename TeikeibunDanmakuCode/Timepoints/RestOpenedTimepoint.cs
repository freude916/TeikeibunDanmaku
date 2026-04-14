using MegaCrit.Sts2.Core.Entities.RestSite;
using TeikeibunDanmaku.Core.Blackboard;

namespace TeikeibunDanmaku.Timepoints;

public sealed class RestOpenedTimepoint : Timepoint<RestOpenedState>
{
    public const string TimepointId = "rest.opened";
    public const string TimepointDisplayName = "火堆打开";

    public override string Id => TimepointId;
    public override string DisplayName => TimepointDisplayName;

    public static RestOpenedTimepoint FromOptions(IEnumerable<RestSiteOption> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new RestOpenedTimepoint
        {
            State = RestOpenedState.FromOptions(options)
        };
    }
}
