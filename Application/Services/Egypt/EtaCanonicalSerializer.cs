using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Application.Services.Egypt
{
    // Produces the canonical string the Egyptian Tax Authority uses to compute the document hash
    // and as the payload for CMS signing. Format per ETA spec:
    //   For every property: "<PROPERTYNAME>" + serializedValue
    //   Strings:    "<VALUE>"
    //   Arrays:     concatenation of each element's serialized form
    //   Objects:    concatenation of properties in the order they appear
    // Property names are upper-cased.
    public static class EtaCanonicalSerializer
    {
        public static string Serialize(object document)
        {
            // Keep nulls — the ETA canonical form renders them as "" (handled in WriteValue).
            // If we ignored them here the document hash would not match the one the
            // tax authority computes against the same payload.
            var json = JsonSerializer.Serialize(document, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });
            using var doc = JsonDocument.Parse(json);
            var sb = new StringBuilder();
            WriteElement(doc.RootElement, sb);
            return sb.ToString();
        }

        private static void WriteElement(JsonElement element, StringBuilder sb)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var prop in element.EnumerateObject())
                    {
                        sb.Append('"').Append(prop.Name.ToUpperInvariant()).Append('"');
                        WriteValue(prop.Value, sb);
                    }
                    break;
                case JsonValueKind.Array:
                    foreach (var item in element.EnumerateArray())
                        WriteValue(item, sb);
                    break;
                default:
                    WriteValue(element, sb);
                    break;
            }
        }

        private static void WriteValue(JsonElement v, StringBuilder sb)
        {
            switch (v.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var prop in v.EnumerateObject())
                    {
                        sb.Append('"').Append(prop.Name.ToUpperInvariant()).Append('"');
                        WriteValue(prop.Value, sb);
                    }
                    break;
                case JsonValueKind.Array:
                    foreach (var item in v.EnumerateArray())
                        WriteValue(item, sb);
                    break;
                case JsonValueKind.String:
                    sb.Append('"').Append(v.GetString()).Append('"');
                    break;
                case JsonValueKind.Number:
                    sb.Append('"')
                      .Append(v.GetDecimal().ToString(CultureInfo.InvariantCulture))
                      .Append('"');
                    break;
                case JsonValueKind.True:
                    sb.Append("\"true\"");
                    break;
                case JsonValueKind.False:
                    sb.Append("\"false\"");
                    break;
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    sb.Append("\"\"");
                    break;
            }
        }
    }
}
