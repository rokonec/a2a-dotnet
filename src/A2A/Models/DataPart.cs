using System.Text.Json;

namespace A2A;

/// <summary>
/// Compatibility helper for creating structured data parts.
/// In v1.0, use <see cref="Part.FromData"/> or set <see cref="Part.Data"/> directly.
/// </summary>
public sealed class DataPart : Part
{
    /// <summary>
    /// Initializes a new data part.
    /// </summary>
    public DataPart()
    {
    }
}