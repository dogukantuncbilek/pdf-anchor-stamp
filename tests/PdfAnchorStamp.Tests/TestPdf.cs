using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace PdfAnchorStamp.Tests;

internal static class TestPdf
{
    public const string VisibleMarker = "{{SIGN_HERE}}";
    public const string HiddenMarker = "{{STAMP}}";
    public const string MultiWordMarker = "APPROVED BY MANAGER";

    public const double VisibleX = 100;
    public const double VisibleBaselineFromTop = 500;

    private const string OnePixelPng =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8DwHwAFBQIAX8jx0gAAAABJRU5ErkJggg==";

    public static string Create(string directory)
    {
        FontSetup.EnsureDefaults();
        var path = Path.Combine(directory, "input.pdf");
        using var document = new PdfDocument();
        var first = document.AddPage();
        using (var gfx = XGraphics.FromPdfPage(first))
        {
            var body = new XFont("Arial", 12);
            gfx.DrawString("Page one text before the marker.", body, XBrushes.Black, new XPoint(60, 100));
            gfx.DrawString(VisibleMarker, body, XBrushes.Black, new XPoint(VisibleX, VisibleBaselineFromTop));
            gfx.DrawString(HiddenMarker, new XFont("Arial", 4), XBrushes.White, new XPoint(300, 650));
        }

        var second = document.AddPage();
        using (var gfx = XGraphics.FromPdfPage(second))
        {
            var body = new XFont("Arial", 12);
            gfx.DrawString(MultiWordMarker, body, XBrushes.Black, new XPoint(80, 200));
            gfx.DrawString(VisibleMarker, body, XBrushes.Black, new XPoint(80, 300));
        }

        document.Save(path);
        return path;
    }

    public static string CreateImage(string directory)
    {
        var path = Path.Combine(directory, "stamp.png");
        File.WriteAllBytes(path, Convert.FromBase64String(OnePixelPng));
        return path;
    }
}
