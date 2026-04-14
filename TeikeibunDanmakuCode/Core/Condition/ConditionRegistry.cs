using System.Text.Json;

namespace TeikeibunDanmaku.Core.Condition;

public sealed class ConditionRegistry
{
    private readonly Dictionary<string, ConditionCodec> _codecs = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _typeOrder = [];

    public ConditionRegistry Register(ConditionCodec codec)
    {
        ArgumentNullException.ThrowIfNull(codec);

        if (!_codecs.ContainsKey(codec.Type))
        {
            _typeOrder.Add(codec.Type);
        }

        _codecs[codec.Type] = codec;
        return this;
    }

    public ConditionCodec Resolve(string type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);

        return _codecs.TryGetValue(type, out var codec) ? codec : throw new JsonException($"Unsupported condition type '{type}'.");
    }

    public bool Contains(string type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        return _codecs.ContainsKey(type);
    }

    public IReadOnlyList<string> ListTypes()
    {
        return _typeOrder.ToArray();
    }

    public static ConditionRegistry CreateDefault()
    {
        var registry = new ConditionRegistry()
            .Register(new EqConditionCodec())
            .Register(new LtConditionCodec())
            .Register(new GtConditionCodec())
            .Register(new FindConditionCodec())
            .Register(new ListContainsConditionCodec())
            .Register(new AndConditionCodec())
            .Register(new OrConditionCodec())
            ;

        registry.ValidateDisplayNameCoverage();
        return registry;
    }

    private void ValidateDisplayNameCoverage()
    {
        foreach (var type in ListTypes())
        {
            if (!ConditionType.HasDisplayName(type))
            {
                throw new InvalidOperationException($"Condition type '{type}' is registered but has no display name.");
            }
        }

        foreach (var type in ConditionType.ListDisplayNameTypes())
        {
            if (!Contains(type))
            {
                throw new InvalidOperationException($"Condition type '{type}' has display name metadata but is not registered.");
            }
        }
    }
}
