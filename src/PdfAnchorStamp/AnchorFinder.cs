using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace PdfAnchorStamp;

public static class AnchorFinder
{
    public static IReadOnlyList<Anchor> Find(string pdfPath, string marker, FindOptions? options = null)
    {
        using var document = PdfDocument.Open(pdfPath);
        return Find(document, marker, options);
    }

    public static IReadOnlyList<Anchor> Find(byte[] pdfBytes, string marker, FindOptions? options = null)
    {
        using var document = PdfDocument.Open(pdfBytes);
        return Find(document, marker, options);
    }

    private static IReadOnlyList<Anchor> Find(PdfDocument document, string marker, FindOptions? options)
    {
        if (string.IsNullOrWhiteSpace(marker))
        {
            throw new ArgumentException("Marker must not be empty.", nameof(marker));
        }

        options ??= FindOptions.Default;
        var tokens = marker.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var comparison = options.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var result = new List<Anchor>();

        foreach (var page in document.GetPages())
        {
            if (options.Pages is not null && !options.Pages.Contains(page.Number))
            {
                continue;
            }

            var words = page.GetWords().ToList();
            for (var i = 0; i + tokens.Length <= words.Count; i++)
            {
                if (!MatchesAt(words, i, tokens, comparison, options.WordContains))
                {
                    continue;
                }

                var matched = words.GetRange(i, tokens.Length);
                result.Add(ToAnchor(page, matched));
                if (options.FirstOnly)
                {
                    return result;
                }

                i += tokens.Length - 1;
            }
        }

        return result;
    }

    private static bool MatchesAt(List<Word> words, int start, string[] tokens, StringComparison comparison, bool contains)
    {
        for (var k = 0; k < tokens.Length; k++)
        {
            var text = words[start + k].Text;
            var ok = contains ? text.Contains(tokens[k], comparison) : string.Equals(text, tokens[k], comparison);
            if (!ok)
            {
                return false;
            }
        }

        return true;
    }

    private static Anchor ToAnchor(Page page, List<Word> matched)
    {
        var left = matched.Min(w => w.BoundingBox.Left);
        var bottom = matched.Min(w => w.BoundingBox.Bottom);
        var right = matched.Max(w => w.BoundingBox.Right);
        var top = matched.Max(w => w.BoundingBox.Top);
        var text = string.Join(" ", matched.Select(w => w.Text));
        return new Anchor(page.Number, text, left, bottom, right - left, top - bottom, page.Width, page.Height);
    }
}
