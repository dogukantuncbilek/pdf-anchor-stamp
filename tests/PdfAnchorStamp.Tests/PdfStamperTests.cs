using UglyToad.PdfPig;

namespace PdfAnchorStamp.Tests;

public sealed class PdfStamperTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("pdf-anchor-stamp-").FullName;
    private readonly string _pdf;
    private readonly string _out;

    public PdfStamperTests()
    {
        _pdf = TestPdf.Create(_dir);
        _out = Path.Combine(_dir, "output.pdf");
    }

    [Fact]
    public void Stamps_text_at_every_anchor()
    {
        var count = AnchorStamper.Run(_pdf, _out, TestPdf.VisibleMarker, new StampSpec { Text = "STAMPED-OK", Color = "#1A7F37" });

        Assert.Equal(2, count);
        var stamped = AnchorFinder.Find(_out, "STAMPED-OK");
        Assert.Equal(2, stamped.Count);
        var original = AnchorFinder.Find(_out, TestPdf.VisibleMarker);
        Assert.InRange(stamped[0].Left, original[0].Left - 1, original[0].Left + 1);
        Assert.InRange(stamped[0].Bottom, original[0].Bottom - 6, original[0].Bottom + 6);
    }

    [Fact]
    public void Stamps_image_on_invisible_marker_with_given_width()
    {
        var image = TestPdf.CreateImage(_dir);
        var spec = new StampSpec { ImagePath = image, Width = 120, Height = 40, Placement = Placement.Above };

        var count = AnchorStamper.Run(_pdf, _out, TestPdf.HiddenMarker, spec);

        Assert.Equal(1, count);
        using var document = PdfDocument.Open(_out);
        var images = document.GetPage(1).GetImages().ToList();
        var placed = Assert.Single(images);
        Assert.InRange(placed.BoundingBox.Width, 119, 121);
        Assert.InRange(placed.BoundingBox.Height, 39, 41);
        Assert.Empty(document.GetPage(2).GetImages());
    }

    [Fact]
    public void Placement_offsets_move_the_stamp()
    {
        AnchorStamper.Run(_pdf, _out, TestPdf.VisibleMarker, new StampSpec { Text = "X", Placement = Placement.Right, OffsetX = 10 }, new FindOptions { FirstOnly = true });

        var marker = AnchorFinder.Find(_out, TestPdf.VisibleMarker, new FindOptions { FirstOnly = true })[0];
        var stamp = AnchorFinder.Find(_out, "X", new FindOptions { Pages = new HashSet<int> { 1 } })[0];
        Assert.InRange(stamp.Left, marker.Right + 9, marker.Right + 11);
    }

    [Fact]
    public void Returns_zero_and_writes_nothing_when_marker_is_missing()
    {
        var count = AnchorStamper.Run(_pdf, _out, "{{NOPE}}", new StampSpec { Text = "X" });

        Assert.Equal(0, count);
        Assert.False(File.Exists(_out));
    }

    [Fact]
    public void Rejects_spec_with_both_or_neither_source()
    {
        var anchors = AnchorFinder.Find(_pdf, TestPdf.VisibleMarker);

        Assert.Throws<ArgumentException>(() => PdfStamper.Stamp(_pdf, _out, anchors, new StampSpec()));
        Assert.Throws<ArgumentException>(() => PdfStamper.Stamp(_pdf, _out, anchors, new StampSpec { Text = "A", ImagePath = "x.png" }));
    }

    public void Dispose() => Directory.Delete(_dir, recursive: true);
}
