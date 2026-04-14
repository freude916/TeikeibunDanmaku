using MegaCrit.Sts2.Core.Runs;
using TeikeibunDanmaku.Core.Blackboard;

namespace TeikeibunDanmaku.Timepoints;

public sealed class RunStartedTimepoint : Timepoint<RunStartedState>
{
    public const string TimepointId = "run.started";
    public const string TimepointDisplayName = "开始游戏";

    public override string Id => TimepointId;
    public override string DisplayName => TimepointDisplayName;

    public static RunStartedTimepoint From(RunState runState)
    {
        ArgumentNullException.ThrowIfNull(runState);

        return new RunStartedTimepoint
        {
            State = RunStartedState.FromRunState(runState)
        };
    }
}
