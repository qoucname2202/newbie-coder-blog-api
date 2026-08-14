using System.Text.Json;
using System.Text.Json.Serialization;

namespace NewbieCoder.Core.Converters;

/// <summary>
/// Converts JSON boolean values to <c>bool?</c>. When a non-boolean value is provided
/// (e.g. a string "true" or a number 1), this converter throws a <c>JsonException</c>
/// with a user-friendly message instead of the default System.Text.Json technical message.
/// </summary>
public sealed class NullableBoolConverter : JsonConverter<bool?>
{
    public override bool? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;

            case JsonTokenType.True:
                return true;

            case JsonTokenType.False:
                return false;

            case JsonTokenType.String:
                var stringValue = reader.GetString();
                if (string.Equals(stringValue, "true", StringComparison.OrdinalIgnoreCase))
                    return true;
                if (string.Equals(stringValue, "false", StringComparison.OrdinalIgnoreCase))
                    return false;
                throw new JsonException(
                    "Invalid acceptTerms format. Expected a boolean value (true or false).");

            case JsonTokenType.Number:
                try
                {
                    var numberValue = reader.GetInt32();
                    if (numberValue == 1) return true;
                    if (numberValue == 0) return false;
                }
                catch (InvalidOperationException)
                {
                    // getInt32() fails if the number is not a standard integer
                }
                throw new JsonException(
                    "Invalid acceptTerms format. Expected a boolean value (true or false).");

            default:
                throw new JsonException(
                    "Invalid acceptTerms format. Expected a boolean value (true or false).");
        }
    }

    public override void Write(Utf8JsonWriter writer, bool? value, JsonSerializerOptions options)
    {
        if (value == null)
            writer.WriteNullValue();
        else
            writer.WriteBooleanValue(value.Value);
    }
}
