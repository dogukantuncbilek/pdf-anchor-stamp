namespace PdfAnchorStamp;

public enum Placement
{
    OnMarker,
    Above,
    Below,
    Right,
    Left
}

public sealed class StampSpec
{
    public string? ImagePath { get; init; }
    public string? Text { get; init; }
    public string FontFamily { get; init; } = "Arial";
    public double FontSize { get; init; } = 12;
    public string Color { get; init; } = "#000000";
    public double? Width { get; init; }
    public double? Height { get; init; }
    public double OffsetX { get; init; }
    public double OffsetY { get; init; }
    public Placement Placement { get; init; } = Placement.OnMarker;

    public bool IsImage => !string.IsNullOrEmpty(ImagePath);

    public void Validate()
    {
        if (IsImage == !string.IsNullOrEmpty(Text))
        {
            throw new ArgumentException("Provide exactly one of ImagePath or Text.");
        }

        if (IsImage && !File.Exists(ImagePath))
        {
            throw new FileNotFoundException("Stamp image not found.", ImagePath);
        }
    }
}
