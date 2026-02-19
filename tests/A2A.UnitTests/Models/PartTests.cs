using System.Text.Json;

namespace A2A.UnitTests.Models;

public class PartTests
{
    [Fact]
    public void FromText_CreatesTextPart()
    {
        var part = Part.FromText("hello");
        Assert.Equal("hello", part.Text);
        Assert.Equal(PartContentCase.Text, part.ContentCase);
        Assert.Null(part.Raw);
        Assert.Null(part.Url);
        Assert.Null(part.Data);
    }

    [Fact]
    public void FromUrl_CreatesUrlPart()
    {
        var part = Part.FromUrl("https://example.com/file.png", "image/png", "file.png");
        Assert.Equal("https://example.com/file.png", part.Url);
        Assert.Equal("image/png", part.MediaType);
        Assert.Equal("file.png", part.Filename);
        Assert.Equal(PartContentCase.Url, part.ContentCase);
    }

    [Fact]
    public void FromRaw_CreatesRawPart()
    {
        var part = Part.FromRaw("base64data==", "application/pdf", "doc.pdf");
        Assert.Equal("base64data==", part.Raw);
        Assert.Equal("application/pdf", part.MediaType);
        Assert.Equal("doc.pdf", part.Filename);
        Assert.Equal(PartContentCase.Raw, part.ContentCase);
    }

    [Fact]
    public void FromData_CreatesDataPart()
    {
        var json = JsonSerializer.SerializeToElement(new { key = "value", number = 42 });
        var part = Part.FromData(json, "application/json");
        Assert.NotNull(part.Data);
        Assert.Equal(PartContentCase.Data, part.ContentCase);
        Assert.Equal("application/json", part.MediaType);
    }

    [Fact]
    public void ContentCase_None_WhenNothingSet()
    {
        var part = new Part();
        Assert.Equal(PartContentCase.None, part.ContentCase);
    }

    [Fact]
    public void TextPart_RoundTrip_Serialization()
    {
        var part = Part.FromText("hello world");
        var json = JsonSerializer.Serialize(part, A2AJsonUtilities.DefaultOptions);
        var deserialized = JsonSerializer.Deserialize<Part>(json, A2AJsonUtilities.DefaultOptions);

        Assert.NotNull(deserialized);
        Assert.Equal("hello world", deserialized.Text);
        Assert.Equal(PartContentCase.Text, deserialized.ContentCase);
    }

    [Fact]
    public void UrlPart_RoundTrip_Serialization()
    {
        var part = Part.FromUrl("https://example.com/img.jpg", "image/jpeg", "img.jpg");
        var json = JsonSerializer.Serialize(part, A2AJsonUtilities.DefaultOptions);
        var deserialized = JsonSerializer.Deserialize<Part>(json, A2AJsonUtilities.DefaultOptions);

        Assert.NotNull(deserialized);
        Assert.Equal("https://example.com/img.jpg", deserialized.Url);
        Assert.Equal("image/jpeg", deserialized.MediaType);
        Assert.Equal("img.jpg", deserialized.Filename);
    }

    [Fact]
    public void DataPart_RoundTrip_Serialization()
    {
        var json = JsonSerializer.SerializeToElement(new { key = "value" });
        var part = Part.FromData(json);
        var serialized = JsonSerializer.Serialize(part, A2AJsonUtilities.DefaultOptions);
        var deserialized = JsonSerializer.Deserialize<Part>(serialized, A2AJsonUtilities.DefaultOptions);

        Assert.NotNull(deserialized);
        Assert.Equal(PartContentCase.Data, deserialized.ContentCase);
        Assert.Equal("value", deserialized.Data!.Value.GetProperty("key").GetString());
    }

    [Fact]
    public void Part_NullProperties_NotSerialized()
    {
        var part = Part.FromText("hi");
        var json = JsonSerializer.Serialize(part, A2AJsonUtilities.DefaultOptions);

        Assert.Contains("\"text\"", json);
        Assert.DoesNotContain("\"raw\"", json);
        Assert.DoesNotContain("\"url\"", json);
        Assert.DoesNotContain("\"data\"", json);
        Assert.DoesNotContain("\"filename\"", json);
        Assert.DoesNotContain("\"mediaType\"", json);
        Assert.DoesNotContain("\"metadata\"", json);
    }

    [Fact]
    public void Part_WithMetadata_RoundTrips()
    {
        var part = Part.FromText("hi");
        part.Metadata = new Dictionary<string, JsonElement>
        {
            ["source"] = JsonSerializer.SerializeToElement("test")
        };

        var json = JsonSerializer.Serialize(part, A2AJsonUtilities.DefaultOptions);
        var deserialized = JsonSerializer.Deserialize<Part>(json, A2AJsonUtilities.DefaultOptions);

        Assert.NotNull(deserialized?.Metadata);
        Assert.Equal("test", deserialized.Metadata["source"].GetString());
    }
}
