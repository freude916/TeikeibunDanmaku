using MegaCrit.Sts2.Core.Runs;

namespace TeikeibunDanmaku.Core.Blackboard;

public sealed class RunStartedState : IBoardState
{
    [DataField("角色名")] public required string CharacterName { get; init; }

    [DataField("进阶")] public required int AscensionLevel { get; init; }

    public static RunStartedState FromRunState(RunState runState)
    {
        ArgumentNullException.ThrowIfNull(runState);

        var player = runState.Players.FirstOrDefault();
        if (player == null)
            throw new InvalidOperationException("RunState.Players is empty.");

        var characterName = player.Character.Title.GetFormattedText();
        if (string.IsNullOrWhiteSpace(characterName))
            characterName = player.Character.Id.Entry;

        return new RunStartedState
        {
            CharacterName = characterName,
            AscensionLevel = runState.AscensionLevel
        };
    }
}
