namespace A2A.UnitTests.Models;

public class ErrorCodeTests
{
    [Fact]
    public void InvalidAgentResponse_HasCorrectCode()
    {
        Assert.Equal(-32006, (int)A2AErrorCode.InvalidAgentResponse);
    }

    [Fact]
    public void ExtendedAgentCardNotConfigured_HasCorrectCode()
    {
        Assert.Equal(-32007, (int)A2AErrorCode.ExtendedAgentCardNotConfigured);
    }

    [Fact]
    public void ExtensionSupportRequired_HasCorrectCode()
    {
        Assert.Equal(-32008, (int)A2AErrorCode.ExtensionSupportRequired);
    }

    [Fact]
    public void VersionNotSupported_HasCorrectCode()
    {
        Assert.Equal(-32009, (int)A2AErrorCode.VersionNotSupported);
    }

    [Fact]
    public void A2AException_WithNewErrorCodes_Works()
    {
        var ex1 = new A2AException("bad response", A2AErrorCode.InvalidAgentResponse);
        Assert.Equal(A2AErrorCode.InvalidAgentResponse, ex1.ErrorCode);

        var ex2 = new A2AException("version", A2AErrorCode.VersionNotSupported);
        Assert.Equal(A2AErrorCode.VersionNotSupported, ex2.ErrorCode);

        var ex3 = new A2AException("ext", A2AErrorCode.ExtensionSupportRequired);
        Assert.Equal(A2AErrorCode.ExtensionSupportRequired, ex3.ErrorCode);

        var ex4 = new A2AException("card", A2AErrorCode.ExtendedAgentCardNotConfigured);
        Assert.Equal(A2AErrorCode.ExtendedAgentCardNotConfigured, ex4.ErrorCode);
    }
}
