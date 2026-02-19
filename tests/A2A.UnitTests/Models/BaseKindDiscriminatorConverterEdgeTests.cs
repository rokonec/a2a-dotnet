using System.Text.Json;

namespace A2A.UnitTests.Models;

public sealed class BaseKindDiscriminatorConverterEdgeTests
{
    [Fact]
    public void Part_Deserialize_Kind_Index_OutOfRange_ReturnsPartWithoutKind()
    {
        // Arrange: In v1.0, Part no longer uses a kind discriminator.
        // Unknown properties like "kind" are simply ignored.
        const string json = "{ \"kind\": \"count\" }";

        // Act
        var part = JsonSerializer.Deserialize<Part>(json, A2AJsonUtilities.DefaultOptions);

        // Assert: Part is returned with all properties null (kind is ignored)
        Assert.NotNull(part);
        Assert.Null(part!.Text);
        Assert.Null(part.Data);
        Assert.Null(part.Url);
        Assert.Null(part.Raw);
    }

    [Fact]
    public void StreamResponse_Deserialize_Empty_ReturnsNoneCase()
    {
        // Arrange: An empty object should deserialize to StreamResponse with no payload
        const string json = "{}";

        // Act
        var result = JsonSerializer.Deserialize<StreamResponse>(json, A2AJsonUtilities.DefaultOptions);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(StreamResponseCase.None, result.PayloadCase);
    }
}
