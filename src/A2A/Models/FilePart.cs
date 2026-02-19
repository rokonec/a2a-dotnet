namespace A2A;

/// <summary>
/// Compatibility helper for creating file parts.
/// In v1.0, use <see cref="Part.FromUrl"/> or <see cref="Part.FromRaw"/> or set
/// <see cref="Part.Url"/>/<see cref="Part.Raw"/> directly with <see cref="Part.Filename"/> and <see cref="Part.MediaType"/>.
/// </summary>
public sealed class FilePart : Part
{
    /// <summary>
    /// Initializes a new file part.
    /// </summary>
    public FilePart()
    {
    }
}