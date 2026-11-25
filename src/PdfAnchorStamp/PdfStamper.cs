using System.Globalization;
using PdfSharp.Drawing;
using PdfSharp.Pdf.IO;

namespace PdfAnchorStamp;

public static class PdfStamper
{
    public static int Stamp(string inputPath, string outputPath, IEnumerable<Anchor> anchors, StampSpec spec)
    {
        spec.Validate();
        FontSetup.EnsureDefaults();
        var anchorList = anchors.ToList();
        if (anchorList.Count == 0)
        {
            return 0;
        }

        using var document = PdfReader.Open(inputPath, PdfDocumentOpenMode.Modify);
        using var image = spec.IsImage ? XImage.FromFile(spec.ImagePath!) : null;
        var font = spec.IsImage ? null : new XFont(spec.FontFamily, spec.FontSize);
        var brush = new XSolidBrush(ParseColor(spec.Color));

        foreach (var group in anchorList.GroupBy(a => a.PageNumber))
        {
            var page = document.Pages[group.Key - 1];
            using var gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
            foreach (var anchor in group)
            {
                var size = StampSize(spec, image, font, gfx);
                var rect = Place(anchor, size, spec);
                if (image is not null)
                {
                    gfx.DrawImage(image, rect);
                }
                else
                {
                    gfx.DrawString(spec.Text!, font!, brush, rect, XStringFormats.TopLeft);
                }
            }
        }

        document.Save(outputPath);
        return anchorList.Count;
    }

    private static XSize StampSize(StampSpec spec, XImage? image, XFont? font, XGraphics gfx)
    {
        if (image is not null)
        {
            var naturalWidth = image.PointWidth;
            var naturalHeight = image.PointHeight;
            if (spec.Width is { } w && spec.Height is { } h)
            {
                return new XSize(w, h);
            }

            if (spec.Width is { } onlyWidth)
            {
                return new XSize(onlyWidth, onlyWidth * naturalHeight / naturalWidth);
            }

            if (spec.Height is { } onlyHeight)
            {
                return new XSize(onlyHeight * naturalWidth / naturalHeight, onlyHeight);
            }

            return new XSize(naturalWidth, naturalHeight);
        }

        var measured = gfx.MeasureString(spec.Text!, font!);
        return new XSize(spec.Width ?? measured.Width, spec.Height ?? measured.Height);
    }

    private static XRect Place(Anchor anchor, XSize size, StampSpec spec)
    {
        var markerTop = anchor.TopFromPageTop;
        var markerBottom = markerTop + anchor.Height;
        double x;
        double y;

        switch (spec.Placement)
        {
            case Placement.Above:
                x = anchor.Left;
                y = markerTop - size.Height;
                break;
            case Placement.Below:
                x = anchor.Left;
                y = markerBottom;
                break;
            case Placement.Right:
                x = anchor.Right;
                y = markerBottom - size.Height;
                break;
            case Placement.Left:
                x = anchor.Left - size.Width;
                y = markerBottom - size.Height;
                break;
            default:
                x = anchor.Left;
                y = markerBottom - size.Height;
                break;
        }

        return new XRect(x + spec.OffsetX, y - spec.OffsetY, size.Width, size.Height);
    }

    private static XColor ParseColor(string color)
    {
        var hex = color.TrimStart('#');
        if (hex.Length != 6)
        {
            throw new ArgumentException("Color must be #RRGGBB.", nameof(color));
        }

        var r = byte.Parse(hex[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        var g = byte.Parse(hex[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        var b = byte.Parse(hex[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return XColor.FromArgb(r, g, b);
    }
}
