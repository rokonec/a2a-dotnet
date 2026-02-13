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
    public void A2AEvent_Deserialize_Kind_Index_Zero_Null_Mapping_ThrowsUnknownKind()
    {
        // Arrange: A2AEventKind.Unknown maps to index 0 which is null in the mapping
        const string json = "{ \"kind\": \"unknown\" }";

        // Act
        var ex = Assert.Throws<A2AException>(() => JsonSerializer.Deserialize<A2AEvent>(json, A2AJsonUtilities.DefaultOptions));

        // Assert
        Assert.Equal(A2AErrorCode.InvalidRequest, ex.ErrorCode);
    }
}
