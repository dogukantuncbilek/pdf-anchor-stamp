namespace PdfAnchorStamp;

public sealed class FindOptions
{
    public bool IgnoreCase { get; init; }
    public bool WordContains { get; init; }
    public bool FirstOnly { get; init; }
    public IReadOnlySet<int>? Pages { get; init; }

    public static readonly FindOptions Default = new();
}
