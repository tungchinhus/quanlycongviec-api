using System.Text.Json;
using System.Text.Json.Serialization;

namespace quanlyfilesBE.DTOs;

/// <summary>
/// Custom JSON converter to parse date-only strings (YYYY-MM-DD) as local DateTime
/// without timezone conversion. This ensures dates sent from frontend match exactly
/// what is stored in the database.
/// </summary>
public class DateOnlyJsonConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var dateString = reader.GetString();
            if (string.IsNullOrWhiteSpace(dateString))
            {
                return null;
            }

            // Parse date string in format YYYY-MM-DD as date-only (no timezone conversion)
            // Use DateTimeStyles.AssumeLocal to parse as local time, then convert to Unspecified
            // to prevent SQL Server from applying timezone conversion
            if (System.DateTime.TryParseExact(dateString, "yyyy-MM-dd", 
                System.Globalization.CultureInfo.InvariantCulture, 
                System.Globalization.DateTimeStyles.AssumeLocal, 
                out var date))
            {
                // Return as Unspecified (no timezone) - SQL Server will store as-is
                return new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Unspecified);
            }

            // Fallback: try parsing with AssumeLocal
            if (System.DateTime.TryParse(dateString, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeLocal, out var parsedDate))
            {
                // Extract date part only, set as Unspecified (no timezone)
                return new DateTime(parsedDate.Year, parsedDate.Month, parsedDate.Day, 0, 0, 0, DateTimeKind.Unspecified);
            }
        }

        // If it's already a DateTime token, read it normally
        if (reader.TokenType == JsonTokenType.String)
        {
            return reader.GetDateTime();
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
        }
        else
        {
            // Write as date-only string (YYYY-MM-DD)
            writer.WriteStringValue(value.Value.ToString("yyyy-MM-dd"));
        }
    }
}
