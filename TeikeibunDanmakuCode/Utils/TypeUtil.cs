using System.Globalization;
using System.Text.Json;

namespace TeikeibunDanmaku.Utils;

public static class TypeUtil
{
    public static Type GetNonNullableType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return Nullable.GetUnderlyingType(type) ?? type;
    }

    public static bool IsNumericType(Type type)
    {
        var nonNullableType = GetNonNullableType(type);

        return nonNullableType == typeof(byte) ||
               nonNullableType == typeof(sbyte) ||
               nonNullableType == typeof(short) ||
               nonNullableType == typeof(ushort) ||
               nonNullableType == typeof(int) ||
               nonNullableType == typeof(uint) ||
               nonNullableType == typeof(long) ||
               nonNullableType == typeof(ulong) ||
               nonNullableType == typeof(float) ||
               nonNullableType == typeof(double) ||
               nonNullableType == typeof(decimal);
    }

    public static bool TryConvertNumericObjectToDouble(object? value, out double result)
    {
        if (value is null)
        {
            result = 0;
            return false;
        }

        switch (value)
        {
            case double d:
                result = d;
                return true;
            case float f:
                result = f;
                return true;
            case decimal m:
                result = (double)m;
                return true;
            default:
                if (value is IConvertible convertible)
                {
                    result = convertible.ToDouble(CultureInfo.InvariantCulture);
                    return true;
                }

                result = default;
                return false;
        }
    }

    public static bool TryParseNumericJsonAsDouble(JsonElement valueElement, out double result)
    {
        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (valueElement.ValueKind)
        {
            case JsonValueKind.Number:
                if (valueElement.TryGetDouble(out var numberValue))
                {
                    result = numberValue;
                    return true;
                }

                break;
            case JsonValueKind.String:
                var text = valueElement.GetString();
                if (double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands,
                        CultureInfo.InvariantCulture, out var parsed))
                {
                    result = parsed;
                    return true;
                }

                break;
        }

        result = 0;
        return false;
    }

    public static bool TryGetBooleanValue(JsonElement element, out bool value)
    {
        if (element.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            value = element.GetBoolean();
            return true;
        }

        value = false;
        return false;
    }
}