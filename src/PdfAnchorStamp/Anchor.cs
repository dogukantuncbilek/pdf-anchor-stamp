namespace PdfAnchorStamp;

public sealed record Anchor(
    int PageNumber,
    string Text,
    double Left,
    double Bottom,
    double Width,
    double Height,
    double PageWidth,
    double PageHeight)
{
    public double Right => Left + Width;
    public double Top => Bottom + Height;
    public double TopFromPageTop => PageHeight - Top;
}
