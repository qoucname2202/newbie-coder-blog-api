using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NewbieCoder.API.Converters;

/// <summary>
/// Strict JSON validation: rejects any inbound request payload containing properties
/// not defined in the target model. This prevents accidental or malicious extra fields
/// (e.g. sending "role": "admin" in a register request).
///
/// The <c>Read</c> method enforces strict validation on incoming JSON.
/// The <c>Write</c> method is a passthrough that uses fresh default options to avoid
/// infinite recursion when Swagger or other consumers serialize types that also
/// participate in the request pipeline.
/// </summary>
public sealed class StrictJsonConverterFactory : JsonConverterFactory
{
    private static readonly HashSet<string> ExcludedPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Swashbuckle",
        "Microsoft.AspNetCore.OpenApi"
    };

    public override bool CanConvert(Type typeToConvert)
    {
        var assemblyName = typeToConvert.Assembly.GetName().Name ?? string.Empty;
        return !ExcludedPrefixes.Any(prefix =>
            assemblyName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(StrictJsonConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter?)Activator.CreateInstance(converterType);
    }

    private sealed class StrictJsonConverter<T> : JsonConverter<T>
    {
        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return default;

            var readerClone = reader;

            using var doc = JsonDocument.ParseValue(ref readerClone);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in root.EnumerateObject())
                {
                    var propName = prop.Name;
                    var pascalPropName = char.ToUpperInvariant(propName[0]) + propName[1..];

                    var propInfo = typeToConvert.GetProperty(
                        pascalPropName,
                        BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.Instance);

                    if (propInfo is null)
                    {
                        throw new JsonException($"Invalid field name '{propName}'. Did you mean '{ToSnakeCase(propName)}'?");
                    }
                }
            }

            // Build a safe options copy that preserves all original serializer settings
            // (PropertyNameCaseInsensitive, PropertyNamingPolicy, etc.) while excluding
            // StrictJsonConverterFactory from the converters list to prevent recursion.
            var safeOptions = new JsonSerializerOptions(options);
            var filteredConverters = new List<JsonConverter>();
            foreach (var converter in options.Converters)
            {
                if (converter is StrictJsonConverterFactory)
                    continue;
                filteredConverters.Add(converter);
            }
            var convertersField = typeof(JsonSerializerOptions).GetField(
                "_converters", BindingFlags.Instance | BindingFlags.NonPublic);
            convertersField?.SetValue(safeOptions, filteredConverters);

            return JsonSerializer.Deserialize<T>(ref reader, safeOptions);
        }

        /// <summary>
        /// Passthrough serialization using fresh default options. This prevents infinite
        /// recursion when Swagger or other layers serialize types that also participate in
        /// the request pipeline (the same converter is in the options).
        /// </summary>
        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, new JsonSerializerOptions());
        }

        private static string ToSnakeCase(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            var result = new System.Text.StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (char.IsUpper(c) && i > 0)
                {
                    result.Append('_');
                    result.Append(char.ToLowerInvariant(c));
                }
                else
                {
                    result.Append(char.ToLowerInvariant(c));
                }
            }
            return result.ToString();
        }
    }
}
