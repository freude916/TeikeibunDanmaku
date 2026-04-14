using TeikeibunDanmaku.Core.Rules;
using TeikeibunDanmaku.Core.Blackboard;
using TeikeibunDanmaku.Display;
using TeikeibunDanmaku.Timepoints;

namespace TeikeibunDanmaku.Core;

public sealed class RuleEngine : IDisposable
{
    private readonly List<Rule> _rules;
    private bool _disposed;

    public RuleEngine(IEnumerable<Rule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        _rules = [.. rules];
        TimepointBus.Published += OnTimepointPublished;
    }

    public void AppendRules(IEnumerable<Rule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        _rules.AddRange(rules);
    }

    public int RuleCount => _rules.Count;

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
        LongStateCache.RefreshFromGame();

        var rulesSnapshot = _rules.ToArray();
        foreach (var rule in rulesSnapshot)
        {
            if (!rule.Matches(timepoint))
            {
                continue;
            }

            foreach (var message in rule.Messages)
            {
                var rendered = MessageTemplateRenderer.Render(message, timepoint.BoardState);
                MainFile.Logger.Info($"Danmaku send: {rendered}");
                DanmakuStore.Publish(rendered);
            }
        }
    }
}
