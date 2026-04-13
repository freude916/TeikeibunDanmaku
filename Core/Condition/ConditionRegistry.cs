using System.Text.Json;

namespace TeikeibunDanmaku.Core.Condition;

public sealed class ConditionRegistry
{
    private readonly Dictionary<string, ConditionCodec> _codecs = new(StringComparer.OrdinalIgnoreCase);

    public ConditionRegistry Register(ConditionCodec codec)
    {
        ArgumentNullException.ThrowIfNull(codec);
        
        _codecs[codec.Type] = codec;
        return this;
    }

    public ConditionCodec Resolve(string type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);

        return _codecs.TryGetValue(type, out var codec) ? codec : throw new JsonException($"Unsupported condition type '{type}'.");
    }

    public static ConditionRegistry CreateDefault()
    {
        return new ConditionRegistry()
            .Register(new EqConditionCodec())
            .Register(new LtConditionCodec())
            .Register(new GtConditionCodec())
            .Register(new FindConditionCodec())
            .Register(new AndConditionCodec())
            .Register(new OrConditionCodec())
            ;
    }
}
