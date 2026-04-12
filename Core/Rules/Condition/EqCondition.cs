using TeikeibunDanmaku.Blackboard;

namespace TeikeibunDanmaku.Core.Rules;

public sealed class EqCondition(BoardFieldDescriptor fieldDescriptor, object? expectedValue)
    : ICondition
{
    private const double Epsilon = 1e-9;

    private readonly BoardFieldDescriptor _fieldDescriptor = fieldDescriptor ?? throw new ArgumentNullException(nameof(fieldDescriptor));

    public bool Evaluate(IBoardState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var actualValue = _fieldDescriptor.Getter(state);
        if (Util.IsNumericType(_fieldDescriptor.ValueType))
        {
            if (!Util.TryConvertNumericObjectToDouble(actualValue, out var actualNumber) ||
                !Util.TryConvertNumericObjectToDouble(expectedValue, out var expectedNumber))
            {
                return false;
            }

            return Math.Abs(actualNumber - expectedNumber) <= Epsilon;
        }

        return Equals(actualValue, expectedValue);
    }
}
