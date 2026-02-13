using System.Text.Json;
using System.Text.Json.Serialization;

namespace A2A;

/// <summary>
/// Identifies which content field is set in a <see cref="Part"/>.
/// </summary>
public enum PartContentCase
{
    /// <summary>No content field is set.</summary>
    None,
    /// <summary>The <see cref="Part.Text"/> field is set.</summary>
    Text,
    /// <summary>The <see cref="Part.Raw"/> field is set.</summary>
    Raw,
    /// <summary>The <see cref="Part.Url"/> field is set.</summary>
    Url,
    /// <summary>The <see cref="Part.Data"/> field is set.</summary>
    Data
}

/// <summary>
/// Represents a container for a section of communication content.
/// Parts can be purely textual, a file (raw bytes or URL), or structured data.
/// Exactly one of <see cref="Text"/>, <see cref="Raw"/>, <see cref="Url"/>, or <see cref="Data"/> must be set.
/// </summary>
[JsonConverter(typeof(PartConverter))]
public class Part
{
    /// <summary>
    /// The string content of a text part.
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// The raw byte content of a file. In JSON serialization, this is encoded as a base64 string.
    /// </summary>
    [JsonPropertyName("raw")]
    public string? Raw { get; set; }

    /// <summary>
    /// A URL pointing to the file's content.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// Arbitrary structured data as a JSON value (object, array, string, number, boolean, or null).
    /// </summary>
    [JsonPropertyName("data")]
    public JsonElement? Data { get; set; }

    /// <summary>
    /// Optional metadata associated with this part.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, JsonElement>? Metadata { get; set; }

    /// <summary>
    /// An optional name for the file (e.g., "document.pdf").
    /// </summary>
    [JsonPropertyName("filename")]
    public string? Filename { get; set; }

    /// <summary>
    /// The media type (MIME type) of the part content (e.g., "text/plain", "application/json", "image/png").
    /// </summary>
    [JsonPropertyName("mediaType")]
    public string? MediaType { get; set; }

    /// <summary>
    /// Identifies which content field is set.
    /// </summary>
    [JsonIgnore]
    public PartContentCase ContentCase =>
        Text is not null ? PartContentCase.Text :
        Raw is not null ? PartContentCase.Raw :
        Url is not null ? PartContentCase.Url :
        Data is not null ? PartContentCase.Data :
        PartContentCase.None;

    /// <summary>
    /// Creates a text part.
    /// </summary>
    /// <param name="text">The text content.</param>
    public static Part FromText(string text) => new() { Text = text };

    /// <summary>
    /// Creates a file part from raw bytes (base64 encoded).
    /// </summary>
    /// <param name="base64Bytes">The base64-encoded file content.</param>
    /// <param name="mediaType">The MIME type of the content.</param>
    /// <param name="filename">An optional filename.</param>
    public static Part FromRaw(string base64Bytes, string? mediaType = null, string? filename = null) =>
        new() { Raw = base64Bytes, MediaType = mediaType, Filename = filename };

    /// <summary>
    /// Creates a file part from a URL.
    /// </summary>
    /// <param name="url">The URL pointing to the file content.</param>
    /// <param name="mediaType">The MIME type of the content.</param>
    /// <param name="filename">An optional filename.</param>
    public static Part FromUrl(string url, string? mediaType = null, string? filename = null) =>
        new() { Url = url, MediaType = mediaType, Filename = filename };

    /// <summary>
    /// Creates a structured data part.
    /// </summary>
    /// <param name="data">The structured data as a JSON element.</param>
    /// <param name="mediaType">The MIME type of the content.</param>
    public static Part FromData(JsonElement data, string? mediaType = null) =>
        new() { Data = data, MediaType = mediaType };
}

internal sealed class PartConverter : JsonConverter<Part>
{
    public override Part Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new A2AException("Expected JSON object for Part", A2AErrorCode.InvalidRequest);
        }

        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        var part = new Part();

        if (root.TryGetProperty("text", out var textProp))
        {
            part.Text = textProp.GetString();
        }

        if (root.TryGetProperty("raw", out var rawProp))
        {
            part.Raw = rawProp.GetString();
        }

        if (root.TryGetProperty("url", out var urlProp))
        {
            part.Url = urlProp.GetString();
        }

        if (root.TryGetProperty("data", out var dataProp))
        {
            part.Data = dataProp.Clone();
        }

        if (root.TryGetProperty("metadata", out var metadataProp) && metadataProp.ValueKind == JsonValueKind.Object)
        {
            var metadata = new Dictionary<string, JsonElement>();
            foreach (var prop in metadataProp.EnumerateObject())
            {
                metadata[prop.Name] = prop.Value.Clone();
            }
            part.Metadata = metadata;
        }

        if (root.TryGetProperty("filename", out var filenameProp))
        {
            part.Filename = filenameProp.GetString();
        }

        if (root.TryGetProperty("mediaType", out var mediaTypeProp))
        {
            part.MediaType = mediaTypeProp.GetString();
        }

        return part;
    }

    public override void Write(Utf8JsonWriter writer, Part value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        if (value.Text is not null)
        {
            writer.WriteString("text", value.Text);
        }

        if (value.Raw is not null)
        {
            writer.WriteString("raw", value.Raw);
        }

        if (value.Url is not null)
        {
            writer.WriteString("url", value.Url);
        }

        if (value.Data is { } data)
        {
            writer.WritePropertyName("data");
            data.WriteTo(writer);
        }

        if (value.Metadata is { Count: > 0 } metadata)
        {
            writer.WritePropertyName("metadata");
            writer.WriteStartObject();
            foreach (var kvp in metadata)
            {
                writer.WritePropertyName(kvp.Key);
                kvp.Value.WriteTo(writer);
            }
            writer.WriteEndObject();
        }

        if (value.Filename is not null)
        {
            writer.WriteString("filename", value.Filename);
        }

        if (value.MediaType is not null)
        {
            writer.WriteString("mediaType", value.MediaType);
        }

        writer.WriteEndObject();
    }
}