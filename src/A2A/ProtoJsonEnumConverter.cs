using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace A2A;

/// <summary>
/// A JSON string enum converter that serializes enum values using their
/// <see cref="EnumMemberAttribute"/> value if present, otherwise the enum member name as-is.
/// This supports the ProtoJSON convention of SCREAMING_SNAKE_CASE enum values.
/// </summary>
/// <typeparam name="TEnum">The type of the enum to convert.</typeparam>
internal sealed class ProtoJsonEnumConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    private static readonly Dictionary<TEnum, string> s_enumToString = [];
    private static readonly Dictionary<string, TEnum> s_stringToEnum = new(StringComparer.OrdinalIgnoreCase);

#pragma warning disable IL2070, IL2090 // DynamicallyAccessedMembers annotations
    static ProtoJsonEnumConverter()
    {
#if NETSTANDARD2_0
        var values = (TEnum[])Enum.GetValues(typeof(TEnum));
#else
        var values = Enum.GetValues<TEnum>();
#endif
        foreach (var value in values)
        {
            var memberInfo = typeof(TEnum).GetField(value.ToString());
            var enumMemberAttr = memberInfo?.GetCustomAttributes(typeof(EnumMemberAttribute), false)
                .FirstOrDefault() as EnumMemberAttribute;

            var serializedName = enumMemberAttr?.Value ?? value.ToString();
            s_enumToString[value] = serializedName;
            s_stringToEnum[serializedName] = value;
        }
    }
#pragma warning restore IL2070, IL2090

    /// <inheritdoc/>
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected string for {typeof(TEnum).Name}, got {reader.TokenType}");
        }

        var value = reader.GetString();
        if (value is null || !s_stringToEnum.TryGetValue(value, out var result))
        {
            throw new JsonException($"Unknown {typeof(TEnum).Name} value: '{value}'");
        }

        return result;
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        if (s_enumToString.TryGetValue(value, out var name))
        {
            writer.WriteStringValue(name);
        }
        else
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
