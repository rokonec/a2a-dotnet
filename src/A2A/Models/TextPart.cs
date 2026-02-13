namespace A2A;

/// <summary>
/// Compatibility helper for creating text parts.
/// In v1.0, use <see cref="Part.FromText"/> or set <see cref="Part.Text"/> directly.
/// </summary>
public sealed class TextPart : Part
{
    /// <summary>
    /// Initializes a new text part.
    /// </summary>
    public TextPart()
    {
    }

    /// <summary>
    /// Initializes a new text part with the specified text.
    /// </summary>
    /// <param name="text">The text content.</param>
    public TextPart(string text)
    {
        Text = text;
    }
}