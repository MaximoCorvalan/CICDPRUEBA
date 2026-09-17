using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ApiAgenteFacturasIA.Converters
{
    /// <summary>
    /// El agente Python / el LLM a veces mandan null, "" o importes en formato
    /// argentino ("35.562,95") en campos que C# espera como decimal.
    /// Sin esto, JsonSerializer tira 500: "could not be converted to System.Decimal".
    /// </summary>
    public sealed class FlexibleDecimalConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) =>
            typeToConvert == typeof(decimal) || typeToConvert == typeof(decimal?);

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            if (typeToConvert == typeof(decimal?))
                return new FlexibleNullableDecimalConverter();
            return new FlexibleDecimalConverter();
        }
    }

    public sealed class FlexibleDecimalConverter : JsonConverter<decimal>
    {
        public override bool HandleNull => true;

        public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => FlexibleDecimal.Read(ref reader) ?? 0m;

        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
            => writer.WriteNumberValue(value);
    }

    public sealed class FlexibleNullableDecimalConverter : JsonConverter<decimal?>
    {
        public override bool HandleNull => true;

        public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => FlexibleDecimal.Read(ref reader);

        public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
        {
            if (value is null)
                writer.WriteNullValue();
            else
                writer.WriteNumberValue(value.Value);
        }
    }

    internal static class FlexibleDecimal
    {
        public static decimal? Read(ref Utf8JsonReader reader)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Null:
                    return null;
                case JsonTokenType.Number:
                    if (reader.TryGetDecimal(out var n))
                        return n;
                    if (reader.TryGetDouble(out var d))
                        return (decimal)d;
                    return null;
                case JsonTokenType.String:
                    return ParseImporte(reader.GetString());
                default:
                    return null;
            }
        }

        public static decimal? ParseImporte(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            var sb = new StringBuilder(raw.Length);
            foreach (var c in raw)
            {
                if (char.IsDigit(c) || c is '.' or ',' or '-')
                    sb.Append(c);
            }

            var s = sb.ToString();
            if (s.Length == 0 || s is "-" or "." or ",")
                return null;

            if (s.Contains(','))
                s = s.Replace(".", "").Replace(',', '.');

            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var valor))
                return valor;
            return null;
        }
    }
}
