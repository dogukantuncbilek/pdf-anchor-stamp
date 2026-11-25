using UglyToad.PdfPig;

namespace PdfAnchorStamp.Tests;

public sealed class AnchorFinderTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("pdf-anchor-stamp-").FullName;
    private readonly string _pdf;

    public AnchorFinderTests()
    {
        _pdf = TestPdf.Create(_dir);
    }

    [Fact]
    public void Finds_visible_marker_on_both_pages_with_page_coordinates()
    {
        var anchors = AnchorFinder.Find(_pdf, TestPdf.VisibleMarker);

        Assert.Equal(2, anchors.Count);
        var first = anchors[0];
        Assert.Equal(1, first.PageNumber);
        Assert.Equal(TestPdf.VisibleMarker, first.Text);
        Assert.InRange(first.Left, TestPdf.VisibleX - 1, TestPdf.VisibleX + 1);
        Assert.InRange(first.TopFromPageTop, TestPdf.VisibleBaselineFromTop - 14, TestPdf.VisibleBaselineFromTop);
        Assert.True(first.Width > 40);
        Assert.True(first.Height > 5);
        Assert.Equal(2, anchors[1].PageNumber);
    }

    [Fact]
    public void Finds_invisible_white_marker()
    {
        var anchors = AnchorFinder.Find(_pdf, TestPdf.HiddenMarker);

        var anchor = Assert.Single(anchors);
        Assert.Equal(1, anchor.PageNumber);
        Assert.InRange(anchor.Left, 299, 301);
    }

    [Fact]
    public void Finds_multi_word_marker_as_one_anchor()
    {
        var anchors = AnchorFinder.Find(_pdf, TestPdf.MultiWordMarker);

        var anchor = Assert.Single(anchors);
        Assert.Equal(2, anchor.PageNumber);
        Assert.Equal(TestPdf.MultiWordMarker, anchor.Text);
    }

    [Fact]
    public void Respects_ignore_case_first_only_and_page_filter()
    {
        var caseSensitive = AnchorFinder.Find(_pdf, "{{sign_here}}");
        var ignoreCase = AnchorFinder.Find(_pdf, "{{sign_here}}", new FindOptions { IgnoreCase = true });
        var firstOnly = AnchorFinder.Find(_pdf, TestPdf.VisibleMarker, new FindOptions { FirstOnly = true });
        var pageTwo = AnchorFinder.Find(_pdf, TestPdf.VisibleMarker, new FindOptions { Pages = new HashSet<int> { 2 } });

        Assert.Empty(caseSensitive);
        Assert.Equal(2, ignoreCase.Count);
        Assert.Single(firstOnly);
        Assert.Equal(2, Assert.Single(pageTwo).PageNumber);
    }

    [Fact]
    public void Word_contains_matches_partial_tokens()
    {
        var exact = AnchorFinder.Find(_pdf, "SIGN_HERE");
        var contains = AnchorFinder.Find(_pdf, "SIGN_HERE", new FindOptions { WordContains = true });

        Assert.Empty(exact);
        Assert.Equal(2, contains.Count);
    }

    [Fact]
    public void Sample_pdf_is_readable_by_pdfpig()
    {
        using var document = PdfDocument.Open(_pdf);
        Assert.Equal(2, document.NumberOfPages);
    }

    public void Dispose() => Directory.Delete(_dir, recursive: true);
}
