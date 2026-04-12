using TeikeibunDanmaku.Core.Timepoints;
using TeikeibunDanmaku.Timepoints;
using TeikeibunDanmaku.Core;

namespace TeikeibunDanmaku.Core.Rules;

public sealed class RuleEngine : IDisposable
{
    private readonly IReadOnlyList<Rule> _rules;
    private bool _disposed;

    public RuleEngine(IReadOnlyList<Rule> rules)
    {
        _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        TimepointBus.Published += OnTimepointPublished;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        TimepointBus.Published -= OnTimepointPublished;
    }

    private void OnTimepointPublished(Timepoint timepoint)
    {
        ArgumentNullException.ThrowIfNull(timepoint);

        foreach (var rule in _rules)
        {
            if (!rule.Matches(timepoint))
            {
                continue;
            }

            foreach (var message in rule.Messages)
            {
                MainFile.Logger.Info($"Danmaku send: {message}");
                DanmakuStore.Publish(message);
            }
        }
    }
}
